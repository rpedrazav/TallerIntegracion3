---
id: manifiestos-k8s-cluster
tipo: infra
titulo: Inventario de Manifiestos Kubernetes, Imágenes y Variables del Clúster (TI3-758)
estado: implementado
fuentes: [k8s/manifests/all-services.yaml, k8s/manifests/postgres-tenant.yaml, new-ingresses.yaml, sa-secret.yaml, Deploy-To-K8s.ps1, Build-Images.ps1, Push-Images.ps1, .github/workflows/docker-build.yml, src/*/appsettings.json, src/*/Dockerfile, docs/qa/S2-052/reporte.md]
verificado_contra_codigo: true
ultima_revision: 2026-11-24
depende_de: [matriz-rutas, ci-cd, ms1-identity, ms2-tax, ms3-catalog, ms4-inventory, ms5-pos]
publica: []
consume: []
reglas: [RN-01, RN-02, RN-07]
---

# Inventario de Manifiestos Kubernetes, Imágenes y Variables del Clúster (TI3-758)

> Documento autocontenido de apoyo operativo para **Rodrigo** (`student-rpedraza` en el clúster universitario `dev.censei.cl`). Consolida todos los manifiestos Kubernetes del repositorio, tags de imágenes Docker, diferencias detectadas, variables de entorno requeridas por cada microservicio y los comandos exactos de actualización para alinear los pods sin buscar información en otros archivos.

---

## 1. Inventario de Archivos de Despliegue e Imágenes en el Repositorio

| Archivo | Propósito | Namespace declarado | Estado / Observación Crítica |
|---|---|---|---|
| `k8s/manifests/all-services.yaml` | Deployments y Services de PostgreSQL + MS-1, MS-2, MS-3, MS-5 + `fortio` + Ingress legacy `api-ingress` | Implícito (contexto activo `student-rpedraza`) | Actualizado en TI3-758 para incluir las URLs internas entre microservicios (`Services__*__BaseUrl` y `PasarelaPago__BaseUrl`). Aún referencia `:latest` y no despliega MS-4, Kafka ni `payment-simulator`. |
| `new-ingresses.yaml` | Ingress productivos HTTPS (`auth-ingress`, `catalog-ingress`, `pos-ingress`) con TLS `globalmart-dev-tls` | `student-rpedraza` | **Vigente.** Enruta `auth-rpedraza.dev.censei.cl` → `tenantidentityservice:80`, `catalog-rpedraza.dev.censei.cl` → `catalogpricingservice:80`, `pos-rpedraza.dev.censei.cl` → `poscartservice:80`. Certificado Let's Encrypt vence el **2027-01-16** (sin `cert-manager`). |
| `k8s/manifests/postgres-tenant.yaml` | Service + Deployment `postgres-tenant` aislado | Implícito | **Duplicado legacy / no aplicar:** `all-services.yaml` usa el nombre `postgres-tenantidentityservice`, no `postgres-tenant`. |
| `sa-secret.yaml` | Manifiesto `Secret` (`kubernetes.io/service-account-token`) para `github-actions` | `student-rpedraza` | Plantilla declarativa sin token embebido en texto plano; para automatización futura de CD. |
| `.github/workflows/docker-build.yml` | Build & Push automático a Docker Hub en push a `dev` y `main` | N/A (GitHub Actions) | Actualizado en TI3-758: publica **doble tag** (`s2-<YYYYMMDD>-<short_sha>` inmutable + `:latest`) e incorpora **MS-4 (`warehouseinventoryservice`)** además de MS-1, MS-2, MS-3 y MS-5. |
| `Build-Images.ps1` / `Push-Images.ps1` | Scripts manuales locales PowerShell | N/A | **Cuidado:** `Build-Images.ps1` usa `$user = "rpedraza"` (sin `v`), mientras que Docker Hub y `all-services.yaml` usan **`rpedrazav/`** (con `v`). Usar siempre `rpedrazav/`. |
| `Deploy-To-K8s.ps1` | Aplica `k8s/manifests/all-services.yaml` usando `estudiantes-rpedraza.kubeconfig` | Contexto kubeconfig | No reaplica `new-ingresses.yaml`; aplicar ambos si se recrean Ingress. |

---

## 2. Tabla Maestra de Microservicios, Imágenes y Tags

> **Hallazgo principal:** En el clúster conviven pods desplegados manualmente en fechas distintas (tags fechados manuales y tags mutables `:latest`). Usar `:latest` con varios despliegues parciales provoca que `kubectl apply` no recree pods si el tag literal no cambia o que distintos MS corran commits incompatibles. **Estándar esperado:** tag trazable `s2-<YYYYMMDD>-<short_sha>` generado por `.github/workflows/docker-build.yml`.

| Servicio | Deployment / Service K8s | Puerto Service → Pod | Imagen Actual en `all-services.yaml` | Tag Esperado Recomendado | Estado en Clúster / Diferencias Detectadas |
|---|---|---|---|---|---|
| **MS-1 Identity** | `tenantidentityservice` | `80` → `8080` | `rpedrazav/tenantidentityservice:latest` | `rpedrazav/tenantidentityservice:s2-<YYYYMMDD>-<sha>` | Expuesto en `https://auth-rpedraza.dev.censei.cl`. En `Production` el seed `admin@demo.cl` / `cajero@demo.cl` **no** corre salvo `ASPNETCORE_ENVIRONMENT=Development`. |
| **MS-2 Tax** | `taxcomplianceservice` | `80` → `8080` | `rpedrazav/taxcomplianceservice:latest` | `rpedrazav/taxcomplianceservice:s2-<YYYYMMDD>-<sha>` | Interno (sin Ingress público). **Diferencia corregida en manifiesto:** faltaba `Services__TenantIdentityService__BaseUrl=http://tenantidentityservice` (intentaba llamar a `localhost:5124`). |
| **MS-3 Catalog** | `catalogpricingservice` | `80` → `8080` | `rpedrazav/catalogpricingservice:latest` | `rpedrazav/catalogpricingservice:s2-<YYYYMMDD>-<sha>` | Expuesto en `https://catalog-rpedraza.dev.censei.cl`. Requiere extensión PostgreSQL `pg_trgm` habilitada en `catalog_db` (la migración la ejecuta). |
| **MS-4 Warehouse** | *(No declarado en `all-services.yaml`)* | `80` → `8080` | *(Ausente en K8s)* | `rpedrazav/warehouseinventoryservice:s2-<YYYYMMDD>-<sha>` | **Diferencia:** `Dockerfile` existe y ahora se compila en `docker-build.yml`, pero no tiene Deployment ni broker Kafka en `all-services.yaml`. |
| **MS-5 POS & Cart** | `poscartservice` | `80` → `8080` | `rpedrazav/poscartservice:latest` | `rpedrazav/poscartservice:s2-<YYYYMMDD>-<sha>` | Expuesto en `https://pos-rpedraza.dev.censei.cl`. **Causa raíz del error 502 S2-052 corregida en manifiesto:** faltaban `Services__CatalogPricingService__BaseUrl`, `Services__TaxComplianceService__BaseUrl` y `PasarelaPago__BaseUrl`. |
| **Payment Simulator** | `payment-simulator` *(pendiente aplicar en K8s)* | `5050` → `5050` | *(Ausente en `all-services.yaml`)* | `node:20-alpine` (ConfigMap `server.js`) o imagen propia | **Diferencia:** Necesario en el clúster para que `POST /ventas/{id}/cobrar-tarjeta` funcione remotamente sin timeout/502. Se incluye YAML listo abajo (§4). |
| **MS-6 / MS-7 / MS-8** | *(No desplegados)* | N/A | N/A | N/A (Fuera de alcance Sprint 2) | Sin controladores activos (`[PLANIFICADO]`). No desplegar para ahorrar cuota de CPU/RAM del namespace estudiantil. |
| **Bases PostgreSQL (x4)** | `postgres-tenantidentityservice`, `postgres-taxcomplianceservice`, `postgres-catalogpricingservice`, `postgres-poscartservice` | `5432` → `5432` | `postgres:16-alpine` | `postgres:16-alpine` | Sin `PersistentVolumeClaim` (`emptyDir` implícito): si un pod `postgres-*` se reinicia, pierde sus datos. Evitar reiniciar pods de BD salvo limpieza intencional. |
| **Fortio (Carga)** | `fortio` | `8080` → `8080` | `fortio/fortio:latest` | `fortio/fortio:latest` | Utilitario de pruebas de carga en `fortio-rpedraza.dev.censei.cl`. |

---

## 3. Matriz Completa de Variables de Entorno por Microservicio (K8s)

Todos los microservicios ASP.NET Core 8 leen jerárquicamente variables con doble guion bajo (`__`). Esta tabla muestra exactamente qué necesita cada Deployment en `student-rpedraza`:

### 3.1 `tenantidentityservice` (MS-1)
| Variable K8s (`env[].name`) | Valor requerido en `student-rpedraza` | Estado en `all-services.yaml` | Notas |
|---|---|---|---|
| `ConnectionStrings__DefaultConnection` | `Host=postgres-tenantidentityservice;Database=tenant_db;Username=postgres;Password=postgres` | ✅ Presente | Conecta al Service K8s de BD de MS-1. |
| `ASPNETCORE_HTTP_PORTS` | `8080` | ✅ Presente | Coincide con `targetPort: 8080` del Service. |
| `ASPNETCORE_ENVIRONMENT` | `Development` *(opcional si se requiere seed automático)* | ⚠️ Implícito (`Production`) | Si es `Production`, no ejecuta Swagger ni seed `cajero@demo.cl` / `admin@demo.cl`. |
| `Jwt__Key` | `<Secret K8s >= 32 chars>` *(default dev en `appsettings.json`)* | ⚠️ Usa `appsettings.json` | **Debe ser idéntica** en MS-1, MS-2, MS-3 y MS-5 (`Jwt__SecretKey` en MS-3). |
| `Jwt__Issuer` / `Jwt__Audience` | `GlobalMartOS` / `GlobalMartOS_Clients` | ✅ Default en `appsettings.json` | Coincide en todos los servicios. |

### 3.2 `taxcomplianceservice` (MS-2)
| Variable K8s (`env[].name`) | Valor requerido en `student-rpedraza` | Estado en `all-services.yaml` | Notas |
|---|---|---|---|
| `ConnectionStrings__DefaultConnection` | `Host=postgres-taxcomplianceservice;Database=tax_db;Username=postgres;Password=postgres` | ✅ Presente | Conecta al Service K8s de BD de MS-2. |
| `Services__TenantIdentityService__BaseUrl` | `http://tenantidentityservice` | ✅ **Agregada en TI3-758** | **Crítica:** sin ella MS-2 intenta consultar el IVA del tenant en `http://localhost:5124` y falla. |
| `ASPNETCORE_HTTP_PORTS` | `8080` | ✅ Presente | Service `taxcomplianceservice` expone puerto `80` → `8080`. |
| `Jwt__Key` | Misma clave que MS-1 | ⚠️ Usa `appsettings.json` | Requerida por `TenantMiddleware` de MS-2. |

### 3.3 `catalogpricingservice` (MS-3)
| Variable K8s (`env[].name`) | Valor requerido en `student-rpedraza` | Estado en `all-services.yaml` | Notas |
|---|---|---|---|
| `ConnectionStrings__DefaultConnection` | `Host=postgres-catalogpricingservice;Database=catalog_db;Username=postgres;Password=postgres` | ✅ Presente | Conecta al Service K8s de BD de MS-3. |
| `ASPNETCORE_HTTP_PORTS` | `8080` | ✅ Presente | Service `catalogpricingservice` expone puerto `80` → `8080`. |
| `Jwt__SecretKey` | Misma clave que `Jwt__Key` de MS-1 | ⚠️ Usa `appsettings.json` | **Atención:** MS-3 lee `Jwt:SecretKey` (con fallback a `Jwt:Key` en `Program.cs`). |

### 3.4 `poscartservice` (MS-5)
| Variable K8s (`env[].name`) | Valor requerido en `student-rpedraza` | Estado en `all-services.yaml` | Notas |
|---|---|---|---|
| `ConnectionStrings__DefaultConnection` | `Host=postgres-poscartservice;Database=pos_db;Username=postgres;Password=postgres` | ✅ Presente | Ejecuta migraciones (`OutboxMessages`, `IntentosPago`, `Pago.Referencia`, `PreserveItemPricePrecision`) al arrancar. |
| `Services__CatalogPricingService__BaseUrl` | `http://catalogpricingservice` | ✅ **Agregada en TI3-758** | **Crítica (resuelve bloqueo 502 S2-052):** antes apuntaba a `http://localhost:5203`. |
| `Services__TaxComplianceService__BaseUrl` | `http://taxcomplianceservice` | ✅ **Agregada en TI3-758** | **Crítica:** antes apuntaba a `http://localhost:5239` al calcular IVA de la venta. |
| `PasarelaPago__BaseUrl` | `http://payment-simulator:5050` | ✅ **Agregada en TI3-758** | Requiere desplegar el pod `payment-simulator` (§4.3). |
| `PasarelaPago__TimeoutSeconds` | `30` | ✅ Default en `appsettings.json` | Permite probar `sim-timeout-v1` (35 s). |
| `Kafka__BootstrapServers` | `kafka:9092` *(si se despliega Kafka)* | ⚠️ Default `localhost:9092` | Si no hay Kafka en K8s, `OutboxPublisherWorker` conserva los eventos en `OutboxMessages` (`PENDIENTE`) con backoff sin bloquear las ventas. |
| `ASPNETCORE_HTTP_PORTS` | `8080` | ✅ Presente | Service `poscartservice` expone puerto `80` → `8080`. |
| `Jwt__Key` | Misma clave que MS-1 | ⚠️ Usa `appsettings.json` | Requerida por autenticación y `TenantMiddleware`. |

---

## 4. Guía Paso a Paso para Rodrigo — Actualizar el Clúster sin Abrir Otros Archivos

### Paso 1: Inspeccionar imágenes y tags actuales en el clúster vivo
```bash
export KUBECONFIG="$HOME/Downloads/estudiantes-rpedraza.kubeconfig"

# Ver qué imagen exacta y tag (latest vs fechado) está corriendo en cada pod:
kubectl -n student-rpedraza get pods \
  -o custom-columns="POD:.metadata.name,STATUS:.status.phase,IMAGE:.spec.containers[*].image,IMAGE_ID:.status.containerStatuses[*].imageID"
```

### Paso 2: Publicar imágenes con tag unificado (automático por GitHub Actions o manual)
Si el workflow `.github/workflows/docker-build.yml` ya corrió en `dev`/`main`, toma el tag `s2-<YYYYMMDD>-<short_sha>` de la pestaña **Actions**. Si deseas construir y subir manualmente desde tu máquina:
```bash
TAG="s2-$(date -u +%Y%m%d)-$(git rev-parse --short HEAD)"
DOCKER_USER="rpedrazav"

for SVC in TenantIdentityService TaxComplianceService CatalogPricingService WarehouseInventoryService POSCartService; do
  IMG=$(echo "$SVC" | tr '[:upper:]' '[:lower:]')
  docker build -t "${DOCKER_USER}/${IMG}:${TAG}" -t "${DOCKER_USER}/${IMG}:latest" -f "src/${SVC}/Dockerfile" "src/${SVC}"
  docker push "${DOCKER_USER}/${IMG}:${TAG}"
  docker push "${DOCKER_USER}/${IMG}:latest"
done
echo "Tag publicado: $TAG"
```

### Paso 3: Desplegar el Simulador de Pagos en `student-rpedraza` (para habilitar cobro con tarjeta remoto)
```bash
# Crea el ConfigMap desde el archivo oficial del simulador y levanta el Service + Deployment liviano (64Mi RAM):
kubectl -n student-rpedraza create configmap payment-simulator-code \
  --from-file=server.js=Docker/simulator/server.js \
  --dry-run=client -o yaml | kubectl apply -f -

cat <<'EOF' | kubectl -n student-rpedraza apply -f -
apiVersion: v1
kind: Service
metadata:
  name: payment-simulator
spec:
  ports:
  - port: 5050
    targetPort: 5050
  selector:
    app: payment-simulator
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: payment-simulator
spec:
  replicas: 1
  selector:
    matchLabels:
      app: payment-simulator
  template:
    metadata:
      labels:
        app: payment-simulator
    spec:
      containers:
      - name: payment-simulator
        image: node:20-alpine
        workingDir: /app
        command: ["node", "server.js"]
        env:
        - name: PORT
          value: "5050"
        - name: HOST
          value: "0.0.0.0"
        ports:
        - containerPort: 5050
        volumeMounts:
        - name: code
          mountPath: /app
          readOnly: true
        resources:
          requests:
            cpu: "10m"
            memory: "32Mi"
          limits:
            cpu: "100m"
            memory: "64Mi"
      volumes:
      - name: code
        configMap:
          name: payment-simulator-code
EOF
```

### Paso 4: Aplicar manifiestos corregidos y fijar el mismo tag inmutable en todos los Deployments
```bash
# 1. Aplicar Services/Deployments (incluye las variables Services__*__BaseUrl corregidas) e Ingress TLS:
kubectl -n student-rpedraza apply -f k8s/manifests/all-services.yaml
kubectl -n student-rpedraza apply -f new-ingresses.yaml

# 2. Fijar el tag inmutable exacto (evita mezcla de tags latest vs fechados):
kubectl -n student-rpedraza set image deployment/tenantidentityservice tenantidentityservice="rpedrazav/tenantidentityservice:${TAG}"
kubectl -n student-rpedraza set image deployment/taxcomplianceservice taxcomplianceservice="rpedrazav/taxcomplianceservice:${TAG}"
kubectl -n student-rpedraza set image deployment/catalogpricingservice catalogpricingservice="rpedrazav/catalogpricingservice:${TAG}"
kubectl -n student-rpedraza set image deployment/poscartservice poscartservice="rpedrazav/poscartservice:${TAG}"

# 3. Esperar rollout limpio:
for DEPLOY in tenantidentityservice taxcomplianceservice catalogpricingservice poscartservice payment-simulator; do
  kubectl -n student-rpedraza rollout status "deployment/${DEPLOY}" --timeout=120s
done
```

### Paso 5: Verificación rápida post-despliegue (HTTPS externo + comunicación interna MS-5 → MS-3 → MS-2)
```bash
curl -fsS https://auth-rpedraza.dev.censei.cl/health
curl -fsS https://catalog-rpedraza.dev.censei.cl/health
curl -fsS https://pos-rpedraza.dev.censei.cl/health
```

---

## Conexiones
- Matriz de rutas e Ingress TLS: [[matriz-rutas]]
- Pipeline CI y publicación de imágenes: [[ci-cd]]
- Servicios desplegados: [[ms1-identity]], [[ms2-tax]], [[ms3-catalog]], [[ms4-inventory]], [[ms5-pos]]

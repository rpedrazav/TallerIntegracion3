---
id: kong
tipo: infra
titulo: Kong API Gateway — Configuración y Rutas
estado: parcial
fuentes: [Docker/config/kong.yaml, Docker/config/README_KONG.md]
verificado_contra_codigo: true
ultima_revision: 2026-11-18
depende_de: [ms1-identity, ms3-catalog, ms4-inventory, ms5-pos]
publica: []
consume: []
reglas: [RN-07]
---
# Kong API Gateway — Configuración y Rutas

> Kong 3.6 en modo DB-less (configuración declarativa). Actúa como punto de entrada único para el frontend. Valida JWT, aplica rate-limiting global y routea a los microservicios.

Solo lo usa `docker compose` (desarrollo local, `API_MODE=local`). El clúster no usa Kong: el frontend `remote` llama a los Ingress por servicio (`k8s/manifests`).

## Upstreams (S3-029)

Los microservicios no están en docker compose: corren en el host con `run-apis.bat`/`run-apis.sh`. Kong los alcanza vía `host.docker.internal` (`extra_hosts: host-gateway` en el servicio `kong`), en los puertos de `launchSettings.json`. El simulador también va por el host (`:5050`), de modo que sirve tanto el contenedor `payment-simulator` (que publica `5050`) como `node Docker/simulator/server.js` lanzado por `run-apis`.

| Servicio Kong | Upstream |
|---|---|
| `tenant-identity-service` | `http://host.docker.internal:5124` |
| `catalog-pricing-service` | `http://host.docker.internal:5203` |
| `warehouse-inventory-service` | `http://host.docker.internal:5205` |
| `pos-cart-service` | `http://host.docker.internal:5000` |
| `payment-simulator-service` | `http://host.docker.internal:5050` |

## Rutas configuradas (verificadas en kong.yaml)

### MS-1 · Tenant & Identity Service (host :5124)

| Ruta Kong | Métodos | JWT | Descripción |
|-----------|---------|-----|-------------|
| `/api/auth/login` | POST, OPTIONS | ❌ Sin JWT | Login público |
| `/api/auth` | GET, PUT, DELETE, OPTIONS | ✅ | Auth protegido |
| `/api/users` | GET, POST, PUT, DELETE, OPTIONS | ✅ | Gestión usuarios |
| `/api/tenants` | GET, POST, PUT, DELETE, OPTIONS | ✅ | Configuración tenant |
| `/api/sucursales` | GET, POST, OPTIONS | ✅ | Sucursales |

### MS-3 · Catalog & Pricing (host :5203)

| Ruta Kong | Métodos | JWT |
|-----------|---------|-----|
| `/api/products` | GET, POST, PUT, DELETE, OPTIONS | ✅ |
| `/api/categories` | GET, POST, PUT, DELETE, OPTIONS | ✅ |
| `/api/prices` | GET, POST, PUT, DELETE, OPTIONS | ✅ |
| `/api/promotions` | GET, POST, PUT, DELETE, OPTIONS | ✅ |

### MS-4 · Warehouse & Inventory (host :5205)

| Ruta Kong | Métodos | JWT |
|-----------|---------|-----|
| `/api/stock` | GET, POST, PUT, DELETE, OPTIONS | ✅ |

### MS-5 · POS & Cart (host :5000)

| Ruta Kong | Métodos | JWT |
|-----------|---------|-----|
| `/api/turnos` | GET, POST, PUT, DELETE, OPTIONS | ✅ |
| `/api/ventas` | GET, POST, PUT, DELETE, OPTIONS | ✅ |

### Simulador de pagos (host :5050)

| Ruta Kong | Métodos | JWT | Descripción |
|-----------|---------|-----|-------------|
| `/simulator/payments`, `/api/simulator/payments` | GET, POST, OPTIONS | ❌ Sin JWT | Autorizar y consultar estado por clave idempotente |
| `/simulator/reversals`, `/api/simulator/reversals` | GET, POST, OPTIONS | ❌ Sin JWT | Reversa simulada y consulta por clave (S2-093); configuración, despliegue remoto [NO VERIFICADO] |
| `/simulator/health`, `/api/simulator/health` | GET, OPTIONS | ❌ Sin JWT | Health check local |

## Servicios SIN rutas Kong

- MS-2 Tax: sin ruta (solo llamado internamente por MS-5)
- MS-6 Supply Chain: sin ruta (planificado)
- MS-7 Analytics: sin ruta (planificado)
- MS-8 Loyalty: sin ruta (planificado)

## Plugins globales

### Rate-limiting
- 100 requests/minuto por IP
- `policy: local` (por instancia de Kong, no distribuido)
- `fault_tolerant: true` (no bloquea si el contador falla)

### CORS
```yaml
origins: ["*"]
methods: [GET, POST, PUT, DELETE, OPTIONS, PATCH]
headers: [Accept, Authorization, Content-Type, Origin, X-Requested-With, X-Tenant-Id]
credentials: true
max_age: 3600
```

## Validación JWT

El plugin `jwt` de Kong valida el token antes de llegar al microservicio: busca el consumer cuyo `jwt_secrets.key` coincide con el claim `iss` y verifica la firma HS256. `kong.yaml` declara el consumer de desarrollo `globalmart-dev` con `key: GlobalMartOS` y la clave `Jwt:Key` de `src/TenantIdentityService/appsettings.json` (S3-029); si cambia esa clave hay que actualizar ambos archivos. Sin consumer, Kong responde 401 «No credentials found for given 'iss'». No se configura `claims_to_verify`, así que la expiración la valida cada microservicio.

En modo `API_MODE=local` el frontend llama a Kong `:8000` (S3-027). Verificado el 2026-11-18 con los servicios de `run-apis`: login 200, rutas protegidas 200 con token y 401 sin token.

## Acceso a Kong

- **Proxy (tráfico app):** `http://localhost:8000`
- **Admin API:** `http://localhost:8001`
- **Kong Manager GUI:** `http://localhost:8002`

## Conexiones
- Servicios enrutados: [[ms1-identity]], [[ms3-catalog]], [[ms4-inventory]], [[ms5-pos]]
- Infraestructura: [[docker-compose]], [[matriz-rutas]]
- JWT: [[rbac-multirol]]

## Fuentes
- `Docker/config/kong.yaml`
- `Docker/config/README_KONG.md`
- `docs/context/infraestructura/matriz-rutas.md`

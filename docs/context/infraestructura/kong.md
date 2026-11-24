---
id: kong
tipo: infra
titulo: Kong API Gateway — Configuración y Rutas
estado: implementado
fuentes: [Docker/config/kong.yaml, Docker/docker-compose.yml, scripts/smoke-test.sh]
verificado_contra_codigo: true
ultima_revision: 2026-11-24
depende_de: [ms1-identity, ms2-tax, ms3-catalog, ms4-inventory, ms5-pos]
publica: []
consume: []
reglas: [RN-02, RN-07]
---
# Kong API Gateway — Configuración y Rutas

> Kong 3.6 en modo DB-less (`Docker/config/kong.yaml`, `_format_version: "3.0"`). Valida tokens JWT emitidos por MS-1, aplica `rate-limiting` y `cors` globales, y enruta a los microservicios del host (`host.docker.internal`) y al contenedor `payment-simulator` (`minimarket-net` / `:5050`). Verificado en vivo en S2-116 (`TI3-747`).

Solo lo usa `docker compose` (desarrollo local, `API_MODE=local`). El clúster no usa Kong: el frontend `remote` llama a los Ingress por servicio (`k8s/manifests`).

## Consumidor y Validación JWT (RN-07)

En modo DB-less, Kong registra el consumidor `globalmart-jwt-issuer` sincronizado con `JwtService.cs` de MS-1:
- **`key` (`iss`):** `"GlobalMartOS"`
- **`algorithm`:** `HS256`
- **`secret`:** clave de desarrollo local coincidente con `src/*/appsettings.json` (en producción debe inyectarse desde gestor de secretos, nunca commitearse).

El plugin `jwt` de Kong valida el token antes de llegar al microservicio: busca el consumer cuyo `jwt_secrets.key` coincide con el claim `iss` y verifica la firma HS256.

## Servicios y Rutas Configuradas (`Docker/config/kong.yaml`)

| Servicio Kong | Upstream | Rutas (`strip_path: false`) | Métodos | Plugin JWT |
|---|---|---|---|---|
| `tenant-identity-service` | `http://host.docker.internal:5124` | `/api/auth/login` | `POST, OPTIONS` | ❌ Público |
| `tenant-identity-service` | `http://host.docker.internal:5124` | `/api/auth/me`, `/api/auth/refresh`, `/api/auth/logout` | `GET, POST, OPTIONS` | ✅ Sí |
| `tenant-identity-service` | `http://host.docker.internal:5124` | `/api/users` | `GET, POST, PUT, DELETE, OPTIONS` | ✅ Sí |
| `tenant-identity-service` | `http://host.docker.internal:5124` | `/api/tenants` | `GET, POST, PUT, DELETE, OPTIONS` | ✅ Sí |
| `tenant-identity-service` | `http://host.docker.internal:5124` | `/api/sucursales` | `GET, POST, PUT, DELETE, OPTIONS` | ✅ Sí |
| `tenant-identity-service` | `http://host.docker.internal:5124` | `/api/roles` | `GET, POST, PUT, DELETE, OPTIONS` | ✅ Sí |
| `tax-compliance-service` | `http://host.docker.internal:5239` | `/api/tax`, `/api/comprobantes` | `GET, POST, OPTIONS` | ✅ Sí |
| `catalog-pricing-service` | `http://host.docker.internal:5203` | `/api/catalog`, `/api/precios`, `/api/categorias`, `/seed` | `GET, POST, PUT, DELETE, OPTIONS` | ✅ Sí |
| `warehouse-inventory-service` | `http://host.docker.internal:5205` | `/api/stock` | `GET, POST, PUT, DELETE, OPTIONS` | ✅ Sí |
| `pos-cart-service` | `http://host.docker.internal:5000` | `/api/turnos` | `GET, POST, PUT, DELETE, OPTIONS` | ✅ Sí |
| `pos-cart-service` | `http://host.docker.internal:5000` | `/api/ventas` | `GET, POST, PUT, PATCH, DELETE, OPTIONS` | ✅ Sí |
| `payment-simulator-service` | `http://host.docker.internal:5050` | `/simulator/payments`, `/api/simulator/payments`, `/simulator/reversals`, `/api/simulator/reversals` | `GET, POST, OPTIONS` | ❌ Sin JWT (usa `Idempotency-Key`, RN-02) |
| `payment-simulator-service` | `http://host.docker.internal:5050` | `/simulator/health`, `/api/simulator/health` | `GET, OPTIONS` | ❌ Público |

> **Nota sobre MS-6, MS-7 y MS-8:** permanecen sin rutas en Kong porque son microservicios `[PLANIFICADO]` sin controladores activos.

## Plugins Globales

- **`rate-limiting`:** 100 peticiones/minuto por IP (`policy: local`, `fault_tolerant: true`).
- **`cors`:** habilita `GET, POST, PUT, DELETE, OPTIONS, PATCH` y cabeceras `Accept, Authorization, Content-Type, Idempotency-Key, Origin, X-Requested-With, X-Tenant-Id`.

## Acceso a Kong

- **Proxy (tráfico app):** `http://localhost:8000`
- **Admin API:** `http://localhost:8001`
- **Kong Manager GUI:** `http://localhost:8002`

## Conexiones
- Servicios enrutados: [[ms1-identity]], [[ms2-tax]], [[ms3-catalog]], [[ms4-inventory]], [[ms5-pos]]
- Infraestructura: [[docker-compose]], [[matriz-rutas]]
- RBAC / JWT: [[rbac-multirol]]

## Fuentes
- `Docker/config/kong.yaml`
- `Docker/docker-compose.yml`

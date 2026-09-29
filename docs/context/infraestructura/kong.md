---
id: kong
tipo: infra
titulo: Kong API Gateway — Configuración y Rutas
estado: parcial
fuentes: [Docker/config/kong.yaml, Docker/config/README_KONG.md]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: [ms1-identity, ms3-catalog, ms5-pos]
publica: []
consume: []
reglas: [RN-07]
---
# Kong API Gateway — Configuración y Rutas

> Kong 3.6 en modo DB-less (configuración declarativa). Actúa como punto de entrada único para el frontend. Valida JWT, aplica rate-limiting global y routea a los microservicios.

## Rutas configuradas (verificadas en kong.yaml)

### MS-1 · Tenant & Identity Service (interno :5001)

| Ruta Kong | Métodos | JWT | Descripción |
|-----------|---------|-----|-------------|
| `/api/auth/login` | POST, OPTIONS | ❌ Sin JWT | Login público |
| `/api/auth` | GET, PUT, DELETE, OPTIONS | ✅ | Auth protegido |
| `/api/users` | GET, POST, PUT, DELETE, OPTIONS | ✅ | Gestión usuarios |
| `/api/tenants` | GET, POST, PUT, DELETE, OPTIONS | ✅ | Configuración tenant |

### MS-3 · Catalog & Pricing (interno :5003)

| Ruta Kong | Métodos | JWT |
|-----------|---------|-----|
| `/api/products` | GET, POST, PUT, DELETE, OPTIONS | ✅ |
| `/api/prices` | GET, POST, PUT, DELETE, OPTIONS | ✅ |
| `/api/promotions` | GET, POST, PUT, DELETE, OPTIONS | ✅ |

### MS-5 · POS & Cart (interno :5005)

| Ruta Kong | Métodos | JWT |
|-----------|---------|-----|
| `/api/turnos` | GET, POST, PUT, DELETE, OPTIONS | ✅ |
| `/api/ventas` | GET, POST, PUT, DELETE, OPTIONS | ✅ |

## Servicios SIN rutas Kong

- MS-2 Tax: sin ruta (solo llamado internamente por MS-5)
- MS-4 Warehouse: sin ruta (solo consume Kafka)
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

El plugin `jwt` de Kong valida el token antes de llegar al microservicio. Kong verifica:
- Firma del JWT con la clave configurada
- Que el token no esté expirado
- Que sea Bearer token válido

**Importante:** El frontend actualmente llama directo a `http://127.0.0.1:5124` (puerto de dev de MS-1) sin pasar por Kong `:8000`. Esto es una discrepancia con el diseño.

## Acceso a Kong

- **Proxy (tráfico app):** `http://localhost:8000`
- **Admin API:** `http://localhost:8001`
- **Kong Manager GUI:** `http://localhost:8002`

## Conexiones
- Servicios enrutados: [[ms1-identity]], [[ms3-catalog]], [[ms5-pos]]
- Infraestructura: [[docker-compose]]
- JWT: [[rbac-multirol]]

## Fuentes
- `Docker/config/kong.yaml`
- `Docker/config/README_KONG.md`

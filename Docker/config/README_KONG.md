# Kong API Gateway — Configuración Declarativa (DB-less)
**Proyecto:** GlobalMart OS — Taller de Integración 3  
**Archivo configurado:** `Docker/config/kong.yaml`  
**Versión Kong:** 3.6 · Modo: DB-less declarativo (`_format_version: "3.0"`)

---

## Servicios Registrados

| ID | Nombre interno | URL upstream | Microservicio |
|----|----------------|--------------|---------------|
| MS-1 | `tenant-identity-service` | `http://ms1:8080` | Tenant & Identity Service |
| MS-3 | `catalog-pricing-service` | `http://ms3:8080` | Catalog & Pricing Service |
| MS-5 | `pos-cart-service` | `http://ms5:8080` | POS & Cart Service |

---

## Rutas y Plugin JWT

> **Regla de seguridad:** Todas las rutas llevan `name: jwt` excepto `POST /api/auth/login`,  
> que es pública por diseño (el usuario aún no tiene token).

### MS-1 — Tenant & Identity Service (`http://ms1:8080`)

| Ruta Kong | Método | Path | Autenticación |
|-----------|--------|------|---------------|
| `auth-login` | `POST` | `/api/auth/login` | ❌ Pública (sin JWT) |
| `auth-me` | `GET` | `/api/auth/me` | ✅ JWT requerido |
| `users-route` | `GET POST PUT DELETE` | `/api/users` | ✅ JWT requerido |
| `tenants-route` | `GET POST PUT DELETE` | `/api/tenants` | ✅ JWT requerido |

> `auth-login` lleva el plugin `rate-limiting` (máx. 10 req/min) en lugar de JWT,  
> para proteger el endpoint de fuerza bruta sin bloquear el acceso público.

### MS-3 — Catalog & Pricing Service (`http://ms3:8080`)

| Ruta Kong | Método | Path | Autenticación |
|-----------|--------|------|---------------|
| `products-collection-route` | `GET POST` | `/api/products` | ✅ JWT requerido |
| `products-item-route` | `GET PUT` | `/api/products/{id}` | ✅ JWT requerido |

> Se usan **dos rutas separadas** para `/api/products` y `/api/products/{id}` porque  
> Kong evalúa paths por prefijo. La ruta de ítem usa regex `~/api/products/[^/]+$`  
> para capturar exactamente un segmento de ID sin solaparse con la colección.

### MS-5 — POS & Cart Service (`http://ms5:8080`)

| Ruta Kong | Método | Path cubierto | Autenticación |
|-----------|--------|---------------|---------------|
| `turnos-route` | `GET POST` | `/api/turnos/abrir` · `/api/turnos/cerrar` · `/api/turnos/activo` | ✅ JWT requerido |
| `ventas-route` | `POST` | `/api/ventas` · `/api/ventas/{id}/items` | ✅ JWT requerido |

> El prefijo `/api/turnos` captura todos los sub-paths (`/abrir`, `/cerrar`, `/activo`)  
> en una sola ruta, evitando duplicación de configuración.

---

## Plugins Globales

| Plugin | Configuración | Propósito |
|--------|--------------|-----------|
| `prometheus` | — | Expone métricas en `:8001/metrics` para Prometheus |
| `rate-limiting` | 100 req/min por IP | Límite global de tráfico para todos los endpoints |
| `cors` | `origins: ["*"]` · `credentials: true` | Permite requests desde la app Electron en desarrollo |

---

## Decisiones de Diseño

1. **`POST /api/auth/login` sin JWT:** Es el único endpoint público. El usuario todavía no posee un token en este punto del flujo, por lo que agregar JWT lo haría inutilizable.

2. **Rutas separadas por nivel de recurso (MS-3):** Kong no soporta parámetros de path tipo `{id}` nativamente en modo declarativo sin regex. Se usa `~/api/products/[^/]+$` para distinguir ítems individuales de la colección.

3. **Un prefijo cubre múltiples sub-paths (MS-5):** `/api/turnos` con `GET` y `POST` captura `/abrir`, `/cerrar` y `/activo` sin necesidad de tres rutas separadas, manteniendo el archivo conciso.

4. **`strip_path: false` en todas las rutas:** Kong reenvía el path completo al microservicio upstream, que es quien define la lógica interna (`/abrir`, `/cerrar`, etc.).

---

## Cómo verificar en ejecución

```bash
# 1. Levantar Kong
docker compose up -d kong

# 2. Verificar que Kong cargó la config sin errores
docker compose logs kong | grep -E "loaded|error"

# 3. Listar servicios registrados via Admin API
curl http://localhost:8001/services | python -m json.tool

# 4. Listar rutas registradas
curl http://localhost:8001/routes | python -m json.tool

# 5. Probar ruta pública (debe responder sin token)
curl -X POST http://localhost:8000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test.com","password":"1234"}'

# 6. Probar ruta protegida sin token (debe devolver 401)
curl http://localhost:8000/api/auth/me

# 7. Validar config declarativa localmente (sin Docker)
curl http://localhost:8001/config -s | python -m json.tool
```

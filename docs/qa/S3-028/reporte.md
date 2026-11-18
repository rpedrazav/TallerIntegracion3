# S3-028 — Verificar login y POS en modos `remote` y `local`

Fecha: 2026-11-17. Sprint 3 (Semana 10). Meta: Frontend simple y agradable.

## Objetivo y Criterios de Aceptación

- **Objetivo:** Probar a mano login, apertura de turno, venta y cobro en efectivo contra el clúster (`remote`) y contra `docker compose` (`local` vía Kong).
- **Entrega verificable:** Registro de la verificación en ambos modos.
- **Aceptación:** Ambos modos completan el flujo sin errores de red ni de rutas.

---

## 1. Verificación en modo `remote` (Clúster Kubernetes real — `dev.censei.cl`)

**Estado:** **VERIFICADO EN VIVO (8/8 pasos OK, 0 errores de red ni de rutas, sin `/api` duplicado).**

Comando ejecutado:
```powershell
node scripts/verify-s3-028.mjs --mode=remote
```

URLs activas desde `src/config/api.ts` (`API_MODE=remote`):
- `identity`: `https://auth-rpedraza.dev.censei.cl/api`
- `catalog`: `https://catalog-rpedraza.dev.censei.cl/api`
- `pos`: `https://pos-rpedraza.dev.censei.cl/api`

| Paso | Método y URL real ejecutada | HTTP | Resultado observado |
|---|---|---:|---|
| 1. Login | `POST https://auth-rpedraza.dev.censei.cl/api/auth/login` | **200** | JWT real emitido para `cajero@demo.cl` (`activeRole: CAJERO`) |
| 2. Turno activo previo | `GET https://pos-rpedraza.dev.censei.cl/api/turnos/activo` | **404** | Esperado (sin turno abierto antes de iniciar) |
| 3. Apertura de turno | `POST https://pos-rpedraza.dev.censei.cl/api/turnos/abrir` | **200** | Turno abierto `303b1a71-effe-433d-94c8-bfbf3f969e3d` (`$ 20.000`) |
| 4. Catálogo MS-3 | `GET https://catalog-rpedraza.dev.censei.cl/api/products?page=1&pageSize=3` | **200** | 3 productos recuperados (`leche`, `$ 1.100`) |
| 5. Creación de venta | `POST https://pos-rpedraza.dev.censei.cl/api/ventas` | **201** | Venta `9dfffef9-5b82-41d0-ae2c-77788adb6944`, subtotal `$ 1.100`, IVA `$ 209`, total `$ 1.309`, `PENDIENTE` |
| 6. Cobro en efectivo | `POST https://pos-rpedraza.dev.censei.cl/api/ventas/9dfffef9-5b82-41d0-ae2c-77788adb6944/cobrar` | **200** | `COMPLETADA`, recibido `$ 3.000`, vuelto MS-5 `$ 1.691` |
| 7. Verificación persistida | `GET https://pos-rpedraza.dev.censei.cl/api/ventas/9dfffef9-5b82-41d0-ae2c-77788adb6944` | **200** | Estado `COMPLETADA`, total `$ 1.309` |
| 8. Cierre de turno | `POST https://pos-rpedraza.dev.censei.cl/api/turnos/cerrar` | **200** | Turno cerrado limpiamente |

---

## 2. Verificación en modo `local` (Docker Compose vía Kong — `http://localhost:8000/api`)

URLs activas desde `src/config/api.ts` (`API_MODE=local`):
- `identity` / `catalog` / `pos` / `inventory`: `http://localhost:8000/api`

### 2.1 Verificación automatizada de flujo UI Electron y paridad con `Docker/config/kong.yaml`

- **Electron E2E (`npm run ci:local` → `e2e/electron/pos.spec.ts` compilado con `API_MODE=local`):** **3/3 aprobadas (exit code 0)**.
  - Incluye el caso `S3-028: modo local (Kong) completa login, apertura de turno, venta y cobro en efectivo sin /api duplicado`, verificando las rutas exactas:
    - `POST http://localhost:8000/api/auth/login`
    - `GET  http://localhost:8000/api/turnos/activo`
    - `POST http://localhost:8000/api/turnos/abrir`
    - `GET  http://localhost:8000/api/products/search`
    - `POST http://localhost:8000/api/ventas`
    - `POST http://localhost:8000/api/ventas/{id}/cobrar`
  - Verifica ausencia total de `/api/api/` y despliegue del comprobante seguro simulado con vuelto MS-5 (`$ 810`).
- **Paridad con `Docker/config/kong.yaml` (`tests/kong-routes-parity.test.cjs`):** **2/2 aprobadas** (total unitarias: **73/73 aprobadas, exit code 0**). Confirma que cada endpoint del flujo calza exactamente con los `paths` declarados en `Docker/config/kong.yaml` (`auth-login-route`, `turnos-route`, `products-route`, `ventas-route`).

### 2.2 Estado del entorno vivo `docker compose` en esta máquina (hallazgo transparente)

- **Demonio Docker Desktop detenido en esta ejecución:** `docker ps` devolvió `failed to connect to the docker API at npipe:////./pipe/dockerDesktopLinuxEngine`. Por ello, la corrida viva contra Kong real queda **[PENDIENTE DE EJECUCIÓN CON DOCKER ACTIVO]**.
- **Hallazgo de infraestructura preexistente (`Docker/config/kong.yaml` vs `Docker/docker-compose.yml`):** `kong.yaml` apunta a `http://tenant-identity-service:5001`, `http://catalog-pricing-service:5003` y `http://pos-cart-service:5005`, servicios que no existen como contenedores en `Docker/docker-compose.yml` (como ya registró `docs/qa/S2-112/reporte.md`: *"Kong → servicios del host: 503"*). Cuando se levanten los contenedores de MS-1..MS-5 en `minimarket-net` o se apunten los upstreams de Kong a `host.docker.internal:{5124,5203,5000}`, ejecutar:
  ```powershell
  node scripts/verify-s3-028.mjs --mode=local
  ```
  para completar la corrida en vivo sin modificar el frontend.

---

## 3. Resumen de Comandos Ejecutados

- `node scripts/verify-s3-028.mjs --mode=remote` → **8/8 OK, exit code 0** (clúster real).
- `npm run typecheck` (`globalmart-frontend`) → **0 errores, exit code 0**.
- `node --test tests/*.test.cjs` (`globalmart-frontend`) → **73/73 aprobadas, exit code 0**.
- `npm run ci:local` (`globalmart-frontend`, `API_MODE=local`) → **3/3 Electron E2E aprobadas, exit code 0**.

# S3-029 — E2E de cierre de turno

Fecha: 2026-11-18. Meta: Q — Calidad y regresión. Caso: [S3-006](../S3-006/caso-prueba.md).

**Aceptación:** la prueba confirma, cierra y verifica que la app sigue respondiendo. **Resultado: CUMPLE.**

## Entrega

- Spec: [`cierre-turno.integrated.spec.ts`](../../../globalmart-frontend/e2e/electron/cierre-turno.integrated.spec.ts) (`@integrado`, Electron real contra MS-1, MS-2, MS-3 y MS-5).
- Modal base de S3-036 adelantado en `CerrarTurnoPage.tsx` (`<dialog>` nativo) para poder automatizar el caso. Reemplaza `window.confirm`.
- `admin-venta.integrated.spec.ts` actualizado: ahora confirma en el modal.

## Ejecución

| Suite | Resultado |
|---|---|
| `cierre-turno.integrated.spec.ts` | 1/1 pasa (4,7 s y 4,8 s en dos corridas) |
| `admin-venta.integrated.spec.ts` | 2/2 pasa (8,5 s) |
| `npm run test:e2e:electron` (sin backend) | 3/3 pasa |
| `npm run typecheck` | 0 errores |

Pasos cubiertos del caso: 1–13. Casos negativos N1 y N2 no automatizados (opcionales).

Evidencia: [modal abierto](01-modal-cierre.png) con monto $1.190 y diferencia $0; [turno cerrado](02-turno-cerrado.png).

## Entorno y limitación encontrada

Servicios con `dotnet run` en el host y `docker compose` para PostgreSQL. Desde S3-027 el build integrado usa `API_MODE=local` (Kong en `:8000`), pero la configuración local de Kong **no funciona con `run-apis`**:

1. `Docker/config/kong.yaml` apunta a `tenant-identity-service:5001`, `pos-cart-service:5005`, etc., servicios que `docker-compose.yml` no define → Kong responde 503.
2. `kong.yaml` no declara `consumers`/`jwt_secrets` → el plugin JWT responde 401 «No credentials found for given 'iss'».

Para esta ejecución se usó un Kong temporal (`kong:3.6`) con una copia del yaml apuntando a `host.docker.internal:5124/5203/5000/5050` y un consumer con `key: GlobalMartOS` y la clave de desarrollo de MS-1. No se modificó `kong.yaml` del repo. Sin ese ajuste, **todas** las suites `@integrado` fallaban en el login. Corregido en el repo; ver la sección siguiente.

## Corrección de Kong local (2026-11-18)

Cambios en `Docker/config/kong.yaml` (solo lo usa `docker compose`; el clúster usa Ingress por servicio en `k8s/manifests` y no tiene Kong):

- Upstreams a `host.docker.internal`: MS-1 `:5124`, MS-3 `:5203`, MS-4 `:5205`, MS-5 `:5000` y simulador `:5050` (sirve tanto el contenedor como `node Docker/simulator/server.js` de `run-apis`). El contenedor `kong` ya tenía `extra_hosts: host-gateway`.
- Ruta nueva `/api/stock` → MS-4 (la usa `AdminInventario` en modo `local`).
- Consumer `globalmart-dev` con `jwt_secrets` (`key: GlobalMartOS`, HS256, clave `Jwt:Key` de MS-1, solo desarrollo).

Docs actualizados: [kong](../../context/infraestructura/kong.md), [como-ejecutar](../../context/proyecto/como-ejecutar.md) y el [README E2E](../../../globalmart-frontend/e2e/electron/README.md).

Verificación con el Kong de `docker compose` recreado con este yaml y los servicios de `dotnet run` en el host:

| Comprobación | Resultado |
|---|---|
| `POST /api/auth/login` vía `:8000` | 200 |
| `GET /api/products`, `/api/users`, `/api/sucursales` con token | 200 |
| `GET /api/products` sin token | 401 |
| `GET /api/simulator/health` vía `:8000` | 200 |
| `cierre-turno.integrated.spec.ts` | 1/1 pasa |
| `tarjeta-pesaje.integrated.spec.ts` | 5/5 pasa |
| `admin-venta.integrated.spec.ts` | 0/2: falla el primer caso (el segundo no corre, suite serial) |

Fallo de `admin-venta`: Kong responde el login con 200 en menos de 1,3 s, pero la app se queda en «Autenticando…» más de 5 s (el `expect` espera 5 s). La traza muestra que el tiempo extra ocurre después de la respuesta, en `window.api.setToken` (IPC → `electron-store`). Una sonda temporal repitió el bloqueo de ~5 s en un login de cajero. No depende de Kong. Causa raíz [NO VERIFICADO]; queda pendiente. En la ejecución anterior de este reporte (Kong temporal) esta suite pasó 2/2.

Entorno: la imagen local `node:20-alpine` del contenedor `payment-simulator` falla con `exec format error`. Se detuvo el contenedor y se usó `node Docker/simulator/server.js` en el host.

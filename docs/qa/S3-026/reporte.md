# S3-026 — Dejar solo modos `remote` y `local` en `config/api.ts`

Fecha: 2026-11-17. Sprint 3 (Semana 10). Meta: Frontend simple y agradable.

## Objetivo y Criterios de Aceptación

- **Objetivo:** Reducir la configuración a dos modos: `remote` (ingress del clúster, por defecto) y `local` (docker compose vía Kong).
- **Entrega verificable:** `config/api.ts` simplificado con una sola variable de modo.
- **Aceptación:** El login y el POS funcionan en ambos modos sin cambiar código.

## Cambios Implementados

1. **Simplificación de `globalmart-frontend/src/config/api.ts`:**
   - Se redujo `ApiMode` a únicamente `'remote' | 'local'` (eliminando `'gateway'`).
   - Se eliminó la lectura de `window.env`, los prefijos `REACT_APP_*` y todos los overrides individuales (`GATEWAY_URL`, `IDENTITY_API_URL`, `CATALOG_API_URL`, `POS_API_URL`, `INVENTORY_API_URL`).
   - Una sola variable de modo (`process.env.API_MODE`, valor por defecto `'remote'`) selecciona las URLs base:
     - `remote`: `https://{auth,catalog,pos,inventory}-rpedraza.dev.censei.cl/api` (verificado en clúster real que expone `/api/*`).
     - `local`: `http://localhost:8000/api` (Docker Compose vía Kong API Gateway).
2. **Inyección en build (`globalmart-frontend/webpack.plugins.ts`) y plantilla (`.env.example`):**
   - `webpack.DefinePlugin` inyecta `'process.env.API_MODE'` al empaquetar el renderer para alternar entre `remote` y `local` sin editar código.
   - `.env.example` documenta únicamente la variable `API_MODE` con sus dos modos.
3. **Alineación del harness Electron E2E (`e2e/electron/build.cjs`, `launch.cjs`, `pos.spec.ts`):**
   - Se eliminó la inyección de `window.env` y overrides individuales del harness E2E, compilando el renderer con `DefinePlugin` (`API_MODE='local'`) apuntando al origen único `http://localhost:8000`.
4. **Pruebas unitarias (`globalmart-frontend/tests/api-config.test.cjs`):**
   - Valida modo por defecto `remote`, modo `local` vía Kong, fallback seguro para valores desconocidos y ausencia de `gateway`, `window.env`, `REACT_APP_*` y overrides individuales.

## Resultados de Verificación

- **`npm run typecheck`:** 0 errores, exit code 0.
- **`node --test tests/*.test.cjs`:** 70/70 aprobadas, exit code 0.
- **`npm run ci:local` (`typecheck` + `e2e/electron/pos.spec.ts` en modo `local`):** 2/2 aprobadas, exit code 0.
- **Playwright navegador (`admin`, `card-search`, `cash-payment`, `demo-mostrador` en modo `remote`):** 16/16 aprobadas, exit code 0.

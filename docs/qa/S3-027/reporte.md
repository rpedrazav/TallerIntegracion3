# S3-027 — Eliminar `config.ts` y el interceptor de `/api` duplicado

Fecha: 2026-11-17. Sprint 3 (Semana 10). Meta: Frontend simple y agradable.

## Objetivo y Criterios de Aceptación

- **Objetivo:** Quitar el archivo de configuración paralelo (`src/config.ts`) y la normalización de rutas en `api/client.ts` una vez que las URLs base sean correctas.
- **Entrega verificable:** `api/client.ts` sin interceptor de normalización.
- **Aceptación:** Ninguna llamada queda con `/api` duplicado y no existen dos fuentes de configuración.

## Cambios Implementados

1. **Eliminación de configuración paralela:**
   - Se eliminó `globalmart-frontend/src/config.ts` (`API_BASE_URL = 'http://localhost:8100'`), dejando a `src/config/api.ts` como fuente única de verdad para URLs de servicios.
2. **Limpieza de `globalmart-frontend/src/api/client.ts`:**
   - Se retiró el interceptor de normalización de `/api` en `createClient`.
   - Se actualizaron las cuatro rutas de turnos en `posApi` (`/turnos/activo`, `/turnos/abrir`, `/turnos/cuadre`, `/turnos/cerrar`) para evitar prefijo `/api/` redundante sobre `baseURL` (`API_URLS.pos` ya termina en `/api` en ambos modos `remote` y `local`).
3. **Prueba automatizada (`globalmart-frontend/tests/api-config.test.cjs`):**
   - Verifica que `src/config.ts` no existe, que `src/api/client.ts` no contiene el interceptor de normalización ni prefijos `'/api/'`, y que ninguna combinación de modo (`remote` / `local`) produce `/api/api/`.

## Resultados de Verificación

- **`npm run typecheck`:** 0 errores, exit code 0.
- **`node --test tests/*.test.cjs`:** 71/71 aprobadas, exit code 0.
- **`npm run ci:local` (`typecheck` + `e2e/electron/pos.spec.ts`):** 2/2 aprobadas, exit code 0.
- **Playwright navegador (`card-search`, `cash-payment`, `pos-recovery`):** 9/9 aprobadas, exit code 0.

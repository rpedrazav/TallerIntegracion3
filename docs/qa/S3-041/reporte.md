# S3-041 / TI3-792 — Mostrar ventas anuladas en historial y cuadre

Fecha: 2026-11-24. Meta: Funcionalidad de punta a punta.

## Objetivo y criterio de aceptación
- Objetivo: Distinguir ventas anuladas en el historial del turno y reflejarlas en el balance de caja.
- Aceptación: El efectivo esperado del cuadre excluye ventas anuladas; se muestra un indicador visual claro para ANULADA.
- Simplificación: Los archivos tocados quedan sin código muerto ni duplicado, y el flujo se puede seguir desde el botón hasta la BD sin desvíos.

## Qué se probó
- `VentasTurnoPage.tsx`:
  - Renderizado de badge `ANULADA` usando tokens del sistema visual (`--color-danger-bg`, `--color-danger-text`, `--color-danger`).
  - Conservación íntegra e inmutable del monto total histórico de la venta.
  - Ocultamiento de la acción "Anular" para ventas que ya se encuentran en estado `ANULADA`.
- `CuadreCaja.tsx`:
  - Aclaración explícita en la leyenda de arqueo de que el cálculo de efectivo esperado vía MS-5 excluye ventas anuladas.
- Suite E2E de Playwright (`e2e/ventas-turno.spec.ts`):
  - 25/25 pruebas ejecutadas exitosamente cubriendo visualización de estados, permisos de ADMIN vs CAJERO, manejo de errores y preservación de registros.
- Typecheck de TypeScript: `npm run typecheck` completado con 0 errores.

## Cómo reproducir
```bash
cd globalmart-frontend
npm run typecheck
npx playwright test e2e/ventas-turno.spec.ts
```

## Resultado
- Playwright E2E: 25/25 aprobadas, exit code 0.
- TypeScript: 0 errores de tipo, exit code 0.

## Límites
- Mock de endpoints API y sesión en pruebas E2E de Playwright. La exclusión real en backend sobre base de datos PostgreSQL se valida en S3-042.

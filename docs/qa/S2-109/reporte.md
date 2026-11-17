# S2-109 / TI3-740 — Resolver errores y recarga POS

Fecha: 2026-11-17. Meta: Terminal POS con simulador.

## Objetivo y Criterios de Aceptación

- **Objetivo:** Consultar estado de intento tras timeout o reinicio y permitir cancelar o continuar sin reautorizar a ciegas.
- **Entrega verificable:** Estados de recuperación para carrito y terminal.
- **Aceptación:** Una recarga no duplica cargo ni pierde el identificador de venta.
- **Prioridad MoSCoW:** Must. Dificultad: Media.
- **Hallazgo o necesidad:** POS debe integrar el terminal de pago simulado sin duplicar cargos.

## Cambios Implementados

1. **Orquestador de recuperación y cancelación segura (`globalmart-frontend/src/payments/posRecovery.ts`):**
   - Persistencia segura por turno (`saveRecoveryAttempt`, `loadRecoveryAttempt`, `clearRecoveryAttempt`) conservando únicamente `ventaId`, `turnoId`, `metodoPago`, `claveIdempotencia`, `estado` y `updatedAt` (sin datos sensibles de tarjeta).
   - `recoverPosStateOnReload`: al recargar o reiniciar el POS, consulta el estado autoritativo en MS-5 (`GET /ventas/turno/{id}`, `GET /ventas/{id}`, `GET /ventas/{id}/intento-tarjeta`) y el terminal (`consultarEstado`) sin emitir jamás nuevas autorizaciones (`chargeCard` / `iniciarCobro`).
     - Si la venta se completó en MS-5 justo durante un timeout/recarga y ya no figura como `PENDIENTE`, se recupera usando el `ventaId` persistido y se muestra como `approved` con su comprobante y referencia opaca sin perder el identificador ni duplicar cargos.
     - Si el intento sigue incierto, conserva `ventaId` y la misma `claveIdempotencia` para la verificación explícita.
     - Si el cobro en efectivo quedó en vuelo al recargar, pasa a verificación (`uncertain`) antes de permitir otro cobro.
   - `cancelPaymentAttemptSafely`: antes de cancelar un intento de tarjeta, consulta MS-5 y el terminal:
     - Si MS-5 ya confirmó `APPROVED` (`COMPLETADA`), recupera el pago aprobado en lugar de cancelar.
     - Si MS-5 mantiene el intento en `UNCERTAIN`, bloquea la cancelación a ciegas y exige `Verificar venta`.
     - Si MS-5 confirma `NONE` o `DECLINED` y el terminal confirma cancelación o ausencia de intento activo, permite transicionar a `cancelled` y continuar el cobro conservando el mismo identificador de venta.

2. **Estados de recuperación en UI (`PaymentModal.tsx`, `PaymentModal.css`, `Pos.tsx`):**
   - `PaymentModal` muestra avisos de recuperación, el identificador de venta (`#xxxx`) y la clave del intento correlacionada en estados `uncertain` y `cancelled`.
   - Expone acciones explícitas: `Verificar venta` y `Cancelar intento` (en estado incierto/procesando) y `Continuar cobro` (tras cancelación confirmada), evitando estados bloqueados y reautorizaciones a ciegas.
   - `Pos.tsx` expone banners de recuperación para carrito y terminal cuando el modal se cierra con un intento incierto o cancelado, permitiendo retomar la verificación o continuar el cobro sin perder la venta.

3. **Pruebas Automatizadas:**
   - Pruebas unitarias en `globalmart-frontend/tests/pos-recovery.test.cjs`:
     - Persistencia y limpieza de `ventaId` + `claveIdempotencia` sin datos sensibles.
     - Recuperación de venta `COMPLETADA` tras recarga con `0` reautorizaciones.
     - Conservación de `ventaId` y `claveIdempotencia` en recarga con intento `UNCERTAIN`.
     - Validación de `cancelPaymentAttemptSafely` frente a `UNCERTAIN`, `APPROVED` y `NONE` + cancelación en terminal.
   - Pruebas E2E en `globalmart-frontend/e2e/pos-recovery.spec.ts`:
     - Recarga tras timeout 504 cuando MS-5 completó el cargo -> recupera `Cobro aprobado`, comprobante, referencia opaca y `#11111111` con `1` sola llamada `POST /cobrar-tarjeta`.
     - Timeout en terminal + recarga -> conserva `#11111111`, permite `Cancelar intento` con seguridad y `Continuar cobro` sin perder la venta pendiente.

## Resultados de Pruebas

- **Typecheck TypeScript (`npm run typecheck`):** 0 errores.
- **Pruebas unitarias (`node --test tests/*.test.cjs`):** 66/66 aprobadas (100%).
- **Pruebas E2E Playwright (`card-search`, `cash-payment`, `comprobante-seguro`, `pos-recovery`):** 11/11 aprobadas (100%).

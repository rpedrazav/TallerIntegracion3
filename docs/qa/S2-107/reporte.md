# S2-107 / TI3-738 — Conectar terminal simulado al cobro

Fecha: 2026-11-11. Meta: Terminal POS con simulador.

## Objetivo y Criterios de Aceptación

- **Objetivo:** Iniciar terminal por preload y enviar la misma clave de idempotencia a MS-5 para autorizar por pasarela simulada.
- **Entrega verificable:** Flujo de tarjeta mediante TerminalPort y cobrar-tarjeta.
- **Aceptación:** Solo la autorización confirmada por MS-5 marca venta COMPLETADA.
- **Prioridad MoSCoW:** Must. Dificultad: Media.
- **Hallazgo o necesidad:** POS debe integrar el terminal de pago simulado sin duplicar cargos.

## Cambios Implementados

1. **Flujo integrado TerminalPort y MS-5 (`synchronizeCardPayment.ts` y `Pos.tsx`):**
   - El POS inicia el terminal mediante la API expuesta por `preload` (`window.api.terminal.iniciarCobro`).
   - Se asegura la generación y conservación estricta de una única clave de idempotencia UUID (`claveIdempotencia`) en memoria para el intento en curso.
   - La misma clave de idempotencia utilizada en el terminal se propaga autoritativamente a MS-5 en la solicitud `posApi.chargeCard` (`POST /ventas/{id}/cobrar-tarjeta`).
   - Se mantiene la protección `single-flight` en el renderer para evitar dobles clics y solicitudes concurrentes con claves distintas.

2. **Criterio de Aceptación — Venta COMPLETADA exclusivamente con autorización MS-5:**
   - La aprobación local del terminal no es suficiente para marcar la venta como pagada; se requiere la confirmación explícita de MS-5 con `status === 'APPROVED'` y estado de venta `COMPLETADA`.
   - Si la respuesta de MS-5 llega con la venta en estado `PENDIENTE` o un resultado no concluyente, el flujo transiciona a `uncertain` y bloquea nuevas autorizaciones a ciegas.
   - Se refuerzan las guardias en `Pos.tsx` tanto en `handleCardPayment` como en `verifyPayment` y en la reconciliación inicial de turno para asegurar que solo una venta con `estado === 'COMPLETADA'` confirmada por MS-5 transicione a estado `approved`.

3. **Pruebas Automatizadas:**
   - Nueva suite de prueba en [`tests/synchronized-card-payment.test.cjs`](file:///C:/Users/Administrator/Desktop/Taller%20integracion%203/TallerIntegracion3/globalmart-frontend/tests/synchronized-card-payment.test.cjs):
     - Caso 1: Terminal aprueba pero MS-5 retorna venta `PENDIENTE` -> Estado resultante `uncertain` (no marca `approved` ni `COMPLETADA`).
     - Caso 2: Terminal aprueba y MS-5 confirma venta `COMPLETADA` -> Estado resultante `approved` con la referencia devuelta por MS-5.
     - Verificación de correlación: comprobación de que la clave enviada a `TerminalPort` es exactamente la misma clave enviada a `cobrar-tarjeta`.
   - Pruebas E2E en Playwright (`card-search.spec.ts`) validan el flujo completo desde la UI.

## Resultados de Pruebas

- **Pruebas unitarias (`node --test tests/*.test.cjs`):** 50/50 aprobadas (100%).
- **Pruebas E2E Playwright (`card-search`):** 5/5 aprobadas (100%).
- **Typecheck TypeScript (`npm run typecheck`):** 0 errores.

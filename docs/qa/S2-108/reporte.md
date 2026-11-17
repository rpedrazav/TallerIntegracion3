# S2-108 / TI3-739 — Mostrar comprobante de pago seguro

Fecha: 2026-11-17. Meta: Terminal POS con simulador.

## Objetivo y Criterios de Aceptación

- **Objetivo:** Tomar monto, método y referencia opaca de `VentaResponse`; no imprimir PAN ni inventar folio DTE.
- **Entrega verificable:** Vista de comprobante rotulada como simulada.
- **Aceptación:** Los montos coinciden con `GET /ventas/{id}` y no contienen datos sensibles.
- **Prioridad MoSCoW:** Must. Dificultad: Media.
- **Hallazgo o necesidad:** POS debe integrar el terminal de pago simulado sin duplicar cargos.

## Cambios Implementados

1. **Proyección segura de comprobante (`globalmart-frontend/src/components/pos/receiptData.ts`):**
   - `buildSafeReceiptData` construye el comprobante tomando exclusivamente campos autoritativos de `VentaResponse` (`id`, `estado`, `created_at`, `metodo_pago`, `subtotal`, `impuestos`, `total`, `items`), la referencia opaca en cobros con tarjeta y el monto recibido / vuelto devuelto por MS-5 en cobros en efectivo.
   - Rotula explícitamente el documento como simulado (`*** COMPROBANTE DE PAGO SIMULADO ***`), no tributario (`DOCUMENTO NO TRIBUTARIO · NO VÁLIDO COMO DTE`) y sin inventar folio DTE (`No emitido (comprobante simulado)`).
   - No recolecta, almacena ni imprime datos sensibles de tarjeta (sin PAN, CVV, número de tarjeta ni fecha de expiración).

2. **Vista de comprobante seguro (`ComprobantePago.tsx` y `ComprobantePago.css`):**
   - Renderiza ticket en formato comprobante con detalle de ítems (incluidos productos pesables en kg), subtotal, impuestos y total provenientes de MS-5.
   - Muestra método de pago, referencia opaca (tarjeta) o monto recibido y vuelto MS-5 (efectivo), rótulo de comprobante simulado y regla `@media print` para impresión limpia.

3. **Integración en el flujo de cobro (`PaymentModal.tsx` y `Pos.tsx`):**
   - `Pos.tsx` conserva el `montoRecibido` autoritativo devuelto por MS-5 y pasa `venta` y `cashReceived` a `PaymentModal`.
   - `PaymentModal.tsx` muestra `ComprobantePago` cuando el pago alcanza estado `approved` confirmado por MS-5, manteniendo intactos los textos de accesibilidad y controles de cierre existentes.

4. **Pruebas Automatizadas:**
   - Pruebas unitarias en `globalmart-frontend/tests/comprobante-pago.test.cjs`:
     - Valida coincidencia exacta de montos, ítems, método y referencia opaca contra `VentaResponse` (`GET /ventas/{id}`).
     - Valida cobro en efectivo con monto recibido y vuelto MS-5 sin referencia de tarjeta.
     - Verifica ausencia de datos sensibles y presencia de rótulos simulados / sin folio DTE inventado.
   - Pruebas E2E en `globalmart-frontend/e2e/comprobante-seguro.spec.ts`:
     - Verifica en navegador el comprobante de tarjeta (subtotal, impuestos, total, método `TARJETA`, referencia opaca, rótulo simulado, sin folio DTE ni datos sensibles).
     - Verifica en navegador el comprobante de efectivo (montos MS-5, monto recibido y vuelto).

## Resultados de Pruebas

- **Typecheck TypeScript (`npm run typecheck`):** 0 errores.
- **Pruebas unitarias (`node --test tests/*.test.cjs`):** 62/62 aprobadas (100%).
- **Pruebas E2E Playwright (`card-search`, `cash-payment`, `comprobante-seguro`):** 9/9 aprobadas (100%).

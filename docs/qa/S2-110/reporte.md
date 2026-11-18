# S2-110 / TI3-741 — Ejecutar demo completa de mostrador

Fecha: 2026-11-17. Meta: Terminal POS con simulador.

## Objetivo y Criterios de Aceptación

- **Objetivo:** Demostrar efectivo, tarjeta por terminal simulado, producto pesable y comprobante.
- **Entrega verificable:** Guion de demo ([`guion-demo.md`](guion-demo.md)) y evidencias de cada meta.
- **Aceptación:** Las cinco metas se prueban con APIs y simuladores declarados, sin alertas placeholder.
- **Prioridad MoSCoW:** Must. Dificultad: Alta.
- **Hallazgo o necesidad:** POS debe integrar el terminal de pago simulado sin duplicar cargos.

## Entregables

1. **Guion de demo de mostrador:** [`docs/qa/S2-110/guion-demo.md`](guion-demo.md) — describe prerrequisitos, servicios/simuladores declarados, 5 escenas paso a paso (G1 a G5) y plan de contingencia.
2. **Demo automatizada reproducible de mostrador:** [`globalmart-frontend/e2e/demo-mostrador.spec.ts`](../../../globalmart-frontend/e2e/demo-mostrador.spec.ts) — ejecuta el recorrido completo (efectivo + comprobante, producto pesable con balanza simulada, tarjeta por terminal POS simulado, timeout + recarga sin doble cargo y comprobante seguro), interceptando `page.on('dialog')` para asegurar **0 alertas placeholder**.
3. **Capturas de evidencia de mostrador (`docs/qa/S2-110/capturas/`):**
   - [01-demo-efectivo-comprobante.png](capturas/01-demo-efectivo-comprobante.png): Cobro en efectivo aprobado con vuelto MS-5 (`$ 810`) y comprobante rotulado como simulado.
   - [02-demo-producto-pesable.png](capturas/02-demo-producto-pesable.png): Producto pesable (`Manzana a granel`, `0.750 kg`) capturado desde balanza simulada con subtotal (`$ 1.500`), impuestos (`$ 285`) y total (`$ 1.785`) desde MS-5.
   - [03-demo-terminal-pos.png](capturas/03-demo-terminal-pos.png): Panel de `Terminal POS (Simulador)` con estado `Conectado`, monto desde MS-5 y aviso PCI sin solicitud de número de tarjeta.
   - [04-demo-recuperacion-timeout.png](capturas/04-demo-recuperacion-timeout.png): Estado `Resultado incierto` tras timeout, conservando identificador `#66666666`, clave de intento y acciones de verificación/cancelación segura.
   - [05-demo-tarjeta-comprobante.png](capturas/05-demo-tarjeta-comprobante.png): Recuperación automática tras recarga (`page.reload()`) sin duplicar cargo (`POST /cobrar-tarjeta` = 1), mostrando `Cobro aprobado`, `Referencia opaca: gw-demo-s2110` y comprobante seguro simulado sin PAN ni folio DTE inventado.

## Evidencias por Meta (G1 – G5)

| Meta | Escenario demostrado | APIs / Simuladores declarados | Pruebas ejecutadas | Resultado |
|---|---|---|---|---|
| **G1 — Administración** | Acceso `ADMIN` / bloqueo `CAJERO`, gestión de usuarios, roles, productos y configuración de IVA (con errores 400/401/403/404/409). | Contratos MS-1 y MS-3 | `admin-flows.spec.ts` (17/17), `admin.spec.ts` (8/8), `productos-admin.spec.ts` (9/9) | **34/34 aprobadas** (exit code 0) |
| **G2 — Carrito y venta (Efectivo)** | Turno activo, búsqueda MS-3, inicio/edición/eliminación/recarga de carrito, cobro efectivo con vuelto MS-5 y aislamiento entre tenants. | Contratos MS-3, MS-2 y MS-5 | `cart.spec.ts` (3/3), `cash-payment.spec.ts` (2/2), `demo-mostrador.spec.ts` (Escena 1) | **5/5 aprobadas** + captura [01](capturas/01-demo-efectivo-comprobante.png) |
| **G3 — Tarjeta simulada** | Autorización idempotente (RN-02), referencia opaca, rechazo con cambio a efectivo y bloqueo de doble clic en timeout. | MS-5 (`cobrar-tarjeta`, `intento-tarjeta`) + Pasarela simulada | `card-search.spec.ts` (5/5), `synchronized-card-payment.test.cjs` (8/8), `demo-mostrador.spec.ts` (Escena 3) | **5/5 E2E + 8/8 unit aprobadas** |
| **G4 — Balanza (Simulada)** | Lectura estable de producto pesable (`0.750 kg`), línea única con importes MS-5, producto unitario sin balanza y respaldo manual ante desconexión. | `SimulatedScaleAdapter` (`window.api.scale`) + MS-5 | `weight-cart.spec.ts` (3/3), `scale-*.test.cjs` (18/18), `demo-mostrador.spec.ts` (Escena 2) | **3/3 E2E + 18/18 unit aprobadas** + captura [02](capturas/02-demo-producto-pesable.png) |
| **G5 — Terminal POS con simulador** | Panel terminal POS (S2-106), sincronización `TerminalPort` + MS-5 con misma clave (S2-107), comprobante seguro simulado sin PAN ni folio DTE (S2-108), recuperación ante timeout/recarga sin doble cargo (S2-109) y demo completa (S2-110). | `SimulatedTerminalAdapter` (`window.api.terminal`), `posRecovery`, `ComprobantePago`, MS-5 | `comprobante-seguro.spec.ts` (2/2), `pos-recovery.spec.ts` (2/2), `demo-mostrador.spec.ts` (1/1), `terminal-*.test.cjs` + `comprobante-pago.test.cjs` + `pos-recovery.test.cjs` (27/27) | **5/5 E2E + 27/27 unit aprobadas** + capturas [03](capturas/03-demo-terminal-pos.png), [04](capturas/04-demo-recuperacion-timeout.png), [05](capturas/05-demo-tarjeta-comprobante.png) |

## Verificación de Ausencia de Alertas Placeholder

- **Inspección estática (`globalmart-frontend/src/**/*.ts*`):** `0` invocaciones a `alert(` / `window.alert(` y `0` textos placeholder de funcionalidades sin implementar.
- **Verificación en tiempo de ejecución (`demo-mostrador.spec.ts`):** Listener `page.on('dialog')` registró `[]` (`0` alertas o diálogos nativos placeholder) a lo largo de todo el flujo de efectivo, pesable, tarjeta, timeout/recarga y comprobante.

## Resultados de Ejecución Registrados

- **Typecheck TypeScript (`npm run typecheck`):** 0 errores, exit code 0.
- **Pruebas unitarias (`node --test tests/*.test.cjs`):** **66/66 aprobadas**, exit code 0.
- **Suite E2E de las 5 metas (`npx playwright test e2e/admin-flows.spec.ts e2e/admin.spec.ts e2e/productos-admin.spec.ts e2e/cart.spec.ts e2e/cash-payment.spec.ts e2e/card-search.spec.ts e2e/weight-cart.spec.ts e2e/comprobante-seguro.spec.ts e2e/pos-recovery.spec.ts e2e/demo-mostrador.spec.ts`):** **52/52 aprobadas**, exit code 0.

## Alcance, Límites y Hallazgos Declarados

- **Regla R5 (S2-111):** Las metas **G4 (Balanza)** y **G5 (Terminal POS)** se acreditan exclusivamente en **modo simulado** mediante `SimulatedScaleAdapter` y `SimulatedTerminalAdapter`. La certificación con hardware físico permanece condicionada a disponer del dispositivo, modelo y protocolo real.
- **Hallazgo preexistente fuera de alcance (`e2e/tenant-config.spec.ts`):** Al ejecutar `npm run test:e2e` completo (53 pruebas), 52/53 aprueban y falla únicamente `e2e/tenant-config.spec.ts` (harness aislado `/?component=tenant-config`) porque el input `Porcentaje IVA (%)` queda `disabled` tras el endurecimiento de `active_role` del commit `0c9dd8e`. La configuración de IVA sí queda validada end-to-end en `e2e/admin-flows.spec.ts` (3/3 aprobadas) y a nivel unitario en `tests/tenant-config-schema.test.cjs` (1/1).

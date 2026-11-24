# S2-106 / TI3-737 — Diseñar pantalla de terminal POS

Fecha: 2026-11-11. Meta: Terminal POS con simulador.

## Objetivo y Criterios de Aceptación

- **Objetivo:** Mostrar monto desde MS-5, estado de conexión, lectura, autorización, rechazo y cancelación sin pedir número de tarjeta.
- **Entrega verificable:** Panel de cobro por terminal con estados claros.
- **Aceptación:** El cajero identifica el resultado y la referencia opaca del intento.
- **Prioridad MoSCoW:** Must. Dificultad: Baja.

## Cambios Implementados

1. **Panel de terminal POS en UI (`PaymentModal.tsx` y `PaymentModal.css`):**
   - Incorporación de panel específico para Terminal POS con indicador visual y textual de estado de conexión (`Conectado` / `Desconectado`).
   - Muestra explícita del monto de la venta obtenido directamente de MS-5 (`Monto desde MS-5: $...`).
   - Visualización de las etapas del ciclo de vida del cobro con tarjeta:
     - **Lectura:** Estado inicial `Listo para iniciar lectura` y estado activo `Leyendo dispositivo en terminal...`.
     - **Autorización:** `Enviando a pasarela simulada` durante el procesamiento, y resultado final (`Aprobada` o `Rechazada`).
     - **Rechazo:** Visualización del resultado de rechazo con motivo, permitiendo al cajero reintentar tarjeta o cambiar método.
     - **Cancelación:** Visualización del estado `Cancelado` en el terminal y botón de acción `Cancelar intento`.
   - **Garantía de seguridad sin datos sensibles:** Ausencia total de inputs de número de tarjeta (PAN), CVV, PIN o fecha de expiración, con advertencia visible de seguridad.
   - **Identificación clara de resultado y referencia opaca:** Destaca la referencia opaca devuelta por la pasarela simulada (`SIM-...`, etc.) para que el cajero identifique unívocamente el intento.

2. **Integración con POS (`Pos.tsx`):**
   - Habilita la propiedad de conexión del terminal (`terminalConnected={true}`).
   - Proporciona manejador `onCancelAttempt` para transicionar de forma segura a `cancelled` y liberar el single-flight lock sin generar cobros duplicados.

3. **Pruebas Automatizadas (`tests/terminal-screen.test.cjs`):**
   - Verificación de ausencia de inputs de tarjeta y campos sensibles.
   - Verificación de presencia de monto MS-5, estado de conexión, etapas (lectura, autorización, rechazo, cancelación) y referencia opaca.
   - Validación de clases CSS asociadas al panel y los indicadores de conexión.

## Resultados de Pruebas

- **Pruebas unitarias (`node --test tests/*.test.cjs`):** 49/49 aprobadas (100%).
- **Pruebas E2E Playwright (`card-search`, `cash-payment`, `cart`):** 10/10 aprobadas (100%).
- **Typecheck TypeScript (`npm run typecheck`):** 0 errores.

# S3-006 — Caso E2E: cierre de turno

Fecha: 2026-11-16. Meta: Q — Calidad y regresión. Fuente: `tareas/plan_sprint3.xlsx`, S3-006.

**Aceptación:** el caso cubre la confirmación con modal y verifica que la app sigue respondiendo tras cerrar.
**Revisión:** pendiente con Diego (dueño de S3-036). Estado: AUTOMATIZADO en S3-029 (`e2e/electron/cierre-turno.integrated.spec.ts`). Los nombres *(acordar)* quedaron fijados en el modal de `CerrarTurnoPage.tsx`; S3-036 trabaja sobre ese mismo modal (ver `docs/context/planificacion/sprint-actual.md`).

## Contexto

- Se reportó que la app se congelaba al cerrar turno; no se reproduce en `dev` (2026-11-14, `docs/context/planificacion/sprint-actual.md`).
- Hoy la confirmación usa `window.confirm` nativo ([CerrarTurnoPage.tsx:233](../../../globalmart-frontend/src/pages/CerrarTurnoPage.tsx#L233)), sospechoso del congelamiento: en Electron, tras un diálogo nativo los inputs pueden dejar de recibir teclado.
- S3-036 (Diego) lo reemplaza por un modal React. S3-029 automatiza **este** caso sobre ese modal. Por eso los pasos 5–8 describen el modal esperado; los nombres marcados *(acordar)* se fijan en la revisión.
- Hoy solo existe cobertura parcial: [admin-venta.integrated.spec.ts:95–117](../../../globalmart-frontend/e2e/electron/admin-venta.integrated.spec.ts#L95) cierra el turno, pero no prueba cancelar, teclado ni que la app responda después.

Flujo bajo prueba: `botón «Confirmar y Cerrar Turno»` → `handleCerrarTurno` → modal → `posApi.closeShift()` → `POST /api/turnos/cerrar` (MS-5 `TurnosController.Cerrar`).

## Precondiciones

| # | Condición |
|---|---|
| P1 | Stack integrado arriba (MS-1, MS-3, MS-5 vía Kong) y app Electron lanzada con el fixture `e2e/electron/integrated-fixture.ts`. |
| P2 | Sesión como `cajero@demo.cl` (seed local). |
| P3 | El cajero tiene un turno ABIERTO con fondo inicial conocido (p. ej. $10.000) y **al menos una venta en efectivo completada**, para que `efectivo_esperado` ≠ 0. Si una corrida anterior dejó un turno abierto, se cierra por API antes (mismo patrón que `admin-venta.integrated.spec.ts:32–35`). |

Datos: `E` = `efectivo_esperado` que devuelve el cuadre inicial (fondo + ventas en efectivo).

## Pasos y resultados esperados

| # | Acción | Resultado esperado |
|---|---|---|
| 1 | Clic en el enlace **Cerrar Turno** del menú. | Se ve el heading «Cierre de Turno y Cuadre de Caja». Se precarga `E` (llamada `POST /api/turnos/cuadre` con monto 0). |
| 2 | Declarar denominaciones que sumen exactamente `E` (inputs `#input-denominacion-<id>`). | El total declarado mostrado es `E`. |
| 3 | Clic en **Calcular Cuadre**. | `POST /api/turnos/cuadre` responde 200 con `efectivo_esperado = E`, `monto_declarado = E`, `diferencia = 0`. Se muestra «sin diferencia» o equivalente. |
| 4 | Clic en **🔒 Confirmar y Cerrar Turno**. | Se abre el modal: `role="dialog"`, `aria-modal="true"`, con el monto declarado `E` en formato `es-CL`. **No** se envía `POST /api/turnos/cerrar` todavía. No aparece ningún diálogo nativo (`page.on('dialog')` no se dispara). |
| 5 | **Cancelar con mouse:** clic en **Cancelar** *(acordar)*. | El modal se cierra. No hubo `POST /api/turnos/cerrar`. El formulario conserva las cantidades. `GET /api/turnos/activo` sigue en 200. |
| 6 | **Cancelar con teclado:** reabrir el modal (paso 4) y presionar `Escape`. | Igual que el paso 5. El foco vuelve al botón «Confirmar y Cerrar Turno». |
| 7 | **Teclado tras cancelar:** escribir un número en un input de denominación y luego restaurarlo a su valor. | El input acepta el texto (verifica que no quedó bloqueado el teclado). |
| 8 | **Confirmar:** reabrir el modal, mover el foco con `Tab` hasta **Confirmar cierre** *(acordar)* y presionar `Enter`. | Se envía **una sola** `POST /api/turnos/cerrar`, responde 200. El modal se cierra. El botón muestra «Cerrando Turno...» mientras espera y queda deshabilitado (sin doble envío). |
| 9 | Esperar el resultado. | Se ve «¡Turno cerrado exitosamente!» con el monto declarado `E`. `GET /api/turnos/activo` del cajero devuelve 404. |
| 10 | **App responde — navegación:** clic en **Volver al POS**. | El POS detecta que no hay turno y redirige a «Apertura de Turno de Caja» ([Pos.tsx:164–165](../../../globalmart-frontend/src/pages/Pos.tsx#L164)) en menos de 10 s. |
| 11 | **App responde — teclado:** escribir un monto en «Monto inicial de caja» (`#monto-inicial`). | El input muestra el valor escrito. Es la comprobación directa del congelamiento reportado. |
| 12 | Clic en **Abrir Turno**. | Se abre un turno nuevo y se ve «Carrito vacío». `GET /api/turnos/activo` vuelve a 200 con un id distinto al cerrado. |
| 13 | Limpieza: cerrar el turno nuevo por API. | `POST /api/turnos/cerrar` 200; el cajero queda sin turno para la siguiente corrida. |

## Casos negativos (opcionales para S3-029)

| # | Situación | Resultado esperado |
|---|---|---|
| N1 | Entrar a «Cerrar Turno» sin turno abierto. | Aviso «No tienes un turno abierto actualmente»; botón de cierre deshabilitado; nunca se abre el modal. |
| N2 | Confirmar cuando MS-5 responde 404 (turno ya cerrado desde otra sesión). | Mensaje «No existe un turno abierto para cerrar.», sin congelamiento; la navegación del paso 10 sigue funcionando. |

## Reglas cubiertas

- **RN-06:** tras cerrar, el POS no permite vender sin abrir otro turno (paso 10).
- **RN-07:** todas las llamadas usan el JWT del cajero; el caso no envía tenant en el body (RN-01).
- **Precios del servidor:** el cuadre lo calcula MS-5; la prueba compara contra la respuesta, no contra un cálculo del renderer.

## Puntos a acordar con Diego (S3-036)

1. Nombres accesibles de los botones del modal (propuesta: «Cancelar» y «Confirmar cierre») y título del diálogo (propuesta: «Confirmar cierre de turno»).
2. Que `Escape` y clic en el fondo cancelen, y que el foco inicial quede en «Cancelar» (patrón de `CrearUsuarioModal.tsx`).
3. Que el modal muestre el monto declarado y la diferencia del cuadre.

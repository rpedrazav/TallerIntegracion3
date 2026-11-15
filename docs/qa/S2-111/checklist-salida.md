# S2-111 / TI3-742 — Criterios de salida del Sprint 2 (congelados)

Fecha de congelamiento: 2026-11-14. Meta: H — Salud técnica del sprint. Responsable: Daniel.

- **Objetivo:** revisar que cada Must tenga prueba y que las metas físicas queden identificadas como condicionadas.
- **Aceptación:** no se marca completa una meta sin evidencia del escenario exigido (`tareas/plan_sprint2_revisado.xlsx`, hoja `Sprint 2`).
- **Fuentes:** `plan_sprint2_revisado.xlsx` (hojas `Resumen` y `Sprint 2`), `docs/qa/*/reporte.md`, `.trx` adjuntos, [[estado-actual]], archivos de prueba en `tests/` y `globalmart-frontend/{e2e,tests}`.

Este documento **no ejecuta pruebas**: fija la regla de salida y la traza de cada Must a su evidencia. Las corridas que cierran la brecha son S2-112 (E2E Admin y venta), S2-113 (E2E tarjeta y pesaje), S2-114 (compilación y cobertura) y S2-115 (Go/No Go).

---

## 1. Reglas de salida congeladas

| # | Regla |
|---|---|
| R1 | Una **tarea Must** se cierra solo con prueba automatizada (o inspección declarada, si el criterio no es ejecutable) **y** resultado registrado `N/N aprobadas, exit code 0` en `docs/qa/<ID>/` o en [[estado-actual]]. |
| R2 | «PASS REPORTADO» (runner interrumpido, sin resumen ni exit code 0), corridas con fallos o pruebas que compilan pero no se ejecutaron **no cierran** la tarea: quedan ◐. |
| R3 | Una **meta** se marca CUMPLIDA solo si su escenario de cierre (columna «Escenario exigido» de §2) tiene evidencia ✅ y todas sus tareas Must están ✅. |
| R4 | Tareas de contrato o diseño se cierran con su documento, pero la meta exige la prueba de la tarea que implementa ese contrato. |
| R5 | G4 y G5 se cierran **solo en simulación**. La integración física queda **CONDICIONADA** a modelo, protocolo y equipo; ninguna prueba con mocks la declara cumplida (S2-081). |
| R6 | No se declara porcentaje de cobertura sin un reporte que lo pruebe (S2-114). |
| R7 | Lo que depende de Kafka se prueba con `docker compose`; en el clúster no hay Kafka y la venta no descuenta stock. |

Leyenda: ✅ prueba + resultado registrado · ◐ prueba existe, resultado no registrado o incompleto · ✗ sin prueba (tarea pendiente) · 📄 contrato/documento (R4).

---

## 2. Metas y escenario exigido

| Meta | Condición de cierre (plan) | Escenario exigido | Evidencia actual | Estado |
|---|---|---|---|---|
| G1 Administración | Backend y UI pasan tests y TypeScript. | Admin crea, edita y desactiva usuarios y productos; CAJERO sin acceso; errores 400/403/409 sin perder datos; `tsc` exit 0. | `admin.spec.ts`, `admin-flows.spec.ts`, `productos-admin.spec.ts` en PASS REPORTADO (S2-102..S2-104); S2-105 pendiente. | **ABIERTA** |
| G2 Carrito y venta | E2E de turno a venta y stock sin pérdida ni datos cruzados. | Turno → venta → cobro efectivo → `sale.completed` → stock descontado una vez; anulación total con `sale.reversed`; RN-01 entre tenants. | Outbox y aislamiento ✅ (S2-007..S2-010, S2-023); E2E de carrito con 2/3 fallos (S2-025); consumidor MS-4 sin cerrar (S2-096..S2-100). | **ABIERTA** |
| G3 Tarjeta simulada | RN-02 cumplida en simulación; sin cargo real ni datos de tarjeta locales. | Aprobación, rechazo y timeout por pasarela simulada; misma clave = una autorización; venta solo COMPLETADA con aprobación de MS-5. | S2-035, S2-052, S2-053, S2-055 (E2E Electron 2/2) ✅; S2-054 y UI de cobro ◐. | **CASI CUMPLIDA** (falta registrar S2-054, S2-047..S2-050) |
| G4 Balanza | Simulador y entrada manual; backend de peso variable. Prueba física **condicionada**. | Peso estable y manual llegan como una sola línea con importes de MS-5; peso inválido no se envía; fallback sin duplicar. | Backend ✅ (S2-062..S2-065); UI y adaptador con pruebas ◐; protocolo físico listo (S2-085). | **ABIERTA (simulación) · CONDICIONADA (física)** |
| G5 Terminal POS | Terminal simulado coordinado con MS-5 y pasarela. Prueba física **condicionada**. | Terminal simulado → `cobrar-tarjeta` → solo aprobación de MS-5 marca COMPLETADA; recarga sin doble cargo; comprobante simulado. | S2-071..S2-073, S2-106, S2-107 ✅; S2-108..S2-110 pendientes. | **ABIERTA (simulación) · CONDICIONADA (física)** |
| H Salud técnica | DoD verificable. | CI bloquea por tipos/tests/API; E2E de Admin, venta, tarjeta y pesaje con exit 0; cobertura medida; Go/No Go con defectos y dueño. | CI tiene typecheck, `node --test`, Playwright y `dotnet test` (`ci.yml`), y E2E Electron (`electron-e2e.yml`); S2-112..S2-120 pendientes. | **ABIERTA** |

**Metas condicionadas (R5).** La certificación con balanza y terminal físicos requiere: modelo y protocolo del equipo, el dispositivo en el laboratorio y la ejecución de [`protocolo-balanza-fisica.md`](../S2-085/protocolo-balanza-fisica.md). Sin eso, el reporte de release (S2-120) debe decir «simulado» para G4 y G5.

---

## 3. Trazabilidad Must → prueba

Solo tareas **Must**. Las Should/Could (S2-030, S2-046, S2-059, S2-069, S2-085, S2-090, S2-104, S2-119) no bloquean la salida.

### G1 — Administración

| ID | Tarea | Prueba / evidencia | Estado |
|---|---|---|---|
| S2-011 | Contrato de baja de producto | 📄 contrato en [[estado-actual]]; prueba en S2-012 | 📄 |
| S2-012 | Desactivación en MS-3 | `DesactivarProductoIntegrationTests` 11/11 (2026-10-14, [[estado-actual]]) | ✅ |
| S2-013 | Acción de productos Admin | `productos-admin.spec.ts` (PASS REPORTADO, S2-103) | ◐ |
| S2-014 | Tipos de TenantConfig | `tenant-config.spec.ts`, `tenant-config-schema.test.cjs`, `tsc` en CI | ◐ |
| S2-015 | Baja y formulario por tenant | `TenantConfigIntegrationTests`, `ProductoTenantIsolationIntegrationTests` | ◐ |
| S2-041 | Estados y permisos Admin | 📄; prueba en `admin.spec.ts` (S2-101) | 📄 |
| S2-042 | Edición de usuario | `ActualizarUsuarioIntegrationTests`, `user-edit.test.cjs`, `admin-flows.spec.ts` | ◐ |
| S2-043 | Desactivación de usuario | `DesactivarUsuarioIntegrationTests`, `user-deactivation.test.cjs` | ◐ |
| S2-044 | Configuración y rol activo | `admin-access.test.cjs`, `tenant-config.spec.ts` | ◐ |
| S2-045 | Flujos Admin en interfaz | `admin-flows.spec.ts` (PASS REPORTADO) | ◐ |
| S2-101 | Rutas Admin y rol activo | store 9/9 + `admin.spec.ts` 8 casos; runner interrumpido ([[estado-actual]]) | ◐ |
| S2-102 | Usuarios inactivos | Node 12/12 exit 0; Playwright 23/23 PASS REPORTADO | ◐ |
| S2-103 | Productos desde Admin | `productos-admin.spec.ts` PASS REPORTADO; integración MS-3 no ejecutada | ◐ |
| S2-105 | Regresión de Administración | — pendiente | ✗ |

### G2 — Carrito, venta y anulación

| ID | Tarea | Prueba / evidencia | Estado |
|---|---|---|---|
| S2-001 | Contrato de creación de venta | `CrearVentaRequestValidatorTests` | ◐ |
| S2-002 | Productos desde MS-3 | `VentaIntegrationTests` | ◐ |
| S2-003 | IVA real en la creación | `VentaIntegrationTests`, `PesableTaxRoundingTests` | ◐ |
| S2-004 | Cobro efectivo restringido | `CobroIntegrationTests` | ◐ |
| S2-005 | Creación segura y turno abierto | `VentaIntegrationTests`, `TurnoIntegrationTests` | ◐ |
| S2-006 | Contrato de evento y reintento | 📄; prueba en S2-009/S2-010 | 📄 |
| S2-007 | Migración de outbox | `OutboxMigrationTests` — [S2-007.trx](../S2-007/S2-007.trx) | ✅ |
| S2-008 | Venta y evento en una transacción | `VentaOutboxTransactionTests` — [S2-008.trx](../S2-008/S2-008.trx) | ✅ |
| S2-009 | Worker de pendientes | `OutboxPublisherWorkerTests` — [S2-009.trx](../S2-009/S2-009.trx) | ✅ |
| S2-010 | Caída y recuperación del broker | `OutboxBrokerRecoveryIntegrationTests` — [S2-010.trx](../S2-010/S2-010.trx) | ✅ |
| S2-016 | URL y token centralizados | Inspección: `api/client.ts` + `config/api.ts`; sin prueba | ◐ |
| S2-017 | Turno antes del carrito | `e2e/electron/pos.spec.ts` (login → turno → POS), [S2-024](../S2-024/reporte.md) 2/2 | ✅ |
| S2-018 | Venta de servidor desde POS | `cart.spec.ts` (recarga recupera venta) | ◐ |
| S2-019 | Agregar y modificar ítems | `cart.spec.ts` | ◐ |
| S2-020 | Eliminación y recuperación | `cart.spec.ts` | ◐ |
| S2-021 | Casos de aceptación de las metas | 📄 sustituido por este checklist (§2) | 📄 |
| S2-022 | Fixtures de dos tenants | `TwoTenantFixtureTests` | ◐ |
| S2-023 | Aislamiento de venta y producto | 32/32 — [reporte](../S2-023/reporte.md) | ✅ |
| S2-024 | Esqueleto E2E Electron | `ci:local` exit 0, 2/2 — [reporte](../S2-024/reporte.md) | ✅ |
| S2-025 | Regresión de carrito | 1/3 aprobado, 2 fallidos — [reporte](../S2-025/reporte.md); `cart.spec.ts` actual sin corrida registrada | ◐ |
| S2-091 | Alcance de anulación | 📄 [anulacion-venta](../../context/dominio/anulacion-venta.md) | 📄 |
| S2-092 | Exponer anulación | `AnulacionVentaIntegrationTests` — [reporte](../S2-092/reporte.md), [trx](../S2-092/S2-092.trx) | ✅ |
| S2-093 | Reversa de tarjeta | `ReversaTarjetaIntegrationTests` 17/17, total 93 .NET + 4 Node — [reporte](../S2-093/reporte.md) | ✅ |
| S2-094 | `sale.reversed` por outbox | `SaleReversedOutboxIntegrationTests` 60/60 — [reporte](../S2-094/reporte.md) | ✅ |
| S2-095 | Probar anulación y permisos | — pendiente | ✗ |
| S2-096 | Política de errores Kafka | 📄 pendiente | ✗ |
| S2-097 | Descuento atómico e idempotente | `StockConsumerIntegrationTests` (previo); cambio pendiente | ✗ |
| S2-098 | Stock no negativo por evento | — pendiente | ✗ |
| S2-099 | Commit y recuperación | — pendiente | ✗ |
| S2-100 | Duplicados y fallo intermedio | — pendiente | ✗ |

### G3 — Cobro con tarjeta simulado

| ID | Tarea | Prueba / evidencia | Estado |
|---|---|---|---|
| S2-031 | Contrato de pasarela | `PaymentGatewayClientTests` | ◐ |
| S2-032 | Persistencia de intento | `IntentoPagoPersistenceTests` | ◐ |
| S2-033 | Endpoint `cobrar-tarjeta` | `CobrarTarjetaIntegrationTests` | ◐ |
| S2-034 | Aprobación y rechazo | `CobrarTarjetaIntegrationTests` | ◐ |
| S2-035 | Cobro seguro e idempotente | `CobroSeguroIntegrationTests` — [reporte](../S2-035/reporte.md) | ✅ |
| S2-036 | Escenarios del simulador | 📄; prueba en S2-038 | 📄 |
| S2-037 | Adaptador HTTP | [S2-037.trx](../S2-037/S2-037.trx) | ✅ |
| S2-038 | Simulador de autorizaciones | [S2-038.trx](../S2-038/S2-038.trx) | ✅ |
| S2-039 | Reintento seguro | `PaymentRetrySafetyTests` — [S2-039.trx](../S2-039/S2-039.trx) | ✅ |
| S2-040 | Secuencia de pago y evento | `PaymentEventSequenceContractTests` — [reporte](../S2-040/reporte.md) | ✅ |
| S2-047 | Cobro en efectivo (UI) | `cash-payment.spec.ts`, 10/10 en [S2-106](../S2-106/reporte.md) | ✅ |
| S2-048 | Cobro con tarjeta (UI) | `card-search.spec.ts`, 5/5 en [S2-107](../S2-107/reporte.md) | ✅ |
| S2-049 | Timeout sin doble cargo | `card-search.spec.ts` (doble clic); backend 54/54 en merge ([[estado-actual]]) | ✅ |
| S2-050 | Estados de pago en POS | `card-search.spec.ts` (rechazo, timeout) | ✅ |
| S2-051 | Matriz RN-02 | 📄 [18 casos](../../../diagramas/matriz/S2-051-matriz-rn02-pagos.md); aceptación E2E de los 18 [NO VERIFICADO] | 📄 |
| S2-052 | Rechazo y timeout por HTTP | [reporte](../S2-052/reporte.md) | ✅ |
| S2-053 | Autorización e idempotencia | [reporte](../S2-053/reporte.md) | ✅ |
| S2-054 | Aislamiento de pago | `CobroSeguroIntegrationTests` (casos S2-054); sin reporte propio | ◐ |
| S2-055 | E2E Electron tarjeta | 2/2 — [reporte](../S2-055/reporte.md) | ✅ |
| S2-056 | Configuración del simulador | Inspección: `Docker/env.example` | ◐ |
| S2-057 | Simulador en entorno local | `PaymentSimulatorIntegrationTests` | ◐ |
| S2-058 | Ruta de pagos en gateway | `PaymentSimulatorIntegrationTests` | ◐ |
| S2-060 | Smoke de despliegue de tarjeta | `CobroTarjetaCorrelationIntegrationTests` | ◐ |

### G4 — Balanza con simulador (física CONDICIONADA)

| ID | Tarea | Prueba / evidencia | Estado |
|---|---|---|---|
| S2-061 | Contrato de peso variable | 📄; prueba en S2-062..S2-065 | 📄 |
| S2-062 | Validar peso al agregar | [reporte](../S2-062/reporte.md) | ✅ |
| S2-063 | Total pesable en servidor | [reporte](../S2-063/reporte.md) | ✅ |
| S2-064 | Cantidad pesable con recálculo | [reporte](../S2-064/reporte.md) | ✅ |
| S2-065 | Fronteras de peso y tenant | [reporte](../S2-065/reporte.md) | ✅ |
| S2-066 | Interfaz de balanza | `scale-contract.test.cjs` | ◐ |
| S2-067 | Adaptador simulado | `simulated-scale-adapter.test.cjs` | ◐ |
| S2-068 | IPC mínimo y validado | `scale-ipc.test.cjs` | ◐ |
| S2-070 | IPC y estados simulados | `scale-main-preload.integration.test.cjs` | ◐ |
| S2-076 | Estado de captura de peso | `weight-cart.spec.ts` (unitario sin balanza) | ◐ |
| S2-077 | Lectura simulada al carrito | `weight-cart.spec.ts` | ◐ |
| S2-078 | Ingreso manual de respaldo | `weight-cart.spec.ts` (pesos inválidos) | ◐ |
| S2-079 | Precio e IVA del servidor | `weight-cart.spec.ts` (importes de MS-5) | ◐ |
| S2-080 | UX de balanza y respaldo | `weight-cart.spec.ts`, 3 escenarios | ◐ |
| S2-081 | Separar pruebas simuladas y físicas | `ScaleValidationPlanTests`, [plan](../S2-081/plan-validacion.md) | ✅ |
| S2-082 | Parser y redondeo | `ScaleParserRoundingIntegrationTests` | ◐ |
| S2-083 | Caída de balanza y modo manual | `ScaleFallbackIntegrationTests` | ◐ |
| S2-084 | Cálculo pesable entre servicios | `PesableEntreServiciosIntegrationTests` (62/62 según grafo) | ◐ (sin reporte en `docs/qa`) |

### G5 — Terminal POS con simulador (física CONDICIONADA)

| ID | Tarea | Prueba / evidencia | Estado |
|---|---|---|---|
| S2-071 | Contrato del terminal | `terminal-port.test.cjs` 4/4 ([[estado-actual]]) | ✅ |
| S2-072 | Simulador de terminal | `terminal-port.test.cjs` 12/12 ([[estado-actual]]) | ✅ |
| S2-073 | IPC limitado del terminal | `terminal-ipc.test.cjs` 6/6 ([[estado-actual]]) | ✅ |
| S2-074 | Estado sincronizado con pasarela | `synchronized-card-payment.test.cjs` | ◐ |
| S2-075 | Estados del terminal simulado | `terminal-port.test.cjs`, `terminal-ipc.test.cjs` | ◐ |
| S2-086 | Límites del terminal simulado | `smoke-simulators.ts` | ◐ |
| S2-087 | Configuración de adaptadores | `smoke-simulators.ts` | ◐ |
| S2-088 | Seguridad de IPC | `smoke-simulators.ts`, `terminal-ipc.test.cjs` | ◐ |
| S2-089 | Smoke de simuladores | `smoke-simulators.ts` | ◐ |
| S2-106 | Pantalla de terminal | 49/49 unitarias + 10/10 E2E — [reporte](../S2-106/reporte.md) | ✅ |
| S2-107 | Terminal simulado al cobro | 50/50 + 5/5 — [reporte](../S2-107/reporte.md) | ✅ |
| S2-108 | Comprobante de pago seguro | — pendiente | ✗ |
| S2-109 | Errores y recarga POS | — pendiente | ✗ |
| S2-110 | Demo completa de mostrador | — pendiente | ✗ |

### H — Salud técnica

| ID | Tarea | Prueba / evidencia | Estado |
|---|---|---|---|
| S2-026 | Inventario de rutas | 📄 [matriz-rutas](../../context/infraestructura/matriz-rutas.md) | 📄 |
| S2-027 | Rutas Kong del flujo crítico | `RoutePrefixIntegrationTests` | ◐ |
| S2-028 | Destinos de APIs parametrizados | Inspección: `config/api.ts` | ◐ |
| S2-029 | TypeScript en CI | `ci.yml` job «Frontend TypeScript Gate (S2-029)» | ✅ |
| S2-111 | Criterios de salida | Este documento | 📄 |
| S2-112 | E2E de Admin y venta | — pendiente | ✗ |
| S2-113 | E2E de tarjeta y pesaje | — pendiente | ✗ |
| S2-114 | Cobertura y compilación | — pendiente | ✗ |
| S2-115 | Aislamiento y recuperación final | — pendiente | ✗ |
| S2-116 | Compose y gateway final | — pendiente | ✗ |
| S2-117 | Pruebas críticas en CI | — pendiente (CI ya ejecuta `dotnet test` y Playwright) | ✗ |
| S2-118 | Nodos de contexto | — pendiente | ✗ |
| S2-120 | Revisión de release | — pendiente | ✗ |

---

## 4. Resumen y gate

| Estado | Tareas Must |
|---|---:|
| ✅ prueba + resultado registrado | 34 |
| ◐ prueba sin resultado registrado / incompleto | 50 |
| 📄 contrato o documento | 10 |
| ✗ sin prueba (pendiente) | 18 |
| **Total Must** | **112** |

**Ninguna meta cumple R3 al 2026-11-14.** El hallazgo de la tarea se confirma: casi la mitad de los Must tienen prueba pero sin corrida registrada con exit code 0, y no existe un gate E2E único.

**Gate de salida** (lo ejecutan S2-112..S2-115 y lo decide S2-120):

- [ ] Toda fila ◐ pasa a ✅ con `docs/qa/<ID>/reporte.md` o queda listada como defecto con dueño en S2-115.
- [ ] Toda fila ✗ se implementa y prueba, o se declara fuera de la salida del sprint en S2-115.
- [ ] `dotnet test GlobalMartOS.sln` exit 0 con `GLOBALMART_TEST_POSTGRES` definido (S2-114).
- [ ] `npm run typecheck`, `node --test tests/*.test.cjs`, `npm run test:e2e` y `npm run ci:local` exit 0, sin PASS REPORTADO (S2-112, S2-113).
- [ ] E2E de G2 corre en `docker compose` con Kafka: venta → `sale.completed` → stock descontado una vez (R7).
- [ ] Cobertura reportada con herramienta y archivo adjunto, sin cifras inventadas (S2-114, R6).
- [ ] El reporte de release dice «simulado» para G4 y G5 y lista lo que falta para la prueba física (R5).

## Límites

- Los estados ✅ se basan en reportes y `.trx` ya registrados; esta tarea no los reejecutó.
- La asignación de pruebas sin etiqueta `S2-xxx` a tareas (filas ◐) se hizo por nombre del archivo y del caso; S2-112..S2-115 deben confirmarla al ejecutar.
- Recuento al 2026-11-14 sobre `dev` (`cae5d5b`).

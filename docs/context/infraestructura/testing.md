---
id: testing
tipo: infra
titulo: Estrategia de Testing — GlobalMart OS
estado: parcial
fuentes: [tests/GlobalMart.IntegrationTests/, tests/GlobalMart.Tests/, globalmart-frontend/tests/, globalmart-frontend/e2e/, scripts/, .github/workflows/ci.yml]
verificado_contra_codigo: true
ultima_revision: 2026-11-24
depende_de: [stack, ci-cd]
publica: []
consume: []
reglas: [RN-01, RN-02, RN-06, RNF-07]
---
# Estrategia de Testing — GlobalMart OS

> Estado real verificado contra ejecución en Release el 2026-11-24. El proyecto cuenta con **318 pruebas automatizadas xUnit en backend** (`GlobalMart.IntegrationTests`), **47 pruebas unitarias en `GlobalMart.Tests`**, **25 pruebas unitarias Node en frontend** (`tests/*.test.cjs`), **smoke de simuladores sin hardware** y **scripts de smoke HTTP/Gateway**, todos aprobados sin fallos. El porcentaje formal de cobertura de líneas (RNF-07 ≥ 80%) permanece `[NO MEDIDO]` por ausencia de colector de cobertura global, por lo que no se atribuye 80%.

## Suites Automatizadas Verificadas (2026-11-24)

| Suite / Proyecto | Tecnología | Casos Ejecutados | Estado Real |
|---|---|---|---|
| `tests/GlobalMart.IntegrationTests` | xUnit + `WebApplicationFactory` + PostgreSQL 16 + Kafka + Simulador Node | **318 / 318 aprobados** (0 fallos, 0 omitidos, Release) | `[IMPLEMENTADO]` |
| `tests/GlobalMart.Tests` | xUnit (.NET 8) | **47 / 47 aprobados** (Identity, Tax, Warehouse) | `[IMPLEMENTADO]` |
| `globalmart-frontend/tests/*.test.cjs` | `node:test` (`npm run test:unit`) | **25 / 25 aprobados** (IPC, contratos balanza/serial, `TerminalPort`, RBAC, Zod IVA, usuarios) | `[IMPLEMENTADO]` |
| `globalmart-frontend/tests/smoke-simulators.ts` | TypeScript / `ts-node` (`npm run smoke:simulators`) | **5 / 5 bloques aprobados** (balanza + terminal mock + bloqueo `RealTerminalAdapter`) | `[IMPLEMENTADO]` |
| `scripts/smoke-test.sh` | Bash + `curl` sobre Kong (:8000) o APIs directas | **100% de aserciones aprobadas** (Auth, Catalog, POS y Pasarela vía Gateway) | `[IMPLEMENTADO]` |

### S2-055 / TI3-686 — Cobro tarjeta desde Electron [IMPLEMENTADO, alcance acotado]

`ElectronCardE2ETests` y `e2e/electron/card-backend.cjs` automatizan apertura, creación de venta y cobro aprobado/rechazado desde main/preload/renderer reales. MS-5 con JWT validado, PostgreSQL migrado y proceso Node del simulador son reales; identidad, catálogo e IVA son fixtures. Comprueba venta, intento, pago, referencia opaca, autorización única y payload outbox. No acredita entrega Kafka (worker deshabilitado), Kong ni integración desplegada MS-1/MS-2/MS-3. Windows 2026-10-19: **2/2 aprobadas, 0 omitidas**, con capturas de apertura, venta y resultado. Comando `npm run test:e2e:card`; requiere `GLOBALMART_TEST_POSTGRES` y sesión gráfica. El comando habilita `GLOBALMART_ELECTRON_E2E=1`; la suite backend ordinaria omite estos E2E para no exigir escritorio. [Reproducción](../../../globalmart-frontend/e2e/electron/README.md) y [evidencias](../../qa/S2-055/reporte.md).

### S2-024 / TI3-655 — Esqueleto E2E de Electron [IMPLEMENTADO]

`globalmart-frontend/playwright.electron.config.ts` ejecuta el main, preload y renderer reales con perfil independiente por caso, login ficticio y APIs interceptadas de MS-1/MS-3/MS-5. Cubre login → 404 sin turno → apertura → POS vacío, persistencia IPC tras recarga y rechazo 401 sin sesión. `npm run ci:local` compila y verifica TypeScript antes de ejecutar la suite. Validación Windows del 2026-10-18: **2/2 aprobadas**. No certifica venta/cobro ni aislamiento RN-01 del backend; los mocks no validan firmas JWT. [Guía](../../../globalmart-frontend/e2e/electron/README.md) y [evidencia](../../qa/S2-024/reporte.md).

## Límites y Brechas Abiertas

| Brecha | Estado / Impacto |
|---|---|
| Métrica porcentual RNF-07 (≥ 80%) | `[NO MEDIDO]` — no se reporta porcentaje global de líneas sin `coverlet.collector` configurado sobre todos los MS. |
| E2E integrado sobre hardware físico | `[NO VERIFICADO]` — balanza y terminal físico bloqueados hasta disponer de equipos y protocolos homologados ([[guia-sustitucion-hardware-real]]). |
| E2E completo contra clúster universitario | `[NO VERIFICADO]` — requiere aplicar las variables corregidas de [[manifiestos-k8s-cluster]] en `student-rpedraza`. |

## Conexiones
- CI/CD: [[ci-cd]]
- Reglas: [[reglas-negocio]] (RN-01, RN-02, RN-06)
- Stack: [[stack]]

## Fuentes
- `tests/GlobalMart.IntegrationTests/`
- `tests/GlobalMart.Tests/`
- `globalmart-frontend/tests/`
- `.github/workflows/ci.yml`

## S2-022 — Fixtures de dos tenants y turnos

[IMPLEMENTADO] Utilidad reutilizable en [Fixtures/README.md](../../../tests/GlobalMart.IntegrationTests/Fixtures/README.md) con esquemas PostgreSQL exclusivos por ejecución, usuarios CAJERO, sucursales, productos, turnos abiertos/cerrados, ventas pendientes/completadas e ítems/pagos efectivos para dos tenants. Limpieza idempotente por tenant y disposición de los esquemas propios. Compilación Release aprobada; 2/2 pruebas aprobadas el 2026-10-12, incluida la de integración contra PostgreSQL 16 desechable en Docker (filtrado por tenant, limpieza idempotente que conserva al otro tenant y eliminación de esquemas verificada). Corregido el seed del rol CAJERO, que ya existe tras crear las tablas. No cambia endpoints ni acredita E2E de frontend.

## S2-025 — Regresión de carrito

[PARCIAL] Suite Playwright sobre el renderer real de POS, con catálogo y puente de sesión controlados. Ejecución 2026-10-12: 1 aprobado (agregar, cantidad, eliminar y recargar), 2 fallidos (sin respuesta de ventas MS-5 y persistencia del carrito al cambiar sesión de tenant). En aquella revisión, Pos usaba `pos_cart` sin partición por tenant y calculaba IVA local al 19%; esa descripción histórica fue superada por la integración del 2026-11-01. Igualdad con totales de MS-5 y aislamiento HTTP de backend [NO VERIFICADO]. No acredita E2E completo ni venta funcional. Reporte y capturas: [S2-025](../../qa/S2-025/reporte.md).

## Integración de ramas — 2026-11-01

`GlobalMart.Tests` forma parte de la solución: 47 pruebas unitarias de Identity, Tax e inventario aprobadas. La suite administrativa previa se conserva en `admin-flows.spec.ts` y se adapta al formulario actual. `cart.spec.ts` valida el renderer con fixtures HTTP de turnos y ventas (IVA 20 %); no acredita integración real con MS-5. Los flujos reales se verifican en las suites .NET/Electron.

S2-082/S2-083/S2-084: se adaptan los harness de extracción de callbacks al flujo actual `readProductWeight` → confirmación → `commitProductToCart`, y se leen subtotal, IVA y total vigentes sin exigir la variable retirada `tasaIva`. La conexión explícita `GLOBALMART_TEST_POSTGRES` también sirve para S2-084. Suite dirigida: 62/62 aprobadas.

Validación final 2026-11-01: solución Release con 445 aprobadas, cero fallos y 1 caso Electron opt-in omitido; StockConsumerIntegrationTests se excluyó por falta de Kafka local. Pruebas Electron de login/turno: 2/2 aprobadas. Evidencia y alcance en [reporte de integración](../../qa/integracion-main-2026-11-01.md).

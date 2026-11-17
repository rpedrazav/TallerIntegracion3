# S3-005 — Línea base de pruebas antes de simplificar

**Base:** rama `dev`, commit `9a4056b` («Ejecutar E2E de tarjeta y pesaje»), árbol limpio. **Fecha:** 2026-11-16, Windows 11, .NET SDK 8.0.425, Node 22.19.0.

**Aceptación:** los fallos previos quedan anotados para no atribuirlos a la simplificación. ✅ Todos los fallos de abajo existen **antes** de cualquier cambio de simplificación. Las tareas de regresión (S3-0xx) deben compararse contra esta tabla.

## Resumen

| # | Comando | Resultado | Fallos preexistentes |
|---|---|---|---|
| 1 | `dotnet build GlobalMartOS.sln` | ✅ 0 errores, 23 advertencias | — |
| 2 | `dotnet test tests/GlobalMart.Tests` | ✅ **52/52** | — |
| 3 | `dotnet test tests/GlobalMart.IntegrationTests` | ⚠️ **279/296** en la base (corrida completa 254/296 + 25 recuperadas al repetir) | 17 de `ReversaTarjetaIntegrationTests` (B1, **corregido después**: 23/23) |
| 4 | Pruebas de consola fuera de la solución (`src/*/tests/*`, `dotnet run`) | ⚠️ 1 de 4 pasa completa | B2, B3, B4 |
| 5 | `npm run typecheck` | ✅ exit 0 | — |
| 6 | `node --test tests/*.test.cjs` | ⚠️ **48/50** | 2 por entorno local (E1) |
| 7 | `node --test Docker/simulator/reversals.test.js` | ✅ 4/4 | — |
| 8 | `bash scripts/smoke-test.sh` | ✅ exit 0 | Avisos E3 |
| 9 | Electron: `npm run test:e2e:electron` + `test:e2e:integrado` | ✅ **9/9** (2 mock + 2 S2-112 + 5 S2-113) | Arranque de Electron intermitente (E4) |
| 10 | Navegador: `npm run test:e2e` | ⛔ no ejecutable aquí | Falta Chromium de Playwright (E2) |

Evidencia: [integracion.trx](integracion.trx) (corrida completa), [integracion-reintento.trx](integracion-reintento.trx) (7 clases repetidas) y [e2e-electron.txt](e2e-electron.txt) (9/9).

## Fallos preexistentes del código o de las pruebas

| ID | Prueba | Fallo | Causa |
|---|---|---|---|
| **B1** ✅ | `ReversaTarjetaIntegrationTests` (17 de 23 casos) | `HttpRequestException`: conexión rechazada contra el simulador que lanza la propia prueba | `new ProcessStartInfo("node", ruta)` pasa la ruta **sin comillas**. Con espacios en la ruta del repo (`…\Semestre 6\…`), Node recibe `C:\Users\…\Semestre` y termina con `MODULE_NOT_FOUND`. En S2-093 pasó 17/17 porque se corrió desde una ruta sin espacios. Depende de dónde esté el repositorio. **Corregido** pasando la ruta con `ArgumentList` (el patrón de las demás pruebas): la clase da **23/23 en 19 s** ([reversa-tarjeta.trx](reversa-tarjeta.trx)). |
| **B2** | `POSCartService.CobroCuadreTest` | 8 pasan, **2 fallan**: «Se publica sale.completed…» y «sale.completed contiene payload…» | La prueba espera el productor Kafka directo; MS-5 publica por outbox desde S2-007. No está en la solución ni en el CI. |
| **B3** | `POSCartService.AgregarItemTest` | Se cae tras 83 checks con `TransactionIgnoredWarning` | Usa EF InMemory, que no soporta la transacción de `CompletarConOutboxAsync`. |
| **B4** | `POSCartService.ManualTest` | **No compila** (4 errores CS1503/CS8130) | Quedó desactualizada respecto de las firmas actuales. |
| — | `TaxComplianceService.ManualTest` | ✅ pasa | — |

## Fallos y límites del entorno (no son del código)

| ID | Qué | Detalle |
|---|---|---|
| **E1** | `admin-access.test.cjs`, `user-edit.test.cjs` | `Cannot find module 'ts-node/register/transpile-only'`. `ts-node` está en `package.json` pero falta en el `node_modules` local; el CI corre `npm ci`. |
| **E2** | Suite Playwright de navegador | No está instalado el Chromium de Playwright (`%LOCALAPPDATA%\ms-playwright`). Requiere `npx playwright install chromium`. |
| **E3** | Smoke | Kong no responde (`000`; en S2-112 daba 503), así que el smoke cae al acceso directo. El cobro de prueba del smoke al simulador da 400 (aviso que ya existía). |
| **E4** | Electron | En ~5 de 8 corridas cercanas, Electron tardó más de 30 s en abrir la ventana (`setting up "pos"` / `page.goto` timeout) y la prueba falló antes de llegar a la lógica; también le pasó a la suite mock. La corrida final dio 9/9. **Si una regresión falla al abrir Electron, hay que repetirla antes de atribuirla al cambio.** |
| **E5** | 25 pruebas de integración (`PosRepositoryTenantIsolationTests` 13, `PesableEntreServiciosIntegrationTests` 7, `VentaOutboxTransactionTests` 2, `TwoTenantFixtureTests`, `SaleReversedOutboxIntegrationTests`, `OutboxMigrationTests`) | En la corrida completa, el PostgreSQL desechable se detuvo a mitad (`Failed to connect to 127.0.0.1:55457`). Al repetirlas con un contenedor nuevo pasaron las 25. |

## Entorno de la corrida

Docker Desktop había perdido todas las imágenes y volúmenes, así que se recreó desde cero:

- `docker compose up` **sin** `prometheus` ni `grafana`: la imagen `prom/prometheus:v2.53.0` falla siempre en la misma capa al descargarla, y solo sirve para monitoreo.
- El simulador de pagos corre en el host (`node Docker/simulator/server.js`), como respaldo de `run-apis`: la imagen `node:20-alpine` quedó corrupta en la caché de Docker (`exec format error`).
- Los 8 Postgres se recrearon con volúmenes nuevos porque la primera inicialización quedó cortada (sin regla `host all all all` en `pg_hba.conf`).
- MS-1..MS-5 se lanzaron como `run-apis` (`dotnet run`, MS-5 con `PasarelaPago__BaseUrl=http://localhost:5050`).
- Integración: `GLOBALMART_TEST_POSTGRES` apuntando a un `postgres:16-alpine` desechable en `127.0.0.1:55457`, con `--blame-hang-timeout 5m`. La corrida completa tarda unos **32 min**.

## Cómo repetir la línea base

```bash
dotnet build GlobalMartOS.sln
dotnet test tests/GlobalMart.Tests
GLOBALMART_TEST_POSTGRES='Host=127.0.0.1;Port=55457;Database=s3005;Username=postgres;Password=<pass>' dotnet test tests/GlobalMart.IntegrationTests --blame-hang-timeout 5m
cd globalmart-frontend && npm run typecheck && node --test tests/*.test.cjs
npm run test:e2e:electron && npm run test:e2e:integrado   # requiere docker compose + run-apis
```

Una regresión se considera limpia si reproduce estos números: los mismos fallos B2–B4 y E1–E3, y nada nuevo. Con la corrección de B1, integración debería dar **296/296** si el PostgreSQL desechable se mantiene arriba. [NO VERIFICADO] en una corrida completa: solo se repitió la clase corregida.

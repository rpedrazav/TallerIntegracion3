# S3-030 — Regresión tras limpieza de archivos y configuración

**Base:** rama `dev`, commit `e9c36f4`, árbol limpio. **Fecha:** 2026-11-18, Windows 11, .NET SDK 8.0.425, Node 22.19.0.
**Compara contra:** la línea base [S3-005](../S3-005/reporte.md) (commit `9a4056b`).
**Cambios de Rodrigo cubiertos:** S3-024 (`6d07488`, basura de la raíz), S3-025 (`ae2dc41`, scripts temporales del frontend), S3-026/S3-027 (`871cc07`, `config/api.ts` con un solo `API_MODE`, sin `src/config.ts` ni interceptor de `/api`) y S3-028 (`1010327`, verificación remote/local).

**Aceptación:** no queda ningún fallo nuevo sin registrar. ✅ No aparece ningún fallo nuevo atribuible a la limpieza. Lo que falló en la primera corrida fue del entorno (E6–E8) y pasó al repetir. También se registra una observación preexistente (O1) que S3-005 no había anotado.

## Comparación contra la línea base

| # | Comando | Línea base S3-005 | S3-030 | Veredicto |
|---|---|---|---|---|
| 1 | `dotnet build GlobalMartOS.sln` | ✅ 0 errores, 23 advertencias | ✅ 0 errores, 23 advertencias (con `--artifacts-path`, ver E6) | Igual |
| 2 | `dotnet test tests/GlobalMart.Tests` | ✅ 52/52 | ✅ **63/63** | Igual. Las 11 de más vienen de tareas posteriores (S2-096..S2-100) |
| 3 | `dotnet test tests/GlobalMart.IntegrationTests` | ⚠️ 279/296 (B1, E5) | ⛔ no ejecutada | Ver «Fuera de alcance» |
| 4 | Pruebas de consola (`src/*/tests/*`) | ⚠️ 1 de 4 (B2–B4) | ⛔ no ejecutadas | Ver «Fuera de alcance» |
| 5 | `npm run typecheck` | ✅ exit 0 | ✅ exit 0 | Igual |
| 6 | `node --test tests/*.test.cjs` | ⚠️ 48/50 (E1) | ⚠️ **62/64** | Igual: solo fallan los mismos 2 por E1 (`admin-access`, `user-edit`). Entran 14 nuevas, entre ellas las de S3-026/027/028 (`api-config`, `kong-routes-parity`), y todas pasan |
| 7 | `node --test Docker/simulator/reversals.test.js` | ✅ 4/4 | ✅ 4/4 | Igual |
| 8 | `bash scripts/smoke-test.sh` | ✅ exit 0, avisos E3 | ✅ exit 0 (tras levantar MS-4, ver E8) | **Mejor:** Kong responde y el login pasa por `:8000` (lo arregló S3-029). Se mantiene el aviso del cobro de prueba (HTTP 400) |
| 9 | Electron mock + integrado | ✅ 9/9 (2 mock + 7 integrado*) | ✅ **11/11** (3 mock + 8 integrado) tras reintentos E7/E8 | Igual. Entran S3-028 (modo local) y el cierre de turno S3-029 |
| 10 | Navegador `npm run test:e2e` | ⛔ E2 | ⛔ E2 | Igual: sigue sin Chromium de Playwright en esta máquina |

\* S3-005 contó «2 S2-112 + 5 S2-113». Hoy el integrado tiene 8 casos: 2 de `admin-venta`, 1 de `cierre-turno` y 5 de `tarjeta-pesaje`.

Evidencia: [e2e-electron.txt](e2e-electron.txt).

## Qué se miró de la limpieza en particular

- **Referencias colgantes:** `git grep` de los archivos borrados (`fix*.js`, `run_fix.js`, `poblar_bd.js`, `test_insert.js`, `Patch-Envs.ps1`, `generate_sql.ps1`, `insert_productos.sql`, `kubelogin*`, `src/config.ts`) fuera de `docs/qa` y `.gitignore`. Ningún código, script ni configuración los usa; solo aparecen en la prueba que verifica su ausencia y en los CSV de tareas.
- **Configuración del frontend (S3-026/027):** el typecheck pasa, `api-config.test.cjs` y `kong-routes-parity.test.cjs` pasan, y los E2E de Electron cubren login, apertura de turno, venta en efectivo, tarjeta (aprobada, rechazada, timeout), pesaje y cierre por `API_MODE=local` contra Kong. No aparece `/api/api/` ni hay 404 de ruta.

## Fallos de la primera corrida (entorno, no del código)

| ID | Qué | Detalle | Resolución |
|---|---|---|---|
| **E6** | `dotnet build` con `MSB3021/MSB3027` | MS-1, MS-2, MS-3 y MS-5 estaban corriendo con `dotnet run` y bloqueaban sus `.exe` en `bin/Debug`. | Se compiló con `--artifacts-path` en un directorio temporal: 0 errores. |
| **E7** | `pos.spec.ts › login → apertura de turno → POS con sesión IPC persistente` | El primer intento murió con `Fatal process out of memory: Zone` en el worker de Playwright, con unos 2,7 GB libres en el equipo. El segundo superó el timeout de 30 s *después* de abrir el turno (el snapshot muestra «¡Turno de caja abierto exitosamente!»). | 2/2 al repetir (21 s y 3,7 s). Es la misma lentitud intermitente de E4, ahora un paso más adelante. |
| **E8** | `tarjeta-pesaje › tarjeta aprobada` → `cobrar-tarjeta → 502`; smoke → MS-4 `000` | El simulador de pagos (`:5050`) y MS-4 (`:5205`) no estaban levantados en esta sesión. | Se levantaron en el host (`node Docker/simulator/server.js`; MS-4 con `dotnet run`): integrado 8/8 y smoke exit 0. Después se detuvieron. |

## Observación preexistente sin registrar

| ID | Qué | Detalle |
|---|---|---|
| **O1** | `scripts/smoke-test.sh:116` imprime `$ ([ 200 = 200 ] && echo Turno activo …)` literal | Hay un espacio entre `$` y `(`, así que la sustitución de comando no se ejecuta. Es solo cosmético: el código HTTP sí se valida. Viene de `512f2b7` (2026-10-16), antes de la línea base; no lo causó la limpieza. |

## Fuera de alcance de esta corrida

- **Integración .NET (#3) y pruebas de consola (#4):** ningún commit de Rodrigo toca `src/` ni `tests/` de .NET; solo archivos sueltos de la raíz y del frontend. La corrida completa de integración toma unos 32 min, más que el límite de 15 min por corrida que fija el equipo. Los cambios de backend que entraron en paralelo (S2-096..S2-100, nmartinez) tienen su evidencia de integración en `docs/qa/S2-100/`. Que la integración completa siga en 296/296 queda **[NO VERIFICADO]** en esta tarea.
- **Navegador (#10):** sigue bloqueado por E2. S3-027 reporta 9/9 en la máquina de Rodrigo; no se reprodujo aquí.

## Cómo repetir

```bash
dotnet build GlobalMartOS.sln --artifacts-path <tmp>      # si los run-apis están arriba
dotnet test tests/GlobalMart.Tests --artifacts-path <tmp>
cd globalmart-frontend && npm run typecheck && node --test tests/*.test.cjs
node --test ../Docker/simulator/reversals.test.js
# Con docker compose + MS-1..MS-5 (run-apis) + node Docker/simulator/server.js:
npm run test:e2e:electron && npm run test:e2e:integrado
bash ../scripts/smoke-test.sh
```

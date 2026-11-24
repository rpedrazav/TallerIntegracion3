# S3-033 — Defectos de la semana para YouTrack

Fecha: 2026-11-24. Rama: `dev`. Meta: Q — Calidad y regresión. Prioridad MoSCoW de la tarea: Should.

**Aceptación:** cada defecto Must tiene responsable antes del cierre de la semana. ✅ Hay 1 defecto Must (D-01) y tiene responsable propuesto. Los responsables son **propuestos** según las áreas de [sprint-actual](../../context/planificacion/sprint-actual.md); Rodrigo (`admin`) los confirma al importar.

**Fuentes:** reportes QA de la semana ([S3-005](../S3-005/reporte.md), [S3-029](../S3-029/reporte.md), [S3-030](../S3-030/reporte.md), [S3-032](../S3-032/reporte.md), [CI-256](../CI-256/reporte.md)) y los remanentes del [release S2-120](../S2-120/registro-release-sprint2.md). No se inventó ningún defecto: la causa raíz que no está confirmada va como [NO VERIFICADO].

**Importación:** [defectos_youtrack.csv](defectos_youtrack.csv) usa las mismas 9 columnas que `tareas/tareas_sprint3_youtrack.csv`. Los defectos entran en la Semana 12 porque el congelamiento de código es el domingo 29-nov.

## Defectos abiertos

| ID | MoSCoW | Defecto | Responsable propuesto | Estimación |
|---|---|---|---|---:|
| D-01 | **Must** | Login se queda en «Autenticando…» más de 5 s por `window.api.setToken` | Diego | 1h |
| D-02 | Should | Electron tarda más de 30 s en abrir o se queda sin memoria en E2E | Rodrigo | 1h |
| D-03 | Should | Los PostgreSQL del clúster no tienen volumen persistente | Rodrigo | 1h |
| D-04 | Should | El cobro de prueba del smoke devuelve HTTP 400 | Sebastián | 30m |
| D-05 | Should | `demo-mostrador.spec.ts` reescribe capturas versionadas en cada corrida | Daniel | 30m |
| D-06 | Could | `smoke-test.sh:116` imprime la sustitución de comando literal | Daniel | 15m |

## Pasos por defecto

### D-01 (Must) — Login lento en `setToken`

- **Pasos:** levantar `docker compose`, `run-apis` y el simulador; `API_MODE=local`; en `globalmart-frontend` ejecutar `npm run test:e2e:integrado -- admin-venta.integrated.spec.ts`. También se repite iniciando sesión como cajero a mano.
- **Esperado:** el login termina en menos de 5 s.
- **Observado:** Kong responde 200 en menos de 1,3 s, pero la UI sigue en «Autenticando…» más de 5 s y el primer caso de `admin-venta` falla (0/2). La traza ubica el tiempo después de la respuesta, en `window.api.setToken` (IPC → `electron-store`). Una sonda temporal repitió ~5 s con un cajero. Antes pasaba 2/2.
- **Causa raíz:** [NO VERIFICADO]. Evidencia: [S3-029](../S3-029/reporte.md).

### D-02 (Should) — Arranque intermitente de Electron

- **Pasos:** ejecutar `npm run test:e2e:electron` y `npm run test:e2e:integrado` varias veces seguidas en Windows.
- **Esperado:** la ventana abre y la prueba llega a su lógica.
- **Observado:** en ~5 de 8 corridas Electron tardó más de 30 s (`setting up "pos"` / `page.goto` timeout); en S3-030 un worker murió con `Fatal process out of memory: Zone` y otro pasó el timeout después de abrir turno. Pasan al repetir. Puede compartir causa con D-01 [NO VERIFICADO]. Evidencia: [S3-005 E4](../S3-005/reporte.md), [S3-030 E7](../S3-030/reporte.md).

### D-03 (Should) — PostgreSQL del clúster sin PVC

- **Pasos:** revisar `k8s/manifests/all-services.yaml` (no hay `PersistentVolumeClaim`) y reiniciar un pod de PostgreSQL.
- **Esperado:** los datos sobreviven al reinicio.
- **Observado:** el manifiesto no declara volúmenes persistentes; el REM-03 de S2-120 lo reporta. Se perdería la demo del clúster ante un reinicio. El clúster lo ejecuta Rodrigo (regla 5 de `AGENTS.md`); este registro no toca manifiestos.

### D-04 (Should) — Cobro de prueba del smoke da 400

- **Pasos:** con el simulador arriba, `bash scripts/smoke-test.sh`.
- **Esperado:** el cobro de prueba al simulador responde 2xx.
- **Observado:** responde 400 en cada corrida (aviso que ya existía, E3 de S3-005 y S3-030). El script termina en exit 0, así que el fallo queda oculto. Causa [NO VERIFICADO]: puede ser un payload obsoleto del smoke.

### D-05 (Should) — E2E ensucia el repositorio

- **Pasos:** `npx playwright test demo-mostrador` y luego `git status`.
- **Esperado:** el árbol queda limpio.
- **Observado:** reescribe las capturas versionadas de `docs/qa/S2-110/capturas/`. Hubo que restaurarlas con `git checkout` en S3-032 y CI-256; un commit con `git add -A` las mezclaría con otra evidencia.

### D-06 (Could) — Texto roto en el smoke

- **Pasos:** `bash scripts/smoke-test.sh` y mirar la línea del turno activo (`scripts/smoke-test.sh:116`).
- **Esperado:** «Turno activo …».
- **Observado:** imprime `$ ([ 200 = 200 ] && echo Turno activo …)` literal por el espacio entre `$` y `(`. Solo cosmético; el código HTTP sí se valida. Viene de `512f2b7`. Evidencia: S3-030 O1.

## Revisados y no registrados

- **Ya corregidos:** B1 `ReversaTarjetaIntegrationTests` (S3-005, 23/23); B2–B4 pruebas de consola obsoletas (ajustadas en S3-009); Kong local sin consumer ni upstreams (S3-029); `/users/{id}` sobre dos tablas, REM-02 (S3-031, sin regresiones); clave `pos_cart` sin tenant, REM-05 (hoy `pos_cart:${tenant_id}:${sub}` en `Pos.tsx:183`); S2-112 y S2-113 (cuatro defectos corregidos en sus reportes).
- **Entorno local, no del código:** E1 (`ts-node` ausente sin `npm ci`), E2 (sin Chromium de Playwright), E5 (PostgreSQL desechable detenido), E6 (`.exe` bloqueados por `dotnet run`), E8 (servicios sin levantar), imagen `node:20-alpine` con `exec format error` y `prom/prometheus` que no descarga.
- **No son defectos:** REM-04 (secretos de desarrollo aceptados), REM-06 (cobertura aún no medida) y la corrida de integración de 32 min.

## Estado

Los 6 defectos **no están creados en YouTrack**: esta sesión no tiene conexión a YouTrack. Para registrarlos, importar el CSV como en el [paso 2 de la guía](../../../tareas/sprint3_actualizar_youtrack.md) y confirmar responsables. Hasta entonces el criterio «Defectos registrados» está [NO VERIFICADO].

# S2-023 — Aislamiento de venta y producto (RN-01)

Fecha: 2026-10-14. Plan: S2-023 / TI3-654. Prioridad: Must.

## Resultado vigente verificado

**APROBADO: 32/32, 0 fallos, 0 omitidos**, compilación y ejecución Release el 2026-10-14. Distribución: 9 casos HTTP MS-3, 10 HTTP MS-5 y 13 directos de repositorios. Duración total informada: 21,7687 minutos. Evidencia automatizada principal: [S2-023.trx](S2-023.trx), idéntica al [TRX del refuerzo](S2-023-refuerzo.trx) generado por esa ejecución.

Ninguna consulta expuso datos cruzados y ninguna mutación alteró datos ajenos en los escenarios ejecutados. Los controles positivos operaron correctamente con recursos propios. La entrega incluye los cambios productivos de defensa en repositorios y la respuesta 404 uniforme de catálogo descritos a continuación.

## Refuerzo posterior solicitado

Se eliminaron los tres `IgnoreQueryFilters()` de actualización de venta y actualización/eliminación de ítem. Cada operación exige explícitamente un tenant en el contexto y compara el tenant almacenado de la venta; no autoriza mediante datos aportados por el objeto recibido. Los filtros globales permanecen activos. Actualizar un recurso invisible lanza `KeyNotFoundException`; eliminar un ítem invisible conserva el contrato de no efectuar cambios.

`PosRepositoryTenantIsolationTests` añade 12 ataques directos sin pasar por controller/servicio: recurso ajeno o contexto sin tenant, con entidades separadas o ya rastreadas por EF. Compara ventas/ítems/pagos persistidos y añade un control positivo de actualización/eliminación propias. MS-3 devuelve ahora el mismo 404 y cuerpo para PUT ajeno e inexistente; la falta de ADMIN al cambiar estado propio conserva 403. Un nuevo control comprueba desactivación/reactivación ADMIN.

**APROBADO: 32/32, 0 fallos, 0 omitidos**, Release el 2026-10-14 (9 casos HTTP MS-3, 10 HTTP MS-5 y 13 directos de repositorios). Duración total informada: 21,7687 minutos. Evidencia: [S2-023-refuerzo.trx](S2-023-refuerzo.trx). La base desechable de esta corrida se retiró al finalizar.

## Alcance

Suite de integración HTTP con JWT firmados y validación real en MS-3/MS-5, PostgreSQL 16 desechable y esquemas exclusivos de la fixture S2-022. Dos subagentes implementaron catálogo y ventas; el agente principal revisó las aserciones y ejecutó la validación conjunta.

| Servicio | Escenarios |
|---|---|
| MS-3 | GET por ID ajeno, listado, búsqueda compartida/exclusiva y lookup con códigos compartidos/exclusivos. PUT y DELETE ajenos. POST con tenant B falsificado en body/header. |
| MS-5 | GET venta pendiente/completada ajena y ventas de turnos B; agregar, modificar, eliminar ítem y cobrar venta B; modificar/eliminar ítem B usando venta A. |
| MS-5 → MS-3 | Crear venta o agregar ítem con producto B y JWT A, pasando por CatalogClient y API real de catálogo. |
| Controles positivos | Consultas del propietario y operaciones permitidas; crear/agregar/modificar/eliminar/cobrar venta A conservando B. |

Las mutaciones rechazadas comparan persistencia antes/después: todas las columnas de productos B mediante SQL, y ventas/ítems/pagos de ambos tenants mediante consultas sin filtros. MS-5 exige además cero eventos publicados por el registrador Kafka. El control positivo comprueba venta completada, pago y evento propios.

## Reproducción

Requiere .NET 8 y una base PostgreSQL exclusiva con permisos CREATE SCHEMA. La variable es obligatoria para no usar accidentalmente bases personales.

```powershell
docker run --detach --rm --name globalmart-s2023-postgres --env POSTGRES_USER=rn01 --env POSTGRES_PASSWORD=rn01_test_only --env POSTGRES_DB=rn01_tests --publish 127.0.0.1:55423:5432 postgres:16
$env:GLOBALMART_TEST_POSTGRES = 'Host=localhost;Port=55423;Database=rn01_tests;Username=rn01;Password=rn01_test_only'
dotnet test tests/GlobalMart.IntegrationTests/GlobalMart.IntegrationTests.csproj --configuration Release --filter 'Plan=S2-023' --logger 'trx;LogFileName=S2-023.trx' --results-directory docs/qa/S2-023
docker stop globalmart-s2023-postgres
```

## Corrección de infraestructura

La primera corrida detectó que `CopyServiceRuntimeDependencies` copiaba ensamblados Debug sobre una compilación Release. Esto ejecutaba un VentasController antiguo con constructores ambiguos. Se corrigieron las rutas del proyecto de pruebas para usar `$(Configuration)` y se repitió la suite. Esta corrección de infraestructura no modificó código productivo; los cambios productivos posteriores corresponden al refuerzo descrito arriba.

La corrida inicial se interrumpió al confirmar ese problema. Al finalizar quedaron tres esquemas de aquella ejecución interrumpida; se retiró el contenedor completo con `--rm`, eliminando también esos datos temporales. La evidencia TRX corresponde a la corrida final aprobada.

## Límites

Las tablas se crean desde el modelo EF vigente: el migrador se sustituye únicamente en los hosts de prueba. Esta suite no valida migraciones. MS-2 usa cálculo fiscal determinista y Kafka un registrador; no verifica aislamiento de MS-2, entrega al broker, Kong ni E2E de Electron. No se prueba anulación HTTP porque el endpoint no está implementado.

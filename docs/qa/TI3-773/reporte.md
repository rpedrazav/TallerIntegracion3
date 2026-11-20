# TI3-773 — Baja lógica de sucursales

Fecha: 2026-11-20. Meta: desactivar sucursales conservando historial e impedir su uso en nuevos turnos y ventas.

## Objetivo y criterio de aceptación

- MS-1 implementa `DELETE /sucursales/{id}` como baja lógica ADMIN y excluye inactivas de `GET /sucursales`.
- MS-5 debe rechazar la apertura de turnos y la creación de ventas si MS-1 no confirma que la sucursal pertenece al tenant y está activa.
- Estado final: **IMPLEMENTADO Y PROBADO LOCALMENTE**.

## Entorno

- PostgreSQL 16 exclusivo de pruebas: `globalmart-ms1-test-postgres-s3-003-p15433`.
- Estado `healthy`, puerto `127.0.0.1:15433`, base `globalmart_ms1_test` y volumen exclusivo `globalmart_ms1_test_pgdata_s3_003`.
- La conexión se definió solo para los procesos de prueba; no se persistieron credenciales.

## Contrato utilizado

- MS-5 consulta el contrato existente y autorizado `GET /sucursales` de MS-1, propagando el JWT recibido.
- El listado solo contiene sucursales activas del tenant autenticado. Ausente, ajena o inactiva producen el mismo resultado operativo: MS-5 responde 404 sin revelar cuál condición ocurrió.
- Si MS-1 no responde, devuelve un estado no exitoso o una respuesta inválida, MS-5 falla cerrado con 503.
- La misma validación se ejecuta antes de abrir un turno y antes de crear una venta; por ello cubre la desactivación posterior a la apertura del turno.

## Cómo reproducir

```powershell
docker inspect globalmart-ms1-test-postgres-s3-003-p15433 --format '<estado, puertos, entorno y volumen>'
$env:GLOBALMART_TEST_POSTGRES='<POSTGRESQL_EXCLUSIVO_DE_PRUEBAS>'
$env:ConnectionStrings__DefaultConnection=$env:GLOBALMART_TEST_POSTGRES
$env:Logging__EventLog__LogLevel__Default='None'
dotnet build src/POSCartService/POSCartService.csproj --no-restore
dotnet build tests/GlobalMart.IntegrationTests/GlobalMart.IntegrationTests.csproj --no-restore --verbosity minimal
dotnet test tests/GlobalMart.IntegrationTests/GlobalMart.IntegrationTests.csproj --no-build --filter FullyQualifiedName~SucursalOperativaIntegrationTests --logger "console;verbosity=minimal"
dotnet test tests/GlobalMart.IntegrationTests/GlobalMart.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~TurnoIntegrationTests|FullyQualifiedName=GlobalMart.IntegrationTests.VentaIntegrationTests.CrearVenta_AgregarDosProductos_CalculaTotalConIvaCorrectamente" --logger "console;verbosity=minimal"
```

## Resultados reales

- Inspección del contenedor: saludable, aislado en loopback; exit code 0.
- Build MS-5: correcto, 0 errores; exit code 0. Advertencia preexistente `MSB3539`.
- Build de integración: correcto, 0 errores; exit code 0. Advertencias preexistentes `MSB3539`.
- `SucursalOperativaIntegrationTests`: 9/9 aprobadas, 0 fallidas, 0 omitidas; exit code 0.
- Regresión dirigida de turnos y venta: 6/6 aprobadas, 0 fallidas, 0 omitidas; exit code 0.
- Evidencia previa de MS-1 en este ticket: `SucursalIntegrationTests` 13/13 aprobadas sobre el mismo PostgreSQL aislado; exit code 0.

## Escenarios cubiertos

- Turno con sucursal activa: 200.
- Turno con sucursal inactiva, ajena o inexistente: 404 y sin persistencia.
- MS-1 indisponible al abrir turno: 503 y sin persistencia.
- Venta con sucursal activa: 201.
- Sucursal desactivada después de abrir el turno: venta 404 y sin persistencia.
- MS-1 indisponible al crear la venta: 503 y sin persistencia.
- `SucursalClient` usa `/sucursales`, propaga el Bearer y reconoce únicamente la sucursal activa devuelta para ese tenant.
- Los turnos, ventas y relaciones históricas existentes no se eliminan ni se modifican.

## Fixtures y límites

- Las pruebas operativas usan MS-5 real mediante TestServer y PostgreSQL real aislado.
- El estado de MS-1 se controla con una fixture de `ISucursalClient`; catálogo e impuestos también usan fixtures HTTP en el escenario de venta.
- El cliente HTTP productivo hacia MS-1 se prueba de forma dirigida con un handler HTTP, incluida ruta, JWT y respuesta.
- La ejecución conjunta de procesos desplegados MS-1 → MS-5 y el clúster remoto quedan [NO VERIFICADOS].
- El entorno desplegado debe definir `Services__IdentityUrl` con la URL alcanzable de MS-1; no se modificó infraestructura en este ticket.

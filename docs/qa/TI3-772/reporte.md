# Evidencia QA — TI3-772

**Fecha:** 2026-11-20  
**Alcance:** `PUT /sucursales/{id}` en MS-1  
**Resultado:** PASS local

## Entorno

- PostgreSQL 16 exclusivo de pruebas: contenedor `globalmart-ms1-test-postgres-s3-003-p15433`.
- Estado verificado: `healthy`; publicación limitada a `127.0.0.1:15433`.
- Base `globalmart_ms1_test`, usuario exclusivo `globalmart_test` y volumen `globalmart_ms1_test_pgdata_s3_003`.
- `GLOBALMART_TEST_POSTGRES` y `ConnectionStrings__DefaultConnection` se definieron solo para el proceso de pruebas. La contraseña se omite de este reporte.
- `Logging__EventLog__LogLevel__Default=None` se definió solo para la ejecución, sin cambiar configuración persistente.
- Mocks: ninguno. Las pruebas usan HTTP real con `Ms1WebApplicationFactory`, JWT firmado y PostgreSQL real.

## Comandos y resultados

```powershell
docker inspect globalmart-ms1-test-postgres-s3-003-p15433 --format '<estado, puertos, entorno y volumen>'
```

- Exit code: `0`.
- Evidencia: contenedor saludable y puerto enlazado únicamente a loopback.

```powershell
dotnet build src/TenantIdentityService/TenantIdentityService.csproj --no-restore
```

- Exit code: `0`.
- Resultado: compilación correcta, 0 errores y 1 advertencia preexistente `MSB3539`.

```powershell
dotnet build tests/GlobalMart.IntegrationTests/GlobalMart.IntegrationTests.csproj --no-restore --verbosity minimal
```

- Exit code: `0`.
- Resultado: compilación correcta, 0 errores; advertencias preexistentes de MSBuild/xUnit en otros proyectos.

```powershell
$env:GLOBALMART_TEST_POSTGRES='<POSTGRESQL_EXCLUSIVO_DE_PRUEBAS>'
$env:ConnectionStrings__DefaultConnection=$env:GLOBALMART_TEST_POSTGRES
$env:Logging__EventLog__LogLevel__Default='None'
dotnet test tests/GlobalMart.IntegrationTests/GlobalMart.IntegrationTests.csproj --no-build --filter FullyQualifiedName~SucursalIntegrationTests --logger "console;verbosity=minimal"
```

- Exit code: `0`.
- Resultado: `8 passed / 0 failed / 0 skipped`.

## Escenarios verificados

| Escenario | Resultado |
|---|---|
| ADMIN actualiza nombre y zona IANA; GET refleja persistencia | PASS |
| CAJERO intenta editar | PASS — HTTP 403 y datos intactos |
| ADMIN intenta editar sucursal de otro tenant | PASS — HTTP 404 y fila ajena intacta |
| Nombre duplicado en el mismo tenant, sin distinguir mayúsculas | PASS — HTTP 409 |
| La sucursal conserva su propio nombre | PASS — HTTP 200 |
| El mismo nombre existe en otro tenant | PASS — HTTP 200 |
| Zona horaria no IANA | PASS — HTTP 400 |
| Regresión de `POST /sucursales` y `GET /sucursales` | PASS |

## Limitaciones

- No se verificó despliegue remoto ni Kubernetes.
- `DELETE /sucursales/{id}` no existe y está fuera del alcance de TI3-772.
- No se ejecutó la solución completa; se validaron MS-1 y la clase de integración directamente relacionada.

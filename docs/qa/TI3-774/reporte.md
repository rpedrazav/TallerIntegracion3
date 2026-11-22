# TI3-774 — Probar edición y baja de sucursales

Fecha: 2026-11-22. Meta: completar la evidencia de integración de `PUT` y `DELETE /sucursales/{id}` en MS-1.

## Objetivo y criterio de aceptación

- Objetivo: cubrir mediante integración HTTP los resultados `200/204/400/404/409` de edición y baja lógica.
- Aceptación: demostrar edición, validación, aislamiento por tenant, conflicto de nombre, preservación histórica y exclusión de inactivas del listado operativo.

## Matriz de cobertura

| Operación | HTTP | Prueba asociada | Resultado |
|---|---:|---|---|
| PUT | 200 | `UpdateSucursal_AdminActualizaNombreYZonaHoraria_Retorna200` | Aprobada |
| PUT | 400 | `UpdateSucursal_ZonaHorariaInvalida_Retorna400` | Aprobada |
| PUT | 404 | `UpdateSucursal_DeOtroTenant_Retorna404YNoRevelaExistencia` | Aprobada |
| PUT | 409 | `UpdateSucursal_NombreDuplicadoSinDistinguirMayusculas_Retorna409` | Aprobada |
| DELETE | 204 | `DeactivateSucursal_AdminConservaFilaYRelacionHistorica_Retorna204` | Aprobada |
| DELETE | 404 | `DeactivateSucursal_Inexistente_Retorna404` | Aprobada |
| DELETE | 404 | `DeactivateSucursal_DeOtroTenant_Retorna404YConservaActiva` | Aprobada |
| DELETE/listado | 204/200 | `DeactivateSucursal_DesapareceDelListadoActivoSinEliminarFila` | Aprobada |

La inspección inicial confirmó que los casos requeridos ya existían en `SucursalIntegrationTests`; no se agregaron pruebas duplicadas.

## Qué se probó

- ADMIN actualiza nombre y zona horaria y obtiene 200.
- Zona horaria inválida obtiene 400.
- Sucursal ajena obtiene 404 y no se modifica.
- Nombre duplicado dentro del tenant, sin distinguir mayúsculas, obtiene 409.
- ADMIN desactiva y obtiene 204; la fila permanece con `Activa=false` y conserva `UsuarioSucursal`.
- DELETE inexistente o de otro tenant obtiene 404; la sucursal ajena permanece activa.
- La sucursal inactiva desaparece de `GET /sucursales` sin borrado físico.
- Regresiones adicionales de CAJERO 403, nombre propio, coincidencia en otro tenant y creación/listado.

## Cómo reproducir

```powershell
docker run --rm -d --name globalmart-ti3-774-postgres -e POSTGRES_DB=globalmart_ti3_774_test -e POSTGRES_USER=globalmart_test -e POSTGRES_PASSWORD=<PASSWORD_DE_TEST> -p 127.0.0.1:15434:5432 postgres:16-alpine
dotnet build src/TenantIdentityService/TenantIdentityService.csproj --no-restore --verbosity minimal
dotnet build tests/GlobalMart.IntegrationTests/GlobalMart.IntegrationTests.csproj --no-restore --verbosity minimal
$env:GLOBALMART_TEST_POSTGRES='Host=127.0.0.1;Port=15434;Database=globalmart_ti3_774_test;Username=globalmart_test;Password=<PASSWORD_DE_TEST>;Include Error Detail=true'
$env:ConnectionStrings__DefaultConnection=$env:GLOBALMART_TEST_POSTGRES
$env:Logging__EventLog__LogLevel__Default='None'
dotnet test tests/GlobalMart.IntegrationTests/GlobalMart.IntegrationTests.csproj --no-build --filter FullyQualifiedName~SucursalIntegrationTests --logger "console;verbosity=minimal"
docker stop globalmart-ti3-774-postgres
```

## Resultado

- Verificación de rama sin Git: `.git/HEAD` apunta a `refs/heads/local`.
- PostgreSQL 16 efímero: saludable, exclusivo en `127.0.0.1:15434`, sin bases externas, `AutoRemove=true`; comprobación exit code 0.
- Build MS-1: correcto, 0 errores, exit code 0; advertencia preexistente `MSB3539`.
- Build de integración: correcto, 0 errores, exit code 0; 20 advertencias preexistentes, ninguna del alcance TI3-774.
- `SucursalIntegrationTests`: 13/13 aprobadas, 0 fallidas, 0 omitidas, exit code 0.
- Limpieza verificada: el contenedor quedó ausente y Docker informó `no such volume` para su volumen anónimo.

## Límites

- Se ejecutó MS-1 con TestServer y PostgreSQL 16 real, local y efímero; no se probó despliegue remoto ni Kubernetes.
- No se modificó código productivo, fixtures ni pruebas porque la matriz requerida ya estaba completa.
- No se generaron archivos `.trx`; el resumen verificable fue emitido por VSTest en consola.

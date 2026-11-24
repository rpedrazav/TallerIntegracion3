# S3-019 — Unificar repositorios de usuarios en MS-1

Fecha: 2026-11-20. Meta: M1 — Microservicios (FEFO y MS-1).

## Objetivo y criterio de aceptación

- Objetivo: dejar un único repositorio de usuarios y migrar las llamadas del repositorio eliminado.
- Aceptación: las pruebas de MS-1 pasan, no quedan referencias al repositorio eliminado y el flujo queda sin código muerto ni duplicado.

## Qué se verificó

- Solo existen `IUsuarioRepository` y `UsuarioRepository`; `IUserRepository` y `UserRepository` están eliminados.
- No hay referencias antiguas en `src/`, registros de inyección de dependencias ni `tests/`.
- `AuthService` depende de `IUsuarioRepository` y `FindByEmailAsync` conserva tenant explícito, `Activo=true` y carga de roles y sucursales.
- Los métodos del contrato conservado tienen consumidores reales; los métodos sin consumidores detectados en S3-003 ya no existen.
- Flujo: `identityApi.login` → `POST /auth/login` → `AuthController` → `AuthService` → `UsuarioRepository` → `TenantDbContext.Usuarios`.
- Autenticación, usuarios, roles y aislamiento entre dos tenants contra PostgreSQL real en una instancia exclusiva de pruebas.

## Cómo reproducir

```powershell
dotnet build src/TenantIdentityService/TenantIdentityService.csproj --no-restore
dotnet test tests/GlobalMart.Tests/GlobalMart.Tests.csproj --no-restore --filter "FullyQualifiedName~AuthController_LoginTests|FullyQualifiedName~AuthService_RoleTests"

# Instancia utilizada: globalmart-ms1-test-postgres-s3-003-p15433,
# 127.0.0.1:15433, base globalmart_ms1_test y credencial efímera de testing.
$testConnection = 'Host=127.0.0.1;Port=15433;Database=globalmart_ms1_test;Username=globalmart_test;Password=<TEST_PASSWORD>'
$env:GLOBALMART_TEST_POSTGRES = $testConnection
$env:ConnectionStrings__DefaultConnection = $testConnection
$env:Logging__EventLog__LogLevel__Default = 'None'
dotnet test tests/GlobalMart.IntegrationTests/GlobalMart.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~UsuarioIntegrationTests|FullyQualifiedName~UserIntegrationTests|FullyQualifiedName~ActualizarUsuarioIntegrationTests|FullyQualifiedName~DesactivarUsuarioIntegrationTests|FullyQualifiedName~AdminCustomRolesIntegrationTests|FullyQualifiedName~TwoTenantFixtureTests" --logger "console;verbosity=minimal"
```

## Resultado

- Build de `TenantIdentityService`: correcto, 0 errores, exit code 0.
- `AuthController_LoginTests` y `AuthService_RoleTests`: 13/13 aprobadas, 0 omitidas, exit code 0.
- Primera ejecución de integración: 2/42 aprobadas y 40 fallidas, exit code 1. El host fue bloqueado por EventLog (`Cannot open log for source '.NET Runtime'`), no por PostgreSQL ni por aserciones del repositorio.
- Segunda ejecución de la misma selección con EventLog deshabilitado solo en el proceso: 42/42 aprobadas, 0 omitidas, exit code 0, duración 12 s.
- PostgreSQL: contenedor `globalmart-ms1-test-postgres-s3-003-p15433` healthy, `127.0.0.1:15433`, volumen exclusivo `globalmart_ms1_test_pgdata_s3_003`; antes de probar tenía 0 tablas públicas. Las fixtures dejaron 0 esquemas temporales `s2022_*`/`admin_users_*`.
- Artefactos: no se generaron `.trx`, capturas ni JSON.

## Límites

- PostgreSQL fue real y aislado; ninguna prueba apuntó a `tenant_db`, volúmenes `docker_pgdata-*`, desarrollo o producción.
- `GLOBALMART_TEST_POSTGRES`, `ConnectionStrings__DefaultConnection` y la desactivación de EventLog existieron únicamente en el proceso de pruebas y no quedaron persistidas.
- EventLog sigue requiriendo el override efímero al ejecutar estas integraciones sin privilegios de escritura; no se modificó código ni configuración persistente.
- El primer intento de contenedor quedó detenido en estado `Created` con el nombre `globalmart-ms1-test-postgres-s3-003` porque Windows tenía reservado el puerto 55433; no contiene un proceso PostgreSQL en ejecución y no se eliminó por la restricción de no borrar contenedores.
- El build emitió la advertencia preexistente `MSB3539`; el proyecto de integración mostró advertencias de analizadores ajenas a esta tarea.

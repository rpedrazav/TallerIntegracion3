# TI3-771 — Ajustar AuthService al repositorio unificado

Fecha: 2026-11-20. Área: MS-1 Tenant & Identity.

## Objetivo y criterio de aceptación

- Objetivo: verificar que `AuthService` use el repositorio unificado sin alterar el contrato de `POST /auth/login`.
- Aceptación: login conserva respuestas y JWT, utiliza `IUsuarioRepository`, no mantiene duplicaciones dentro del alcance y pasan las pruebas pertinentes.

## Qué se verificó

- `AuthService` depende exclusivamente de `IUsuarioRepository`; no existen referencias de código, DI o pruebas a `IUserRepository/UserRepository`.
- `UsuarioRepository.FindByEmailAsync` filtra por tenant y `Activo=true`, y carga roles y sucursales.
- Flujo sin desvíos: `identityApi.login` → `POST /auth/login` → `AuthController` → `AuthService` → `UsuarioRepository` → `TenantDbContext.Usuarios`.
- Respuestas: `200` con `LoginResponse`, `400` con `ValidationProblemDetails` y `401` genérico para credenciales, usuario, tenant o estado inválidos.
- JWT: el código conserva `sub`, `jti`, `iat`, `tenant_id`, email, nombre, roles JSON, claims individuales de rol, `active_role`, `sucursal_id`, issuer, audience y expiración configurable; `expiresAt` usa la misma cantidad de horas.
- Las pruebas específicas afirman el `400` sin invocar autenticación/JWT, el `401` de usuario inactivo sin revelar su estado, el contrato completo del `200`, los claims de identidad/tenant/roles y la expiración de ocho horas.
- `AuthService` conserva las colecciones de roles y sucursales cargadas y llama exactamente una vez a `IUsuarioRepository.FindByEmailAsync` con email y tenant.
- No se encontró código muerto o duplicación del repositorio dentro de los archivos permitidos; no se modificó código productivo.

## Cómo reproducir

```powershell
dotnet build src/TenantIdentityService/TenantIdentityService.csproj --no-restore
dotnet test tests/GlobalMart.Tests/GlobalMart.Tests.csproj --no-restore --filter "FullyQualifiedName~AuthController_LoginTests|FullyQualifiedName~AuthService_RoleTests" --logger "console;verbosity=minimal"

$testConnection = 'Host=127.0.0.1;Port=15433;Database=globalmart_ms1_test;Username=globalmart_test;Password=<TEST_PASSWORD>'
$env:GLOBALMART_TEST_POSTGRES = $testConnection
$env:ConnectionStrings__DefaultConnection = $testConnection
$env:Logging__EventLog__LogLevel__Default = 'None'
dotnet test tests/GlobalMart.IntegrationTests/GlobalMart.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~UsuarioIntegrationTests|FullyQualifiedName~UserIntegrationTests|FullyQualifiedName~ActualizarUsuarioIntegrationTests|FullyQualifiedName~DesactivarUsuarioIntegrationTests|FullyQualifiedName~AdminCustomRolesIntegrationTests|FullyQualifiedName~TwoTenantFixtureTests" --logger "console;verbosity=minimal"
```

## Resultado

- Build de `TenantIdentityService`: correcto, 0 errores, exit code 0.
- `AuthController_LoginTests` + `AuthService_RoleTests`: 15/15 aprobadas, 0 omitidas, exit code 0.
- Regresión MS-1 con PostgreSQL real: 42/42 aprobadas, 0 omitidas, exit code 0, duración 15 s.
- La selección unitaria cubrió `200`, `400`, `401`, formato de respuesta, usuario inactivo, no filtración, repositorio unificado, roles/sucursales, claims JWT y expiración. La integración cubrió login real de usuario creado, rechazo tras desactivación, rol personalizado/base, usuarios y aislamiento multi-tenant.
- PostgreSQL: `globalmart-ms1-test-postgres-s3-003-p15433`, healthy en `127.0.0.1:15433`, base y volumen exclusivos de pruebas; quedaron 0 esquemas temporales de fixtures.
- Artefactos: no se generaron `.trx`, capturas ni JSON.

## Límites

- Los servicios externos no participan en `/auth/login`; PostgreSQL fue real y exclusivo de pruebas, mientras dependencias internas del controlador se aislaron con Moq en las pruebas unitarias.
- No quedan escenarios del contrato solicitado marcados como `[NO VERIFICADO]`.
- EventLog se deshabilitó solo en el proceso de integración; las variables de prueba no quedaron persistidas.
- Persisten advertencias preexistentes `MSB3539`, XML de MS-3 y analizadores xUnit ajenas a TI3-771.

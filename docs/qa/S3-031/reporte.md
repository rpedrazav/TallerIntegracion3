# S3-031 — Regresión de MS-1 tras unificar repositorios

Fecha: 2026-11-20. Meta: Q Calidad y regresión. Commit probado: `b23cfd3` (rama `dev`, incluye S3-003, TI3-770..773).

## Objetivo y criterio de aceptación

- Objetivo: verificar login, usuarios, roles y sucursales tras la unificación de `IUserRepository`/`UserRepository` en `IUsuarioRepository`/`UsuarioRepository` (Martín).
- Aceptación: login y administración de usuarios funcionan igual que antes.

## Qué se probó

Automatizado:
- Unitarias MS-1 (`AuthController_LoginTests`, `AuthService_RoleTests`): contrato 200/400/401, usuario inactivo, claims JWT, expiración y uso del repositorio unificado.
- Integración MS-1 con PostgreSQL real: login, CRUD de usuarios, desactivación, roles base y personalizados, sucursales (POST/PUT/DELETE lógico), configuración del tenant, validador de creación y aislamiento entre dos tenants.

Manual, por HTTP a través de Kong (`:8000`, el mismo camino que usa el frontend en `API_MODE=local`), con el seed de desarrollo:

| # | Escenario | Esperado | Obtenido |
|---|---|---|---|
| 1 | Login `admin@demo.cl` | 200; `tenant_id` del seed, `active_role=ADMIN` | 200 ✔ |
| 2 | Login `cajero@demo.cl` | 200; `active_role=CAJERO` | 200 ✔ |
| 3 | Contraseña incorrecta | 401 genérico | 401 «Credenciales incorrectas.» ✔ |
| 4 | Email inexistente | 401 con el mismo mensaje (no revela existencia) | 401 ✔ |
| 5 | Body inválido (email vacío) | 400 | 400 ✔ |
| 6 | `GET /users` como admin | 200 | 200 (3 usuarios) ✔ |
| 7 | `GET /users/roles` como admin | 200 | 200 (`ADMIN`, `CAJERO`, `REPONEDOR`) ✔ |
| 8 | `GET /sucursales` como admin | 200 | 200 (0 sucursales en el seed) ✔ |
| 9 | `GET /users` como cajero | 403 | 403 ✔ |
| 10 | `GET /users` sin token | 401 | 401 (Kong) ✔ |
| 11 | `POST /users` rol `CAJERO` | 201, tenant tomado del JWT | 201, `tenantId` del admin ✔ |
| 12 | Login del usuario nuevo | 200, `active_role=CAJERO` | 200 ✔ |
| 13 | `POST /users` email duplicado | 409 | 409 ✔ |
| 14 | `PUT /users/{id}` | 200 | 200 ✔ |
| 15 | `DELETE /users/{id}` (desactivar) | 204 | 204 ✔ |
| 16 | Login del usuario desactivado | 401 genérico | 401 ✔ |

En los tres logins exitosos la respuesta conserva los campos `token`, `usuario` y `expiresAt`, y el JWT trae `iss=GlobalMartOS` y `aud=GlobalMartOS_Clients`. Salida completa: `login-manual.txt`.

## Cómo reproducir

```bash
# PostgreSQL desechable solo para pruebas
docker run -d --name globalmart-ms1-test-s3-031 -e POSTGRES_USER=globalmart_test \
  -e POSTGRES_PASSWORD=<TEST_PASSWORD> -e POSTGRES_DB=globalmart_ms1_test \
  -p 127.0.0.1:15433:5432 postgres:16-alpine

dotnet test tests/GlobalMart.Tests/GlobalMart.Tests.csproj --filter "FullyQualifiedName~MS1_TenantIdentity"

export GLOBALMART_TEST_POSTGRES='Host=127.0.0.1;Port=15433;Database=globalmart_ms1_test;Username=globalmart_test;Password=<TEST_PASSWORD>'
export ConnectionStrings__DefaultConnection="$GLOBALMART_TEST_POSTGRES"
dotnet test tests/GlobalMart.IntegrationTests/GlobalMart.IntegrationTests.csproj --filter "FullyQualifiedName~AuthIntegrationTests|FullyQualifiedName~UsuarioIntegrationTests|FullyQualifiedName~UserIntegrationTests|FullyQualifiedName~ActualizarUsuarioIntegrationTests|FullyQualifiedName~DesactivarUsuarioIntegrationTests|FullyQualifiedName~AdminCustomRolesIntegrationTests|FullyQualifiedName~TwoTenantFixtureTests|FullyQualifiedName~SucursalIntegrationTests|FullyQualifiedName~CrearUsuarioDtoValidatorTests|FullyQualifiedName~TenantConfigIntegrationTests"

# Prueba manual: infraestructura + MS-1 en el host, luego el script
docker compose -f Docker/docker-compose.yml up -d
dotnet run --project src/TenantIdentityService --launch-profile http
bash docs/qa/S3-031/login-manual.sh
```

## Resultado

- Unitarias MS-1: **15/15 aprobadas**, exit code 0 (`ms1-unit.trx`).
- Integración MS-1 con PostgreSQL real: **79/79 aprobadas**, 0 omitidas, exit code 0, 10 s (`ms1-integration.trx`).
- Manual vía Kong: **16/16 escenarios con el resultado esperado** (`login-manual.txt`, `login-manual.sh`).
- No se encontraron regresiones. Se cumple el criterio de aceptación: login y administración de usuarios responden con los mismos códigos, contratos y reglas (RN-01, RN-07) que cubren las pruebas previas.

## Límites

- La prueba manual se hizo por HTTP vía Kong, no con clics en la UI de Electron. La pantalla de login y la de Administración de usuarios quedan **[NO VERIFICADO]** en esta tarea.
- «Igual que antes» se apoya en las pruebas existentes y en los contratos que documentan TI3-771/772/773; no hay una captura de respuestas anterior al cambio con la cual comparar byte a byte.
- El seed local no tiene sucursales, así que `sucursal_id` va vacío en el JWT y el ciclo de sucursales solo se cubrió con las pruebas de integración.
- La prueba manual dejó un usuario de prueba desactivado (`s3031.<timestamp>@demo.cl`) en la base de desarrollo local `postgres-tenant`; no se usó el clúster.
- MS-2..MS-5 y Kafka no participan; tampoco se probó el clúster.

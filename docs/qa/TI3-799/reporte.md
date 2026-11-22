# TI3-799 — Reactivación de usuarios

Fecha: 2026-11-22. Meta: permitir que un ADMIN activo reactive usuarios de su tenant sin alterar sus datos ni relaciones.

## Objetivo y criterio de aceptación

- Objetivo: implementar `POST /users/{id}/reactivate` con aislamiento por tenant y comprobación del ADMIN solicitante contra MS-1.
- Aceptación: el usuario recupera `Activo=true`, conserva credenciales, roles, sucursal e historial y puede iniciar sesión nuevamente.

## Qué se probó

- ADMIN activo reactiva un usuario de su tenant y obtiene 204.
- Usuario ya activo admite reactivación repetida con 204.
- Usuario inexistente o de otro tenant obtiene 404 sin cambios.
- CAJERO obtiene 403 y una solicitud sin JWT obtiene 401.
- Un ADMIN desactivado, aunque conserve un JWT vigente con rol ADMIN, obtiene 403.
- La reactivación conserva email, hash de contraseña, fechas, sucursal y roles.
- Ciclo HTTP real: login 200 → DELETE 204 → login 401 → reactivación 204 → login 200.
- Regresión dirigida de desactivación, usuarios y autenticación.

## Cómo reproducir

```powershell
docker run --rm -d --name globalmart-ti3-799-postgres -e POSTGRES_DB=globalmart_ti3_799_test -e POSTGRES_USER=globalmart_test -e POSTGRES_PASSWORD=<PASSWORD_DE_TEST> -p 127.0.0.1:15435:5432 postgres:16-alpine
dotnet build src/TenantIdentityService/TenantIdentityService.csproj --no-restore --verbosity minimal
dotnet build tests/GlobalMart.IntegrationTests/GlobalMart.IntegrationTests.csproj --no-restore --verbosity minimal
$env:GLOBALMART_TEST_POSTGRES='Host=127.0.0.1;Port=15435;Database=globalmart_ti3_799_test;Username=globalmart_test;Password=<PASSWORD_DE_TEST>;Include Error Detail=true'
$env:ConnectionStrings__DefaultConnection=$env:GLOBALMART_TEST_POSTGRES
$env:Logging__EventLog__LogLevel__Default='None'
dotnet test tests/GlobalMart.IntegrationTests/GlobalMart.IntegrationTests.csproj --no-build --filter FullyQualifiedName~ReactivarUsuarioIntegrationTests --logger "console;verbosity=minimal"
dotnet test tests/GlobalMart.IntegrationTests/GlobalMart.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~DesactivarUsuarioIntegrationTests|FullyQualifiedName~UsuarioIntegrationTests|FullyQualifiedName~AuthIntegrationTests" --logger "console;verbosity=minimal"
docker stop globalmart-ti3-799-postgres
```

## Resultado

- Rama verificada sin Git: `.git/HEAD` apunta a `refs/heads/dev`.
- PostgreSQL 16 efímero: saludable, exclusivo en `127.0.0.1:15435`, `AutoRemove=true`; comprobación exit code 0.
- Build MS-1: correcto, 0 errores, exit code 0; advertencia preexistente `MSB3539`.
- Build de integración: correcto, 0 errores, exit code 0; advertencias preexistentes fuera de TI3-799.
- `ReactivarUsuarioIntegrationTests`: 8/8 aprobadas, 0 fallidas, 0 omitidas, exit code 0.
- Regresión de desactivación, usuarios y autenticación: 33/33 aprobadas, 0 fallidas, 0 omitidas, exit code 0.
- Limpieza verificada: el contenedor quedó ausente y Docker informó `no such volume` para su volumen anónimo.

## Límites

- Las pruebas usan MS-1 mediante TestServer y PostgreSQL 16 real, local y efímero.
- Frontend, despliegue remoto y Kubernetes no fueron modificados ni verificados.
- La revocación general de JWT permanece fuera de alcance; el endpoint nuevo consulta el estado actual del ADMIN antes de escribir.
- No se generaron archivos `.trx`; VSTest emitió resumen final y exit code 0 en ambas corridas.

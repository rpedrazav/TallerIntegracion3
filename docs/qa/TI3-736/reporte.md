# S2-105 / TI3-736 — Regresión administrativa

Fecha: 2026-11-19. Meta: cerrar la regresión administrativa y exigir ADMIN en POST/PUT de productos de MS-3.

## Objetivo y criterio de aceptación
- Objetivo: comprobar altas, edición, baja y configuración para ADMIN/CAJERO y dos tenants, corrigiendo la autorización de escritura de productos.
- Aceptación: sin errores TypeScript ni llamadas a endpoints inexistentes.

## Qué se probó
- `POST /products`: prueba preparada para comprobar CAJERO 403 sin escritura, ADMIN 201 y persistencia exclusiva en el tenant del JWT.
- `PUT /products/{id}`: prueba actualizada para comprobar CAJERO 403 también en edición ordinaria, ADMIN 200, persistencia propia y tenant ajeno intacto.
- Guarda frontend: ADMIN permitido; CAJERO activo, incluso con ADMIN asignado, bloqueado en `/admin`, `/admin/usuarios` y `/admin/productos`.
- Regresiones frontend de alta, edición y baja de usuarios, roles de inactivos, configuración de tenant, CRUD/filtros/errores de productos y contratos de URL.
- Contratos inspeccionados: todas las llamadas administrativas corresponden a rutas existentes de MS-1/MS-3; no se detectaron endpoints inexistentes.

## Cómo reproducir
```powershell
cd globalmart-frontend
npm.cmd run typecheck
node --test tests/admin-access.test.cjs tests/user-edit.test.cjs tests/user-deactivation.test.cjs tests/tenant-config-schema.test.cjs tests/api-config.test.cjs tests/kong-routes-parity.test.cjs
npm.cmd run test:e2e -- e2e/admin.spec.ts e2e/admin-flows.spec.ts e2e/productos-admin.spec.ts
cd ..
dotnet build src/CatalogPricingService/CatalogPricingService.csproj --no-restore
dotnet test tests/GlobalMart.IntegrationTests/GlobalMart.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~ProductoTenantIsolationIntegrationTests|FullyQualifiedName~DesactivarProductoIntegrationTests" --logger "console;verbosity=minimal"
dotnet test tests/GlobalMart.IntegrationTests/GlobalMart.IntegrationTests.csproj --no-build --no-restore --filter "FullyQualifiedName=GlobalMart.IntegrationTests.UsuarioIntegrationTests.GetUsers_ConRolCajero_Retorna403Forbidden|FullyQualifiedName=GlobalMart.IntegrationTests.ActualizarUsuarioIntegrationTests.Put_UsuarioDelMismoTenant_Returns200YPersisteSoloDatosBasicos|FullyQualifiedName=GlobalMart.IntegrationTests.DesactivarUsuarioIntegrationTests.Delete_Cajero_Returns403SinCambios|FullyQualifiedName=GlobalMart.IntegrationTests.TenantConfigIntegrationTests.UpdateConfig_ConTenantDiferente_Retorna403" --logger "console;verbosity=minimal"
```

## Resultado
- TypeScript: aprobado, exit code 0.
- Build de `CatalogPricingService`: aprobado con 0 errores y 1 advertencia MSBuild preexistente, exit code 0.
- Pruebas Node de acceso, usuarios, configuración y URLs: 20/20 aprobadas, exit code 0.
- Regresión Playwright administrativa combinada: 34/34 aprobadas, resumen final `34 passed (2.7m)`, exit code 0. Incluye 8 casos de acceso Admin, 17 de usuarios/configuración y 9 de productos.
- Integración MS-3 seleccionada: proyecto compilado; 0/22 ejecutadas con resultado funcional, exit code 1. Trece casos no iniciaron porque PostgreSQL rechazó `127.0.0.1:5435`; nueve no iniciaron porque falta `GLOBALMART_TEST_POSTGRES`.
- Regresión MS-1/config seleccionada: 0/4 ejecutadas con resultado funcional, exit code 1; las factories fuerzan `Development` y una advertencia EF intentó escribirse en Windows EventLog sin permisos durante la migración. Deshabilitar ese proveedor solo para el proceso evitaría el error de logging, pero no se repitió porque tampoco existe PostgreSQL en el puerto 5433.
- Artefactos QA persistentes: ninguno; `test-results/` y `playwright-report/` se eliminaron tras la corrida y el puerto 4175 quedó libre.

## Límites
- Los E2E interceptan APIs mediante `page.route()`; no prueban MS-1/MS-3 reales.
- Autorización HTTP y aislamiento entre dos tenants quedan [NO VERIFICADOS] en ejecución local hasta disponer del PostgreSQL de pruebas y `GLOBALMART_TEST_POSTGRES`.
- No hay PostgreSQL escuchando en 5432–5435, servicio PostgreSQL/Docker disponible ni instalación de Docker en este equipo. No se sustituyó por una base de desarrollo o producción.
- `playwright.config.ts` inicia `node e2e/serve.cjs` con `reuseExistingServer: false`. Tras completar los casos, ese proceso hoja permaneció vivo aunque ya no escuchaba; se cerró únicamente ese webServer, tras lo cual Playwright emitió el resumen y exit code 0. No quedaron procesos ni servidores de la corrida.
- No se ejecutó despliegue ni validación remota.

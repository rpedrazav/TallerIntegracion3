# S2-022 — Fixtures de dos tenants y turnos

`TwoTenantFixture` crea tres esquemas únicos por ejecución en PostgreSQL (Identity,
Catalog y POS), a partir de los modelos EF actuales. No ejecuta Program, seeds ni
migraciones y no accede a tablas personales. Requiere una base existente y permisos
CREATE SCHEMA. Puede usarse una base de pruebas vacía; no requiere Kafka ni servicios HTTP.

Cada tenant tiene cajero activo con contraseña bcrypt y rol CAJERO, sucursal asignada,
producto activo de $100, turno abierto con fondo $10000, turno histórico cerrado,
venta pendiente y venta completada con ítems e IVA 19%, y un pago efectivo de $119.
El mismo código de barras en ambos tenants permite probar la unicidad por tenant.
Los IDs y correos son nuevos en cada instancia.

```powershell
$env:GLOBALMART_TEST_POSTGRES = 'Host=localhost;Port=5436;Database=pos_db;Username=admin;Password=admin_password'
dotnet test tests/GlobalMart.IntegrationTests --filter FullyQualifiedName~TwoTenantFixtureTests
```

Uso: crear con `await using var fixture = new TwoTenantFixture(connectionString)`,
llamar `InitializeAsync()`, usar `TenantA`/`TenantB` y abrir contextos con
`IdentityDb(tenant)`, `CatalogDb(tenant)` y `PosDb(tenant)` mediante `await using`.
Todos los contextos fijan CurrentTenantId y conservan los filtros reales del modelo.
`Connection("pos")` (o identity/catalog) permite configurar una WebApplicationFactory
en entorno Testing; sus JWT deben usar CajeroId/TenantId/SucursalId de la fixture.
Los clientes externos y el productor Kafka deben sustituirse en esas pruebas HTTP.

`CleanTenantAsync(tenant)` elimina ventas (con cascada a ítems, pagos y anulaciones),
turnos, precios, productos, usuarios y sucursales, y el tenant. Es idempotente y rechaza
tenants de otra fixture. Los roles globales permanecen hasta DisposeAsync, que elimina
exclusivamente los esquemas propios, también cuando falla la inicialización.
Cerrar clientes/contextos antes de disponer la fixture. Para datos adicionales de
pruebas, usar los IDs del tenant; si se crean categorías jerárquicas, eliminarlas de
hijas a padres antes de la limpieza final. La limpieza entre servicios no es una
transacción distribuida; una falla puede reintentarse.

Esta fixture prepara escenarios y verifica persistencia/filtros EF; no demuestra
por sí sola RN-06 por HTTP ni constituye una prueba E2E de Electron. Usa Usuarios
del modelo de login, no la proyección externa `users` de PUT/DELETE de MS-1.

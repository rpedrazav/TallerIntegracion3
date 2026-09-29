---
id: convenciones-codigo
tipo: proyecto
titulo: Convenciones de Código — GlobalMart OS
estado: implementado
fuentes: [src/, Directory.Build.props, Directory.Build.targets]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: [stack]
publica: []
consume: []
reglas: []
---
# Convenciones de Código — GlobalMart OS

> Patrones observados en el código real. No inventados; derivados del análisis de la base de código.

## Estructura de microservicio (patrón común)

```
{ServiceName}/
├── Controllers/     Controllers API REST
├── Data/            DbContext + Migrations/
├── DTOs/            Data Transfer Objects (request/response)
├── Exceptions/      Excepciones de dominio personalizadas
├── Messaging/       Kafka producers/consumers (IHostedService)
├── Middleware/      TenantMiddleware.cs (en todos los servicios)
├── Models/          Entidades de dominio
├── Repositories/    Interfaces + implementaciones de acceso a datos
├── Services/        Lógica de negocio
├── Validators/      FluentValidation validators
└── Program.cs       Configuración ASP.NET Core
```

## Naming conventions (C#)

- **Controllers:** `{Entidad}Controller.cs` — hereda `ControllerBase`
- **Repositories:** `I{Entidad}Repository.cs` / `{Entidad}Repository.cs`
- **Services:** `I{Nombre}Service.cs` / `{Nombre}Service.cs`
- **DTOs:** `{Accion}{Entidad}Dto.cs` (ej: `CrearVentaRequest`, `VentaResponse`)
- **Validators:** `{Dto}Validator.cs`
- **Routes:** kebab-case o camelCase en plural (ej: `/turnos`, `/ventas`, `/products`)

## Multi-tenant pattern (en todos los servicios)

```csharp
// En cada Controller — extrae tenant_id del JWT
var tenantClaim = User.FindFirst("tenant_id")?.Value;
if (!Guid.TryParse(tenantClaim, out var tenantId))
    return Unauthorized(new { message = "El token no contiene un tenant_id válido." });

// TenantMiddleware lo inyecta automáticamente en DbContext
// NO filtrar manualmente en queries — EF Core lo hace via HasQueryFilter
```

## JWT claims requeridos

```csharp
// Patrón de extracción en MS-5 (POS) — cajero_id puede venir en 3 claims
var cajeroClaim = User.FindFirst("cajero_id")?.Value
    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
    ?? User.FindFirst("sub")?.Value;
```

## Respuestas de error

Patrón consistente de respuestas:
```csharp
return Unauthorized(new { message = "..." });  // 401
return NotFound(new { message = "..." });       // 404
return Conflict(new { error = "..." });         // 409
return StatusCode(503, new { message = "...", error = "..." }); // 503
```

## FluentValidation

Todos los validators se registran automáticamente:
```csharp
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
```

## Program.cs — Orden de middlewares (crítico)

```csharp
app.UseHttpsRedirection();
app.UseCors("ElectronApp");
app.UseAuthentication();   // 1: valida JWT
app.UseAuthorization();    // 2: verifica permisos/roles
app.UseTenantMiddleware(); // 3: extrae tenant_id e inyecta en DbContext
app.MapControllers();
app.MapHealthChecks("/health");
```

## Seed de desarrollo

MS-1 incluye seed en `Development`:
- TenantId fijo: `aaaaaaaa-0000-0000-0000-000000000001`
- Usuario: `cajero@demo.cl` / `demo1234`
- RolId fijo: `11111111-0000-0000-0000-000000000001`

## Migraciones EF Core

- Se aplican automáticamente al iniciar en Development (`db.Database.Migrate()`)
- Naming: `{timestamp}_{DescripcionCamelCase}.cs`
- Última migración (2026-09-27): `AddProductoNombreTrgmIndex` — agrega índice trigram en Catalog

## Kafka consumer pattern (MS-4)

```csharp
// IHostedService con loop de consumo manual
// EnableAutoCommit = false → commit manual después de procesar
// Error en procesamiento → log + commit igualmente (no retry infinito)
// Idempotencia: verificar EventosKafkaProcesados antes de procesar
```

## Conexiones
- Stack: [[stack]]
- Multi-tenant: [[multi-tenant]]
- Arquitectura: [[arquitectura]]

## Fuentes
- `src/POSCartService/Controllers/VentasController.cs`
- `src/TenantIdentityService/Program.cs`
- `src/WarehouseInventoryService/Messaging/KafkaConsumerService.cs`
- `Directory.Build.props`

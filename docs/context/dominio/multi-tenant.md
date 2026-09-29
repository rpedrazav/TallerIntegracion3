---
id: multi-tenant
tipo: dominio
titulo: Diseño Multi-Tenant — GlobalMart OS
estado: implementado
fuentes: [src/TenantIdentityService/Middleware/TenantMiddleware.cs, src/POSCartService/Middleware/TenantMiddleware.cs, GlobalMart_ContextMaster.md#sec14]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: [ms1-identity]
publica: []
consume: []
reglas: [RN-01, RNF-01, RF-14]
---
# Diseño Multi-Tenant — GlobalMart OS

> El aislamiento de tenants es la regla más importante del sistema (RN-01). Ningún query puede devolver datos de otro tenant.

## Patrón implementado: Shared Database, Tenant Discriminator

Todos los microservicios comparten el patrón:
1. Campo `tenant_id` (GUID) en cada tabla
2. JWT con claim `tenant_id` en cada request
3. TenantMiddleware extrae el tenant del JWT e inyecta en DbContext
4. EF Core aplica `HasQueryFilter` automáticamente

## Implementación del TenantMiddleware

```csharp
// Patrón común en todos los servicios (verificado en MS-1, MS-4, MS-5)
public class TenantMiddleware
{
    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        var tenantId = context.User.FindFirst("tenant_id")?.Value;
        if (tenantId == null) { context.Response.StatusCode = 401; return; }
        db.CurrentTenantId = Guid.Parse(tenantId);
        await _next(context);
    }
}

// Registro en Program.cs (siempre DESPUÉS de UseAuthentication y UseAuthorization)
app.UseTenantMiddleware();
```

## Modelo de datos

```sql
-- Ejemplo: tabla de productos en MS-3
CREATE TABLE Productos (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    TenantId UUID NOT NULL,        -- discriminador obligatorio
    Nombre VARCHAR(200) NOT NULL,
    PrecioBase DECIMAL(10,2) NOT NULL,
    -- ...
);

CREATE INDEX IX_Productos_TenantId ON Productos(TenantId);
```

## En Kafka consumer (MS-4)

Cuando el evento viene de Kafka (no de un request HTTP), no hay JWT. El tenant se extrae del payload del evento:

```csharp
// KafkaConsumerService.cs
context.CurrentTenantId = evento.TenantId;  // tenant del evento, no del JWT
```

## Configuración por tenant

Cada tenant puede configurar (en MS-1):
- **País:** determina la entidad fiscal (SII/AFIP/IRS)
- **Moneda base:** CLP, ARS, USD, EUR
- **Idioma:** es, en, pt
- **Zona horaria:** crítica para cuadres de caja y reportes diarios
- **Porcentaje de IVA:** configurable, consultado por MS-2

## JWT y multi-tenant

```json
{
  "sub": "<user_id>",
  "tenant_id": "<tenant_id>",  // ← discriminador
  "roles": ["CAJERO"],
  "active_role": "CAJERO",
  "exp": 1725820800
}
```

Kong valida el JWT antes de llegar al servicio. El TenantMiddleware valida que el `tenant_id` del JWT sea válido para ese tenant.

## Reglas críticas de aislamiento

- **RN-01:** Ningún query puede retornar datos de otro tenant → implementado via EF Core HasQueryFilter
- **RNF-01:** `tenant_id` como discriminador obligatorio en cada tabla
- Un ADMIN de tenant A **no puede** ver ni modificar datos del tenant B
- Un ADMIN de tenant A **no puede** asignar el rol SUPER_ADMIN (solo viola aislamiento horizontal)

## Brechas conocidas

| Brecha | Estado |
|--------|--------|
| Sin gestión de sucursales (TI-11) | sucursal_id es campo libre en JWT |
| `sucursal_id` no validado en MS-1 | NO VERIFICADO si hay constraint |

## Conexiones
- Implementación en servicios: [[ms1-identity]], [[ms2-tax]], [[ms3-catalog]], [[ms4-inventory]], [[ms5-pos]]
- JWT: [[rbac-multirol]]
- Reglas: [[reglas-negocio]] (RN-01)

## Fuentes
- `src/TenantIdentityService/Middleware/TenantMiddleware.cs`
- `src/POSCartService/Middleware/TenantMiddleware.cs`
- `src/WarehouseInventoryService/Middleware/TenantMiddleware.cs`
- `GlobalMart_ContextMaster.md` §14

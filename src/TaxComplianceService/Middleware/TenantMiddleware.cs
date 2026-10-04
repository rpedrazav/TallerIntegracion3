using TaxComplianceService.Data;

namespace TaxComplianceService.Middleware;

/// <summary>
/// Middleware multi-tenant de MS-2.
/// Extrae el <c>tenant_id</c> del JWT y lo inyecta en <see cref="TaxDbContext.CurrentTenantId"/>,
/// de modo que los <c>HasQueryFilter</c> globales aislen las lecturas por tenant (RN-01).
/// Mismo patrÃ³n que el TenantMiddleware de MS-4 (WarehouseInventoryService).
/// </summary>
public sealed class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, TaxDbContext db)
    {
        if (context.Request.Path.StartsWithSegments("/health")) { await _next(context); return; }
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/health") || path.StartsWith("/swagger"))
        {
            await _next(context);
            return;
        }

        var tenantClaim = context.User.FindFirst("tenant_id")?.Value
            ?? context.User.FindFirst("TenantId")?.Value;

        if (string.IsNullOrWhiteSpace(tenantClaim) || !Guid.TryParse(tenantClaim, out var tenantId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Token invÃ¡lido: falta tenant_id" });
            return;
        }

        db.CurrentTenantId = tenantId;
        await _next(context);
    }
}


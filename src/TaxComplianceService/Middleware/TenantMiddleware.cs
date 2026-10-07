using TaxComplianceService.Data;

namespace TaxComplianceService.Middleware;

/// <summary>
/// TI3-460: Middleware multi-tenant de MS-2 (Tax &amp; Compliance Service).
/// Copiado del patrón de MS-1, extrae el <c>tenant_id</c> del JWT y lo inyecta
/// en <see cref="TaxDbContext.CurrentTenantId"/>, de modo que los
/// <c>HasQueryFilter</c> globales aíslen las lecturas por tenant (RN-01).
/// </summary>
public sealed class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, TaxDbContext db)
    {
        // Endpoints públicos que NO requieren tenant_id
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/health") || path.StartsWith("/swagger"))
        {
            await _next(context);
            return;
        }

        // Extraer tenant_id del claim del JWT (soporta ambas convenciones de naming)
        var tenantClaim = context.User.FindFirst("tenant_id")?.Value
            ?? context.User.FindFirst("TenantId")?.Value;

        if (string.IsNullOrWhiteSpace(tenantClaim) || !Guid.TryParse(tenantClaim, out var tenantId))
        {
            _logger.LogWarning("Request rechazado: JWT sin claim tenant_id válido. Path: {Path}", path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Token inválido: falta tenant_id" });
            return;
        }

        // Inyectar el tenant_id en el DbContext → EF Core filtra automáticamente
        db.CurrentTenantId = tenantId;

        _logger.LogDebug("Tenant {TenantId} establecido para request {Path}", tenantId, path);

        await _next(context);
    }
}

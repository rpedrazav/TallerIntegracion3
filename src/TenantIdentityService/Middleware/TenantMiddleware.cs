using TenantIdentityService.Data;

namespace TenantIdentityService.Middleware;

/// <summary>
/// Middleware ASP.NET Core que extrae el tenant_id del JWT
/// y lo inyecta en el TenantDbContext para que EF Core filtre
/// automáticamente todos los queries por el tenant del request actual.
/// 
/// Se ejecuta en CADA request ANTES de llegar al Controller.
/// Implementa la regla de negocio RN-01: aislamiento total entre tenants.
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, TenantDbContext db)
    {
        // Endpoints públicos que NO requieren tenant_id en el JWT (solo login y health)
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/auth/login") ||
            path.StartsWith("/health") ||
            path.StartsWith("/swagger"))
        {
            await _next(context);
            return;
        }

        // Extraer tenant_id del claim del JWT
        var tenantClaim = context.User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(tenantClaim) || !Guid.TryParse(tenantClaim, out var tenantId))
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

/// <summary>
/// Extension method para registrar TenantMiddleware en el pipeline de forma limpia.
/// Uso: app.UseTenantMiddleware();
/// </summary>
public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantMiddleware(this IApplicationBuilder builder)
        => builder.UseMiddleware<TenantMiddleware>();
}

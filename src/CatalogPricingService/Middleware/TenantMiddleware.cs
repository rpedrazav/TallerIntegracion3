using CatalogPricingService.Data;

namespace CatalogPricingService.Middleware;

public sealed class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _environment;

    public TenantMiddleware(RequestDelegate next, IWebHostEnvironment environment)
    {
        _next = next;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context, CatalogDbContext db)
    {
        var tenantValue = context.User.FindFirst("tenant_id")?.Value;

        if (_environment.IsDevelopment() &&
            string.IsNullOrWhiteSpace(tenantValue))
        {
            tenantValue = context.Request.Headers["X-Tenant-ID"].FirstOrDefault();
        }

        if (!Guid.TryParse(tenantValue, out var tenantId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Falta un tenant_id válido en el JWT o en X-Tenant-ID (solo Development)."
            });
            return;
        }

        db.CurrentTenantId = tenantId;
        await _next(context);
    }
}
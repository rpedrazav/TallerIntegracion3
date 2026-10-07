extern alias TaxComplianceServiceAlias;

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using TaxDbContext = TaxComplianceServiceAlias::TaxComplianceService.Data.TaxDbContext;
using TenantMiddleware = TaxComplianceServiceAlias::TaxComplianceService.Middleware.TenantMiddleware;

namespace GlobalMart.IntegrationTests;

/// <summary>
/// TI3-460: Pruebas unitarias para el TenantMiddleware de MS-2 (TaxComplianceService).
/// Valida el aislamiento multi-tenant (RN-01), rutas públicas y extracción de claims.
/// </summary>
public class Ms2TenantMiddlewareTests
{
    private static TaxDbContext CreateTaxDbContext()
    {
        var options = new DbContextOptionsBuilder<TaxDbContext>().Options;
        return new TaxDbContext(options);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/live")]
    [InlineData("/swagger")]
    [InlineData("/swagger/v1/swagger.json")]
    public async Task InvokeAsync_RutasPublicas_OmiteVerificacionYEjecutaNext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;

        using var db = CreateTaxDbContext();
        var logger = NullLogger<TenantMiddleware>.Instance;
        var nextExecuted = false;

        var middleware = new TenantMiddleware(_ =>
        {
            nextExecuted = true;
            return Task.CompletedTask;
        }, logger);

        await middleware.InvokeAsync(context, db);

        Assert.True(nextExecuted);
        Assert.Null(db.CurrentTenantId);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_SinClaimTenantId_Retorna401Unauthorized()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/tax/calculate";
        context.User = new ClaimsPrincipal(new ClaimsIdentity()); // Sin claims

        using var db = CreateTaxDbContext();
        var logger = NullLogger<TenantMiddleware>.Instance;
        var nextExecuted = false;

        var middleware = new TenantMiddleware(_ =>
        {
            nextExecuted = true;
            return Task.CompletedTask;
        }, logger);

        await middleware.InvokeAsync(context, db);

        Assert.False(nextExecuted);
        Assert.Null(db.CurrentTenantId);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ConClaimTenantIdInvalido_Retorna401Unauthorized()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/comprobantes";
        var claims = new[] { new Claim("tenant_id", "not-a-guid") };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        using var db = CreateTaxDbContext();
        var logger = NullLogger<TenantMiddleware>.Instance;
        var nextExecuted = false;

        var middleware = new TenantMiddleware(_ =>
        {
            nextExecuted = true;
            return Task.CompletedTask;
        }, logger);

        await middleware.InvokeAsync(context, db);

        Assert.False(nextExecuted);
        Assert.Null(db.CurrentTenantId);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ConClaimTenantIdValido_InyectaTenantIdYEjecutaNext()
    {
        var expectedTenantId = Guid.NewGuid();
        var context = new DefaultHttpContext();
        context.Request.Path = "/tax/calculate";
        var claims = new[] { new Claim("tenant_id", expectedTenantId.ToString()) };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        using var db = CreateTaxDbContext();
        var logger = NullLogger<TenantMiddleware>.Instance;
        var nextExecuted = false;

        var middleware = new TenantMiddleware(_ =>
        {
            nextExecuted = true;
            return Task.CompletedTask;
        }, logger);

        await middleware.InvokeAsync(context, db);

        Assert.True(nextExecuted);
        Assert.Equal(expectedTenantId, db.CurrentTenantId);
    }

    [Fact]
    public async Task InvokeAsync_ConClaimPascalCaseTenantId_InyectaTenantIdYEjecutaNext()
    {
        var expectedTenantId = Guid.NewGuid();
        var context = new DefaultHttpContext();
        context.Request.Path = "/comprobantes";
        var claims = new[] { new Claim("TenantId", expectedTenantId.ToString()) };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        using var db = CreateTaxDbContext();
        var logger = NullLogger<TenantMiddleware>.Instance;
        var nextExecuted = false;

        var middleware = new TenantMiddleware(_ =>
        {
            nextExecuted = true;
            return Task.CompletedTask;
        }, logger);

        await middleware.InvokeAsync(context, db);

        Assert.True(nextExecuted);
        Assert.Equal(expectedTenantId, db.CurrentTenantId);
    }
}

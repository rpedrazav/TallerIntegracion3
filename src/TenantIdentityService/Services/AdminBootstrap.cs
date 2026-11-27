using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TenantIdentityService.Data;
using TenantIdentityService.DTOs;
using TenantIdentityService.Repositories;

namespace TenantIdentityService.Services;

/// <summary>
/// Alta de un minimarket por consola (--bootstrap-admin), con la misma validación y el mismo
/// método que POST /api/tenants. Sirve para el primer tenant de un entorno sin seed.
/// </summary>
public static class AdminBootstrap
{
    public static async Task RunAsync(IServiceProvider services)
    {
        if (!Guid.TryParse(Required("GLOBALMART_BOOTSTRAP_TENANT_ID"), out var tenantId) || tenantId == Guid.Empty)
            throw new InvalidOperationException("GLOBALMART_BOOTSTRAP_TENANT_ID debe ser un UUID válido.");
        var request = new CrearTenantRequest
        {
            Nombre = Required("GLOBALMART_BOOTSTRAP_TENANT_NAME"),
            Codigo = Required("GLOBALMART_BOOTSTRAP_TENANT_CODE"),
            AdminEmail = Required("GLOBALMART_BOOTSTRAP_ADMIN_EMAIL"),
            AdminPassword = Required("GLOBALMART_BOOTSTRAP_ADMIN_PASSWORD"),
        };

        using var scope = services.CreateScope();
        var validacion = await scope.ServiceProvider.GetRequiredService<IValidator<CrearTenantRequest>>().ValidateAsync(request);
        if (!validacion.IsValid)
            throw new InvalidOperationException(string.Join(" ", validacion.Errors.Select(e => e.ErrorMessage)));

        await scope.ServiceProvider.GetRequiredService<TenantDbContext>().Database.MigrateAsync();
        var tenant = await scope.ServiceProvider.GetRequiredService<ITenantRepository>().CreateWithAdminAsync(request, tenantId);
        Console.WriteLine($"Minimarket {tenant.Codigo} ({tenant.Id}) creado con ADMIN {request.AdminEmail}");
    }

    private static string Required(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value : throw new InvalidOperationException($"Falta {name}.");
}

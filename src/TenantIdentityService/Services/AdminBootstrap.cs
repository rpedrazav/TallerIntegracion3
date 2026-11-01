using Microsoft.EntityFrameworkCore;
using TenantIdentityService.Data;
using TenantIdentityService.Models;

namespace TenantIdentityService.Services;

/// <summary>Provisionamiento explícito por consola; nunca expuesto como endpoint público.</summary>
public static class AdminBootstrap
{
    public static async Task RunAsync(IServiceProvider services)
    {
        var tenantText = Required("GLOBALMART_BOOTSTRAP_TENANT_ID");
        var tenantName = Required("GLOBALMART_BOOTSTRAP_TENANT_NAME");
        var email = Required("GLOBALMART_BOOTSTRAP_ADMIN_EMAIL").Trim().ToLowerInvariant();
        var password = Required("GLOBALMART_BOOTSTRAP_ADMIN_PASSWORD");
        if (!Guid.TryParse(tenantText, out var tenantId) || tenantId == Guid.Empty)
            throw new InvalidOperationException("GLOBALMART_BOOTSTRAP_TENANT_ID debe ser un UUID válido.");
        if (password.Length < 8 || !password.Any(char.IsUpper) || !password.Any(char.IsLower)
            || !password.Any(char.IsDigit) || !password.Any(c => !char.IsLetterOrDigit(c)))
            throw new InvalidOperationException("La contraseña inicial debe tener 8 caracteres, mayúscula, minúscula, número y símbolo.");
        if (!System.Net.Mail.MailAddress.TryCreate(email, out _))
            throw new InvalidOperationException("GLOBALMART_BOOTSTRAP_ADMIN_EMAIL no es válido.");

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        await db.Database.MigrateAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant is null)
        {
            tenant = new Tenant
            {
                Id = tenantId, Nombre = tenantName, Pais = "CL", Moneda = "CLP",
                Idioma = "es", ZonaHoraria = "America/Santiago", PorcentajeIva = 19,
            };
            db.Tenants.Add(tenant);
        }

        if (await db.Usuarios.IgnoreQueryFilters().AnyAsync(u => u.TenantId == tenantId && u.Email.ToLower() == email))
            throw new InvalidOperationException("Ya existe una cuenta con ese correo en el tenant.");

        var adminRole = await db.Roles.SingleAsync(r => r.Nombre == "ADMIN");
        var admin = new Usuario
        {
            TenantId = tenantId, Nombre = "Administrador", Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password), Activo = true,
        };
        admin.UsuarioRoles.Add(new UsuarioRol { UsuarioId = admin.Id, RolId = adminRole.Id });
        db.Usuarios.Add(admin);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        Console.WriteLine($"Administrador creado para tenant {tenantId}: {email}");
    }

    private static string Required(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value : throw new InvalidOperationException($"Falta {name}.");
}

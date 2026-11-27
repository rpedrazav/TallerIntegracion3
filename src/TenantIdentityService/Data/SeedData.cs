using Microsoft.EntityFrameworkCore;
using TenantIdentityService.Models;

namespace TenantIdentityService.Data;

/// <summary>
/// Seed de desarrollo (solo IsDevelopment). Contraseña de todos los usuarios: demo1234.
/// Minimarkets "demo" y "donpepe" con CAJERO, ADMIN y una sucursal; el SUPER_ADMIN de la
/// plataforma vive en el tenant "globalmart". Cada registro se crea solo si falta.
/// Los IDs de tenant y sucursal coinciden con los seeds de MS-3 y MS-4.
/// </summary>
public static class SeedData
{
    private static readonly Guid RolCajero     = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid RolAdmin      = Guid.Parse("11111111-0000-0000-0000-000000000003");
    private static readonly Guid RolSuperAdmin = Guid.Parse("11111111-0000-0000-0000-000000000004");

    public static void Initialize(TenantDbContext db)
    {
        SeedTenant(db, "aaaaaaaa-0000-0000-0000-000000000001", "Minimarket Demo", "demo",
            ("cccccccc-0000-0000-0000-000000000001", "Sucursal Demo", "Av. Providencia 1234, Santiago"),
            ("bbbbbbbb-0000-0000-0000-000000000001", "Cajero Demo", "cajero@demo.cl", RolCajero),
            ("bbbbbbbb-0000-0000-0000-000000000002", "Admin Demo", "admin@demo.cl", RolAdmin));
        SeedTenant(db, "aaaaaaaa-0000-0000-0000-000000000002", "Minimarket Don Pepe", "donpepe",
            ("cccccccc-0000-0000-0000-000000000002", "Sucursal Centro", "Av. Alemania 0450, Temuco"),
            ("bbbbbbbb-0000-0000-0000-000000000011", "Cajero Don Pepe", "cajero@donpepe.cl", RolCajero),
            ("bbbbbbbb-0000-0000-0000-000000000012", "Admin Don Pepe", "admin@donpepe.cl", RolAdmin));
        SeedTenant(db, "aaaaaaaa-0000-0000-0000-000000000099", "GlobalMart OS", "globalmart", null,
            ("bbbbbbbb-0000-0000-0000-000000000099", "Super Admin", "superadmin@globalmart.cl", RolSuperAdmin));
        db.SaveChanges();
    }

    private static void SeedTenant(
        TenantDbContext db, string id, string nombre, string codigo,
        (string Id, string Nombre, string Direccion)? sucursalSeed,
        params (string Id, string Nombre, string Email, Guid RolId)[] usuarios)
    {
        var tenantId = Guid.Parse(id);
        if (!db.Tenants.Any(t => t.Id == tenantId))
            db.Tenants.Add(new Tenant
            {
                Id = tenantId, Nombre = nombre, Codigo = codigo, Pais = "CL", Moneda = "CLP",
                Idioma = "es", ZonaHoraria = "America/Santiago", PorcentajeIva = 19
            });

        // Se reutiliza la sucursal activa que ya tenga el tenant; si no tiene, se crea la del seed.
        Sucursal? sucursal = null;
        if (sucursalSeed is { } s)
        {
            sucursal = db.Sucursales.IgnoreQueryFilters().FirstOrDefault(x => x.TenantId == tenantId && x.Activa);
            if (sucursal is null)
            {
                sucursal = new Sucursal
                {
                    Id = Guid.Parse(s.Id), TenantId = tenantId, Nombre = s.Nombre,
                    Direccion = s.Direccion, ZonaHoraria = "America/Santiago"
                };
                db.Sucursales.Add(sucursal);
            }
        }

        foreach (var u in usuarios)
        {
            var usuarioId = Guid.Parse(u.Id);
            if (db.Usuarios.IgnoreQueryFilters().Any(x => x.Id == usuarioId || (x.TenantId == tenantId && x.Email == u.Email)))
                continue;
            var usuario = new Usuario
            {
                Id = usuarioId, TenantId = tenantId, Nombre = u.Nombre, Email = u.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("demo1234")
            };
            usuario.UsuarioRoles.Add(new UsuarioRol { UsuarioId = usuarioId, RolId = u.RolId });
            // El cajero necesita sucursal para abrir turno (sucursal_id del JWT).
            if (sucursal is not null && u.RolId == RolCajero)
                usuario.UsuarioSucursales.Add(new UsuarioSucursal { UsuarioId = usuarioId, SucursalId = sucursal.Id });
            db.Usuarios.Add(usuario);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Npgsql;
using TenantIdentityService.Data;
using TenantIdentityService.DTOs;
using TenantIdentityService.Exceptions;
using TenantIdentityService.Models;

namespace TenantIdentityService.Repositories;

public class TenantRepository : ITenantRepository
{
    private readonly TenantDbContext _db;

    public TenantRepository(TenantDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Obtiene un tenant por ID sin tracking (para lecturas).
    /// </summary>
    public async Task<Tenant?> GetByIdAsync(Guid id)
    {
        return await _db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(tenant => tenant.Id == id && tenant.Activo);
    }

    /// <summary>
    /// TI3-458: Obtiene un tenant por ID CON tracking (para actualizaciones).
    /// Necesario porque EF Core requiere tracking para Update/SaveChanges.
    /// </summary>
    public async Task<Tenant?> GetByIdForUpdateAsync(Guid id)
    {
        return await _db.Tenants
            .FirstOrDefaultAsync(tenant => tenant.Id == id && tenant.Activo);
    }

    public async Task<Tenant> UpdateAsync(Tenant tenant)
    {
        _db.Tenants.Update(tenant);
        await _db.SaveChangesAsync();
        return tenant;
    }

    public Task<List<Tenant>> ListAsync() =>
        _db.Tenants.AsNoTracking().OrderBy(t => t.Nombre).ToListAsync();

    public async Task<Tenant> CreateWithAdminAsync(CrearTenantRequest request, Guid? id = null)
    {
        var tenant = new Tenant
        {
            Id = id ?? Guid.NewGuid(), Nombre = request.Nombre.Trim(), Codigo = request.Codigo.Trim().ToLowerInvariant(),
            Pais = "CL", Moneda = "CLP", Idioma = "es", ZonaHoraria = "America/Santiago", PorcentajeIva = 19,
        };
        if (await _db.Tenants.AnyAsync(t => t.Codigo == tenant.Codigo || t.Nombre == tenant.Nombre))
            throw new DuplicateTenantException(tenant.Codigo);

        var sucursal = new Sucursal { TenantId = tenant.Id, Nombre = "Sucursal principal" };
        var admin = new Usuario
        {
            TenantId = tenant.Id, Nombre = "Administrador", Email = request.AdminEmail.Trim().ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.AdminPassword),
        };
        var adminRolId = await _db.Roles
            .Where(r => r.Nombre == "ADMIN" && r.TenantId == null)
            .Select(r => r.Id)
            .SingleAsync();
        admin.UsuarioRoles.Add(new UsuarioRol { UsuarioId = admin.Id, RolId = adminRolId });
        admin.UsuarioSucursales.Add(new UsuarioSucursal { UsuarioId = admin.Id, SucursalId = sucursal.Id });
        _db.AddRange(tenant, sucursal, admin);

        // Un solo SaveChanges: EF lo envuelve en una transacción, un fallo no deja datos parciales.
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new DuplicateTenantException(tenant.Codigo);
        }
        return tenant;
    }
}
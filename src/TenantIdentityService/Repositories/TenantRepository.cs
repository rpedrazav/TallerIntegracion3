using Microsoft.EntityFrameworkCore;
using TenantIdentityService.Data;
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
}
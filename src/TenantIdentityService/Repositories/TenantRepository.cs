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

    public async Task<Tenant?> GetByIdAsync(Guid id)
    {
        return await _db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(tenant => tenant.Id == id && tenant.Activo);
    }
}
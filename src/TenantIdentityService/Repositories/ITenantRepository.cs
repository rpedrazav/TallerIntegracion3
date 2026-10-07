using TenantIdentityService.Models;

namespace TenantIdentityService.Repositories;

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid id);

    /// <summary>
    /// TI3-458: Obtiene un tenant con tracking de EF Core habilitado (para actualizaciones).
    /// </summary>
    Task<Tenant?> GetByIdForUpdateAsync(Guid id);

    Task<Tenant> UpdateAsync(Tenant tenant);
}
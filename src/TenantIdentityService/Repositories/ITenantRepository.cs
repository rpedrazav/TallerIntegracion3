using TenantIdentityService.DTOs;
using TenantIdentityService.Models;

namespace TenantIdentityService.Repositories;

public interface ITenantRepository
{
    Task<List<Tenant>> ListAsync();

    /// <summary>S3-094: crea el minimarket con su configuración, sucursal inicial y ADMIN, todo o nada.</summary>
    Task<Tenant> CreateWithAdminAsync(CrearTenantRequest request, Guid? id = null);

    Task<Tenant?> GetByIdAsync(Guid id);

    /// <summary>
    /// TI3-458: Obtiene un tenant con tracking de EF Core habilitado (para actualizaciones).
    /// </summary>
    Task<Tenant?> GetByIdForUpdateAsync(Guid id);

    Task<Tenant> UpdateAsync(Tenant tenant);
}
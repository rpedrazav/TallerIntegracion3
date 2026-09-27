using TenantIdentityService.Models;

namespace TenantIdentityService.Repositories;

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid id);
}
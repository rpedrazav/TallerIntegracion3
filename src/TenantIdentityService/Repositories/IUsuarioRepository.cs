using TenantIdentityService.DTOs;
using TenantIdentityService.Models;

namespace TenantIdentityService.Repositories;

public interface IUsuarioRepository
{
    Task<IEnumerable<Usuario>> GetAllAsync(Guid tenantId);

    Task<PagedResult<Usuario>> GetActivePagedAsync(Guid tenantId, int page, int pageSize);

    Task<Usuario?> GetByIdAsync(Guid id, Guid tenantId);

    Task<Usuario> CreateAsync(Usuario usuario);

    Task<Usuario> UpdateAsync(Usuario usuario);

    Task<bool> DeactivateAsync(Guid id, Guid tenantId);
}
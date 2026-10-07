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
    Task<UsuarioActualizadoDto?> UpdateBasicAsync(Guid id, Guid tenantId, ActualizarUsuarioDto request);
    Task<bool> DeactivateAsync(Guid id, Guid tenantId);
    Task<AssignRolesResult> AssignRolesAsync(Guid userId, Guid tenantId, IEnumerable<Guid> roleIds);
    Task<AssignRolesResult> AssignRolesAsync(Guid userId, IEnumerable<string> roleNames, Guid tenantId);
    Task<IReadOnlyList<string>> ValidateRoleNamesAsync(IEnumerable<string> roleNames);
    Task<bool> DeletePermanentlyAsync(Guid id, Guid tenantId);
}

using TenantIdentityService.DTOs;
using TenantIdentityService.Models;

namespace TenantIdentityService.Repositories;

public interface IUsuarioRepository
{
    Task<Usuario?> FindByEmailAsync(string email, Guid tenantId);
    Task<PagedResult<Usuario>> GetActivePagedAsync(Guid tenantId, int page, int pageSize);
    Task<Usuario?> GetByIdAsync(Guid id, Guid tenantId);
    Task<Usuario> CreateAsync(Usuario usuario);
    Task<UsuarioActualizadoDto?> UpdateBasicAsync(Guid id, Guid tenantId, ActualizarUsuarioDto request);
    Task<bool> DeactivateAsync(Guid id, Guid tenantId);
    Task<bool> ReactivateAsync(Guid id, Guid tenantId);
    Task<AssignRolesResult> AssignRolesAsync(Guid userId, Guid tenantId, IEnumerable<Guid> roleIds);
    Task<AssignRolesResult> AssignRolesAsync(Guid userId, IEnumerable<string> roleNames, Guid tenantId);
    Task<IReadOnlyList<string>> ValidateRoleNamesAsync(IEnumerable<string> roleNames);
}

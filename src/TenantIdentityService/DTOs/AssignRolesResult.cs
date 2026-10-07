using TenantIdentityService.Models;

namespace TenantIdentityService.DTOs;

public class AssignRolesResult
{
    public bool UserFound { get; init; }
    public IReadOnlyCollection<Guid> InvalidRoles { get; init; } = [];
    public Usuario? Usuario { get; init; }
}

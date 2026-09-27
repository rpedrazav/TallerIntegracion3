using TenantIdentityService.Models;

namespace TenantIdentityService.DTOs;

public class AssignRolesResult
{
    public bool UserFound { get; init; }

    public IReadOnlyCollection<string> InvalidRoles { get; init; } = [];

    public Usuario? Usuario { get; init; }
}
namespace TenantIdentityService.DTOs;

/// <summary>Body de POST /api/tenants (SUPER_ADMIN) y entrada de --bootstrap-admin.</summary>
public sealed record CrearTenantRequest
{
    public string Nombre { get; init; } = string.Empty;

    /// <summary>Código de login del minimarket (ej: "donpepe"); se guarda en minúsculas.</summary>
    public string Codigo { get; init; } = string.Empty;

    public string AdminEmail { get; init; } = string.Empty;

    public string AdminPassword { get; init; } = string.Empty;
}

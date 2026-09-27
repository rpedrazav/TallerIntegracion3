namespace TenantIdentityService.DTOs;

public class UsuarioDto
{
    public Guid Id { get; init; }

    public Guid TenantId { get; init; }

    public string Nombre { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public bool Activo { get; init; }

    public DateTime CreadoEn { get; init; }

    public DateTime? UltimoLogin { get; init; }
}
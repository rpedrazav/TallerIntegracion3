namespace TenantIdentityService.Models;

/// <summary>
/// Tabla de unión M:N entre Usuario y Rol.
/// Un usuario puede tener múltiples roles (CAJERO + REPONEDOR, etc.).
/// </summary>
public class UsuarioRol
{
    public Guid UsuarioId { get; set; }
    public Guid RolId { get; set; }

    public DateTime AsignadoEn { get; set; } = DateTime.UtcNow;

    // Relaciones de navegación
    public Usuario Usuario { get; set; } = null!;
    public Rol Rol { get; set; } = null!;
}

namespace TenantIdentityService.Models;

/// <summary>
/// Rol RBAC global o personalizado para un tenant.
/// </summary>
public class Rol
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Nombre del rol (CAJERO, REPONEDOR, ADMIN, SUPER_ADMIN, CLIENTE_AFILIADO).</summary>
    public string Nombre { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    /// <summary>Null para roles base del sistema; identificador del tenant para roles propios.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Capacidad heredada por un rol personalizado: ADMIN, CAJERO o REPONEDOR.</summary>
    public string? RolBase { get; set; }

    // Relaciones de navegación
    public ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();
}

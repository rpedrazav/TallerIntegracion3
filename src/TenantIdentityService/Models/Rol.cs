namespace TenantIdentityService.Models;

/// <summary>
/// Rol del sistema RBAC. Los roles son globales (no por tenant).
/// Valores posibles: CAJERO, REPONEDOR, ADMIN, SUPER_ADMIN, CLIENTE_AFILIADO.
/// </summary>
public class Rol
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Nombre del rol (CAJERO, REPONEDOR, ADMIN, SUPER_ADMIN, CLIENTE_AFILIADO).</summary>
    public string Nombre { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    // Relaciones de navegación
    public ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();
}

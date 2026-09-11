namespace TenantIdentityService.Models;

/// <summary>
/// Usuario del sistema GlobalMart OS.
/// Pertenece a un tenant. Puede tener múltiples roles simultáneos (RBAC multi-rol).
/// </summary>
public class Usuario
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Discriminador multi-tenant. Obligatorio en todas las tablas.</summary>
    public Guid TenantId { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    /// <summary>Hash bcrypt de la contraseña. Nunca se almacena en texto plano.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Si false, el usuario no puede iniciar sesión (desactivado por el Admin).</summary>
    public bool Activo { get; set; } = true;

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public DateTime? UltimoLogin { get; set; }

    // Relaciones de navegación
    public Tenant Tenant { get; set; } = null!;
    public ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();
    public ICollection<UsuarioSucursal> UsuarioSucursales { get; set; } = new List<UsuarioSucursal>();
}

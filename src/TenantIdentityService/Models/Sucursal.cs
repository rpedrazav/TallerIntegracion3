namespace TenantIdentityService.Models;

/// <summary>
/// Sucursal de un minimarket. Cada tenant puede tener múltiples sucursales.
/// Inventarios y precios son independientes por sucursal.
/// </summary>
public class Sucursal
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Discriminador multi-tenant obligatorio.</summary>
    public Guid TenantId { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Direccion { get; set; } = string.Empty;

    public bool Activa { get; set; } = true;

    public DateTime CreadaEn { get; set; } = DateTime.UtcNow;

    // Relaciones de navegación
    public Tenant Tenant { get; set; } = null!;
    public ICollection<UsuarioSucursal> UsuarioSucursales { get; set; } = new List<UsuarioSucursal>();
}

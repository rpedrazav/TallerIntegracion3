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

    /// <summary>
    /// Zona horaria IANA propia de la sucursal (ej: "America/Santiago", "America/Argentina/Buenos_Aires").
    /// Si es <c>null</c>, la sucursal hereda <see cref="Tenant.ZonaHoraria"/>:
    /// <c>ZonaHoraria efectiva = Sucursal.ZonaHoraria ?? Tenant.ZonaHoraria</c>.
    /// Permite que un tenant opere sucursales en zonas horarias distintas.
    /// </summary>
    public string? ZonaHoraria { get; set; }

    public bool Activa { get; set; } = true;

    public DateTime CreadaEn { get; set; } = DateTime.UtcNow;

    // Relaciones de navegación
    public Tenant Tenant { get; set; } = null!;
    public ICollection<UsuarioSucursal> UsuarioSucursales { get; set; } = new List<UsuarioSucursal>();
}

namespace TenantIdentityService.Models;

/// <summary>
/// Representa un minimarket (tenant) dentro de la plataforma GlobalMart OS.
/// Cada tenant es completamente independiente: datos, configuración fiscal, moneda e idioma.
/// </summary>
public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Nombre { get; set; } = string.Empty;

    /// <summary>Código ISO del país (CL, AR, US, etc.). Determina la Entidad Fiscal a usar.</summary>
    public string Pais { get; set; } = string.Empty;

    /// <summary>Código ISO de la moneda base (CLP, ARS, USD, EUR).</summary>
    public string Moneda { get; set; } = string.Empty;

    /// <summary>Código de idioma (es, en, pt).</summary>
    public string Idioma { get; set; } = "es";

    /// <summary>IANA timezone string (America/Santiago, America/Buenos_Aires, etc.).</summary>
    public string ZonaHoraria { get; set; } = "America/Santiago";

    /// <summary>Porcentaje de IVA aplicable (ej: 19 para Chile, 21 para Argentina).</summary>
    public decimal PorcentajeIva { get; set; } = 19;

    /// <summary>Stock mínimo como porcentaje del stock objetivo que activa la alerta.</summary>
    public decimal UmbralStockMinimo { get; set; } = 10;

    /// <summary>Porcentaje de variación de tipo de cambio que activa actualización automática FX.</summary>
    public decimal UmbralVariacionFx { get; set; } = 1;

    /// <summary>Si true, el creador de una OC no puede ser el mismo que la aprueba.</summary>
    public bool SeparacionFuncionesOc { get; set; } = false;

    public bool Activo { get; set; } = true;

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    // Relaciones de navegación
    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    public ICollection<Sucursal> Sucursales { get; set; } = new List<Sucursal>();
}

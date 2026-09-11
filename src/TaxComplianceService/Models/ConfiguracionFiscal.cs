namespace TaxComplianceService.Models;

/// <summary>
/// Configuración fiscal de un tenant.
/// Define el tipo de documento, prefijo y folio actual para la emisión de DTE.
/// Un tenant puede tener múltiples configuraciones (boleta, factura, nota de crédito...).
/// </summary>
public class ConfiguracionFiscal
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Discriminador multi-tenant obligatorio.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Tipo de documento (BOLETA, FACTURA, NOTA_CREDITO, NOTA_DEBITO).</summary>
    public string TipoDocumento { get; set; } = string.Empty;

    /// <summary>
    /// Prefijo del documento según país (ej: "B" para boleta en Chile).
    /// Puede ser vacío según normativa del país.
    /// </summary>
    public string Prefijo { get; set; } = string.Empty;

    /// <summary>Número de folio actual. Se incrementa con cada DTE emitido.</summary>
    public long FolioActual { get; set; } = 1;

    /// <summary>Folio máximo asignado por la Entidad Fiscal en el CAF actual.</summary>
    public long FolioMaximo { get; set; } = 0;

    /// <summary>Porcentaje de IVA u otro impuesto aplicable (configurado por pais/tenant).</summary>
    public decimal PorcentajeImpuesto { get; set; } = 19;

    /// <summary>
    /// Pais de la entidad fiscal: CL (SII), AR (AFIP), US (IRS).
    /// Determina el protocolo de comunicación con la entidad fiscal.
    /// </summary>
    public string PaisEntidadFiscal { get; set; } = "CL";

    public bool Activa { get; set; } = true;

    public DateTime CreadaEn { get; set; } = DateTime.UtcNow;

    // Relaciones de navegación
    public ICollection<DocumentoTributario> Documentos { get; set; } = new List<DocumentoTributario>();
}

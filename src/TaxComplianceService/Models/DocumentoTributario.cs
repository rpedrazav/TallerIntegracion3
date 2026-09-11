namespace TaxComplianceService.Models;

/// <summary>
/// Documento Tributario Electrónico (DTE) emitido por GlobalMart OS.
/// Puede ser boleta, factura, nota de crédito, etc.
/// Su contenido XML es el documento firmado digitalmente que se envía a la Entidad Fiscal.
/// </summary>
public class DocumentoTributario
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Discriminador multi-tenant obligatorio.</summary>
    public Guid TenantId { get; set; }

    public Guid ConfiguracionFiscalId { get; set; }

    /// <summary>Número de folio asignado al documento.</summary>
    public long Numero { get; set; }

    /// <summary>Tipo de documento (BOLETA, FACTURA, NOTA_CREDITO, NOTA_DEBITO).</summary>
    public string TipoDocumento { get; set; } = string.Empty;

    /// <summary>
    /// Estado del DTE:
    /// PENDIENTE → en proceso de emisión
    /// EMITIDO → enviado a la Entidad Fiscal y validado
    /// RECHAZADO → rechazado por la Entidad Fiscal
    /// ANULADO → anulado por devolución de venta
    /// PENDING_DTE → la Entidad Fiscal no respondió, se reintenta en background
    /// </summary>
    public string Estado { get; set; } = "PENDIENTE";

    /// <summary>XML del DTE firmado digitalmente según normativa del país.</summary>
    public string ContenidoXml { get; set; } = string.Empty;

    /// <summary>Código de autorización retornado por la Entidad Fiscal (CAE en Argentina, etc.).</summary>
    public string? CodigoAutorizacion { get; set; }

    /// <summary>ID de la venta en MS-5 que generó este documento.</summary>
    public Guid VentaId { get; set; }

    /// <summary>Monto total del documento (base imponible + impuesto).</summary>
    public decimal MontoTotal { get; set; }

    /// <summary>Monto del impuesto calculado.</summary>
    public decimal MontoImpuesto { get; set; }

    /// <summary>Cantidad de reintentos realizados si la Entidad Fiscal no respondió.</summary>
    public int Reintentos { get; set; } = 0;

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public DateTime? EmitidoEn { get; set; }

    // Relaciones de navegación
    public ConfiguracionFiscal ConfiguracionFiscal { get; set; } = null!;
}

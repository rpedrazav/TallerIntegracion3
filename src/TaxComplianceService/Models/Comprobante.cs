namespace TaxComplianceService.Models;

/// <summary>
/// Comprobante de venta emitido por MS-2, asociado a una venta completada en MS-5 (POSCartService).
/// </summary>
public class Comprobante
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Discriminador multi-tenant obligatorio.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Número correlativo del comprobante.</summary>
    public long NumeroCorrelativo { get; set; }

    /// <summary>ID de la venta en MS-5 (POSCartService) que generó este comprobante.</summary>
    public Guid VentaId { get; set; }

    /// <summary>ID del cajero (usuario de MS-1) que emitió el comprobante.</summary>
    public Guid CajeroId { get; set; }

    /// <summary>Detalle de items vendidos, en JSON. Mapea a columna jsonb.</summary>
    public string Items { get; set; } = "[]";

    public decimal Subtotal { get; set; }

    public decimal Iva { get; set; }

    public decimal Total { get; set; }

    public DateTime FechaEmision { get; set; } = DateTime.UtcNow;
}

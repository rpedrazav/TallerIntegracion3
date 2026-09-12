namespace SupplyChainService.Models;

/// <summary>
/// Histórico de cálculos de Costo Landed por producto, independiente de
/// la orden de compra (para análisis de variación de costos en el tiempo).
/// </summary>
public class CostoLandedHistorico
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Referencia al Producto en Catalog &amp; Pricing Service (MS-3). Sin navigation property: bases de datos separadas.</summary>
    public Guid ProductoId { get; set; }

    public Guid TenantId { get; set; }

    public decimal CostoCalculado { get; set; }

    /// <summary>Desglose del cálculo en JSON (flete, seguro, aranceles, tipo de cambio usado, etc.). Mapea a columna JSONB.</summary>
    public string Detalle { get; set; } = "{}";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
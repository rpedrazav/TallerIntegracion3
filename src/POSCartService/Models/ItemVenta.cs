namespace POSCartService.Models;

public class ItemVenta
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid VentaId { get; set; }

    /// <summary>Referencia al Producto en Catalog &amp; Pricing Service (MS-3). Sin navigation property: bases de datos separadas.</summary>
    public Guid ProductoId { get; set; }

    /// <summary>Snapshot del nombre al momento de la venta (histórico, no depende de que el producto exista o cambie después en MS-3).</summary>
    public string NombreProducto { get; set; } = string.Empty;

    public decimal Cantidad { get; set; }

    /// <summary>Solo aplica a productos de peso variable (balanza). Null para productos por unidad.</summary>
    public decimal? PesoKg { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal Subtotal { get; set; }

    // Relación de navegación (misma base de datos: MS-5)
    public Venta Venta { get; set; } = null!;
}
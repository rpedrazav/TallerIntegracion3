namespace SupplyChainService.Models;

public class OrdenCompraItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrdenCompraId { get; set; }

    /// <summary>Referencia al Producto en Catalog &amp; Pricing Service (MS-3). Sin navigation property: bases de datos separadas.</summary>
    public Guid ProductoId { get; set; }

    public decimal Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal Subtotal { get; set; }

    // Relación de navegación (misma base de datos: MS-6)
    public OrdenCompra OrdenCompra { get; set; } = null!;
}
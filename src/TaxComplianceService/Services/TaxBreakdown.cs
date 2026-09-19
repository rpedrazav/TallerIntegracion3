namespace TaxComplianceService.Services;

/// <summary>
/// Desglose (breakdown) detallado de impuestos para un item individual.
/// </summary>
public class TaxItemBreakdown
{
    private string? _nombre;

    /// <summary>
    /// Nombre o descripción del producto/item.
    /// </summary>
    public string? Nombre
    {
        get => _nombre;
        set => _nombre = value;
    }

    /// <summary>
    /// Alias de Nombre.
    /// </summary>
    public string? Descripcion
    {
        get => _nombre;
        set => _nombre = value;
    }

    /// <summary>
    /// Precio unitario del item.
    /// </summary>
    public decimal Precio { get; set; }

    /// <summary>
    /// Cantidad de unidades o peso.
    /// </summary>
    public decimal Cantidad { get; set; }

    /// <summary>
    /// Subtotal del item: precio * cantidad.
    /// </summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Monto de IVA correspondiente a este item: subtotal * (porcentajeIva / 100).
    /// </summary>
    public decimal Iva { get; set; }

    /// <summary>
    /// Total del item: subtotal + iva.
    /// </summary>
    public decimal Total { get; set; }
}

/// <summary>
/// Desglose (breakdown) completo del cálculo fiscal de una venta u operación.
/// </summary>
public class TaxBreakdown
{
    /// <summary>
    /// Subtotal calculado: sum(precio * cantidad) de todos los items.
    /// </summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Porcentaje de IVA aplicado en el cálculo.
    /// </summary>
    public decimal PorcentajeIva { get; set; }

    /// <summary>
    /// Monto de IVA calculado: subtotal * (porcentajeIva / 100).
    /// </summary>
    public decimal Iva { get; set; }

    /// <summary>
    /// Alias de Iva para coincidir con la nomenclatura de MontoImpuesto si es necesario.
    /// </summary>
    public decimal MontoIva => Iva;

    /// <summary>
    /// Monto total: subtotal + iva.
    /// </summary>
    public decimal Total { get; set; }

    /// <summary>
    /// Desglose detallado item por item.
    /// </summary>
    public List<TaxItemBreakdown> Items { get; set; } = new();

    /// <summary>
    /// Alias de Items.
    /// </summary>
    public List<TaxItemBreakdown> Detalles => Items;

    /// <summary>
    /// Subtotal redondeado a 2 decimales para formatos de moneda estándar.
    /// </summary>
    public decimal SubtotalRedondeado => Math.Round(Subtotal, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// IVA redondeado a 2 decimales para formatos de moneda estándar.
    /// </summary>
    public decimal IvaRedondeado => Math.Round(Iva, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Total redondeado a 2 decimales para formatos de moneda estándar.
    /// </summary>
    public decimal TotalRedondeado => Math.Round(Total, 2, MidpointRounding.AwayFromZero);
}

namespace TaxComplianceService.Services;

/// <summary>
/// Desglose (breakdown) detallado de impuestos para un item individual.
/// </summary>
public class TaxItemBreakdown
{
    private string? _nombre;
    private bool _exento;

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
    /// Porcentaje de IVA aplicado al item (0 si es exento).
    /// </summary>
    public decimal PorcentajeIva { get; set; }

    /// <summary>
    /// Monto de IVA correspondiente a este item: subtotal * (porcentajeIva / 100).
    /// 0 si el item es exento o el porcentaje es 0.
    /// </summary>
    public decimal Iva { get; set; }

    /// <summary>
    /// Total del item: subtotal + iva.
    /// Para items exentos o IVA = 0: subtotal == total.
    /// </summary>
    public decimal Total { get; set; }

    /// <summary>
    /// Indica si el item fue tratado como exento de impuesto (IVA = 0).
    /// </summary>
    public bool Exento
    {
        get => _exento;
        set => _exento = value;
    }

    /// <summary>
    /// Alias de Exento.
    /// </summary>
    public bool EsExento
    {
        get => _exento;
        set => _exento = value;
    }
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
    /// Porcentaje de IVA general aplicado en el cálculo.
    /// </summary>
    public decimal PorcentajeIva { get; set; }

    /// <summary>
    /// Monto de IVA calculado: suma del IVA de todos los items gravados.
    /// Para canastas 100% exentas o IVA general = 0: Iva == 0.
    /// </summary>
    public decimal Iva { get; set; }

    /// <summary>
    /// Alias de Iva para coincidir con la nomenclatura de MontoImpuesto si es necesario.
    /// </summary>
    public decimal MontoIva => Iva;

    /// <summary>
    /// Monto total: subtotal + iva.
    /// Para canastas exentas o IVA = 0: Total == Subtotal.
    /// </summary>
    public decimal Total { get; set; }

    /// <summary>
    /// Monto subtotal correspondiente a items exentos de impuesto.
    /// </summary>
    public decimal MontoExento => Items.Where(i => i.Exento).Sum(i => i.Subtotal);

    /// <summary>
    /// Monto subtotal correspondiente a items gravados con impuesto.
    /// </summary>
    public decimal MontoGravado => Items.Where(i => !i.Exento).Sum(i => i.Subtotal);

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

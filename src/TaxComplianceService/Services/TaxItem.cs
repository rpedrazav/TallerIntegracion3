namespace TaxComplianceService.Services;

/// <summary>
/// Representa un item o línea de producto para el cálculo fiscal.
/// Soporta productos gravados y exentos de impuestos.
/// </summary>
public class TaxItem
{
    private string? _nombre;
    private decimal _precio;
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
    /// Alias de Nombre para compatibilidad de nomenclatura.
    /// </summary>
    public string? Descripcion
    {
        get => _nombre;
        set => _nombre = value;
    }

    /// <summary>
    /// Precio unitario del item.
    /// </summary>
    public decimal Precio
    {
        get => _precio;
        set => _precio = value;
    }

    /// <summary>
    /// Alias de Precio para compatibilidad con modelos como ItemVenta (PrecioUnitario).
    /// </summary>
    public decimal PrecioUnitario
    {
        get => _precio;
        set => _precio = value;
    }

    /// <summary>
    /// Cantidad de unidades o peso (ej. kg) del producto.
    /// </summary>
    public decimal Cantidad { get; set; }

    /// <summary>
    /// Indica si el producto está exento de IVA (IVA = 0).
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

    /// <summary>
    /// Porcentaje de IVA específico para este producto (opcional).
    /// Si no se especifica, se aplica la tasa general del tenant / operación.
    /// </summary>
    public decimal? PorcentajeIva { get; set; }

    /// <summary>
    /// Constructor por defecto.
    /// </summary>
    public TaxItem() { }

    /// <summary>
    /// Constructor con precio, cantidad, nombre y flag de exención opcional.
    /// </summary>
    public TaxItem(decimal precio, decimal cantidad, string? nombre = null, bool exento = false)
    {
        Precio = precio;
        Cantidad = cantidad;
        Nombre = nombre;
        Exento = exento;
    }

    /// <summary>
    /// Conversión implícita para permitir instanciación directa desde tuplas (precio, cantidad).
    /// </summary>
    public static implicit operator TaxItem((decimal precio, decimal cantidad) tuple)
        => new(tuple.precio, tuple.cantidad);

    /// <summary>
    /// Conversión implícita para permitir instanciación desde tuplas (precio, cantidad, exento).
    /// </summary>
    public static implicit operator TaxItem((decimal precio, decimal cantidad, bool exento) tuple)
        => new(tuple.precio, tuple.cantidad, null, tuple.exento);
}

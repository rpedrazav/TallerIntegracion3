namespace TaxComplianceService.Services;

/// <summary>
/// Representa un item o línea de producto para el cálculo fiscal.
/// </summary>
public class TaxItem
{
    private string? _nombre;
    private decimal _precio;

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
    /// Constructor por defecto.
    /// </summary>
    public TaxItem() { }

    /// <summary>
    /// Constructor con precio y cantidad.
    /// </summary>
    public TaxItem(decimal precio, decimal cantidad, string? nombre = null)
    {
        Precio = precio;
        Cantidad = cantidad;
        Nombre = nombre;
    }

    /// <summary>
    /// Conversión implícita para permitir instanciación directa desde tuplas (precio, cantidad).
    /// </summary>
    public static implicit operator TaxItem((decimal precio, decimal cantidad) tuple)
        => new(tuple.precio, tuple.cantidad);
}

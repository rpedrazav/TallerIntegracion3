namespace POSCartService.Models;

/// <summary>
/// Un pago individual asociado a una Venta. Una Venta con MetodoPago=MIXTO
/// tiene múltiples Pagos (ej. una fila EFECTIVO + una fila TARJETA).
/// </summary>
public enum MetodoPago
{
    EFECTIVO,
    TARJETA
}

public class Pago
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid VentaId { get; set; }

    public MetodoPago Metodo { get; set; }

    public decimal Monto { get; set; }

    /// <summary>Solo aplica a pagos en efectivo.</summary>
    public decimal Vuelto { get; set; } = 0;

    // Relación de navegación (misma base de datos: MS-5)
    public Venta Venta { get; set; } = null!;
}
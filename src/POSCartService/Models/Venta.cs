namespace POSCartService.Models;

/// <summary>
/// Estado de la venta, según ERD (M3).
/// NOTA: el diagrama de estados de Daniel (DA3) incluye además CANCELADA
/// (venta abandonada antes de cobrar, sin impacto en stock ni boleta).
/// Pendiente de confirmar con Martín si se agrega al ENUM de la tabla.
/// </summary>
public enum EstadoVenta
{
    PENDIENTE,
    COMPLETADA,
    ANULADA
}

public enum MetodoPagoVenta
{
    EFECTIVO,
    TARJETA,
    MIXTO
}

public class Venta
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }

    public Guid TurnoId { get; set; }

    /// <summary>Denormalizado desde Turno para reportes sin join (según ERD). Sin navigation property.</summary>
    public Guid CajeroId { get; set; }

    /// <summary>Denormalizado desde Turno para reportes sin join (según ERD). Sin navigation property.</summary>
    public Guid SucursalId { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Impuestos { get; set; }

    public decimal Total { get; set; }

    public MetodoPagoVenta MetodoPago { get; set; }

    public EstadoVenta Estado { get; set; } = EstadoVenta.PENDIENTE;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Relaciones de navegación (misma base de datos: MS-5)
    public Turno Turno { get; set; } = null!;
    public ICollection<ItemVenta> Items { get; set; } = new List<ItemVenta>();
    public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
    public Anulacion? Anulacion { get; set; }
}
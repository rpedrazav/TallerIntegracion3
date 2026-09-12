namespace POSCartService.Models;

/// <summary>
/// Anulación de una venta ya completada. Relación 1:1 con Venta
/// (una venta se anula una sola vez).
/// </summary>
public class Anulacion
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid VentaId { get; set; }

    public string Motivo { get; set; } = string.Empty;

    /// <summary>Usuario (ADMINISTRADOR) que autorizó la anulación. Sin navigation property: base de datos externa (MS-1).</summary>
    public Guid AutorizadoPor { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Relación de navegación (misma base de datos: MS-5)
    public Venta Venta { get; set; } = null!;
}
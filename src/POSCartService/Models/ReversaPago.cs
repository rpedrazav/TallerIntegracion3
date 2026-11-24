namespace POSCartService.Models;

public enum EstadoReversa { PENDIENTE, APROBADA, RECHAZADA }

/// <summary>Una reversa total por venta; Id es también su clave idempotente en pasarela.</summary>
public sealed class ReversaPago
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid VentaId { get; set; }
    public string ReferenciaPago { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string Moneda { get; set; } = string.Empty;
    public EstadoReversa Estado { get; set; }
    public string? ReferenciaReversa { get; set; }
    public string? CodigoRechazo { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public Guid AutorizadoPor { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Venta Venta { get; set; } = null!;
}

namespace POSCartService.Models;

public enum OutboxStatus
{
    PENDIENTE = 0,
    EN_PROCESO = 1,
    PUBLICADO = 2,
    FALLIDO = 3
}

/// <summary>
/// Snapshot durable de un evento de MS-5. Los reintentos reutilizan EventId y Payload.
/// La integración con el cobro y el publicador se implementan en tareas posteriores.
/// </summary>
public class OutboxMessage
{
    // Se asigna desde el evento original; EF no debe generar otra identidad.
    public Guid EventId { get; set; }
    public Guid TenantId { get; set; }
    public Guid VentaId { get; set; }
    public string EventType { get; set; } = "sale.completed";
    public int SchemaVersion { get; set; } = 1;
    public string Topic { get; set; } = "sale.completed";
    public string MessageKey { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public OutboxStatus Status { get; set; } = OutboxStatus.PENDIENTE;
    public int AttemptCount { get; set; }
    public int TotalAttemptCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? NextAttemptAt { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public Guid? LeaseToken { get; set; }
    public DateTime? LockedUntil { get; set; }
    public string? LastError { get; set; }
}

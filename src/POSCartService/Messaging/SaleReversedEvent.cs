using System.Text.Json;
using System.Text.Json.Serialization;
using POSCartService.Models;

namespace POSCartService.Messaging;

/// <summary>Anulación total confirmada. No acredita devolución física de efectivo ni reposición de stock.</summary>
public sealed class SaleReversedEvent
{
    public const string Topic = "sale.reversed";

    [JsonPropertyName("event_id")]
    public Guid EventId { get; init; }
    [JsonPropertyName("schema_version")]
    public int SchemaVersion { get; init; } = 1;
    [JsonPropertyName("tenant_id")]
    public Guid TenantId { get; init; }
    [JsonPropertyName("venta_id")]
    public Guid VentaId { get; init; }
    [JsonPropertyName("anulacion_id")]
    public Guid AnulacionId { get; init; }
    [JsonPropertyName("sucursal_id")]
    public Guid SucursalId { get; init; }
    [JsonPropertyName("autorizado_por")]
    public Guid AutorizadoPor { get; init; }
    [JsonPropertyName("motivo")]
    public string Motivo { get; init; } = string.Empty;
    [JsonPropertyName("metodo_pago")]
    public string MetodoPago { get; init; } = string.Empty;
    [JsonPropertyName("subtotal")]
    public decimal Subtotal { get; init; }
    [JsonPropertyName("iva")]
    public decimal Iva { get; init; }
    [JsonPropertyName("total")]
    public decimal Total { get; init; }
    [JsonPropertyName("items")]
    public List<SaleCompletedItemEvent> Items { get; init; } = new();
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; }

    /// <summary>Crear una sola vez dentro de la transacción de anulación; el worker reutiliza el snapshot.</summary>
    public static OutboxMessage CreateOutbox(Venta venta)
    {
        var anulacion = venta.Anulacion ?? throw new InvalidOperationException("La venta requiere una anulación confirmada.");
        if (venta.Estado != EstadoVenta.ANULADA || anulacion.VentaId != venta.Id)
            throw new InvalidOperationException("La anulación no corresponde a la venta anulada.");

        var evento = new SaleReversedEvent
        {
            // La auditoría identifica la única anulación lógica, no cada intento de entrega a Kafka.
            EventId = anulacion.Id, AnulacionId = anulacion.Id,
            TenantId = venta.TenantId, VentaId = venta.Id, SucursalId = venta.SucursalId,
            AutorizadoPor = anulacion.AutorizadoPor, Motivo = anulacion.Motivo,
            MetodoPago = venta.MetodoPago.ToString(), Subtotal = venta.Subtotal,
            Iva = venta.Impuestos, Total = venta.Total, Timestamp = anulacion.CreatedAt,
            Items = venta.Items.OrderBy(i => i.Id).Select(i => new SaleCompletedItemEvent
            {
                ProductoId = i.ProductoId, Cantidad = i.Cantidad, PrecioUnitario = i.PrecioUnitario
            }).ToList()
        };
        return new OutboxMessage
        {
            EventId = evento.EventId, TenantId = evento.TenantId, VentaId = evento.VentaId,
            EventType = Topic, Topic = Topic, SchemaVersion = evento.SchemaVersion,
            MessageKey = evento.TenantId.ToString(), Payload = JsonSerializer.Serialize(evento),
            CreatedAt = evento.Timestamp, Status = OutboxStatus.PENDIENTE
        };
    }
}

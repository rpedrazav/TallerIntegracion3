using System.Text.Json.Serialization;

namespace WarehouseInventoryService.Messaging;

/// <summary>
/// Representa el payload del evento Kafka "sale.completed" publicado por MS-5 (POSCartService).
/// </summary>
public sealed class SaleCompletedEvent
{
    [JsonPropertyName("event_id")]
    public Guid EventId { get; set; }

    [JsonPropertyName("venta_id")]
    public Guid VentaId { get; set; }

    [JsonPropertyName("tenant_id")]
    public Guid TenantId { get; set; }

    [JsonPropertyName("sucursal_id")]
    public Guid SucursalId { get; set; }

    [JsonPropertyName("items")]
    public List<SaleCompletedItem> Items { get; set; } = new();

    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

public sealed class SaleCompletedItem
{
    [JsonPropertyName("producto_id")]
    public Guid ProductoId { get; set; }

    [JsonPropertyName("cantidad")]
    public decimal Cantidad { get; set; }

    [JsonPropertyName("lote_id")]
    public Guid? LoteId { get; set; }
}

namespace WarehouseInventoryService.Messaging;

/// <summary>
/// Representa el payload del evento Kafka "sale.completed" publicado por MS-5 (POSCartService).
/// </summary>
public sealed class SaleCompletedEvent
{
    public Guid VentaId { get; set; }
    public Guid TenantId { get; set; }
    public Guid SucursalId { get; set; }
    public List<SaleCompletedItem> Items { get; set; } = new();
    public decimal Total { get; set; }
    public DateTime Timestamp { get; set; }
}

public sealed class SaleCompletedItem
{
    public Guid ProductoId { get; set; }
    public decimal Cantidad { get; set; }
    public Guid? LoteId { get; set; }
}

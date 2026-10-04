using System.Text.Json.Serialization;

namespace POSCartService.Messaging;

/// <summary>
/// Payload del evento Kafka "sale.completed" publicado por MS-5 al completar una venta.
/// Consumido por MS-4 (descuento de stock), MS-7 (analytics) y MS-8 (puntos de lealtad).
/// </summary>
public sealed class SaleCompletedEvent
{
    /// <summary>
    /// Identificador único del evento (GUID nuevo por cada publicación).
    /// Distinto de VentaId: permite republicar un evento sin colisionar con la idempotencia del consumer.
    /// </summary>
    [JsonPropertyName("event_id")]
    public Guid EventId { get; init; } = Guid.NewGuid();

    [JsonPropertyName("tenant_id")]
    public Guid TenantId { get; init; }

    [JsonPropertyName("venta_id")]
    public Guid VentaId { get; init; }

    [JsonPropertyName("cajero_id")]
    public Guid CajeroId { get; init; }

    [JsonPropertyName("sucursal_id")]
    public Guid SucursalId { get; init; }

    [JsonPropertyName("items")]
    public List<SaleCompletedItemEvent> Items { get; init; } = new();

    [JsonPropertyName("subtotal")]
    public decimal Subtotal { get; init; }

    /// <summary>Importe de IVA calculado por MS-2.</summary>
    [JsonPropertyName("iva")]
    public decimal Iva { get; init; }

    [JsonPropertyName("total")]
    public decimal Total { get; init; }

    /// <summary>
    /// Método de pago de la venta (EFECTIVO | TARJETA | MIXTO).
    /// Permite que MS-8 distinga el medio de pago para reglas de puntos.
    /// </summary>
    [JsonPropertyName("metodo_pago")]
    public string MetodoPago { get; init; } = string.Empty;

    /// <summary>Instante UTC en que se completó la venta.</summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Ítem de línea dentro del evento sale.completed.
/// Incluye precio_unitario para que MS-7 pueda calcular ingresos por producto.
/// </summary>
public sealed class SaleCompletedItemEvent
{
    [JsonPropertyName("producto_id")]
    public Guid ProductoId { get; init; }

    [JsonPropertyName("cantidad")]
    public decimal Cantidad { get; init; }

    [JsonPropertyName("precio_unitario")]
    public decimal PrecioUnitario { get; init; }
}

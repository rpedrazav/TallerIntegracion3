using System.Text.Json.Serialization;

namespace POSCartService.DTOs;

/// <summary>
/// Cuerpo del request para POST /ventas.
/// El cajero envía la lista de ítems del carrito y el método de pago.
/// El tenant_id, cajero_id y sucursal_id se extraen del JWT + turno activo.
/// </summary>
public sealed record CrearVentaRequest
{
    [JsonPropertyName("items")]
    public List<ItemVentaRequest> Items { get; init; } = new();

    [JsonPropertyName("metodo_pago")]
    public string MetodoPago { get; init; } = string.Empty;
}

/// <summary>
/// Ítem de línea dentro del request de creación de venta.
/// </summary>
public sealed record ItemVentaRequest
{
    [JsonPropertyName("producto_id")]
    public Guid ProductoId { get; init; }

    [JsonPropertyName("nombre_producto")]
    public string NombreProducto { get; init; } = string.Empty;

    [JsonPropertyName("cantidad")]
    public decimal Cantidad { get; init; }

    /// <summary>
    /// Solo para productos de peso variable (balanza). Null para productos por unidad.
    /// </summary>
    [JsonPropertyName("peso_kg")]
    public decimal? PesoKg { get; init; }

    [JsonPropertyName("precio_unitario")]
    public decimal PrecioUnitario { get; init; }
}

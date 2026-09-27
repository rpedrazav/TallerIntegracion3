using System.Text.Json.Serialization;
using POSCartService.Models;

namespace POSCartService.DTOs;

/// <summary>
/// Respuesta de POST /ventas — representa la venta recién creada en estado PENDIENTE.
/// </summary>
public sealed record VentaResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("turno_id")]
    public Guid TurnoId { get; init; }

    [JsonPropertyName("cajero_id")]
    public Guid CajeroId { get; init; }

    [JsonPropertyName("sucursal_id")]
    public Guid SucursalId { get; init; }

    [JsonPropertyName("tenant_id")]
    public Guid TenantId { get; init; }

    [JsonPropertyName("subtotal")]
    public decimal Subtotal { get; init; }

    [JsonPropertyName("impuestos")]
    public decimal Impuestos { get; init; }

    [JsonPropertyName("total")]
    public decimal Total { get; init; }

    [JsonPropertyName("metodo_pago")]
    public string MetodoPago { get; init; } = string.Empty;

    [JsonPropertyName("estado")]
    public string Estado { get; init; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("items")]
    public List<ItemVentaResponse> Items { get; init; } = new();

    /// <summary>
    /// Convierte un modelo <see cref="Venta"/> en su DTO de respuesta.
    /// </summary>
    public static VentaResponse FromModel(Venta venta) => new()
    {
        Id         = venta.Id,
        TurnoId    = venta.TurnoId,
        CajeroId   = venta.CajeroId,
        SucursalId = venta.SucursalId,
        TenantId   = venta.TenantId,
        Subtotal   = venta.Subtotal,
        Impuestos  = venta.Impuestos,
        Total      = venta.Total,
        MetodoPago = venta.MetodoPago.ToString(),
        Estado     = venta.Estado.ToString(),
        CreatedAt  = venta.CreatedAt,
        Items      = venta.Items.Select(i => ItemVentaResponse.FromModel(i)).ToList()
    };
}

/// <summary>
/// Ítem de línea dentro de la respuesta de venta.
/// </summary>
public sealed record ItemVentaResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("producto_id")]
    public Guid ProductoId { get; init; }

    [JsonPropertyName("nombre_producto")]
    public string NombreProducto { get; init; } = string.Empty;

    [JsonPropertyName("cantidad")]
    public decimal Cantidad { get; init; }

    [JsonPropertyName("peso_kg")]
    public decimal? PesoKg { get; init; }

    [JsonPropertyName("precio_unitario")]
    public decimal PrecioUnitario { get; init; }

    [JsonPropertyName("subtotal")]
    public decimal Subtotal { get; init; }

    [JsonPropertyName("iva")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? Iva { get; init; }

    [JsonPropertyName("total")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? Total { get; init; }

    public static ItemVentaResponse FromModel(Models.ItemVenta item) =>
        FromModel(item, null, null);

    public static ItemVentaResponse FromModel(Models.ItemVenta item, decimal? iva, decimal? total) => new()
    {
        Id             = item.Id,
        ProductoId     = item.ProductoId,
        NombreProducto = item.NombreProducto,
        Cantidad       = item.Cantidad,
        PesoKg         = item.PesoKg,
        PrecioUnitario = item.PrecioUnitario,
        Subtotal       = item.Subtotal,
        Iva            = iva,
        Total          = total
    };
}



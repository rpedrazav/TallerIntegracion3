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
/// 
/// CONTRATO S2-001:
/// - Campos aceptados del cliente: producto_id, cantidad, peso_kg.
/// - Campos calculados por MS-5: precio_unitario, nombre_producto, subtotal, iva, total.
/// Ningún precio o impuesto aportado por el renderer se considera autoridad.
/// </summary>
public sealed record ItemVentaRequest
{
    [JsonPropertyName("producto_id")]
    public Guid ProductoId { get; init; }

    /// <summary>
    /// Informativo / opcional. El nombre oficial es obtenido autoritativamente por MS-5 desde MS-3.
    /// </summary>
    [JsonPropertyName("nombre_producto")]
    public string NombreProducto { get; init; } = string.Empty;

    /// <summary>
    /// Cantidad a vender para productos unitarios (por defecto 1).
    /// Para pesables, peso_kg es obligatorio y cantidad, si se envía, debe coincidir.
    /// </summary>
    [JsonPropertyName("cantidad")]
    public decimal? Cantidad { get; init; }

    /// <summary>
    /// Solo para productos de peso variable (balanza). Null para productos por unidad.
    /// </summary>
    [JsonPropertyName("peso_kg")]
    public decimal? PesoKg { get; init; }

    /// <summary>
    /// Informativo / opcional. Ningún precio aportado por el cliente/renderer se considera autoridad.
    /// El precio real es resuelto autoritativamente por MS-5 desde MS-3 (CatalogPricingService).
    /// </summary>
    [JsonPropertyName("precio_unitario")]
    public decimal PrecioUnitario { get; init; } = 0m;
}

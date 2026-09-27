using System.Text.Json.Serialization;

namespace POSCartService.DTOs;

// ── Request para POST /ventas/{id}/items ──────────────────────────────────────

/// <summary>
/// Cuerpo del request para agregar un ítem a un carrito / venta pendiente.
/// El precio unitario y nombre se obtienen automáticamente consultando MS-3.
/// El IVA se calcula automáticamente consultando MS-2.
/// </summary>
public sealed record AgregarItemRequest
{
    [JsonPropertyName("producto_id")]
    public Guid ProductoId { get; init; }

    [JsonPropertyName("cantidad")]
    public decimal Cantidad { get; init; } = 1;

    /// <summary>
    /// Peso en kilogramos para productos de peso variable (balanza).
    /// Si se especifica, se toma como la cantidad a facturar.
    /// </summary>
    [JsonPropertyName("peso_kg")]
    public decimal? PesoKg { get; init; }
}

// ── Request para PUT /ventas/{id}/items/{itemId} ─────────────────────────────

/// <summary>
/// Cuerpo del request para modificar la cantidad o peso de un ítem en el carrito.
/// </summary>
public sealed record ModificarCantidadItemRequest
{
    [JsonPropertyName("cantidad")]
    public decimal Cantidad { get; init; }

    [JsonPropertyName("peso_kg")]
    public decimal? PesoKg { get; init; }
}

// ── DTOs de MS-3 (CatalogPricingService) ──────────────────────────────────────


/// <summary>
/// Modelo del producto obtenido desde MS-3 (GET /api/products/{id}).
/// </summary>
public sealed record ProductoCatalogDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;

    [JsonPropertyName("precioBase")]
    public decimal PrecioBase { get; init; }

    [JsonPropertyName("precio_base")]
    public decimal? PrecioBaseSnake { init { if (value.HasValue) PrecioBase = value.Value; } }

    [JsonPropertyName("esPesoVariable")]
    public bool EsPesoVariable { get; init; }

    [JsonPropertyName("es_peso_variable")]
    public bool? EsPesoVariableSnake { init { if (value.HasValue) EsPesoVariable = value.Value; } }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; init; } = true;

    [JsonPropertyName("is_active")]
    public bool? IsActiveSnake { init { if (value.HasValue) IsActive = value.Value; } }
}

// ── DTOs de MS-2 (TaxComplianceService) ───────────────────────────────────────

/// <summary>
/// Ítem para enviar en el cálculo de impuestos a MS-2 (POST /api/tax/calculate).
/// </summary>
public sealed record TaxItemDto
{
    [JsonPropertyName("nombre")]
    public string? Nombre { get; init; }

    [JsonPropertyName("precio")]
    public decimal Precio { get; init; }

    [JsonPropertyName("cantidad")]
    public decimal Cantidad { get; init; }

    [JsonPropertyName("exento")]
    public bool EsExento { get; init; }
}

/// <summary>
/// Resultado del cálculo fiscal retornado por MS-2.
/// </summary>
public sealed record TaxCalculationResult
{
    [JsonPropertyName("subtotal")]
    public decimal Subtotal { get; init; }

    [JsonPropertyName("porcentajeIva")]
    public decimal PorcentajeIva { get; init; }

    [JsonPropertyName("iva")]
    public decimal Iva { get; init; }

    [JsonPropertyName("total")]
    public decimal Total { get; init; }

    [JsonPropertyName("items")]
    public List<TaxItemBreakdownDto> Items { get; init; } = new();
}

/// <summary>
/// Desglose por ítem individual devuelto por MS-2.
/// </summary>
public sealed record TaxItemBreakdownDto
{
    [JsonPropertyName("nombre")]
    public string? Nombre { get; init; }

    [JsonPropertyName("precio")]
    public decimal Precio { get; init; }

    [JsonPropertyName("cantidad")]
    public decimal Cantidad { get; init; }

    [JsonPropertyName("subtotal")]
    public decimal Subtotal { get; init; }

    [JsonPropertyName("porcentajeIva")]
    public decimal PorcentajeIva { get; init; }

    [JsonPropertyName("iva")]
    public decimal Iva { get; init; }

    [JsonPropertyName("total")]
    public decimal Total { get; init; }

    [JsonPropertyName("exento")]
    public bool Exento { get; init; }
}

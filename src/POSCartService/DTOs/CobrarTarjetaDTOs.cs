using System.Text.Json.Serialization;

namespace POSCartService.DTOs;

/// <summary>
/// Cuerpo del request para POST /ventas/{id}/cobrar-tarjeta.
/// Permite enviar parámetros de control para entornos de prueba e idempotencia.
/// Cumple RN-02: Nunca recibe datos de tarjeta (PAN, CVV, PIN, etc.).
/// </summary>
public sealed record CobrarTarjetaRequest
{
    /// <summary>
    /// Clave de idempotencia única para este intento de cobro.
    /// Si no se especifica en el cuerpo, puede tomarse del header HTTP 'Idempotency-Key'
    /// o generarse automáticamente como un nuevo GUID. Si se proporciona debe ser
    /// un UUID no vacío; cuerpo y encabezado deben coincidir cuando están presentes.
    /// La repetición retorna 409 sin volver a autorizar en la pasarela.
    /// </summary>
    [JsonPropertyName("idempotency_key")]
    public string? IdempotencyKey { get; init; }

    /// <summary>
    /// Identificador del escenario de prueba para pasarela simulada (ej. 'sim-approved-v1', 'sim-declined-v1', 'sim-timeout-v1').
    /// Si es nulo o vacío, se utiliza el escenario por defecto ('sim-approved-v1').
    /// </summary>
    [JsonPropertyName("scenario_id")]
    public string? ScenarioId { get; init; }

    /// <summary>
    /// Monto a cobrar. Si se envía, debe coincidir exactamente con el total de la venta.
    /// </summary>
    [JsonPropertyName("monto")]
    public decimal? Monto { get; init; }
}

/// <summary>
/// Respuesta del endpoint POST /ventas/{id}/cobrar-tarjeta.
/// Retorna la venta y las referencias opacas del procesamiento de la pasarela.
/// </summary>
public sealed record CobrarTarjetaResponse(
    [property: JsonPropertyName("venta")] VentaResponse Venta,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("transaction_id")] string? TransactionId,
    [property: JsonPropertyName("authorization_code")] string? AuthorizationCode,
    [property: JsonPropertyName("gateway_reference")] string? GatewayReference,
    [property: JsonPropertyName("message")] string? Message
);

using System.Text.Json.Serialization;

namespace POSCartService.DTOs;

/// <summary>
/// Solicitud segura hacia la pasarela simulada. No contiene datos de tarjeta.
/// </summary>
public sealed record PaymentGatewayRequest
{
    [JsonPropertyName("request_id")]
    public Guid RequestId { get; init; }

    [JsonPropertyName("tenant_id")]
    public Guid TenantId { get; init; }

    [JsonPropertyName("sale_id")]
    public Guid SaleId { get; init; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }

    [JsonPropertyName("currency")]
    public string Currency { get; init; } = string.Empty;

    [JsonPropertyName("scenario_id")]
    public string ScenarioId { get; init; } = string.Empty;
}

public sealed record PaymentGatewayResponse
{
    [JsonPropertyName("request_id")]
    public Guid RequestId { get; init; }

    [JsonPropertyName("transaction_id")]
    public string TransactionId { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("authorization_code")]
    public string? AuthorizationCode { get; init; }

    [JsonPropertyName("gateway_reference")]
    public string? GatewayReference { get; init; }

    [JsonPropertyName("decline_code")]
    public string? DeclineCode { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("processed_at")]
    public DateTimeOffset ProcessedAt { get; init; }
}

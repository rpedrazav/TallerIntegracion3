using System.Text.Json.Serialization;

namespace POSCartService.DTOs;

public sealed record ReversalRequest(
    [property: JsonPropertyName("request_id")] Guid RequestId,
    [property: JsonPropertyName("tenant_id")] Guid TenantId,
    [property: JsonPropertyName("sale_id")] Guid SaleId,
    [property: JsonPropertyName("gateway_reference")] string GatewayReference,
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("scenario_id")] string ScenarioId);

public sealed record ReversalResponse(
    [property: JsonPropertyName("request_id")] Guid RequestId,
    [property: JsonPropertyName("tenant_id")] Guid TenantId,
    [property: JsonPropertyName("sale_id")] Guid SaleId,
    [property: JsonPropertyName("gateway_reference")] string GatewayReference,
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("reversal_reference")] string? ReversalReference,
    [property: JsonPropertyName("decline_code")] string? DeclineCode);

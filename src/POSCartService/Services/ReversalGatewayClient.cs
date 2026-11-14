using System.Net;
using System.Net.Http.Json;
using POSCartService.DTOs;
using POSCartService.Exceptions;

namespace POSCartService.Services;

public sealed class ReversalGatewayClient(IHttpClientFactory clients) : IReversalGatewayClient
{
    public async Task<ReversalResponse> ReverseAsync(ReversalRequest request)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "simulator/reversals") { Content = JsonContent.Create(request) };
        message.Headers.Add("Idempotency-Key", request.RequestId.ToString());
        using var response = await clients.CreateClient(PaymentGatewayClient.HttpClientName).SendAsync(message);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ReversalResponse>()
            ?? throw new ExternalServiceException("PaymentGateway", "Respuesta de reversa vacía.");
        Validate(result, request);
        return result;
    }

    public async Task<ReversalResponse?> GetStatusAsync(Guid tenantId, Guid requestId)
    {
        using var message = new HttpRequestMessage(HttpMethod.Get, $"simulator/reversals/{requestId}?tenant_id={tenantId}");
        message.Headers.Add("Idempotency-Key", requestId.ToString());
        using var response = await clients.CreateClient(PaymentGatewayClient.HttpClientName).SendAsync(message);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ReversalResponse>()
            ?? throw new ExternalServiceException("PaymentGateway", "Consulta de reversa vacía.");
    }

    internal static void Validate(ReversalResponse result, ReversalRequest request)
    {
        if (result.RequestId != request.RequestId || result.TenantId != request.TenantId || result.SaleId != request.SaleId
            || result.GatewayReference != request.GatewayReference || result.Amount != request.Amount || result.Currency != request.Currency
            || result.Status is not ("REVERSED" or "DECLINED")
            || (result.Status == "REVERSED" && (string.IsNullOrWhiteSpace(result.ReversalReference) || result.ReversalReference.Length > 200))
            || (result.Status == "DECLINED" && (string.IsNullOrWhiteSpace(result.DeclineCode) || result.DeclineCode.Length > 100)))
            throw new ExternalServiceException("PaymentGateway", "Respuesta de reversa inconsistente; consultar el intento existente.");
    }
}

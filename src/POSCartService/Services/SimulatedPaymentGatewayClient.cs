using POSCartService.DTOs;

namespace POSCartService.Services;

/// <summary>
/// Pasarela de pago y reversa 100% simulada en memoria para despliegues sin pod externo
/// de simulador (activada mediante PasarelaPago:ModoSimulado = true).
/// No procesa ni almacena datos sensibles de tarjeta (PCI DSS).
/// </summary>
public sealed class SimulatedPaymentGatewayClient : IPaymentGatewayClient, IReversalGatewayClient
{
    public Task<PaymentGatewayResponse> ProcessPaymentAsync(
        PaymentGatewayRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(BuildApprovedPayment(request.RequestId));
    }

    public Task<PaymentGatewayResponse?> GetPaymentStatusAsync(
        Guid tenantId,
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<PaymentGatewayResponse?>(BuildApprovedPayment(requestId));
    }

    public Task<ReversalResponse> ReverseAsync(ReversalRequest request)
    {
        return Task.FromResult(BuildApprovedReversal(request));
    }

    public Task<ReversalResponse?> GetStatusAsync(Guid tenantId, Guid requestId)
    {
        var token = requestId.ToString("N")[..8].ToUpperInvariant();
        return Task.FromResult<ReversalResponse?>(new ReversalResponse(
            RequestId: requestId,
            TenantId: tenantId,
            SaleId: Guid.Empty,
            GatewayReference: $"SIM-APPR-{token}",
            Amount: 0m,
            Currency: "CLP",
            Status: "APPROVED",
            ReversalReference: $"SIM-REV-{token}",
            DeclineCode: null));
    }

    private static PaymentGatewayResponse BuildApprovedPayment(Guid requestId)
    {
        var token = requestId.ToString("N")[..8].ToUpperInvariant();
        return new PaymentGatewayResponse
        {
            RequestId = requestId,
            TransactionId = $"sim-tx-{token.ToLowerInvariant()}",
            Status = "APPROVED",
            AuthorizationCode = $"SIM{token[..6]}",
            GatewayReference = $"SIM-APPR-{token}",
            DeclineCode = null,
            Message = "Pago aprobado en pasarela simulada.",
            ProcessedAt = DateTimeOffset.UtcNow
        };
    }

    private static ReversalResponse BuildApprovedReversal(ReversalRequest request)
    {
        var token = request.RequestId.ToString("N")[..8].ToUpperInvariant();
        return new ReversalResponse(
            RequestId: request.RequestId,
            TenantId: request.TenantId,
            SaleId: request.SaleId,
            GatewayReference: request.GatewayReference,
            Amount: request.Amount,
            Currency: request.Currency,
            Status: "APPROVED",
            ReversalReference: $"SIM-REV-{token}",
            DeclineCode: null);
    }
}

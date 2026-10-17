using POSCartService.DTOs;

namespace POSCartService.Services;

/// <summary>
/// Límite HTTP para procesar pagos mediante una pasarela externa o su simulador.
/// </summary>
public interface IPaymentGatewayClient
{
    Task<PaymentGatewayResponse> ProcessPaymentAsync(
        PaymentGatewayRequest request,
        CancellationToken cancellationToken = default);

    Task<PaymentGatewayResponse?> GetPaymentStatusAsync(
        Guid tenantId,
        Guid requestId,
        CancellationToken cancellationToken = default);
}

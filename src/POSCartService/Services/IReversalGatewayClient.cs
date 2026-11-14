using POSCartService.DTOs;

namespace POSCartService.Services;

/// <summary>Capacidad de reversa independiente del cobro; utiliza la misma pasarela configurada.</summary>
public interface IReversalGatewayClient
{
    Task<ReversalResponse> ReverseAsync(ReversalRequest request);
    Task<ReversalResponse?> GetStatusAsync(Guid tenantId, Guid requestId);
}

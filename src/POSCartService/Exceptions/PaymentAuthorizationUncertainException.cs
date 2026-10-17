namespace POSCartService.Exceptions;

/// <summary>
/// Indica que una autorización agotó su tiempo de espera y la consulta posterior
/// no permitió confirmar si la pasarela la procesó. No debe reintentarse con una
/// identidad nueva mientras el estado siga incierto.
/// </summary>
public sealed class PaymentAuthorizationUncertainException : TimeoutException
{
    public Guid TenantId { get; }
    public Guid RequestId { get; }

    public PaymentAuthorizationUncertainException(
        Guid tenantId,
        Guid requestId,
        Exception innerException)
        : base(
            $"La autorización no respondió y su estado no pudo confirmarse para RequestId={requestId}. No se debe iniciar otro cobro automáticamente.",
            innerException)
    {
        TenantId = tenantId;
        RequestId = requestId;
    }
}

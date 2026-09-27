namespace POSCartService.Exceptions;

/// <summary>
/// Se lanza cuando un microservicio externo (ej. MS-3 CatalogPricingService o MS-2 TaxComplianceService)
/// no responde o devuelve un código de error inesperado.
/// El controlador lo captura para retornar 502 Bad Gateway.
/// </summary>
public class ExternalServiceException : Exception
{
    public string ServiceName { get; }
    public int? StatusCode { get; }

    public ExternalServiceException(string serviceName, string message, int? statusCode = null, Exception? innerException = null)
        : base($"[{serviceName}] {message}", innerException)
    {
        ServiceName = serviceName;
        StatusCode = statusCode;
    }
}

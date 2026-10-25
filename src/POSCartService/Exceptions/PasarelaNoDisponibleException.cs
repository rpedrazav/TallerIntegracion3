namespace POSCartService.Exceptions;

/// <summary>
/// Excepción lanzada cuando la pasarela o el simulador de pagos experimenta
/// pérdida temporal de conectividad, caída o timeout (S2-060).
/// </summary>
public class PasarelaNoDisponibleException : Exception
{
    public string CorrelationId { get; }
    public string IntentoId { get; }

    public PasarelaNoDisponibleException(string mensaje, string correlationId, string intentoId, Exception? innerException = null)
        : base(mensaje, innerException)
    {
        CorrelationId = correlationId;
        IntentoId = intentoId;
    }
}

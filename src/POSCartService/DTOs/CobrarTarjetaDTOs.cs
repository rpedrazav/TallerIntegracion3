namespace POSCartService.DTOs;

/// <summary>
/// Solicitud para procesar cobro con tarjeta a través de la pasarela de pagos.
/// RN-02: Nunca recibe datos sensibles de tarjeta (PAN, CVV, fecha expiración).
/// Solo recibe identificadores opacos o tokens provistos por el terminal/cliente.
/// </summary>
public record CobrarTarjetaRequest
{
    /// <summary>
    /// Token opaco emitido por el terminal o pasarela externa (PCI-DSS compliant).
    /// </summary>
    public string? TokenTransaccion { get; init; }

    /// <summary>
    /// Identificador opaco de correlación para trazabilidad distribuida (S2-059).
    /// Si no se proporciona, el servicio generará uno automáticamente.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Modo de simulación para entornos de desarrollo y smoke tests en CI (S2-060):
    /// "APROBADO" | "RECHAZADO" | "TIMEOUT" | "INDISPONIBLE".
    /// </summary>
    public string? SimularResultado { get; init; }
}

/// <summary>
/// Respuesta exitosa de cobro con tarjeta.
/// </summary>
public record CobrarTarjetaResponse(
    VentaResponse Venta,
    string CorrelationId,
    string IntentoId,
    string? CodigoAutorizacion,
    string? ReferenciaExterna,
    string Estado = "APROBADO");

/// <summary>
/// Respuesta cuando el cobro con tarjeta es rechazado por la pasarela.
/// </summary>
public record CobroTarjetaRechazadoResponse(
    string Error,
    string CodigoRespuesta,
    string Motivo,
    string CorrelationId,
    string IntentoId,
    string Estado = "RECHAZADO");

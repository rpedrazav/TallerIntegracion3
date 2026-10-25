namespace POSCartService.Services;

public enum EstadoCobroPasarela
{
    APROBADO,
    RECHAZADO,
    ERROR_COMUNICACION
}

public class SolicitudCobroPasarela
{
    public Guid VentaId { get; set; }
    public Guid TenantId { get; set; }
    public decimal Monto { get; set; }
    public string Moneda { get; set; } = "CLP";
    public string? TokenTransaccion { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string IntentoId { get; set; } = string.Empty;
    public string? ModoSimulado { get; set; }
}

public class ResultadoCobroPasarela
{
    public EstadoCobroPasarela Estado { get; set; }
    public string? CodigoAutorizacion { get; set; }
    public string? ReferenciaExterna { get; set; }
    public string? CodigoRespuesta { get; set; }
    public string? Mensaje { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string IntentoId { get; set; } = string.Empty;
}

public interface IPasarelaPagoClient
{
    Task<ResultadoCobroPasarela> ProcesarCobroAsync(
        SolicitudCobroPasarela solicitud,
        CancellationToken cancellationToken = default);
}

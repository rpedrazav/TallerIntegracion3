using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace POSCartService.Services;

/// <summary>
/// Cliente para pasarela de pagos con trazabilidad correlacionada (S2-059)
/// y soporte para entorno reproducible / smoke tests (S2-060).
/// RN-02: Nunca procesa ni almacena datos de tarjeta (PAN, CVV) localmente.
/// </summary>
public class PasarelaPagoClient : IPasarelaPagoClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PasarelaPagoClient> _logger;

    public PasarelaPagoClient(
        IHttpClientFactory httpClientFactory,
        ILogger<PasarelaPagoClient> logger)
    {
        _httpClient = httpClientFactory.CreateClient("PasarelaPago");
        _logger = logger;
    }

    public async Task<ResultadoCobroPasarela> ProcesarCobroAsync(
        SolicitudCobroPasarela solicitud,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        // 1. Log de correlación al inicio del intento de cobro (IDs opacos, sin secretos)
        _logger.LogInformation(
            "[COBRO_CORRELATION] Evento=Inicio VentaId={VentaId} IntentoId={IntentoId} CorrelationId={CorrelationId} Monto={Monto} Moneda={Moneda}",
            solicitud.VentaId, solicitud.IntentoId, solicitud.CorrelationId, solicitud.Monto, solicitud.Moneda);

        // 2. Si se especifica ModoSimulado (para CI, staging o smoke tests S2-060)
        if (!string.IsNullOrWhiteSpace(solicitud.ModoSimulado))
        {
            return ProcesarSimulacion(solicitud, stopwatch);
        }

        // 3. Flujo HTTP hacia pasarela externa o simulador desplegado
        try
        {
            var payload = new
            {
                venta_id = solicitud.VentaId,
                tenant_id = solicitud.TenantId,
                monto = solicitud.Monto,
                moneda = solicitud.Moneda,
                token_transaccion = solicitud.TokenTransaccion,
                correlation_id = solicitud.CorrelationId,
                intento_id = solicitud.IntentoId
            };

            var requestContent = new StringContent(
                JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("/v1/charges", requestContent, cancellationToken);
            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;

                var codigoAuth = root.TryGetProperty("authorization_code", out var ac) ? ac.GetString() : $"AUTH-{Guid.NewGuid():N}"[..12].ToUpperInvariant();
                var rrn = root.TryGetProperty("rrn", out var r) ? r.GetString() : $"RRN-{DateTime.UtcNow.Ticks}";

                _logger.LogInformation(
                    "[COBRO_CORRELATION] Evento=Aprobado VentaId={VentaId} IntentoId={IntentoId} CorrelationId={CorrelationId} CodigoAutorizacion={CodigoAutorizacion} ReferenciaExterna={ReferenciaExterna} DuracionMs={DuracionMs}",
                    solicitud.VentaId, solicitud.IntentoId, solicitud.CorrelationId, codigoAuth, rrn, stopwatch.ElapsedMilliseconds);

                return new ResultadoCobroPasarela
                {
                    Estado = EstadoCobroPasarela.APROBADO,
                    CodigoAutorizacion = codigoAuth,
                    ReferenciaExterna = rrn,
                    CorrelationId = solicitud.CorrelationId,
                    IntentoId = solicitud.IntentoId,
                    Mensaje = "Cobro aprobado exitosamente"
                };
            }
            else
            {
                _logger.LogWarning(
                    "[COBRO_CORRELATION] Evento=Rechazado VentaId={VentaId} IntentoId={IntentoId} CorrelationId={CorrelationId} CodigoRespuesta={CodigoRespuesta} Motivo={Motivo} DuracionMs={DuracionMs}",
                    solicitud.VentaId, solicitud.IntentoId, solicitud.CorrelationId, ((int)response.StatusCode).ToString(), "Rechazo por pasarela", stopwatch.ElapsedMilliseconds);

                return new ResultadoCobroPasarela
                {
                    Estado = EstadoCobroPasarela.RECHAZADO,
                    CodigoRespuesta = ((int)response.StatusCode).ToString(),
                    Mensaje = "Cobro rechazado por la pasarela de pagos",
                    CorrelationId = solicitud.CorrelationId,
                    IntentoId = solicitud.IntentoId
                };
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "[COBRO_CORRELATION] Evento=ErrorComunicacion VentaId={VentaId} IntentoId={IntentoId} CorrelationId={CorrelationId} Error={Error} DuracionMs={DuracionMs}",
                solicitud.VentaId, solicitud.IntentoId, solicitud.CorrelationId, ex.Message, stopwatch.ElapsedMilliseconds);

            return new ResultadoCobroPasarela
            {
                Estado = EstadoCobroPasarela.ERROR_COMUNICACION,
                CodigoRespuesta = "ERR_GATEWAY_TIMEOUT",
                Mensaje = "Pérdida temporal de conexión con el simulador de pasarela",
                CorrelationId = solicitud.CorrelationId,
                IntentoId = solicitud.IntentoId
            };
        }
    }

    private ResultadoCobroPasarela ProcesarSimulacion(SolicitudCobroPasarela solicitud, Stopwatch stopwatch)
    {
        var modo = solicitud.ModoSimulado?.Trim().ToUpperInvariant();
        stopwatch.Stop();

        switch (modo)
        {
            case "APROBADO":
                var authCode = $"AUTH-{Guid.NewGuid():N}"[..12].ToUpperInvariant();
                var rrn = $"RRN-{DateTime.UtcNow.Ticks}";

                _logger.LogInformation(
                    "[COBRO_CORRELATION] Evento=Aprobado VentaId={VentaId} IntentoId={IntentoId} CorrelationId={CorrelationId} CodigoAutorizacion={CodigoAutorizacion} ReferenciaExterna={ReferenciaExterna} DuracionMs={DuracionMs}",
                    solicitud.VentaId, solicitud.IntentoId, solicitud.CorrelationId, authCode, rrn, stopwatch.ElapsedMilliseconds);

                return new ResultadoCobroPasarela
                {
                    Estado = EstadoCobroPasarela.APROBADO,
                    CodigoAutorizacion = authCode,
                    ReferenciaExterna = rrn,
                    CorrelationId = solicitud.CorrelationId,
                    IntentoId = solicitud.IntentoId,
                    Mensaje = "Cobro aprobado (simulado)"
                };

            case "RECHAZADO":
                _logger.LogWarning(
                    "[COBRO_CORRELATION] Evento=Rechazado VentaId={VentaId} IntentoId={IntentoId} CorrelationId={CorrelationId} CodigoRespuesta={CodigoRespuesta} Motivo={Motivo} DuracionMs={DuracionMs}",
                    solicitud.VentaId, solicitud.IntentoId, solicitud.CorrelationId, "REC-51", "Fondos insuficientes o tarjeta declinada", stopwatch.ElapsedMilliseconds);

                return new ResultadoCobroPasarela
                {
                    Estado = EstadoCobroPasarela.RECHAZADO,
                    CodigoRespuesta = "REC-51",
                    Mensaje = "Fondos insuficientes o tarjeta declinada",
                    CorrelationId = solicitud.CorrelationId,
                    IntentoId = solicitud.IntentoId
                };

            case "TIMEOUT":
            case "INDISPONIBLE":
            default:
                _logger.LogError(
                    "[COBRO_CORRELATION] Evento=ErrorComunicacion VentaId={VentaId} IntentoId={IntentoId} CorrelationId={CorrelationId} Error={Error} DuracionMs={DuracionMs}",
                    solicitud.VentaId, solicitud.IntentoId, solicitud.CorrelationId, "Pérdida temporal de conexión con el simulador de tarjeta", stopwatch.ElapsedMilliseconds);

                return new ResultadoCobroPasarela
                {
                    Estado = EstadoCobroPasarela.ERROR_COMUNICACION,
                    CodigoRespuesta = "ERR_SIMULATOR_DOWN",
                    Mensaje = "El simulador de pasarela no responde o no está disponible",
                    CorrelationId = solicitud.CorrelationId,
                    IntentoId = solicitud.IntentoId
                };
        }
    }
}

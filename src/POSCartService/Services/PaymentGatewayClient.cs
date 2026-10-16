using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using POSCartService.DTOs;
using POSCartService.Exceptions;

namespace POSCartService.Services;

/// <summary>
/// Adaptador HTTP de la pasarela. Envía solo referencias de negocio y propaga
/// RequestId como Idempotency-Key; nunca recibe ni transmite datos de tarjeta.
/// </summary>
public sealed class PaymentGatewayClient : IPaymentGatewayClient
{
    public const string HttpClientName = "PasarelaPago";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PaymentGatewayClient> _logger;

    public PaymentGatewayClient(
        IHttpClientFactory httpClientFactory,
        ILogger<PaymentGatewayClient> logger)
    {
        _httpClientFactory = httpClientFactory
            ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PaymentGatewayResponse> ProcessPaymentAsync(
        PaymentGatewayRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validate(request);

        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var message = new HttpRequestMessage(HttpMethod.Post, "simulator/payments")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Add("Idempotency-Key", request.RequestId.ToString());

        var startedAt = Stopwatch.GetTimestamp();
        _logger.LogInformation(
            "Iniciando pago por pasarela RequestId={RequestId} TenantId={TenantId} SaleId={SaleId} ScenarioId={ScenarioId}",
            request.RequestId,
            request.TenantId,
            request.SaleId,
            request.ScenarioId);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(message, cancellationToken);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            var elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
            _logger.LogWarning(
                "Timeout de pasarela RequestId={RequestId} TenantId={TenantId} SaleId={SaleId} ScenarioId={ScenarioId} Status={Status} ElapsedMs={ElapsedMs}",
                request.RequestId,
                request.TenantId,
                request.SaleId,
                request.ScenarioId,
                "TIMED_OUT",
                elapsedMs);
            throw new TimeoutException(
                $"La pasarela no respondió dentro del tiempo configurado para RequestId={request.RequestId}.",
                ex);
        }
        catch (HttpRequestException ex)
        {
            var elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
            _logger.LogError(
                ex,
                "Error HTTP de pasarela RequestId={RequestId} TenantId={TenantId} SaleId={SaleId} ScenarioId={ScenarioId} Status={Status} ElapsedMs={ElapsedMs}",
                request.RequestId,
                request.TenantId,
                request.SaleId,
                request.ScenarioId,
                "HTTP_ERROR",
                elapsedMs);
            throw new ExternalServiceException(
                "PaymentGateway",
                "No fue posible conectar con la pasarela.",
                innerException: ex);
        }

        using (response)
        {
            var elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Respuesta HTTP no exitosa de pasarela RequestId={RequestId} TenantId={TenantId} SaleId={SaleId} ScenarioId={ScenarioId} StatusCode={StatusCode} ElapsedMs={ElapsedMs}",
                    request.RequestId,
                    request.TenantId,
                    request.SaleId,
                    request.ScenarioId,
                    (int)response.StatusCode,
                    elapsedMs);
                throw new ExternalServiceException(
                    "PaymentGateway",
                    $"La pasarela devolvió HTTP {(int)response.StatusCode}.",
                    (int)response.StatusCode);
            }

            PaymentGatewayResponse? result;
            try
            {
                result = await response.Content.ReadFromJsonAsync<PaymentGatewayResponse>(
                    cancellationToken: cancellationToken);
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "Respuesta inválida de pasarela RequestId={RequestId} TenantId={TenantId} SaleId={SaleId} ScenarioId={ScenarioId}",
                    request.RequestId,
                    request.TenantId,
                    request.SaleId,
                    request.ScenarioId);
                throw new ExternalServiceException(
                    "PaymentGateway",
                    "La pasarela devolvió un payload inválido.",
                    innerException: ex);
            }

            if (result is null || result.RequestId != request.RequestId)
            {
                _logger.LogError(
                    "Respuesta inconsistente de pasarela RequestId={RequestId} TenantId={TenantId} SaleId={SaleId} ScenarioId={ScenarioId}",
                    request.RequestId,
                    request.TenantId,
                    request.SaleId,
                    request.ScenarioId);
                throw new ExternalServiceException(
                    "PaymentGateway",
                    "La pasarela devolvió una respuesta vacía o con request_id diferente.");
            }

            _logger.LogInformation(
                "Pago procesado por pasarela RequestId={RequestId} TenantId={TenantId} SaleId={SaleId} ScenarioId={ScenarioId} Status={Status} TransactionId={TransactionId} ElapsedMs={ElapsedMs}",
                request.RequestId,
                request.TenantId,
                request.SaleId,
                request.ScenarioId,
                result.Status,
                result.TransactionId,
                elapsedMs);

            return result;
        }
    }

    private static void Validate(PaymentGatewayRequest request)
    {
        if (request.RequestId == Guid.Empty)
            throw new ArgumentException("request_id es obligatorio.", nameof(request));
        if (request.TenantId == Guid.Empty)
            throw new ArgumentException("tenant_id es obligatorio.", nameof(request));
        if (request.SaleId == Guid.Empty)
            throw new ArgumentException("sale_id es obligatorio.", nameof(request));
        if (request.Amount <= 0)
            throw new ArgumentException("amount debe ser mayor que cero.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Currency))
            throw new ArgumentException("currency es obligatorio.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.ScenarioId))
            throw new ArgumentException("scenario_id es obligatorio.", nameof(request));
    }
}

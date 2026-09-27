using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using POSCartService.DTOs;
using POSCartService.Exceptions;

namespace POSCartService.Services;

/// <summary>
/// Implementación de <see cref="ITaxClient"/> para MS-2 (TaxComplianceService).
/// Soporta endpoints directos (/tax/calculate) y a través de Gateway (/api/tax/calculate).
/// </summary>
public class TaxClient : ITaxClient
{
    public const string HttpClientName = "TaxComplianceService";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TaxClient> _logger;

    public TaxClient(IHttpClientFactory httpClientFactory, ILogger<TaxClient> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<TaxCalculationResult?> CalculateTaxAsync(
        IEnumerable<TaxItemDto> items,
        decimal? porcentajeIva = null,
        string? bearerToken = null,
        CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);

        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            var token = bearerToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? bearerToken["Bearer ".Length..].Trim()
                : bearerToken;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var requestBody = new
        {
            items = items.Select(i => new
            {
                nombre = i.Nombre,
                precio = i.Precio,
                cantidad = i.Cantidad,
                exento = i.EsExento
            }),
            porcentajeIva
        };

        _logger.LogInformation("[TaxClient] Enviando cálculo de IVA a MS-2 ({BaseAddress})", client.BaseAddress);

        HttpResponseMessage response;
        try
        {
            // Intentar primero con la ruta de Gateway /api/tax/calculate
            response = await client.PostAsJsonAsync("api/tax/calculate", requestBody, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                // Fallback directo a /tax/calculate si se accede a MS-2 sin Gateway
                var directResponse = await client.PostAsJsonAsync("tax/calculate", requestBody, cancellationToken);
                if (directResponse.IsSuccessStatusCode)
                {
                    response = directResponse;
                }
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "[TaxClient] Error de red al solicitar cálculo a MS-2");
            throw new ExternalServiceException("MS-2 TaxComplianceService", $"Error de conexión con MS-2: {ex.Message}", innerException: ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("[TaxClient] MS-2 devolvió status {StatusCode}: {Content}", response.StatusCode, content);
            throw new ExternalServiceException("MS-2 TaxComplianceService", $"MS-2 devolvió código {(int)response.StatusCode}: {content}", (int)response.StatusCode);
        }

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = await response.Content.ReadFromJsonAsync<TaxCalculationResult>(options, cancellationToken);
        return result;
    }
}

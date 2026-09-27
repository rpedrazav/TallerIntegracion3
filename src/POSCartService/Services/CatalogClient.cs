using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using POSCartService.DTOs;
using POSCartService.Exceptions;

namespace POSCartService.Services;

/// <summary>
/// Implementación de <see cref="ICatalogClient"/> usando HttpClientFactory.
/// Soporta endpoints directos (/products/{id}) y a través de Gateway (/api/products/{id}).
/// </summary>
public class CatalogClient : ICatalogClient
{
    public const string HttpClientName = "CatalogPricingService";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CatalogClient> _logger;

    public CatalogClient(IHttpClientFactory httpClientFactory, ILogger<CatalogClient> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<ProductoCatalogDto?> GetProductAsync(
        Guid productId,
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

        _logger.LogInformation("[CatalogClient] Consultando producto {ProductId} en MS-3 ({BaseAddress})", productId, client.BaseAddress);

        HttpResponseMessage response;
        try
        {
            // Intentar primero con la ruta de Gateway /api/products/{id}
            response = await client.GetAsync($"api/products/{productId}", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                // Fallback directo a /products/{id} si se accede a MS-3 sin Gateway
                var directResponse = await client.GetAsync($"products/{productId}", cancellationToken);
                if (directResponse.IsSuccessStatusCode)
                {
                    response = directResponse;
                }
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "[CatalogClient] Error de red al consultar MS-3 para producto {ProductId}", productId);
            throw new ExternalServiceException("MS-3 CatalogPricingService", $"Error de conexión con MS-3: {ex.Message}", innerException: ex);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("[CatalogClient] Producto {ProductId} no encontrado en MS-3 (404)", productId);
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("[CatalogClient] MS-3 devolvió status {StatusCode}: {Content}", response.StatusCode, content);
            throw new ExternalServiceException("MS-3 CatalogPricingService", $"MS-3 devolvió código {(int)response.StatusCode}: {content}", (int)response.StatusCode);
        }

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var producto = await response.Content.ReadFromJsonAsync<ProductoCatalogDto>(options, cancellationToken);
        return producto;
    }
}

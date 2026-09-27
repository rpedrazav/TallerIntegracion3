using System.Net;
using System.Net.Http.Json;

namespace TaxComplianceService.Services;

/// <summary>
/// Cliente HTTP tipado que consume el endpoint GET /tenants/{id}/config de MS-1
/// para obtener el <c>porcentaje_iva</c> configurado para cada tenant.
/// Usa <see cref="IHttpClientFactory"/> para gestión correcta del ciclo de vida de los sockets.
/// </summary>
public class TenantConfigClient : ITenantConfigClient
{
    /// <summary>Nombre del cliente HTTP registrado en el contenedor DI.</summary>
    public const string HttpClientName = "TenantIdentityService";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TenantConfigClient> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="TenantConfigClient"/>.
    /// </summary>
    /// <param name="httpClientFactory">Factory para crear instancias de <see cref="HttpClient"/>.</param>
    /// <param name="logger">Logger para diagnóstico.</param>
    public TenantConfigClient(IHttpClientFactory httpClientFactory, ILogger<TenantConfigClient> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger            = logger            ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<TenantConfigResponse?> GetTenantConfigAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);

        _logger.LogInformation(
            "[TenantConfigClient] Consultando config de tenant {TenantId} en MS-1 ({BaseAddress})",
            tenantId, client.BaseAddress);

        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync($"tenants/{tenantId}/config", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "[TenantConfigClient] Error de red al consultar config del tenant {TenantId}", tenantId);
            throw;
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning(
                "[TenantConfigClient] Tenant {TenantId} no encontrado en MS-1 (404)", tenantId);
            return null;
        }

        response.EnsureSuccessStatusCode();

        var config = await response.Content.ReadFromJsonAsync<TenantConfigResponse>(
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "[TenantConfigClient] PorcentajeIva={PorcentajeIva} obtenido para tenant {TenantId}",
            config?.PorcentajeIva, tenantId);

        return config;
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;

namespace TaxComplianceService.Services;

/// <summary>
/// Cliente HTTP tipado que consume el endpoint GET /tenants/{id}/config de MS-1
/// para obtener el <c>porcentaje_iva</c> configurado para cada tenant.
/// Usa <see cref="IHttpClientFactory"/> para gestin correcta del ciclo de vida de los sockets.
/// </summary>
public class TenantConfigClient : ITenantConfigClient
{
    public const string HttpClientName = "TenantIdentityService";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TenantConfigClient> _logger;

    public TenantConfigClient(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, ILogger<TenantConfigClient> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger            = logger            ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TenantConfigResponse?> GetTenantConfigAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);

        // EXTRAER Y REENVIAR EL TOKEN JWT AL MS-1
        var authHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader.Substring("Bearer ".Length).Trim();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

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

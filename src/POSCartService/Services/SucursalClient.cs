using System.Net.Http.Headers;
using System.Net.Http.Json;
using POSCartService.Exceptions;

namespace POSCartService.Services;

public sealed class SucursalClient : ISucursalClient
{
    public const string HttpClientName = "TenantIdentityService";
    private readonly IHttpClientFactory _httpClientFactory;

    public SucursalClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<bool> IsActiveAsync(
        Guid sucursalId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "sucursales");
        request.Headers.Authorization = AuthenticationHeaderValue.Parse(bearerToken);

        HttpResponseMessage response;
        try
        {
            response = await _httpClientFactory.CreateClient(HttpClientName)
                .SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw Unavailable(ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw Unavailable(ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
                throw new ExternalServiceException(
                    "MS-1 TenantIdentityService",
                    $"No pudo confirmar la sucursal (HTTP {(int)response.StatusCode}).",
                    (int)response.StatusCode);

            try
            {
                var sucursales = await response.Content.ReadFromJsonAsync<List<SucursalEstado>>(
                    cancellationToken: cancellationToken);
                if (sucursales is null)
                    throw new System.Text.Json.JsonException("MS-1 no devolvió una lista de sucursales.");
                return sucursales.Any(s => s.Id == sucursalId && s.Activa);
            }
            catch (System.Text.Json.JsonException ex)
            {
                throw Unavailable(ex);
            }
        }
    }

    private static ExternalServiceException Unavailable(Exception ex) =>
        new("MS-1 TenantIdentityService", "No se pudo validar la sucursal.", innerException: ex);

    private sealed record SucursalEstado(Guid Id, bool Activa);
}

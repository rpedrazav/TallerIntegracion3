using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace WarehouseInventoryService.Services;

public sealed record RecepcionValidationFailure(int StatusCode, string Message);

public sealed class RecepcionValidator
{
    private readonly IHttpClientFactory _httpClients;
    private readonly IConfiguration _configuration;

    public RecepcionValidator(IHttpClientFactory httpClients, IConfiguration configuration)
    {
        _httpClients = httpClients;
        _configuration = configuration;
    }

    public async Task<RecepcionValidationFailure?> ValidateAsync(
        Guid productoId,
        Guid sucursalId,
        decimal cantidad,
        DateOnly fechaVencimiento,
        string? ubicacion,
        string bearer,
        CancellationToken cancellationToken)
    {
        if (productoId == Guid.Empty || sucursalId == Guid.Empty ||
            cantidad <= 0 || cantidad > 99_999_999 ||
            decimal.Round(cantidad, 4) != cantidad ||
            fechaVencimiento < DateOnly.FromDateTime(DateTime.UtcNow) ||
            ubicacion?.Trim().Length > 100)
            return new(400, "Producto, sucursal, cantidad, vencimiento o ubicación inválidos.");

        try
        {
            var client = _httpClients.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            using var productRequest = AuthorizedRequest(
                $"{_configuration["Services:CatalogUrl"]?.TrimEnd('/') ?? "http://localhost:5203"}/products/{productoId}",
                bearer);
            using var productResponse = await client.SendAsync(productRequest, cancellationToken);
            if (productResponse.StatusCode == HttpStatusCode.NotFound)
                return new(404, "Producto no encontrado.");
            if (!productResponse.IsSuccessStatusCode)
                return new(503, "No se pudo validar el producto.");

            return await ValidateSucursalAsync(client, sucursalId, bearer, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new(503, "No se pudo contactar identidad o catálogo.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new(503, "La validación de identidad o catálogo agotó el tiempo.");
        }
        catch (JsonException)
        {
            return new(503, "Identidad devolvió una respuesta inválida.");
        }
    }

    public async Task<RecepcionValidationFailure?> ValidateSucursalAsync(
        Guid sucursalId,
        string bearer,
        CancellationToken cancellationToken)
    {
        if (sucursalId == Guid.Empty)
            return new(400, "Sucursal inválida.");

        try
        {
            var client = _httpClients.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            return await ValidateSucursalAsync(client, sucursalId, bearer, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new(503, "No se pudo contactar identidad.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new(503, "La validación de identidad agotó el tiempo.");
        }
        catch (JsonException)
        {
            return new(503, "Identidad devolvió una respuesta inválida.");
        }
    }

    private async Task<RecepcionValidationFailure?> ValidateSucursalAsync(
        HttpClient client,
        Guid sucursalId,
        string bearer,
        CancellationToken cancellationToken)
    {
        using var request = AuthorizedRequest(
            $"{_configuration["Services:IdentityUrl"]?.TrimEnd('/') ?? "http://localhost:5124"}/sucursales",
            bearer);
        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return new(503, "No se pudo validar la sucursal.");

        using var branches = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (branches.RootElement.ValueKind != JsonValueKind.Array)
            return new(503, "Identidad devolvió una respuesta inválida.");
        var validBranch = branches.RootElement.EnumerateArray().Any(branch =>
            branch.TryGetProperty("id", out var id) && id.TryGetGuid(out var branchId) && branchId == sucursalId &&
            branch.TryGetProperty("activa", out var active) && active.ValueKind == JsonValueKind.True);
        return validBranch ? null : new(404, "Sucursal no encontrada o inactiva.");
    }

    private static HttpRequestMessage AuthorizedRequest(string url, string bearer)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = AuthenticationHeaderValue.Parse(bearer);
        return request;
    }
}

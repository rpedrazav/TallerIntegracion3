using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace GlobalMart.IntegrationTests;

/// <summary>
/// Pruebas de integración para validar el enrutamiento dual (con y sin prefijo /api)
/// requerido para la integración limpia entre Kong API Gateway y llamadas directas (TI3-658 / S2-027).
/// </summary>
public class RoutePrefixIntegrationTests :
    IClassFixture<Ms1WebApplicationFactory>,
    IClassFixture<Ms3WebApplicationFactory>,
    IClassFixture<Ms5WebApplicationFactory>
{
    private readonly HttpClient _clientMs1;
    private readonly HttpClient _clientMs3;
    private readonly HttpClient _clientMs5;

    public RoutePrefixIntegrationTests(
        Ms1WebApplicationFactory factoryMs1,
        Ms3WebApplicationFactory factoryMs3,
        Ms5WebApplicationFactory factoryMs5)
    {
        _clientMs1 = factoryMs1.CreateClient();
        _clientMs3 = factoryMs3.CreateClient();
        _clientMs5 = factoryMs5.CreateClient();
    }

    [Theory]
    [InlineData("/auth/login")]
    [InlineData("/api/auth/login")]
    public async Task Ms1_AuthLogin_BothRoutePrefixes_DoNotReturn404(string path)
    {
        var request = new
        {
            Email = "cajero@demo.cl",
            Password = "demo1234",
            TenantId = "aaaaaaaa-0000-0000-0000-000000000001"
        };

        var response = await _clientMs1.PostAsJsonAsync(path, request);

        // No debe retornar 404 Not Found (ambas rutas existen y responden exitosamente o con validación)
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/products")]
    [InlineData("/api/products")]
    public async Task Ms3_Products_BothRoutePrefixes_DoNotReturn404(string path)
    {
        // Sin token debe devolver 401 Unauthorized, NUNCA 404 Not Found
        var response = await _clientMs3.GetAsync(path);
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/categories")]
    [InlineData("/api/categories")]
    public async Task Ms3_Categories_BothRoutePrefixes_DoNotReturn404(string path)
    {
        // Sin token debe devolver 401 Unauthorized, NUNCA 404 Not Found
        var response = await _clientMs3.GetAsync(path);
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/turnos/activo")]
    [InlineData("/api/turnos/activo")]
    public async Task Ms5_Turnos_BothRoutePrefixes_DoNotReturn404(string path)
    {
        // Sin token debe devolver 401 Unauthorized, NUNCA 404 Not Found
        var response = await _clientMs5.GetAsync(path);
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/ventas")]
    [InlineData("/api/ventas")]
    public async Task Ms5_Ventas_BothRoutePrefixes_DoNotReturn404(string path)
    {
        // Sin token debe devolver 401 Unauthorized, NUNCA 404 Not Found
        var response = await _clientMs5.GetAsync(path);
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

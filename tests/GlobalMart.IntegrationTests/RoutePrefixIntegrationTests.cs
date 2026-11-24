using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
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

    private readonly Ms5WebApplicationFactory _factoryMs5;

    public RoutePrefixIntegrationTests(
        Ms1WebApplicationFactory factoryMs1,
        Ms3WebApplicationFactory factoryMs3,
        Ms5WebApplicationFactory factoryMs5)
    {
        _clientMs1 = factoryMs1.CreateClient();
        _clientMs3 = factoryMs3.CreateClient();
        _clientMs5 = factoryMs5.CreateClient();
        _factoryMs5 = factoryMs5;
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

    [Fact]
    public async Task Ms5_Turnos_CanonicalApiRoute_DoesNotReturn404()
    {
        // Sin token debe devolver 401 Unauthorized, NUNCA 404 Not Found
        var response = await _clientMs5.GetAsync("/api/turnos/activo");
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        // Alias /turnos/activo retirado debe responder 404 Not Found cuando la petición llega autenticada
        var token = CreateCajeroToken();
        using var retiredReq = new HttpRequestMessage(HttpMethod.Post, "/turnos/cerrar");
        retiredReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var retiredResponse = await _clientMs5.SendAsync(retiredReq);
        Assert.Equal(HttpStatusCode.NotFound, retiredResponse.StatusCode);
    }

    [Fact]
    public async Task Ms5_Ventas_CanonicalApiRoute_DoesNotReturn404()
    {
        // Sin token debe devolver 401 Unauthorized, NUNCA 404 Not Found
        var response = await _clientMs5.GetAsync("/api/ventas");
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        // Alias /ventas retirado debe responder 404 Not Found cuando la petición llega autenticada
        var token = CreateCajeroToken();
        using var retiredReq = new HttpRequestMessage(HttpMethod.Get, "/ventas");
        retiredReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var retiredResponse = await _clientMs5.SendAsync(retiredReq);
        Assert.Equal(HttpStatusCode.NotFound, retiredResponse.StatusCode);
    }

    private string CreateCajeroToken()
    {
        var configuration = _factoryMs5.Services.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        var key = configuration["Jwt:Key"] ?? "GlobalMartOS_SuperSecretKey_ChangeInProduction_Min32Chars!!";
        var issuer = configuration["Jwt:Issuer"] ?? "GlobalMartOS";
        var audience = configuration["Jwt:Audience"] ?? "GlobalMartOS_Clients";

        var claims = new[]
        {
            new Claim("tenant_id", "aaaaaaaa-0000-0000-0000-000000000001"),
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim("cajero_id", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "CAJERO")
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}

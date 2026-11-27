using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace GlobalMart.IntegrationTests;

public class AuthIntegrationTests : IClassFixture<Ms1WebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthIntegrationTests(Ms1WebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("demo")]
    [InlineData(" DEMO ")]
    public async Task Login_CodigoDeMinimarket_ReturnsJwtWithAllClaims(string codigo)
    {
        var request = new { Email = "cajero@demo.cl", Password = "demo1234", TenantCodigo = codigo };

        var response = await _client.PostAsJsonAsync("/auth/login", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(json.RootElement.GetProperty("token").GetString());

        Assert.Contains(jwt.Claims, claim => claim.Type == "tenant_id" && claim.Value == "aaaaaaaa-0000-0000-0000-000000000001");
        Assert.Contains(jwt.Claims, claim => claim.Type == "tenant_nombre" && claim.Value == "Minimarket Demo");
        Assert.Contains(jwt.Claims, claim => claim.Type == "sucursal_id");
        Assert.Contains(jwt.Claims, claim => claim.Type == "active_role" && claim.Value == "CAJERO");
        Assert.Contains(jwt.Claims, claim => claim.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" && claim.Value == "CAJERO");
    }

    [Theory]
    [InlineData("no-existe", "demo1234")]
    [InlineData("demo", "incorrecta")]
    public async Task Login_CodigoInexistenteOContrasenaIncorrecta_Retorna401Generico(string codigo, string password)
    {
        var request = new { Email = "cajero@demo.cl", Password = password, TenantCodigo = codigo };

        var response = await _client.PostAsJsonAsync("/auth/login", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Credenciales incorrectas.", json.RootElement.GetProperty("message").GetString());
    }
}

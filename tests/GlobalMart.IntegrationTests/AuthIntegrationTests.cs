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

    [Fact]
    public async Task Login_ValidTestUser_ReturnsJwtWithAllClaims()
    {
        var request = new
        {
            Email = "cajero@demo.cl",
            Password = "demo1234",
            TenantId = "aaaaaaaa-0000-0000-0000-000000000001"
        };

        var response = await _client.PostAsJsonAsync("/auth/login", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);

        var token = json.RootElement.TryGetProperty("token", out var tokenElement)
            ? tokenElement.GetString()
            : json.RootElement.TryGetProperty("access_token", out var accessTokenElement)
                ? accessTokenElement.GetString()
                : null;

        Assert.False(string.IsNullOrWhiteSpace(token));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token!);

        Assert.Contains(jwt.Claims, claim => claim.Type == "tenant_id" && claim.Value == "aaaaaaaa-0000-0000-0000-000000000001");
        Assert.Contains(jwt.Claims, claim => claim.Type == "sucursal_id");
        Assert.Contains(jwt.Claims, claim => claim.Type == "active_role" && claim.Value == "CAJERO");
        Assert.Contains(jwt.Claims, claim => claim.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" && claim.Value == "CAJERO");
    }
}

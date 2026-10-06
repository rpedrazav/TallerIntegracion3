extern alias TenantIdentityServiceAlias;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace GlobalMart.IntegrationTests;

public class UsuarioIntegrationTests : IClassFixture<Ms1WebApplicationFactory>
{
    private static readonly Guid TenantDemoId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    private readonly Ms1WebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UsuarioIntegrationTests(Ms1WebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUsers_SinJwt_Retorna401Unauthorized()
    {
        var response = await _client.GetAsync("/users");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_ConRolCajero_Retorna403Forbidden()
    {
        var token = CreateToken(TenantDemoId, Guid.NewGuid(), "CAJERO");
        var request = new HttpRequestMessage(HttpMethod.Get, "/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("/users")]
    [InlineData("/api/users")]
    [InlineData("/api/v1/users")]
    public async Task GetUsers_ConRolAdmin_RutasCompatiblesRetornanOkConListaDeUsuariosYRoles(string ruta)
    {
        var adminId = Guid.NewGuid();
        var token = CreateToken(TenantDemoId, adminId, "ADMIN");

        var request = new HttpRequestMessage(HttpMethod.Get, ruta);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);

        Assert.True(json.RootElement.TryGetProperty("items", out var itemsElement));
        Assert.True(itemsElement.GetArrayLength() >= 1);

        var primerUsuario = itemsElement[0];
        Assert.True(primerUsuario.TryGetProperty("id", out _));
        Assert.True(primerUsuario.TryGetProperty("nombre", out _));
        Assert.True(primerUsuario.TryGetProperty("email", out _));
        Assert.True(primerUsuario.TryGetProperty("activo", out var activoElement));
        Assert.True(activoElement.GetBoolean());
        Assert.True(primerUsuario.TryGetProperty("roles", out var rolesElement));
        Assert.True(rolesElement.GetArrayLength() > 0);
    }

    [Fact]
    public async Task GetUsers_AislamientoMultiTenant_NoRetornaUsuariosDeOtroTenant()
    {
        var otroTenantId = Guid.NewGuid();
        var token = CreateToken(otroTenantId, Guid.NewGuid(), "ADMIN");

        var request = new HttpRequestMessage(HttpMethod.Get, "/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);

        Assert.True(json.RootElement.TryGetProperty("items", out var itemsElement));
        Assert.Equal(0, itemsElement.GetArrayLength());
    }

    private string CreateToken(Guid tenantId, Guid userId, string role)
    {
        var configuration = _factory.Services.GetRequiredService<IConfiguration>();
        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key no configurada.");
        var issuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("JWT Issuer no configurado.");
        var audience = configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("JWT Audience no configurado.");

        var claims = new[]
        {
            new Claim("tenant_id", tenantId.ToString()),
            new Claim("sub", userId.ToString()),
            new Claim("active_role", role),
            new Claim(ClaimTypes.Role, role)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

extern alias TenantIdentityServiceAlias;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using TenantDbContext = TenantIdentityServiceAlias::TenantIdentityService.Data.TenantDbContext;
using Tenant = TenantIdentityServiceAlias::TenantIdentityService.Models.Tenant;

namespace GlobalMart.IntegrationTests;

/// <summary>
/// TI3-458 / TI3-459: Pruebas de integración para TenantConfigController.
/// Valida GET/PUT /tenants/{id}/config, autorización por rol ADMIN,
/// aislamiento de tenant y validaciones de esquema.
/// </summary>
public class TenantConfigIntegrationTests : IClassFixture<Ms1WebApplicationFactory>
{
    private readonly Ms1WebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TenantConfigIntegrationTests(Ms1WebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<Tenant> CreateTestTenantAsync(Guid tenantId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        db.Database.Migrate();

        var tenant = new Tenant
        {
            Id = tenantId,
            Nombre = $"Tenant Test Config {tenantId:N}",
            Pais = "CL",
            Moneda = "CLP",
            Idioma = "es",
            ZonaHoraria = "America/Santiago",
            PorcentajeIva = 19m,
            Activo = true
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        return tenant;
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

    [Fact]
    public async Task GetConfig_SinToken_Retorna401()
    {
        var tenantId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/tenants/{tenantId}/config");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetConfig_ConTenantDiferente_Retorna403()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        await CreateTestTenantAsync(tenantA);

        var token = CreateToken(tenantB, Guid.NewGuid(), "ADMIN");
        var request = new HttpRequestMessage(HttpMethod.Get, $"/tenants/{tenantA}/config");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetConfig_ConTokenValido_RetornaConfig()
    {
        var tenantId = Guid.NewGuid();
        await CreateTestTenantAsync(tenantId);

        var token = CreateToken(tenantId, Guid.NewGuid(), "CAJERO");
        var request = new HttpRequestMessage(HttpMethod.Get, $"/tenants/{tenantId}/config");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        Assert.Equal("CL", root.GetProperty("pais").GetString());
        Assert.Equal("CLP", root.GetProperty("moneda").GetString());
        Assert.Equal("es", root.GetProperty("idioma").GetString());
        Assert.Equal("America/Santiago", root.GetProperty("zonaHoraria").GetString());
        Assert.Equal(19m, root.GetProperty("porcentajeIva").GetDecimal());
    }

    [Fact]
    public async Task UpdateConfig_SinRolAdmin_Retorna403()
    {
        var tenantId = Guid.NewGuid();
        await CreateTestTenantAsync(tenantId);

        // Token con rol CAJERO, no ADMIN
        var token = CreateToken(tenantId, Guid.NewGuid(), "CAJERO");
        var request = new HttpRequestMessage(HttpMethod.Put, $"/tenants/{tenantId}/config")
        {
            Content = JsonContent.Create(new
            {
                pais = "AR",
                moneda = "ARS",
                idioma = "es",
                porcentajeIva = 21m,
                zonaHoraria = "America/Argentina/Buenos_Aires"
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateConfig_ConTenantDiferente_Retorna403()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        await CreateTestTenantAsync(tenantA);

        // Token de ADMIN de tenantB intentando editar tenantA
        var token = CreateToken(tenantB, Guid.NewGuid(), "ADMIN");
        var request = new HttpRequestMessage(HttpMethod.Put, $"/tenants/{tenantA}/config")
        {
            Content = JsonContent.Create(new
            {
                pais = "AR",
                moneda = "ARS",
                idioma = "es",
                porcentajeIva = 21m,
                zonaHoraria = "America/Argentina/Buenos_Aires"
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateConfig_ConDatosInvalidos_Retorna400BadRequest()
    {
        var tenantId = Guid.NewGuid();
        await CreateTestTenantAsync(tenantId);

        var token = CreateToken(tenantId, Guid.NewGuid(), "ADMIN");
        var request = new HttpRequestMessage(HttpMethod.Put, $"/tenants/{tenantId}/config")
        {
            Content = JsonContent.Create(new
            {
                pais = "XX", // País inválido
                moneda = "INVALID", // Moneda inválida
                idioma = "fr", // Idioma inválido
                porcentajeIva = 85m, // IVA > 50%
                zonaHoraria = "Fake/Timezone" // Zona inválida
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateConfig_ConAdminYDatosValidos_ActualizaConfigYRetorna200()
    {
        var tenantId = Guid.NewGuid();
        await CreateTestTenantAsync(tenantId);

        var token = CreateToken(tenantId, Guid.NewGuid(), "ADMIN");
        var updatePayload = new
        {
            pais = "PE",
            moneda = "PEN",
            idioma = "es",
            porcentajeIva = 18m,
            zonaHoraria = "America/Lima"
        };

        var request = new HttpRequestMessage(HttpMethod.Put, $"/tenants/{tenantId}/config")
        {
            Content = JsonContent.Create(updatePayload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        Assert.Equal("PE", root.GetProperty("pais").GetString());
        Assert.Equal("PEN", root.GetProperty("moneda").GetString());
        Assert.Equal("es", root.GetProperty("idioma").GetString());
        Assert.Equal("America/Lima", root.GetProperty("zonaHoraria").GetString());
        Assert.Equal(18m, root.GetProperty("porcentajeIva").GetDecimal());

        // Verificar en base de datos que se haya persistido
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        var tenantEnDb = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);

        Assert.NotNull(tenantEnDb);
        Assert.Equal("PE", tenantEnDb.Pais);
        Assert.Equal("PEN", tenantEnDb.Moneda);
        Assert.Equal("es", tenantEnDb.Idioma);
        Assert.Equal("America/Lima", tenantEnDb.ZonaHoraria);
        Assert.Equal(18m, tenantEnDb.PorcentajeIva);
    }
}

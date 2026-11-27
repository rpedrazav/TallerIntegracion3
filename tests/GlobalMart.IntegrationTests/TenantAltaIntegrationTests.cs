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
using Identity = TenantIdentityServiceAlias::TenantIdentityService;
using Xunit;

namespace GlobalMart.IntegrationTests;

/// <summary>S3-094: alta de minimarket (POST/GET /api/tenants) solo para SUPER_ADMIN.</summary>
public class TenantAltaIntegrationTests : IClassFixture<Ms1WebApplicationFactory>
{
    private static readonly Guid TenantDemoId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private const string Password = "Gm7!Alta2026";
    private readonly Ms1WebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TenantAltaIntegrationTests(Ms1WebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Crear_SuperAdmin_Retorna201YSuAdminIniciaSesionDeInmediato()
    {
        var codigo = CodigoUnico();

        var response = await SendAsync(HttpMethod.Post, "SUPER_ADMIN", Alta(codigo));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var creado = await response.Content.ReadFromJsonAsync<JsonElement>();
        var tenantId = creado.GetProperty("id").GetGuid();
        Assert.Equal(codigo, creado.GetProperty("codigo").GetString());

        var login = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = $"admin@{codigo}.cl", password = Password, tenantCodigo = codigo.ToUpperInvariant() });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var session = await login.Content.ReadFromJsonAsync<JsonElement>();
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(session.GetProperty("token").GetString());
        Assert.Contains(jwt.Claims, c => c.Type == "tenant_id" && c.Value == tenantId.ToString());
        Assert.Contains(jwt.Claims, c => c.Type == "tenant_nombre" && c.Value == $"Minimarket {codigo}");
        Assert.Contains(jwt.Claims, c => c.Type == "active_role" && c.Value == "ADMIN");
        Assert.Contains(jwt.Claims, c => c.Type == "sucursal_id" && Guid.TryParse(c.Value, out _));

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<Identity.Data.TenantDbContext>();
        var tenant = await db.Tenants.SingleAsync(t => t.Id == tenantId);
        Assert.Equal(("CL", "CLP", 19m), (tenant.Pais, tenant.Moneda, tenant.PorcentajeIva));

        var listado = await SendAsync(HttpMethod.Get, "SUPER_ADMIN");
        Assert.Equal(HttpStatusCode.OK, listado.StatusCode);
        var tenants = await listado.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(tenants.EnumerateArray(), t => t.GetProperty("id").GetGuid() == tenantId);
    }

    [Theory]
    [InlineData("ADMIN")]
    [InlineData("CAJERO")]
    public async Task CrearYListar_SinSuperAdmin_Retorna403SinCrear(string role)
    {
        var codigo = CodigoUnico();

        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Post, role, Alta(codigo))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Get, role)).StatusCode);
        Assert.False(await ExisteAsync(codigo));
    }

    [Fact]
    public async Task Crear_CodigoDuplicado_Retorna409()
    {
        var codigo = CodigoUnico();
        Assert.Equal(HttpStatusCode.Created, (await SendAsync(HttpMethod.Post, "SUPER_ADMIN", Alta(codigo))).StatusCode);

        var duplicado = Alta(codigo) with { Nombre = $"Otro {codigo}" };
        Assert.Equal(HttpStatusCode.Conflict, (await SendAsync(HttpMethod.Post, "SUPER_ADMIN", duplicado)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await SendAsync(HttpMethod.Post, "SUPER_ADMIN", Alta("demo"))).StatusCode);
    }

    [Fact]
    public async Task Crear_FalloAlGuardarElAdmin_NoDejaTenantNiSucursal()
    {
        var codigo = CodigoUnico();
        // Email de más de 200 caracteres: el validador lo frena en HTTP; aquí se salta para forzar
        // el fallo en el INSERT de Usuarios, después de los de Tenants y Sucursales.
        var request = Alta(codigo) with { AdminEmail = new string('a', 250) + "@demo.cl" };

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<Identity.Repositories.ITenantRepository>();
            await Assert.ThrowsAsync<DbUpdateException>(() => repository.CreateWithAdminAsync(request));
        }

        Assert.False(await ExisteAsync(codigo));
        await using var verify = _factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<Identity.Data.TenantDbContext>();
        Assert.False(await db.Sucursales.IgnoreQueryFilters().AnyAsync(s => s.Tenant.Nombre == request.Nombre));
    }

    private static Identity.DTOs.CrearTenantRequest Alta(string codigo) => new()
    {
        Nombre = $"Minimarket {codigo}",
        Codigo = codigo,
        AdminEmail = $"admin@{codigo}.cl",
        AdminPassword = Password
    };

    private static string CodigoUnico() => $"t{Guid.NewGuid():N}"[..20];

    private async Task<bool> ExisteAsync(string codigo)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<Identity.Data.TenantDbContext>();
        return await db.Tenants.AnyAsync(t => t.Codigo == codigo);
    }

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string role, object? body = null)
    {
        var request = new HttpRequestMessage(method, "/api/tenants");
        if (body is not null) request.Content = JsonContent.Create(body);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(role));
        return _client.SendAsync(request);
    }

    private string CreateToken(string role)
    {
        var configuration = _factory.Services.GetRequiredService<IConfiguration>();
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims:
            [
                new Claim("tenant_id", TenantDemoId.ToString()),
                new Claim("sub", Guid.NewGuid().ToString()),
                new Claim("active_role", role),
                new Claim(ClaimTypes.Role, role)
            ],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!)), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

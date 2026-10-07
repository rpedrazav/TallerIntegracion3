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

public class UserIntegrationTests : IClassFixture<Ms1WebApplicationFactory>
{
    private readonly Ms1WebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UserIntegrationTests(Ms1WebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostUsers_ConAdminJwt_HasheaPasswordConBcryptYPersisteUsuario()
    {
        // 1. Preparar Tenant en BD
        var tenantId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
            db.Database.Migrate();

            var tenant = new Tenant
            {
                Id = tenantId,
                Nombre = $"Tenant Test Users {tenantId:N}",
                Pais = "CL",
                Moneda = "CLP",
                Idioma = "es",
                ZonaHoraria = "America/Santiago",
                PorcentajeIva = 19
            };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
        }

        // 2. Generar JWT con rol ADMIN y claim tenant_id
        var configuration = _factory.Services.GetRequiredService<IConfiguration>();
        var token = CreateAdminToken(configuration, tenantId, adminId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var requestBody = new
        {
            Nombre = "Cajero Nuevo",
            Email = $"cajero_{Guid.NewGuid():N}@test.cl",
            Password = "PasswordSegura123!"
        };

        // 3. Ejecutar POST /api/v1/users
        var response = await _client.PostAsJsonAsync("/api/v1/users", requestBody);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(responseContent);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("id", out var idElement));
        var userId = idElement.GetGuid();

        Assert.Equal(requestBody.Nombre, root.GetProperty("nombre").GetString());
        Assert.Equal(requestBody.Email.ToLowerInvariant(), root.GetProperty("email").GetString());
        Assert.Equal(tenantId, root.GetProperty("tenantId").GetGuid());
        Assert.True(root.GetProperty("activo").GetBoolean());

        // 4. Verificar en Base de Datos: Password hasheado con BCrypt y asignación de tenant_id
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
            var usuarioEnDb = await db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);

            Assert.NotNull(usuarioEnDb);
            Assert.Equal(tenantId, usuarioEnDb.TenantId);
            Assert.Equal(requestBody.Nombre, usuarioEnDb.Nombre);
            Assert.Equal(requestBody.Email.ToLowerInvariant(), usuarioEnDb.Email);
            Assert.NotEqual(requestBody.Password, usuarioEnDb.PasswordHash);
            Assert.True(BCrypt.Net.BCrypt.Verify(requestBody.Password, usuarioEnDb.PasswordHash));
        }
    }

    [Fact]
    public async Task PostUsers_EmailDuplicadoEnMismoTenant_Retorna409Conflict()
    {
        var tenantId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
            db.Database.Migrate();

            var tenant = new Tenant
            {
                Id = tenantId,
                Nombre = $"Tenant Test Duplicate {tenantId:N}",
                Pais = "CL",
                Moneda = "CLP",
                Idioma = "es",
                ZonaHoraria = "America/Santiago",
                PorcentajeIva = 19
            };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
        }

        var configuration = _factory.Services.GetRequiredService<IConfiguration>();
        var token = CreateAdminToken(configuration, tenantId, adminId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var emailComun = $"duplicado_{Guid.NewGuid():N}@test.cl";

        var request1 = new
        {
            Nombre = "Primer Usuario",
            Email = emailComun,
            Password = "PasswordSegura123!"
        };

        var request2 = new
        {
            Nombre = "Segundo Usuario",
            Email = emailComun.ToUpperInvariant(),
            Password = "OtraPasswordSegura123!"
        };

        var response1 = await _client.PostAsJsonAsync("/api/v1/users", request1);
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);

        var response2 = await _client.PostAsJsonAsync("/api/v1/users", request2);
        Assert.Equal(HttpStatusCode.Conflict, response2.StatusCode);
    }

    [Fact]
    public async Task PostUsers_SinPasswordFuerte_Retorna400BadRequest()
    {
        var tenantId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        var configuration = _factory.Services.GetRequiredService<IConfiguration>();
        var token = CreateAdminToken(configuration, tenantId, adminId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var requestInvalido = new
        {
            Nombre = "Usuario Invalido",
            Email = "usuario@test.cl",
            Password = "123" // Menor a 8 caracteres, sin mayúscula/símbolo
        };

        var response = await _client.PostAsJsonAsync("/api/v1/users", requestInvalido);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostUsers_SinToken_Retorna401Unauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var request = new
        {
            Nombre = "Usuario Anonimo",
            Email = "anonimo@test.cl",
            Password = "PasswordSegura123!"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/users", request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static string CreateAdminToken(IConfiguration configuration, Guid tenantId, Guid adminId)
    {
        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key no configurada.");
        var issuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("JWT Issuer no configurado.");
        var audience = configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("JWT Audience no configurado.");

        var claims = new[]
        {
            new Claim("tenant_id", tenantId.ToString()),
            new Claim("sub", adminId.ToString()),
            new Claim("active_role", "ADMIN"),
            new Claim(ClaimTypes.Role, "ADMIN")
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

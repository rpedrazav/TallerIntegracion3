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

public class SucursalIntegrationTests : IClassFixture<Ms1WebApplicationFactory>
{
    private static readonly Guid TenantDemoId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private readonly Ms1WebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SucursalIntegrationTests(Ms1WebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task UpdateSucursal_AdminActualizaNombreYZonaHoraria_Retorna200()
    {
        var id = await CrearSucursalAsync(NombreUnico("Original"));
        var nombreActualizado = NombreUnico("Actualizada");

        var response = await PutAsync(id, "ADMIN", new
        {
            nombre = nombreActualizado,
            zonaHoraria = "America/Argentina/Buenos_Aires"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(id, body.RootElement.GetProperty("id").GetGuid());
        Assert.Equal(nombreActualizado, body.RootElement.GetProperty("nombre").GetString());
        Assert.Equal("America/Argentina/Buenos_Aires", body.RootElement.GetProperty("zonaHoraria").GetString());

        var listado = await GetSucursalesAsync();
        var actualizada = listado.EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == id);
        Assert.Equal(nombreActualizado, actualizada.GetProperty("nombre").GetString());
    }

    [Fact]
    public async Task UpdateSucursal_Cajero_Retorna403YNoModifica()
    {
        var nombre = NombreUnico("Protegida");
        var id = await CrearSucursalAsync(nombre);

        var response = await PutAsync(id, "CAJERO", new
        {
            nombre = NombreUnico("Prohibida"),
            zonaHoraria = "UTC"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var listado = await GetSucursalesAsync();
        Assert.Equal(nombre, listado.EnumerateArray()
            .Single(x => x.GetProperty("id").GetGuid() == id)
            .GetProperty("nombre").GetString());
    }

    [Fact]
    public async Task UpdateSucursal_DeOtroTenant_Retorna404YNoRevelaExistencia()
    {
        var otroTenantId = Guid.NewGuid();
        var sucursalId = Guid.NewGuid();
        var nombre = NombreUnico("Ajena");

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Identity.Data.TenantDbContext>();
            db.CurrentTenantId = otroTenantId;
            db.Tenants.Add(new Identity.Models.Tenant
            {
                Id = otroTenantId,
                Nombre = NombreUnico("Tenant"),
                Pais = "CL",
                Moneda = "CLP"
            });
            db.Sucursales.Add(new Identity.Models.Sucursal
            {
                Id = sucursalId,
                TenantId = otroTenantId,
                Nombre = nombre,
                Direccion = "Dirección de prueba"
            });
            await db.SaveChangesAsync();
        }

        var response = await PutAsync(sucursalId, "ADMIN", new
        {
            nombre = NombreUnico("Intrusión"),
            zonaHoraria = "UTC"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await using var verificationScope = _factory.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<Identity.Data.TenantDbContext>();
        var persistida = await verificationDb.Sucursales.IgnoreQueryFilters()
            .SingleAsync(s => s.Id == sucursalId);
        Assert.Equal(nombre, persistida.Nombre);
    }

    [Fact]
    public async Task UpdateSucursal_NombreDuplicadoSinDistinguirMayusculas_Retorna409()
    {
        var nombreOcupado = NombreUnico("Centro");
        await CrearSucursalAsync(nombreOcupado);
        var id = await CrearSucursalAsync(NombreUnico("Norte"));

        var response = await PutAsync(id, "ADMIN", new
        {
            nombre = nombreOcupado.ToUpperInvariant(),
            zonaHoraria = "America/Santiago"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UpdateSucursal_ConservaSuPropioNombre_Retorna200()
    {
        var nombre = NombreUnico("Propia");
        var id = await CrearSucursalAsync(nombre);

        var response = await PutAsync(id, "ADMIN", new
        {
            nombre,
            zonaHoraria = "UTC"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateSucursal_NombreExistenteEnOtroTenant_Retorna200()
    {
        var nombreCompartido = NombreUnico("Compartida");
        var otroTenantId = Guid.NewGuid();
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Identity.Data.TenantDbContext>();
            db.CurrentTenantId = otroTenantId;
            db.Tenants.Add(new Identity.Models.Tenant
            {
                Id = otroTenantId,
                Nombre = NombreUnico("Tenant"),
                Pais = "CL",
                Moneda = "CLP"
            });
            db.Sucursales.Add(new Identity.Models.Sucursal
            {
                TenantId = otroTenantId,
                Nombre = nombreCompartido,
                Direccion = "Dirección de prueba"
            });
            await db.SaveChangesAsync();
        }

        var id = await CrearSucursalAsync(NombreUnico("Local"));
        var response = await PutAsync(id, "ADMIN", new
        {
            nombre = nombreCompartido,
            zonaHoraria = "UTC"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateSucursal_ZonaHorariaInvalida_Retorna400()
    {
        var id = await CrearSucursalAsync(NombreUnico("Zona"));

        var response = await PutAsync(id, "ADMIN", new
        {
            nombre = NombreUnico("Zona válida"),
            zonaHoraria = "Chile/Continental"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateYListSucursales_RegresionConservaContrato()
    {
        var nombre = NombreUnico("Regresión");
        var id = await CrearSucursalAsync(nombre);

        var listado = await GetSucursalesAsync();

        var creada = listado.EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == id);
        Assert.Equal(nombre, creada.GetProperty("nombre").GetString());
        Assert.Equal("America/Santiago", creada.GetProperty("zonaHoraria").GetString());
    }

    private async Task<Guid> CrearSucursalAsync(string nombre)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/sucursales")
        {
            Content = JsonContent.Create(new
            {
                nombre,
                direccion = "Dirección de prueba",
                zonaHoraria = "America/Santiago"
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(TenantDemoId, "ADMIN"));

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("id").GetGuid();
    }

    private async Task<HttpResponseMessage> PutAsync(Guid id, string role, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/sucursales/{id}")
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(TenantDemoId, role));
        return await _client.SendAsync(request);
    }

    private async Task<JsonElement> GetSucursalesAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/sucursales");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(TenantDemoId, "ADMIN"));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }

    private string CreateToken(Guid tenantId, string role)
    {
        var configuration = _factory.Services.GetRequiredService<IConfiguration>();
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims:
            [
                new Claim("tenant_id", tenantId.ToString()),
                new Claim("sub", Guid.NewGuid().ToString()),
                new Claim("active_role", role),
                new Claim(ClaimTypes.Role, role)
            ],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string NombreUnico(string prefijo) => $"{prefijo} {Guid.NewGuid():N}";
}

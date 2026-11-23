extern alias POSCartServiceAlias;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Pos = POSCartServiceAlias::POSCartService;
using PosProgram = POSCartServiceAlias::Program;
using Xunit;

namespace GlobalMart.IntegrationTests;

public class SucursalOperativaIntegrationTests : IClassFixture<Ms5WebApplicationFactory>
{
    private static readonly Guid ProductId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private readonly Ms5WebApplicationFactory _factory;

    public SucursalOperativaIntegrationTests(Ms5WebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AbrirTurno_SucursalActiva_Retorna200()
    {
        var tenantId = Guid.NewGuid();
        var sucursalId = Guid.NewGuid();
        var sucursales = new StubSucursalClient();
        sucursales.SetActive(tenantId, sucursalId, true);
        using var factory = Configure(sucursales);
        using var client = CreateClient(factory, tenantId, Guid.NewGuid());

        var response = await AbrirTurnoAsync(client, sucursalId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("inactiva")]
    [InlineData("ajena")]
    [InlineData("inexistente")]
    public async Task AbrirTurno_SucursalNoDisponible_Retorna404SinPersistir(string escenario)
    {
        var tenantId = Guid.NewGuid();
        var cajeroId = Guid.NewGuid();
        var sucursalId = Guid.NewGuid();
        var sucursales = new StubSucursalClient();
        if (escenario == "ajena")
            sucursales.SetActive(Guid.NewGuid(), sucursalId, true);
        using var factory = Configure(sucursales);
        using var client = CreateClient(factory, tenantId, cajeroId);

        var response = await AbrirTurnoAsync(client, sucursalId);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Pos.Data.PosCartDbContext>();
        Assert.False(await db.Turnos.IgnoreQueryFilters().AnyAsync(t => t.CajeroId == cajeroId));
    }

    [Fact]
    public async Task AbrirTurno_Ms1NoDisponible_Retorna503SinPersistir()
    {
        var tenantId = Guid.NewGuid();
        var cajeroId = Guid.NewGuid();
        var sucursales = new StubSucursalClient { Unavailable = true };
        using var factory = Configure(sucursales);
        using var client = CreateClient(factory, tenantId, cajeroId);

        var response = await AbrirTurnoAsync(client, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Pos.Data.PosCartDbContext>();
        Assert.False(await db.Turnos.IgnoreQueryFilters().AnyAsync(t => t.CajeroId == cajeroId));
    }

    [Fact]
    public async Task CrearVenta_SucursalActiva_Retorna201()
    {
        var tenantId = Guid.NewGuid();
        var sucursalId = Guid.NewGuid();
        var sucursales = new StubSucursalClient();
        sucursales.SetActive(tenantId, sucursalId, true);
        using var factory = Configure(sucursales);
        using var client = CreateClient(factory, tenantId, Guid.NewGuid());
        Assert.Equal(HttpStatusCode.OK, (await AbrirTurnoAsync(client, sucursalId)).StatusCode);

        var response = await CrearVentaAsync(client);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CrearVenta_SucursalDesactivadaDespuesDeAbrir_Retorna404SinCrearVenta()
    {
        var tenantId = Guid.NewGuid();
        var cajeroId = Guid.NewGuid();
        var sucursalId = Guid.NewGuid();
        var sucursales = new StubSucursalClient();
        sucursales.SetActive(tenantId, sucursalId, true);
        using var factory = Configure(sucursales);
        using var client = CreateClient(factory, tenantId, cajeroId);
        Assert.Equal(HttpStatusCode.OK, (await AbrirTurnoAsync(client, sucursalId)).StatusCode);
        sucursales.SetActive(tenantId, sucursalId, false);

        var response = await CrearVentaAsync(client);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Pos.Data.PosCartDbContext>();
        Assert.False(await db.Ventas.IgnoreQueryFilters().AnyAsync(v => v.CajeroId == cajeroId));
    }

    [Fact]
    public async Task CrearVenta_Ms1NoDisponibleDespuesDeAbrir_Retorna503SinCrearVenta()
    {
        var tenantId = Guid.NewGuid();
        var cajeroId = Guid.NewGuid();
        var sucursalId = Guid.NewGuid();
        var sucursales = new StubSucursalClient();
        sucursales.SetActive(tenantId, sucursalId, true);
        using var factory = Configure(sucursales);
        using var client = CreateClient(factory, tenantId, cajeroId);
        Assert.Equal(HttpStatusCode.OK, (await AbrirTurnoAsync(client, sucursalId)).StatusCode);
        sucursales.Unavailable = true;

        var response = await CrearVentaAsync(client);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Pos.Data.PosCartDbContext>();
        Assert.False(await db.Ventas.IgnoreQueryFilters().AnyAsync(v => v.CajeroId == cajeroId));
    }

    [Fact]
    public async Task SucursalClient_ConsultaListadoAutorizadoYPropagaBearer()
    {
        var sucursalId = Guid.NewGuid();
        var handler = new IdentityHandler(sucursalId);
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://identity/") };
        var sut = new Pos.Services.SucursalClient(new SingleClientFactory(client));

        var active = await sut.IsActiveAsync(sucursalId, "Bearer token-prueba");

        Assert.True(active);
        Assert.Equal("/sucursales", handler.RequestPath);
        Assert.Equal("Bearer token-prueba", handler.Authorization);
    }

    private WebApplicationFactory<PosProgram> Configure(StubSucursalClient sucursales) =>
        _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<Pos.Services.ISucursalClient>();
            services.AddSingleton<Pos.Services.ISucursalClient>(sucursales);
            services.AddHttpClient(Pos.Services.CatalogClient.HttpClientName)
                .ConfigurePrimaryHttpMessageHandler(() => new CatalogHandler());
            services.AddHttpClient(Pos.Services.TaxClient.HttpClientName)
                .ConfigurePrimaryHttpMessageHandler(() => new TaxHandler());
        }));

    private static HttpClient CreateClient(
        WebApplicationFactory<PosProgram> factory,
        Guid tenantId,
        Guid cajeroId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateToken(factory.Services.GetRequiredService<IConfiguration>(), tenantId, cajeroId));
        return client;
    }

    private static Task<HttpResponseMessage> AbrirTurnoAsync(HttpClient client, Guid sucursalId) =>
        client.PostAsJsonAsync("/api/turnos/abrir", new
        {
            sucursal_id = sucursalId,
            monto_fondo_inicial = 10_000m
        });

    private static Task<HttpResponseMessage> CrearVentaAsync(HttpClient client) =>
        client.PostAsJsonAsync("/api/ventas", new
        {
            items = new[] { new { producto_id = ProductId, cantidad = 1m } },
            metodo_pago = "EFECTIVO"
        });

    private static string CreateToken(IConfiguration configuration, Guid tenantId, Guid cajeroId)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!)),
            SecurityAlgorithms.HmacSha256);
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims:
            [
                new Claim("tenant_id", tenantId.ToString()),
                new Claim("sub", cajeroId.ToString()),
                new Claim("cajero_id", cajeroId.ToString()),
                new Claim(ClaimTypes.Role, "CAJERO")
            ],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials));
    }

    private sealed class StubSucursalClient : Pos.Services.ISucursalClient
    {
        private readonly HashSet<(Guid TenantId, Guid SucursalId)> _active = new();
        public bool Unavailable { get; set; }

        public void SetActive(Guid tenantId, Guid sucursalId, bool active)
        {
            if (active) _active.Add((tenantId, sucursalId));
            else _active.Remove((tenantId, sucursalId));
        }

        public Task<bool> IsActiveAsync(Guid sucursalId, string bearerToken, CancellationToken cancellationToken = default)
        {
            if (Unavailable)
                throw new Pos.Exceptions.ExternalServiceException("MS-1", "No disponible.");
            var token = new JwtSecurityTokenHandler().ReadJwtToken(bearerToken["Bearer ".Length..]);
            var tenantId = Guid.Parse(token.Claims.Single(c => c.Type == "tenant_id").Value);
            return Task.FromResult(_active.Contains((tenantId, sucursalId)));
        }
    }

    private sealed class CatalogHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    id = ProductId,
                    nombre = "Producto prueba",
                    precioBase = 100m,
                    esPesoVariable = false,
                    isActive = true
                })
            });
    }

    private sealed class TaxHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    subtotal = 100m,
                    porcentajeIva = 19m,
                    iva = 19m,
                    total = 119m,
                    items = new[]
                    {
                        new { nombre = "Producto prueba", precio = 100m, cantidad = 1m, subtotal = 100m, porcentajeIva = 19m, iva = 19m, total = 119m, exento = false }
                    }
                })
            });
    }

    private sealed class IdentityHandler(Guid activeId) : HttpMessageHandler
    {
        public string? RequestPath { get; private set; }
        public string? Authorization { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestPath = request.RequestUri?.AbsolutePath;
            Authorization = request.Headers.Authorization?.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new[] { new { id = activeId, activa = true } })
            });
        }
    }

    private sealed class SingleClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }
}

extern alias POSCartServiceAlias;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using CatalogClient = POSCartServiceAlias::POSCartService.Services.CatalogClient;
using ICatalogClient = POSCartServiceAlias::POSCartService.Services.ICatalogClient;
using PosCartDbContext = POSCartServiceAlias::POSCartService.Data.PosCartDbContext;
using EstadoVenta = POSCartServiceAlias::POSCartService.Models.EstadoVenta;
using IKafkaProducerService = POSCartServiceAlias::POSCartService.Services.IKafkaProducerService;
using ITaxClient = POSCartServiceAlias::POSCartService.Services.ITaxClient;
using ITurnoService = POSCartServiceAlias::POSCartService.Services.ITurnoService;
using SaleCompletedEvent = POSCartServiceAlias::POSCartService.Messaging.SaleCompletedEvent;
using MetodoPago = POSCartServiceAlias::POSCartService.Models.MetodoPago;
using TaxClient = POSCartServiceAlias::POSCartService.Services.TaxClient;
using IVentaService = POSCartServiceAlias::POSCartService.Services.IVentaService;
using VentasController = POSCartServiceAlias::POSCartService.Controllers.VentasController;
using Xunit;

namespace GlobalMart.IntegrationTests;

public class CobroIntegrationTests : IClassFixture<Ms5WebApplicationFactory>
{
    private static readonly Guid ProductOneId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ProductTwoId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly Ms5WebApplicationFactory _factory;

    public CobroIntegrationTests(Ms5WebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CobrarEfectivo_MontoMayorAlTotal_CalculaVueltoYCompletaVenta()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        var tenantId = Guid.NewGuid();
        var cajeroId = Guid.NewGuid();
        var sucursalId = Guid.NewGuid();
        const decimal montoRecibido = 350m;
        const decimal vueltoEsperado = 52.50m;

        var configuredFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddControllers().AddControllersAsServices();
                services.Configure<MvcOptions>(options => options.ModelValidatorProviders.Clear());
                services.AddSingleton<IKafkaProducerService, NoOpKafkaProducerService>();
                services.AddTransient<VentasController>(serviceProvider =>
                    new VentasController(
                        serviceProvider.GetRequiredService<IVentaService>(),
                        serviceProvider.GetRequiredService<ITurnoService>(),
                        serviceProvider.GetRequiredService<ICatalogClient>(),
                        serviceProvider.GetRequiredService<ITaxClient>(),
                        serviceProvider.GetRequiredService<ILogger<VentasController>>()));
                services.AddHttpClient(CatalogClient.HttpClientName)
                    .ConfigurePrimaryHttpMessageHandler(() => new CatalogStubHandler());
                services.AddHttpClient(TaxClient.HttpClientName)
                    .ConfigurePrimaryHttpMessageHandler(() => new TaxStubHandler());
            });
        });

        var configuration = configuredFactory.Services.GetRequiredService<IConfiguration>();
        var token = CreateCajeroToken(configuration, tenantId, cajeroId);

        using (var migrationScope = configuredFactory.Services.CreateScope())
        {
            var migrationDb = migrationScope.ServiceProvider.GetRequiredService<PosCartDbContext>();
            await migrationDb.Database.MigrateAsync();
        }

        using var client = configuredFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var abrirTurnoResponse = await client.PostAsJsonAsync("/turnos/abrir", new
        {
            sucursal_id = sucursalId,
            monto_fondo_inicial = 50000m
        });
        Assert.Equal(HttpStatusCode.OK, abrirTurnoResponse.StatusCode);

        var crearVentaResponse = await client.PostAsJsonAsync("/ventas", new
        {
            items = new[]
            {
                new
                {
                    producto_id = ProductOneId,
                    nombre_producto = "Producto uno",
                    cantidad = 2m,
                    precio_unitario = 100m
                }
            },
            metodo_pago = "EFECTIVO"
        });
        Assert.Equal(HttpStatusCode.Created, crearVentaResponse.StatusCode);

        var ventaId = await ReadGuidPropertyAsync(crearVentaResponse, "id");

        var agregarItemResponse = await client.PostAsJsonAsync($"/ventas/{ventaId}/items", new
        {
            producto_id = ProductTwoId,
            cantidad = 1m
        });
        Assert.Equal(HttpStatusCode.Created, agregarItemResponse.StatusCode);

        var cobrarResponse = await client.PostAsJsonAsync($"/ventas/{ventaId}/cobrar", new
        {
            monto_recibido = montoRecibido
        });
        Assert.Equal(HttpStatusCode.OK, cobrarResponse.StatusCode);

        using var scope = configuredFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PosCartDbContext>();
        var venta = await db.Ventas
            .Include(v => v.Pagos)
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(v => v.Id == ventaId);

        Assert.NotNull(venta);
        Assert.Equal(EstadoVenta.COMPLETADA, venta!.Estado);

        var pago = Assert.Single(venta.Pagos);
        Assert.Equal(MetodoPago.EFECTIVO, pago.Metodo);
        Assert.Equal(montoRecibido, pago.Monto);
        Assert.Equal(vueltoEsperado, pago.Vuelto);
    }

    private static async Task<Guid> ReadGuidPropertyAsync(HttpResponseMessage response, string propertyName)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty(propertyName).GetGuid();
    }

    private static string CreateCajeroToken(IConfiguration configuration, Guid tenantId, Guid cajeroId)
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
            new Claim("sub", cajeroId.ToString()),
            new Claim("cajero_id", cajeroId.ToString()),
            new Claim(ClaimTypes.Role, "CAJERO")
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

    private sealed class CatalogStubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var productId = Guid.Parse(request.RequestUri!.Segments[^1]);
            var response = productId == ProductOneId
                ? new { id = ProductOneId, nombre = "Producto uno", precioBase = 100m, esPesoVariable = false, isActive = true }
                : productId == ProductTwoId
                    ? new { id = ProductTwoId, nombre = "Producto dos", precioBase = 50m, esPesoVariable = false, isActive = true }
                    : null;

            return Task.FromResult(response is null
                ? new HttpResponseMessage(HttpStatusCode.NotFound)
                : JsonResponse(HttpStatusCode.OK, response));
        }
    }

    private sealed class TaxStubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new
            {
                subtotal = 250m,
                porcentajeIva = 19m,
                iva = 47.50m,
                total = 297.50m,
                items = new[]
                {
                    new { nombre = "Producto uno", precio = 100m, cantidad = 2m, subtotal = 200m, porcentajeIva = 19m, iva = 38m, total = 238m, exento = false },
                    new { nombre = "Producto dos", precio = 50m, cantidad = 1m, subtotal = 50m, porcentajeIva = 19m, iva = 9.50m, total = 59.50m, exento = false }
                }
            };

            return Task.FromResult(JsonResponse(HttpStatusCode.OK, response));
        }
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, object value)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = JsonContent.Create(value)
        };
    }

    private sealed class NoOpKafkaProducerService : IKafkaProducerService
    {
        public Task PublicarAsync(
            string topico,
            string mensaje,
            string? clave = null,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task PublicarSaleCompletedAsync(
            SaleCompletedEvent evento,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}

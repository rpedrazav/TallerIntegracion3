extern alias POSCartServiceAlias;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using CatalogClient = POSCartServiceAlias::POSCartService.Services.CatalogClient;
using ICatalogClient = POSCartServiceAlias::POSCartService.Services.ICatalogClient;
using PosCartDbContext = POSCartServiceAlias::POSCartService.Data.PosCartDbContext;
using ITaxClient = POSCartServiceAlias::POSCartService.Services.ITaxClient;
using ITurnoService = POSCartServiceAlias::POSCartService.Services.ITurnoService;
using TaxClient = POSCartServiceAlias::POSCartService.Services.TaxClient;
using IVentaService = POSCartServiceAlias::POSCartService.Services.IVentaService;
using Venta = POSCartServiceAlias::POSCartService.Models.Venta;
using VentasController = POSCartServiceAlias::POSCartService.Controllers.VentasController;
using Xunit;

namespace GlobalMart.IntegrationTests;

public class VentaIntegrationTests : IClassFixture<Ms5WebApplicationFactory>
{
    private static readonly Guid ProductOneId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ProductTwoId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly Ms5WebApplicationFactory _factory;

    public VentaIntegrationTests(Ms5WebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CrearVenta_AgregarDosProductos_CalculaTotalConIvaCorrectamente()
    {
        var tenantId = Guid.NewGuid();
        var cajeroId = Guid.NewGuid();
        var sucursalId = Guid.NewGuid();

        var configuredFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddControllers().AddControllersAsServices();
                services.AddTransient<VentasController>(serviceProvider =>
                    new VentasController(
                        serviceProvider.GetRequiredService<IVentaService>(),
                        serviceProvider.GetRequiredService<ITurnoService>(),
                        serviceProvider.GetRequiredService<ICatalogClient>(),
                        serviceProvider.GetRequiredService<ITaxClient>(),
                        serviceProvider.GetRequiredService<ILogger<VentasController>>()));
                services.AddHttpClient(CatalogClient.HttpClientName)
                    .ConfigurePrimaryHttpMessageHandler(() => new CatalogStubHandler(ProductOneId, ProductTwoId));
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

        var turnoId = await ReadGuidPropertyAsync(abrirTurnoResponse, "id");

        var crearVentaResponse = await client.PostAsJsonAsync("/ventas", new
        {
            items = new[]
            {
                new
                {
                    producto_id = ProductOneId,
                    cantidad = 2m
                }
            },
            metodo_pago = "EFECTIVO"
        });
        Assert.Equal(HttpStatusCode.Created, crearVentaResponse.StatusCode);

        var ventaId = await ReadGuidPropertyAsync(crearVentaResponse, "id");
        var ventaTurnoId = await ReadGuidPropertyAsync(crearVentaResponse, "turno_id");
        Assert.Equal(turnoId, ventaTurnoId);

        // Verificar que la venta creada ya tiene subtotal, impuestos y total calculados desde MS-3 y MS-2
        using (var initialScope = configuredFactory.Services.CreateScope())
        {
            var initialDb = initialScope.ServiceProvider.GetRequiredService<PosCartDbContext>();
            var initialVenta = await initialDb.Ventas
                .IgnoreQueryFilters()
                .Include(v => v.Items)
                .SingleOrDefaultAsync(v => v.Id == ventaId);

            Assert.NotNull(initialVenta);
            Assert.Single(initialVenta.Items);
            var itemInicial = initialVenta.Items.First();
            Assert.Equal("Producto uno", itemInicial.NombreProducto);
            Assert.Equal(100m, itemInicial.PrecioUnitario);
            Assert.Equal(200m, initialVenta.Subtotal);
            Assert.Equal(38m, initialVenta.Impuestos);
            Assert.Equal(238m, initialVenta.Total);
        }

        var agregarItemResponse = await client.PostAsJsonAsync($"/ventas/{ventaId}/items", new
        {
            producto_id = ProductTwoId,
            cantidad = 1m
        });
        Assert.Equal(HttpStatusCode.Created, agregarItemResponse.StatusCode);

        using var scope = configuredFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PosCartDbContext>();
        Venta? venta = await db.Ventas
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(v => v.Id == ventaId);

        Assert.NotNull(venta);
        Assert.Equal(250m, venta.Subtotal);
        Assert.Equal(47.50m, venta.Impuestos);
        Assert.Equal(297.50m, venta.Total);
    }

    [Fact]
    public async Task CrearVenta_ProductoInexistente_Retorna404SinPersistir()
    {
        var tenantId = Guid.NewGuid();
        var cajeroId = Guid.NewGuid();
        var sucursalId = Guid.NewGuid();
        var productoInexistenteId = Guid.Parse("99999999-9999-9999-9999-999999999999");

        var configuredFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddControllers().AddControllersAsServices();
                services.AddTransient<VentasController>(serviceProvider =>
                    new VentasController(
                        serviceProvider.GetRequiredService<IVentaService>(),
                        serviceProvider.GetRequiredService<ITurnoService>(),
                        serviceProvider.GetRequiredService<ICatalogClient>(),
                        serviceProvider.GetRequiredService<ITaxClient>(),
                        serviceProvider.GetRequiredService<ILogger<VentasController>>()));
                services.AddHttpClient(CatalogClient.HttpClientName)
                    .ConfigurePrimaryHttpMessageHandler(() => new CatalogStubHandler(ProductOneId, ProductTwoId));
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

        var response = await client.PostAsJsonAsync("/ventas", new
        {
            items = new[]
            {
                new
                {
                    producto_id = productoInexistenteId,
                    cantidad = 1m
                }
            },
            metodo_pago = "EFECTIVO"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // Verificar que NO se persistió ninguna venta ni ítems
        using var scope = configuredFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PosCartDbContext>();
        var totalVentas = await db.Ventas.IgnoreQueryFilters().CountAsync(v => v.TenantId == tenantId);
        Assert.Equal(0, totalVentas);
    }

    [Fact]
    public async Task CrearVenta_ProductoInactivo_Retorna400SinPersistir()
    {
        var tenantId = Guid.NewGuid();
        var cajeroId = Guid.NewGuid();
        var sucursalId = Guid.NewGuid();
        var productoInactivoId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var configuredFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddControllers().AddControllersAsServices();
                services.AddTransient<VentasController>(serviceProvider =>
                    new VentasController(
                        serviceProvider.GetRequiredService<IVentaService>(),
                        serviceProvider.GetRequiredService<ITurnoService>(),
                        serviceProvider.GetRequiredService<ICatalogClient>(),
                        serviceProvider.GetRequiredService<ITaxClient>(),
                        serviceProvider.GetRequiredService<ILogger<VentasController>>()));
                services.AddHttpClient(CatalogClient.HttpClientName)
                    .ConfigurePrimaryHttpMessageHandler(() => new CatalogStubHandler(ProductOneId, ProductTwoId, productoInactivoId));
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

        var response = await client.PostAsJsonAsync("/ventas", new
        {
            items = new[]
            {
                new
                {
                    producto_id = productoInactivoId,
                    cantidad = 1m
                }
            },
            metodo_pago = "EFECTIVO"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // Verificar que NO se persistió ninguna venta ni ítems
        using var scope = configuredFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PosCartDbContext>();
        var totalVentas = await db.Ventas.IgnoreQueryFilters().CountAsync(v => v.TenantId == tenantId);
        Assert.Equal(0, totalVentas);
    }

    // ── S2-005: Turno ausente ─────────────────────────────────────────────────

    /// <summary>
    /// S2-005 — Caso: cajero sin turno ABIERTO intenta crear venta.
    /// Esperado: 409 Conflict, sin persistir ninguna venta.
    /// </summary>
    [Fact]
    public async Task CrearVenta_SinTurnoAbierto_Retorna409SinPersistir()
    {
        var tenantId   = Guid.NewGuid();
        var cajeroId   = Guid.NewGuid();

        var configuredFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddControllers().AddControllersAsServices();
                services.AddTransient<VentasController>(serviceProvider =>
                    new VentasController(
                        serviceProvider.GetRequiredService<IVentaService>(),
                        serviceProvider.GetRequiredService<ITurnoService>(),
                        serviceProvider.GetRequiredService<ICatalogClient>(),
                        serviceProvider.GetRequiredService<ITaxClient>(),
                        serviceProvider.GetRequiredService<ILogger<VentasController>>()));
                services.AddHttpClient(CatalogClient.HttpClientName)
                    .ConfigurePrimaryHttpMessageHandler(() => new CatalogStubHandler(ProductOneId, ProductTwoId));
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

        // No se abre turno — el cajero no tiene turno ABIERTO

        var response = await client.PostAsJsonAsync("/ventas", new
        {
            items = new[]
            {
                new
                {
                    producto_id = ProductOneId,
                    cantidad    = 1m
                }
            },
            metodo_pago = "EFECTIVO"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        // Verificar que NO se persistió ninguna venta
        using var scope = configuredFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PosCartDbContext>();
        var totalVentas = await db.Ventas.IgnoreQueryFilters().CountAsync(v => v.TenantId == tenantId);
        Assert.Equal(0, totalVentas);
    }

    // ── S2-005: Precio falsificado ignorado ────────────────────────────────────

    /// <summary>
    /// S2-005 — Caso: el renderer envía precio_unitario falso (99999) en el request.
    /// Esperado: 201 Created, precio persistido proviene de MS-3 (100m), no del request.
    /// </summary>
    [Fact]
    public async Task CrearVenta_PrecioFalsificadoEsIgnorado_UsaPrecioDeMS3()
    {
        var tenantId   = Guid.NewGuid();
        var cajeroId   = Guid.NewGuid();
        var sucursalId = Guid.NewGuid();

        var configuredFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddControllers().AddControllersAsServices();
                services.AddTransient<VentasController>(serviceProvider =>
                    new VentasController(
                        serviceProvider.GetRequiredService<IVentaService>(),
                        serviceProvider.GetRequiredService<ITurnoService>(),
                        serviceProvider.GetRequiredService<ICatalogClient>(),
                        serviceProvider.GetRequiredService<ITaxClient>(),
                        serviceProvider.GetRequiredService<ILogger<VentasController>>()));
                services.AddHttpClient(CatalogClient.HttpClientName)
                    .ConfigurePrimaryHttpMessageHandler(() => new CatalogStubHandler(ProductOneId, ProductTwoId));
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
            sucursal_id         = sucursalId,
            monto_fondo_inicial = 50000m
        });
        Assert.Equal(HttpStatusCode.OK, abrirTurnoResponse.StatusCode);

        // El request incluye precio_unitario falsificado (99999).
        // El servidor DEBE ignorarlo y usar el precio de MS-3 (100m para ProductOneId).
        var crearVentaResponse = await client.PostAsJsonAsync("/ventas", new
        {
            items = new[]
            {
                new
                {
                    producto_id     = ProductOneId,
                    cantidad        = 1m,
                    precio_unitario = 99999m   // precio falsificado — debe ser ignorado
                }
            },
            metodo_pago = "EFECTIVO"
        });

        Assert.Equal(HttpStatusCode.Created, crearVentaResponse.StatusCode);
        var ventaId = await ReadGuidPropertyAsync(crearVentaResponse, "id");

        // Verificar que el precio persistido viene de MS-3 (100m), no del request (99999)
        using var scope = configuredFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PosCartDbContext>();
        var venta = await db.Ventas
            .IgnoreQueryFilters()
            .Include(v => v.Items)
            .SingleOrDefaultAsync(v => v.Id == ventaId);

        Assert.NotNull(venta);
        Assert.Single(venta.Items);
        var item = venta.Items.First();

        // El precio unitario debe ser 100m (de MS-3), no 99999m (del request)
        Assert.Equal(100m, item.PrecioUnitario);

        // El subtotal, IVA y total deben ser coherentes con precio de 100m.
        // TaxStubHandler calcula 19% → subtotal=100, iva=19, total=119
        Assert.Equal(100m, venta.Subtotal);
        Assert.Equal(19m,  venta.Impuestos);
        Assert.Equal(119m, venta.Total);
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
        private readonly Guid _productOneId;
        private readonly Guid _productTwoId;
        private readonly Guid? _productInactiveId;

        public CatalogStubHandler(Guid productOneId, Guid productTwoId, Guid? productInactiveId = null)
        {
            _productOneId = productOneId;
            _productTwoId = productTwoId;
            _productInactiveId = productInactiveId;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var productId = Guid.Parse(request.RequestUri!.Segments[^1]);
            var response = productId == _productOneId
                ? new { id = _productOneId, nombre = "Producto uno", precioBase = 100m, esPesoVariable = false, isActive = true }
                : productId == _productTwoId
                    ? new { id = _productTwoId, nombre = "Producto dos", precioBase = 50m, esPesoVariable = false, isActive = true }
                    : (_productInactiveId.HasValue && productId == _productInactiveId.Value)
                        ? new { id = _productInactiveId.Value, nombre = "Producto inactivo", precioBase = 20m, esPesoVariable = false, isActive = false }
                        : null;

            if (response is null)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            return Task.FromResult(JsonResponse(HttpStatusCode.OK, response));
        }
    }

    private sealed class TaxStubHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            using var doc = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            var itemsElement = doc.RootElement.GetProperty("items");

            decimal subtotal = 0m;
            var breakdown = new List<object>();

            foreach (var item in itemsElement.EnumerateArray())
            {
                var nombre = item.GetProperty("nombre").GetString();
                var precio = item.GetProperty("precio").GetDecimal();
                var cantidad = item.GetProperty("cantidad").GetDecimal();
                var itemSubtotal = Math.Round(precio * cantidad, 2);
                var itemIva = Math.Round(itemSubtotal * 0.19m, 2);
                var itemTotal = itemSubtotal + itemIva;

                subtotal += itemSubtotal;
                breakdown.Add(new
                {
                    nombre,
                    precio,
                    cantidad,
                    subtotal = itemSubtotal,
                    porcentajeIva = 19m,
                    iva = itemIva,
                    total = itemTotal,
                    exento = false
                });
            }

            var totalIva = Math.Round(subtotal * 0.19m, 2);
            var total = subtotal + totalIva;

            var response = new
            {
                subtotal,
                porcentajeIva = 19m,
                iva = totalIva,
                total,
                items = breakdown
            };

            return JsonResponse(HttpStatusCode.OK, response);
        }
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, object value)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = JsonContent.Create(value)
        };
        return response;
    }
}

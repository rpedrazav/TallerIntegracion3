extern alias POSCartServiceAlias;
extern alias TaxComplianceServiceAlias;

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

    [Theory]
    [Trait("Plan", "S2-062")]
    [InlineData(true, "{}", false, "peso_kg")]
    [InlineData(true, "{\"peso_kg\":null,\"cantidad\":1}", false, "peso_kg")]
    [InlineData(true, "{\"peso_kg\":0}", false, "peso_kg")]
    [InlineData(true, "{\"peso_kg\":-0.500}", false, "peso_kg")]
    [InlineData(true, "{\"peso_kg\":0.009}", false, "peso_kg")]
    [InlineData(true, "{\"peso_kg\":0.7505}", false, "peso_kg")]
    [InlineData(true, "{\"peso_kg\":0.750,\"cantidad\":1}", false, "cantidad")]
    [InlineData(true, "{\"peso_kg\":0.750,\"cantidad\":0}", false, "cantidad")]
    [InlineData(true, "{\"peso_kg\":0.750,\"cantidad\":-1}", false, "cantidad")]
    [InlineData(false, "{\"peso_kg\":0.750,\"cantidad\":2}", false, "peso_kg")]
    [InlineData(false, "{\"cantidad\":0}", false, "cantidad")]
    [InlineData(false, "{\"cantidad\":-1}", false, "cantidad")]
    [InlineData(true, "{\"peso_kg\":0.010}", true, null)]
    [InlineData(true, "{\"peso_kg\":0.750}", true, null)]
    [InlineData(true, "{\"peso_kg\":0.750,\"cantidad\":0.750}", true, null)]
    [InlineData(true, "{\"peso_kg\":0.7500}", true, null)]
    [InlineData(true, "{\"peso_kg\":1.234}", true, null)]
    [InlineData(false, "{\"cantidad\":2}", true, null)]
    [InlineData(false, "{}", true, null)]
    [InlineData(false, "{\"cantidad\":2,\"peso_kg\":null}", true, null)]
    public async Task AgregarItem_ValidaPesoDeCatalogo_SinMutacionAnteRechazo(
        bool pesable, string entrada, bool valido, string? campoError)
    {
        var tenantId = Guid.NewGuid();
        var cajeroId = Guid.NewGuid();
        var taxHandler = new TaxStubHandler();
        using var configuredFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddControllers().AddControllersAsServices();
                services.AddTransient<VentasController>(sp => new VentasController(
                    sp.GetRequiredService<IVentaService>(), sp.GetRequiredService<ITurnoService>(),
                    sp.GetRequiredService<ICatalogClient>(), sp.GetRequiredService<ITaxClient>(),
                    sp.GetRequiredService<ILogger<VentasController>>()));
                services.AddHttpClient(CatalogClient.HttpClientName)
                    .ConfigurePrimaryHttpMessageHandler(() => new CatalogStubHandler(
                        ProductOneId, ProductTwoId, productTwoPesable: pesable));
                services.AddHttpClient(TaxClient.HttpClientName)
                    .ConfigurePrimaryHttpMessageHandler(() => taxHandler);
            });
        });
        using (var migrationScope = configuredFactory.Services.CreateScope())
            await migrationScope.ServiceProvider.GetRequiredService<PosCartDbContext>().Database.MigrateAsync();

        using var client = configuredFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            CreateCajeroToken(configuredFactory.Services.GetRequiredService<IConfiguration>(), tenantId, cajeroId));
        var turno = await client.PostAsJsonAsync("/turnos/abrir", new
        {
            sucursal_id = Guid.NewGuid(), monto_fondo_inicial = 50000m
        });
        Assert.Equal(HttpStatusCode.OK, turno.StatusCode);
        var crear = await client.PostAsJsonAsync("/ventas", new
        {
            items = new[] { new { producto_id = ProductOneId, cantidad = 2m } },
            metodo_pago = "EFECTIVO"
        });
        Assert.Equal(HttpStatusCode.Created, crear.StatusCode);
        var ventaId = await ReadGuidPropertyAsync(crear, "id");

        // Contextos nuevos y AsNoTracking: se compara persistencia, no objetos en memoria.
        async Task<Venta> LeerVenta()
        {
            using var scope = configuredFactory.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<PosCartDbContext>().Ventas
                .IgnoreQueryFilters().AsNoTracking().Include(v => v.Items)
                .SingleAsync(v => v.Id == ventaId && v.TenantId == tenantId);
        }
        static string Snapshot(Venta venta) => JsonSerializer.Serialize(new
        {
            venta.Id, venta.TenantId, venta.Estado, venta.Subtotal, venta.Impuestos, venta.Total,
            Items = venta.Items.OrderBy(i => i.Id).Select(i => new
            {
                i.Id, i.VentaId, i.ProductoId, i.NombreProducto,
                i.Cantidad, i.PesoKg, i.PrecioUnitario, i.Subtotal
            })
        });
        var antes = await LeerVenta();
        var llamadasTax = taxHandler.Calls;
        var payload = JsonSerializer.Deserialize<Dictionary<string, object?>>(entrada)!;
        payload["producto_id"] = ProductTwoId;
        // Intentos de falsificar clasificación y precio no deben reemplazar al catálogo.
        payload["es_peso_variable"] = !pesable;
        payload["precio_unitario"] = 99999m;
        var response = await client.PostAsJsonAsync($"/api/ventas/{ventaId}/items", payload);
        var despues = await LeerVenta();
        if (!valido)
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(campoError!, (await response.Content.ReadAsStringAsync()).ToLowerInvariant());
            Assert.Equal(Snapshot(antes), Snapshot(despues));
            Assert.Equal(llamadasTax, taxHandler.Calls);
            return;
        }

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var input = JsonDocument.Parse(entrada);
        var cantidad = pesable ? input.RootElement.GetProperty("peso_kg").GetDecimal()
            : input.RootElement.TryGetProperty("cantidad", out var c) ? c.GetDecimal() : 1m;
        Assert.Equal(2, despues.Items.Count);
        var agregado = Assert.Single(despues.Items.Where(i => i.ProductoId == ProductTwoId));
        Assert.Equal(cantidad, agregado.Cantidad);
        Assert.Equal(pesable ? (decimal?)cantidad : null, agregado.PesoKg);
        Assert.Equal(50m, agregado.PrecioUnitario);
        Assert.Equal(Math.Round(50m * cantidad, 2), agregado.Subtotal);
        Assert.Equal(200m + agregado.Subtotal, despues.Subtotal);
        Assert.Equal(Math.Round(despues.Subtotal * 0.19m, 2), despues.Impuestos);
        Assert.Equal(despues.Subtotal + despues.Impuestos, despues.Total);
        Assert.Equal(llamadasTax + 1, taxHandler.Calls);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(cantidad, body.RootElement.GetProperty("cantidad").GetDecimal());
    }

    [Theory]
    [Trait("Plan", "S2-063")]
    [InlineData("2000", "0.750", "19", "1500.00", "285.00")]
    [InlineData("12.3456", "0.750", "19", "9.26", "1.76")]
    [InlineData("12.3456", "0.500", "21", "6.17", "1.30")]
    [InlineData("2000", "0.010", "0", "20.00", "0.00")]
    [InlineData("1", "0.125", "19", "0.12", "0.02")]
    [InlineData("1", "0.135", "19", "0.14", "0.03")]
    [InlineData("0", "0.750", "19", "0.00", "0.00")]
    public async Task Pesable_CalculoAutoritativo_ConservaPrecioYTotalesTrasRecalcular(
        string precioTexto, string pesoTexto, string tasaTexto, string subtotalTexto, string ivaTexto)
    {
        decimal D(string value) => decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        var precio = D(precioTexto);
        var peso = D(pesoTexto);
        var tasa = D(tasaTexto);
        var subtotal = D(subtotalTexto);
        var iva = D(ivaTexto);
        var tenant = Guid.NewGuid();
        var cajero = Guid.NewGuid();
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddHttpClient(CatalogClient.HttpClientName).ConfigurePrimaryHttpMessageHandler(
                () => new CatalogStubHandler(ProductOneId, ProductTwoId,
                    productTwoPesable: true, productTwoPrice: precio));
            // Serializa/deserializa por HTTP usando el calculador productivo de MS-2.
            services.AddHttpClient(TaxClient.HttpClientName).ConfigurePrimaryHttpMessageHandler(
                () => new RealTaxCalculatorHandler(tasa));
        }));
        using (var scope = factory.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<PosCartDbContext>().Database.MigrateAsync();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            CreateCajeroToken(factory.Services.GetRequiredService<IConfiguration>(), tenant, cajero));
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/turnos/abrir", new
        {
            sucursal_id = Guid.NewGuid(), monto_fondo_inicial = 0m
        })).StatusCode);
        var crear = await client.PostAsJsonAsync("/ventas", new
        {
            items = new[] { new { producto_id = ProductOneId, cantidad = 2m } }, metodo_pago = "EFECTIVO"
        });
        Assert.Equal(HttpStatusCode.Created, crear.StatusCode);
        var ventaId = await ReadGuidPropertyAsync(crear, "id");
        var response = await client.PostAsJsonAsync($"/ventas/{ventaId}/items", new
        {
            producto_id = ProductTwoId, peso_kg = peso,
            precio_unitario = 99999m, subtotal = 99999m, impuestos = 99999m, total = 99999m,
            porcentajeIva = 99m
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(precio, body.RootElement.GetProperty("precio_unitario").GetDecimal());
        Assert.Equal(subtotal, body.RootElement.GetProperty("subtotal").GetDecimal());
        Assert.Equal(iva, body.RootElement.GetProperty("iva").GetDecimal());
        Assert.Equal(subtotal + iva, body.RootElement.GetProperty("total").GetDecimal());

        async Task Verificar(decimal baseUnitaria)
        {
            using var scope = factory.Services.CreateScope();
            var venta = await scope.ServiceProvider.GetRequiredService<PosCartDbContext>().Ventas
                .IgnoreQueryFilters().AsNoTracking().Include(v => v.Items)
                .SingleAsync(v => v.Id == ventaId && v.TenantId == tenant);
            var item = Assert.Single(venta.Items.Where(i => i.ProductoId == ProductTwoId));
            Assert.Equal(precio, item.PrecioUnitario);
            Assert.Equal(peso, item.PesoKg);
            Assert.Equal(peso, item.Cantidad);
            Assert.Equal(subtotal, item.Subtotal);
            Assert.Equal(baseUnitaria + subtotal, venta.Subtotal);
            Assert.Equal(baseUnitaria * tasa / 100m + iva, venta.Impuestos);
            Assert.Equal(venta.Subtotal + venta.Impuestos, venta.Total);
            Assert.Equal(venta.Items.Sum(i => i.Subtotal), venta.Subtotal);
            // La respuesta tras recargar también debe coincidir con la base.
            using var get = JsonDocument.Parse(await client.GetStringAsync($"/ventas/{ventaId}"));
            Assert.Equal(venta.Total, get.RootElement.GetProperty("total").GetDecimal());
        }
        await Verificar(200m);
        // Otra petición obliga a cargar el snapshot persistido y recalcular el carrito mixto.
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync($"/ventas/{ventaId}/items",
            new { producto_id = ProductOneId, cantidad = 1m })).StatusCode);
        await Verificar(300m);
    }

    private sealed class RealTaxCalculatorHandler(decimal tasa) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using var doc = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("porcentajeIva").ValueKind);
            var items = doc.RootElement.GetProperty("items").EnumerateArray().Select(i =>
                new TaxComplianceServiceAlias::TaxComplianceService.Services.TaxItem(
                    i.GetProperty("precio").GetDecimal(), i.GetProperty("cantidad").GetDecimal(),
                    i.GetProperty("nombre").GetString())).ToList();
            var result = TaxComplianceServiceAlias::TaxComplianceService.Services.TaxCalculatorService.Calculate(items, tasa);
            return JsonResponse(HttpStatusCode.OK, result);
        }
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
        private readonly bool _productTwoPesable;
        private readonly decimal _productTwoPrice;

        public CatalogStubHandler(Guid productOneId, Guid productTwoId, Guid? productInactiveId = null, bool productTwoPesable = false, decimal productTwoPrice = 50m)
        {
            _productOneId = productOneId;
            _productTwoId = productTwoId;
            _productInactiveId = productInactiveId;
            _productTwoPesable = productTwoPesable;
            _productTwoPrice = productTwoPrice;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var productId = Guid.Parse(request.RequestUri!.Segments[^1]);
            var response = productId == _productOneId
                ? new { id = _productOneId, nombre = "Producto uno", precioBase = 100m, esPesoVariable = false, isActive = true }
                : productId == _productTwoId
                    ? new { id = _productTwoId, nombre = "Producto dos", precioBase = _productTwoPrice, esPesoVariable = _productTwoPesable, isActive = true }
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
        public int Calls { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Calls++;
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

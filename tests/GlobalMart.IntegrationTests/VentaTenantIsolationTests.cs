extern alias POSCartServiceAlias;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GlobalMart.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
using Pos = POSCartServiceAlias::POSCartService;

namespace GlobalMart.IntegrationTests;

/// <summary>S2-023: real JWT middleware, HTTP controllers and PostgreSQL for both POS and catalog.</summary>
[Trait("Category", "RN01")]
[Trait("Plan", "S2-023")]
public sealed class VentaTenantIsolationTests : IAsyncLifetime
{
    private TwoTenantFixture _fixture = null!;
    private Rn01CatalogFactory _catalog = null!;
    private PosFactory _pos = null!;
    private HttpClient _a = null!;
    private HttpClient _b = null!;

    public async Task InitializeAsync()
    {
        var connection = Environment.GetEnvironmentVariable("GLOBALMART_TEST_POSTGRES")
            ?? throw new InvalidOperationException("Configure GLOBALMART_TEST_POSTGRES; ver Fixtures/README.md.");
        _fixture = new TwoTenantFixture(connection);
        await _fixture.InitializeAsync();
        _catalog = new Rn01CatalogFactory(_fixture);
        using var bootstrap = _catalog.CreateClient();
        _pos = new PosFactory(_fixture, _catalog);
        _a = _pos.CreateClient();
        _b = _pos.CreateClient();
        Rn01HttpSupport.Authenticate(_a, _fixture.TenantA, "CAJERO");
        Rn01HttpSupport.Authenticate(_b, _fixture.TenantB, "CAJERO");
    }

    [Fact]
    public async Task Consultas_VentaYTurnoAjeno_NoExponenDatos()
    {
        var a = _fixture.TenantA;
        var b = _fixture.TenantB;
        using var own = await _a.GetAsync($"/ventas/{a.VentaPendienteId}");
        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        using var ownJson = await JsonAsync(own);
        Assert.Equal(a.TenantId, ownJson.RootElement.GetProperty("tenant_id").GetGuid());
        using var owner = await _b.GetAsync($"/ventas/{b.VentaPendienteId}");
        Assert.Equal(HttpStatusCode.OK, owner.StatusCode);
        foreach (var id in new[] { b.VentaPendienteId, b.VentaCompletadaId })
        {
            using var denied = await _a.GetAsync($"/ventas/{id}");
            Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);
            Assert.DoesNotContain("Producto fixture", await denied.Content.ReadAsStringAsync());
        }
        foreach (var turn in new[] { b.TurnoAbiertoId, b.TurnoCerradoId })
        {
            using var denied = await _a.GetAsync($"/ventas/turno/{turn}");
            Assert.Equal(HttpStatusCode.OK, denied.StatusCode);
            using var json = await JsonAsync(denied);
            Assert.Empty(json.RootElement.EnumerateArray());
            using var allowed = await _b.GetAsync($"/ventas/turno/{turn}");
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
            using var ownedJson = await JsonAsync(allowed);
            Assert.Single(ownedJson.RootElement.EnumerateArray());
        }
    }

    [Theory]
    [InlineData("add")]
    [InlineData("update")]
    [InlineData("delete")]
    [InlineData("charge")]
    [InlineData("own-sale-foreign-item-update")]
    [InlineData("own-sale-foreign-item-delete")]
    public async Task Mutaciones_VentaOItemAjeno_404SinCambiarPersistenciaNiPublicar(string operation)
    {
        var b = _fixture.TenantB;
        await using var db = _fixture.PosDb(b);
        var item = await db.ItemsVenta.SingleAsync(i => i.VentaId == b.VentaPendienteId);
        var before = await SnapshotAsync();
        var sale = operation.StartsWith("own-sale") ? _fixture.TenantA.VentaPendienteId : b.VentaPendienteId;
        using var response = operation switch
        {
            "add" => await _a.PostAsJsonAsync($"/ventas/{sale}/items", new { producto_id = _fixture.TenantA.ProductoId, cantidad = 2m }),
            "update" or "own-sale-foreign-item-update" => await _a.PutAsJsonAsync($"/ventas/{sale}/items/{item.Id}", new { cantidad = 3m }),
            "delete" or "own-sale-foreign-item-delete" => await _a.DeleteAsync($"/ventas/{sale}/items/{item.Id}"),
            "charge" => await _a.PostAsJsonAsync($"/ventas/{sale}/cobrar", new { monto_recibido = 1000m }),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(before, await SnapshotAsync());
        Assert.Empty(_pos.Kafka.Events);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProductoAjeno_CrearOAgregar_ConsultaMS3RealYRechazaSinPersistir(bool add)
    {
        // Positive catalog control with exactly the same token that MS5 will propagate.
        using var catalogClient = _catalog.CreateClient();
        catalogClient.DefaultRequestHeaders.Authorization = _a.DefaultRequestHeaders.Authorization;
        using var own = await catalogClient.GetAsync($"/products/{_fixture.TenantA.ProductoId}");
        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        var before = await SnapshotAsync();
        using var response = add
            ? await _a.PostAsJsonAsync($"/ventas/{_fixture.TenantA.VentaPendienteId}/items", new { producto_id = _fixture.TenantB.ProductoId, cantidad = 1m })
            : await _a.PostAsJsonAsync("/ventas", new { items = new[] { new { producto_id = _fixture.TenantB.ProductoId, cantidad = 1m } }, metodo_pago = "EFECTIVO" });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(before, await SnapshotAsync());
        Assert.Empty(_pos.Kafka.Events);
    }

    [Fact]
    public async Task OperacionesPropias_CrearAgregarModificarEliminarYCobrar_FuncionanYConservanTenantB()
    {
        var beforeB = await SnapshotAsync(_fixture.TenantB.TenantId);
        using var created = await _a.PostAsJsonAsync("/ventas", new { items = new[] { new { producto_id = _fixture.TenantA.ProductoId, cantidad = 1m } }, metodo_pago = "EFECTIVO" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var saleJson = await JsonAsync(created);
        var id = saleJson.RootElement.GetProperty("id").GetGuid();
        Assert.Equal(_fixture.TenantA.TenantId, saleJson.RootElement.GetProperty("tenant_id").GetGuid());
        using var added = await _a.PostAsJsonAsync($"/ventas/{id}/items", new { producto_id = _fixture.TenantA.ProductoId, cantidad = 1m });
        Assert.Equal(HttpStatusCode.Created, added.StatusCode);
        using var itemJson = await JsonAsync(added);
        var item = itemJson.RootElement.GetProperty("id").GetGuid();
        using var updated = await _a.PutAsJsonAsync($"/ventas/{id}/items/{item}", new { cantidad = 2m });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        using var deleted = await _a.DeleteAsync($"/ventas/{id}/items/{item}");
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        using var charged = await _a.PostAsJsonAsync($"/ventas/{id}/cobrar", new { monto_recibido = 119m });
        Assert.Equal(HttpStatusCode.OK, charged.StatusCode);
        await using var db = _fixture.PosDb(_fixture.TenantA);
        var sale = await db.Ventas.Include(v => v.Items).Include(v => v.Pagos).SingleAsync(v => v.Id == id);
        Assert.Equal(Pos.Models.EstadoVenta.COMPLETADA, sale.Estado);
        Assert.Single(sale.Items);
        Assert.Equal(119m, sale.Total);
        Assert.Equal(119m, Assert.Single(sale.Pagos).Monto);
        var published = Assert.Single(_pos.Kafka.Events);
        Assert.Equal(_fixture.TenantA.TenantId, published.TenantId);
        Assert.Equal(id, published.VentaId);
        Assert.Equal(beforeB, await SnapshotAsync(_fixture.TenantB.TenantId));
    }

    private static async Task<JsonDocument> JsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync());

    private async Task<string> SnapshotAsync(Guid? tenant = null)
    {
        // Ignore filters deliberately: persistence assertions must detect writes in either tenant.
        await using var db = _fixture.PosDb(_fixture.TenantA);
        var sales = await db.Ventas.IgnoreQueryFilters().Where(v => tenant == null || v.TenantId == tenant)
            .OrderBy(v => v.Id).Select(v => new { v.Id, v.TenantId, v.CajeroId, v.SucursalId, v.TurnoId, v.Estado, v.Subtotal, v.Impuestos, v.Total, v.MetodoPago, v.CreatedAt }).ToListAsync();
        var ids = sales.Select(v => v.Id).ToArray();
        var items = await db.ItemsVenta.IgnoreQueryFilters().Where(i => ids.Contains(i.VentaId)).OrderBy(i => i.Id)
            .Select(i => new { i.Id, i.VentaId, i.ProductoId, i.NombreProducto, i.PrecioUnitario, i.Cantidad, i.PesoKg, i.Subtotal }).ToListAsync();
        var payments = await db.Pagos.IgnoreQueryFilters().Where(p => ids.Contains(p.VentaId)).OrderBy(p => p.Id)
            .Select(p => new { p.Id, p.VentaId, p.Metodo, p.Monto, p.Vuelto }).ToListAsync();
        return JsonSerializer.Serialize(new { sales, items, payments });
    }

    public async Task DisposeAsync()
    {
        _a?.Dispose();
        _b?.Dispose();
        if (_pos is not null) await _pos.DisposeAsync();
        if (_catalog is not null) await _catalog.DisposeAsync();
        if (_fixture is not null) await _fixture.DisposeAsync();
    }

    private sealed class PosFactory(TwoTenantFixture fixture, Rn01CatalogFactory catalog) : Ms5WebApplicationFactory
    {
        public RecordingKafka Kafka { get; } = new();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<Pos.Data.PosCartDbContext>>();
                services.AddSingleton(new DbContextOptionsBuilder<Pos.Data.PosCartDbContext>()
                    .UseNpgsql(fixture.Connection("pos")).ReplaceService<IMigrator, FixtureModelMigrator>().Options);
                Rn01HttpSupport.ConfigureJwt(services);
                services.RemoveAll<Pos.Services.IKafkaProducerService>();
                services.AddSingleton<Pos.Services.IKafkaProducerService>(Kafka);
                services.AddHttpClient(Pos.Services.CatalogClient.HttpClientName)
                    .ConfigurePrimaryHttpMessageHandler(() => catalog.Server.CreateHandler());
                services.AddHttpClient(Pos.Services.TaxClient.HttpClientName)
                    .ConfigurePrimaryHttpMessageHandler(() => new TaxHandler());
            });
        }
    }

    private sealed class RecordingKafka : Pos.Services.IKafkaProducerService
    {
        public List<Pos.Messaging.SaleCompletedEvent> Events { get; } = new();
        public Task PublicarAsync(string topico, string mensaje, string? clave = null, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Use the sale.completed event recorder.");
        public Task PublicarSaleCompletedAsync(Pos.Messaging.SaleCompletedEvent evento, CancellationToken cancellationToken = default)
        {
            Events.Add(evento);
            return Task.CompletedTask;
        }
    }

    private sealed class TaxHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            var subtotal = json.RootElement.GetProperty("items").EnumerateArray()
                .Sum(i => i.GetProperty("precio").GetDecimal() * i.GetProperty("cantidad").GetDecimal());
            var iva = Math.Round(subtotal * .19m, 2);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { subtotal, iva, total = subtotal + iva, porcentajeIva = 19m, items = Array.Empty<object>() })
            };
        }
    }
}

extern alias POSCartServiceAlias;
extern alias CatalogPricingServiceAlias;
extern alias TaxComplianceServiceAlias;

using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GlobalMart.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
using Pos = POSCartServiceAlias::POSCartService;
using Tax = TaxComplianceServiceAlias::TaxComplianceService.Services;
using Catalog = CatalogPricingServiceAlias::CatalogPricingService;

namespace GlobalMart.IntegrationTests;

/// <summary>S2-084. HTTP MS5 → HTTP MS3 + real MS2 calculator adapter, isolated PostgreSQL,
/// then production renderer expressions/CartItem executed in Node. No MS2 HTTP or Electron claim.</summary>
public sealed class PesableEntreServiciosIntegrationTests
{
    public static IEnumerable<object[]> Cases()
    {
        yield return new object[] { "reference", new[] { 2000m }, new[] { .750m }, new[] { 1500m }, new[] { 285m }, 1785m };
        yield return new object[] { "four-decimals", new[] { 1234.5678m }, new[] { .750m }, new[] { 925.93m }, new[] { 175.93m }, 1101.86m };
        yield return new object[] { "subtotal-even-down", new[] { 1.005m }, new[] { 1m }, new[] { 1m }, new[] { .19m }, 1.19m };
        yield return new object[] { "subtotal-even-up", new[] { 1.015m }, new[] { 1m }, new[] { 1.02m }, new[] { .19m }, 1.21m };
        yield return new object[] { "iva-even-up", new[] { .5m }, new[] { 1m }, new[] { .5m }, new[] { .10m }, .60m };
        yield return new object[] { "iva-even-down", new[] { 1.5m }, new[] { 1m }, new[] { 1.5m }, new[] { .28m }, 1.78m };
        yield return new object[] { "multi-line", new[] { .03m, .03m }, new[] { 1m, 1m }, new[] { .03m, .03m }, new[] { .01m, .01m }, .08m };
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task RealMs2Calculator_AndProductionRenderer(string name, decimal[] prices, decimal[] weights,
        decimal[] lineSubtotals, decimal[] lineTaxes, decimal total)
    {
        var result = Calculate(prices, weights);
        CheckTax(result, lineSubtotals, lineTaxes, total);
        await Renderer(new { subtotal = result.Subtotal, impuestos = result.Iva, total = result.Total,
            items = prices.Select((p, i) => new { precio_unitario = p, cantidad = weights[i], subtotal = lineSubtotals[i] }) });
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task HttpMs3ToMs5_PersistedPriceAndTotalsAgree(string name, decimal[] prices, decimal[] weights,
        decimal[] lineSubtotals, decimal[] lineTaxes, decimal total)
    {
        await using var fixture = await Fixture();
        var tenant = fixture.TenantA;
        var ids = new List<Guid>();
        await using (var db = fixture.CatalogDb(tenant))
        {
            for (var i = 0; i < prices.Length; i++)
            {
                var id = Guid.NewGuid(); ids.Add(id);
                db.Productos.Add(new Catalog.Models.Producto { Id = id, TenantId = tenant.TenantId,
                    Nombre = $"S2084 {name} {i}", CodigoBarras = "s2084-" + id.ToString("N"),
                    PrecioBase = prices[i], EsPesoVariable = true, UomBaseId = Guid.NewGuid(),
                    IsActive = true, CreatedAt = DateTime.UtcNow });
            }
            await db.SaveChangesAsync();
        }
        using var catalog = new Rn01CatalogFactory(fixture);
        using var catalogClient = catalog.CreateClient();
        Rn01HttpSupport.Authenticate(catalogClient, tenant);
        for (var i = 0; i < ids.Count; i++)
        {
            var product = await catalogClient.GetFromJsonAsync<Pos.DTOs.ProductoCatalogDto>($"api/products/{ids[i]}");
            Assert.NotNull(product); Assert.Equal(prices[i], product.PrecioBase); Assert.True(product.EsPesoVariable);
        }
        var tax = new RealCalculatorAdapter();
        using var pos = new IsolatedPosFactory(fixture, catalog, tax);
        using var client = pos.CreateClient();
        Rn01HttpSupport.Authenticate(client, tenant);
        var response = await client.PostAsJsonAsync("api/ventas", new { metodo_pago = "EFECTIVO",
            items = ids.Select((id, i) => new { producto_id = id, cantidad = weights[i], peso_kg = weights[i] }) });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var sale = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotNull(tax.Last); CheckTax(tax.Last!, lineSubtotals, lineTaxes, total);
        Assert.Equal(lineSubtotals.Sum(), sale.GetProperty("subtotal").GetDecimal());
        Assert.Equal(lineTaxes.Sum(), sale.GetProperty("impuestos").GetDecimal());
        Assert.Equal(total, sale.GetProperty("total").GetDecimal());
        var saleId = sale.GetProperty("id").GetGuid();
        await using var stored = fixture.PosDb(tenant);
        var persisted = await stored.Ventas.Include(v => v.Items).SingleAsync(v => v.Id == saleId);
        Assert.Equal(total, persisted.Total); Assert.Equal(lineSubtotals.Sum(), persisted.Subtotal);
        Assert.Equal(lineTaxes.Sum(), persisted.Impuestos); Assert.Equal(tenant.TurnoAbiertoId, persisted.TurnoId);
        for (var i = 0; i < ids.Count; i++)
        {
            var item = persisted.Items.Single(x => x.ProductoId == ids[i]);
            Assert.Equal(prices[i], item.PrecioUnitario); Assert.Equal(weights[i], item.Cantidad);
            Assert.Equal(weights[i], item.PesoKg); Assert.Equal(lineSubtotals[i], item.Subtotal);
        }
        await Renderer(sale);
        Assert.Equal(1, tax.Calls);
        var beforeA = await Snapshot(fixture, tenant);
        var beforeB = await Snapshot(fixture, fixture.TenantB);
        var rejectedRequest = new { metodo_pago = "EFECTIVO",
            items = new[] { new { producto_id = ids[0], cantidad = .75m, peso_kg = .75m } } };
        using var anonymous = pos.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"api/ventas/{saleId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("api/ventas", rejectedRequest)).StatusCode);
        Assert.Equal(1, tax.Calls);
        Assert.Equal(beforeA, await Snapshot(fixture, tenant));
        Assert.Equal(beforeB, await Snapshot(fixture, fixture.TenantB));
        Rn01HttpSupport.Authenticate(client, fixture.TenantB);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"api/ventas/{saleId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("api/ventas", new { metodo_pago = "EFECTIVO",
            items = new[] { new { producto_id = ids[0], cantidad = .75m, peso_kg = .75m } } })).StatusCode);
        Assert.Equal(1, tax.Calls);
        Assert.Equal(beforeA, await Snapshot(fixture, tenant));
        Assert.Equal(beforeB, await Snapshot(fixture, fixture.TenantB));
        await using var other = fixture.PosDb(fixture.TenantB);
        Assert.False(await other.Ventas.AnyAsync(v => v.Id == saleId));
        await using (var db = fixture.PosDb(tenant))
        {
            var shift = await db.Turnos.SingleAsync(t => t.Id == tenant.TurnoAbiertoId);
            shift.Estado = Pos.Models.EstadoTurno.CERRADO; await db.SaveChangesAsync();
        }
        Rn01HttpSupport.Authenticate(client, tenant);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("api/ventas", new { metodo_pago = "EFECTIVO",
            items = new[] { new { producto_id = ids[0], cantidad = .75m, peso_kg = .75m } } })).StatusCode);
        Assert.Equal(1, tax.Calls);
        Assert.Equal(beforeA, await Snapshot(fixture, tenant));
        Assert.Equal(beforeB, await Snapshot(fixture, fixture.TenantB));
    }

    private static async Task<string> Snapshot(TwoTenantFixture fixture, TenantData tenant)
    {
        await using var db = fixture.PosDb(tenant);
        var sales = await db.Ventas.AsNoTracking().Include(v => v.Items).OrderBy(v => v.Id).ToListAsync();
        return JsonSerializer.Serialize(sales.Select(v => new { v.Id, v.TenantId, v.Estado,
            v.Subtotal, v.Impuestos, v.Total, Items = v.Items.OrderBy(i => i.Id).Select(i => new
            { i.Id, i.ProductoId, i.PrecioUnitario, i.Cantidad, i.PesoKg, i.Subtotal }) }));
    }

    private static Tax.TaxBreakdown Calculate(decimal[] prices, decimal[] weights) => Tax.TaxCalculatorService.Calculate(
        prices.Select((p, i) => new Tax.TaxItem { Nombre = "S2084 " + i, Precio = p, Cantidad = weights[i] }), 19m);
    private static void CheckTax(Tax.TaxBreakdown tax, decimal[] subs, decimal[] taxes, decimal total)
    {
        Assert.Equal(subs, tax.Items.Select(i => i.Subtotal)); Assert.Equal(taxes, tax.Items.Select(i => i.Iva));
        Assert.Equal(subs.Sum(), tax.Subtotal); Assert.Equal(taxes.Sum(), tax.Iva); Assert.Equal(total, tax.Total);
        Assert.Equal(tax.Subtotal + tax.Iva, tax.Total);
    }
    private static async Task<TwoTenantFixture> Fixture()
    {
        var connection = Environment.GetEnvironmentVariable("S2084_TEST_CONNECTION");
        Assert.False(string.IsNullOrWhiteSpace(connection), "Set S2084_TEST_CONNECTION to an isolated PostgreSQL instance; defaults are forbidden.");
        var fixture = new TwoTenantFixture(connection!); await fixture.InitializeAsync(); return fixture;
    }
    private static string Root()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "GlobalMartOS.sln"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Repository root unavailable.");
    }
    private static async Task Renderer(object sale)
    {
        var root = Root();
        var start = new ProcessStartInfo("node") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        start.ArgumentList.Add(Path.Combine(root, "tests", "GlobalMart.IntegrationTests", "s2084-renderer.cjs"));
        start.ArgumentList.Add(root); start.ArgumentList.Add(JsonSerializer.Serialize(sale));
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException) { process.Kill(true); throw; }
        Assert.True(process.ExitCode == 0, await stdout + await stderr);
    }
    private sealed class RealCalculatorAdapter : Pos.Services.ITaxClient
    {
        public int Calls { get; private set; }
        public Tax.TaxBreakdown? Last { get; private set; }
        public Task<Pos.DTOs.TaxCalculationResult?> CalculateTaxAsync(IEnumerable<Pos.DTOs.TaxItemDto> items,
            decimal? porcentajeIva = null, string? bearerToken = null, CancellationToken cancellationToken = default)
        {
            Calls++;
            Assert.False(string.IsNullOrWhiteSpace(bearerToken));
            Last = Tax.TaxCalculatorService.Calculate(items.Select(i => new Tax.TaxItem { Nombre = i.Nombre, Precio = i.Precio, Cantidad = i.Cantidad }), 19m);
            return Task.FromResult(JsonSerializer.Deserialize<Pos.DTOs.TaxCalculationResult>(JsonSerializer.Serialize(Last), new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        }
    }
    private sealed class IsolatedPosFactory(TwoTenantFixture fixture, Rn01CatalogFactory catalog, RealCalculatorAdapter tax) : Ms5WebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder); builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<Pos.Data.PosCartDbContext>>();
                services.AddSingleton(new DbContextOptionsBuilder<Pos.Data.PosCartDbContext>().UseNpgsql(fixture.Connection("pos"))
                    .ReplaceService<IMigrator, FixtureModelMigrator>().Options);
                Rn01HttpSupport.ConfigureJwt(services);
                services.RemoveAll<Pos.Services.ITaxClient>(); services.AddSingleton<Pos.Services.ITaxClient>(tax);
                services.AddHttpClient(Pos.Services.CatalogClient.HttpClientName, c => c.BaseAddress = new Uri("http://localhost/"))
                    .ConfigurePrimaryHttpMessageHandler(() => catalog.Server.CreateHandler());
            });
        }
    }
}



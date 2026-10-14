extern alias CatalogPricingServiceAlias;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GlobalMart.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;
using Producto = CatalogPricingServiceAlias::CatalogPricingService.Models.Producto;

namespace GlobalMart.IntegrationTests;

[Trait("Category", "RN01")]
[Trait("Plan", "S2-023")]
public sealed class ProductoTenantIsolationIntegrationTests : IAsyncLifetime
{
    private TwoTenantFixture _fixture = null!;
    private Rn01CatalogFactory _factory = null!;
    private HttpClient _client = null!;
    private readonly Guid _foreignOnlyProduct = Guid.NewGuid();
    private const string ForeignBarcode = "S2023-only-B";

    public async Task InitializeAsync()
    {
        _fixture = new TwoTenantFixture(Environment.GetEnvironmentVariable("GLOBALMART_TEST_POSTGRES")
            ?? throw new InvalidOperationException("Configure GLOBALMART_TEST_POSTGRES; ver Fixtures/README.md."));
        await _fixture.InitializeAsync();
        await using (var db = _fixture.CatalogDb(_fixture.TenantB))
        {
            db.Productos.Add(new Producto
            {
                Id = _foreignOnlyProduct, TenantId = _fixture.TenantB.TenantId,
                Nombre = "ExclusivoTenantB", CodigoBarras = ForeignBarcode,
                PrecioBase = 234m, UomBaseId = Guid.NewGuid(), IsActive = true, CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
        _factory = new Rn01CatalogFactory(_fixture);
        _client = _factory.CreateClient();
        Rn01HttpSupport.Authenticate(_client, _fixture.TenantA);
        // Even an explicit competing header must not override the validated JWT tenant.
        _client.DefaultRequestHeaders.Add("X-Tenant-ID", _fixture.TenantB.TenantId.ToString());
    }

    [Fact]
    public async Task GetById_TenantBAjeno_404_Propio200()
    {
        using var foreign = await _client.GetAsync($"/products/{_fixture.TenantB.ProductoId}");
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        using var own = await _client.GetAsync($"/products/{_fixture.TenantA.ProductoId}");
        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        Assert.Equal(_fixture.TenantA.ProductoId, (await own.Content.ReadFromJsonAsync<Producto>())!.Id);
        Rn01HttpSupport.Authenticate(_client, _fixture.TenantB);
        using var owner = await _client.GetAsync($"/products/{_fixture.TenantB.ProductoId}");
        Assert.Equal(HttpStatusCode.OK, owner.StatusCode);
    }

    [Theory]
    [InlineData("/products?page=1&pageSize=100")]
    [InlineData("/products/search?q=Producto&page=1&pageSize=100")]
    public async Task ConsultasPaginadas_SoloDevuelvenProductosDelJwt(string route)
    {
        using var response = await _client.GetAsync(route);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, json.GetProperty("totalCount").GetInt32());
        var product = Assert.Single(json.GetProperty("data").EnumerateArray());
        Assert.Equal(_fixture.TenantA.ProductoId, product.GetProperty("id").GetGuid());
        Assert.Equal(_fixture.TenantA.TenantId, product.GetProperty("tenantId").GetGuid());
    }

    [Fact]
    public async Task Search_TerminoExclusivoB_NoFiltraExistenciaNiDatos()
    {
        using var denied = await _client.GetAsync("/products/search?q=ExclusivoTenantB");
        Assert.Equal(HttpStatusCode.OK, denied.StatusCode);
        var empty = await denied.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, empty.GetProperty("totalCount").GetInt32());
        Assert.Empty(empty.GetProperty("data").EnumerateArray());
        Rn01HttpSupport.Authenticate(_client, _fixture.TenantB);
        using var allowed = await _client.GetAsync("/products/search?q=ExclusivoTenantB");
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        var json = await allowed.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(_foreignOnlyProduct, Assert.Single(json.GetProperty("data").EnumerateArray()).GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Lookup_MismoBarcodeResuelvePropio_BarcodeExclusivoAjeno404()
    {
        using var shared = await _client.GetAsync("/products/lookup?barcode=7790000000001");
        Assert.Equal(HttpStatusCode.OK, shared.StatusCode);
        Assert.Equal(_fixture.TenantA.ProductoId, (await shared.Content.ReadFromJsonAsync<Producto>())!.Id);
        using var foreign = await _client.GetAsync($"/products/lookup?barcode={ForeignBarcode}");
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Rn01HttpSupport.Authenticate(_client, _fixture.TenantB);
        using var owner = await _client.GetAsync($"/products/lookup?barcode={ForeignBarcode}");
        Assert.Equal(HttpStatusCode.OK, owner.StatusCode);
        Assert.Equal(_foreignOnlyProduct, (await owner.Content.ReadFromJsonAsync<Producto>())!.Id);
    }

    [Fact]
    public async Task Put_ProductoBAjenoEInexistente_Mismo404YSnapshotIntacto_Propio200()
    {
        var before = await SnapshotB();
        var payload = new { nombre = "Intento de edición", codigoBarras = "nuevo-edicion", precioBase = 999m,
            isActive = false, tenantId = _fixture.TenantB.TenantId, uomBaseId = Guid.NewGuid() };
        using var denied = await _client.PutAsJsonAsync($"/products/{_fixture.TenantB.ProductoId}", payload);
        using var missing = await _client.PutAsJsonAsync($"/products/{Guid.NewGuid()}", payload);
        Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(await missing.Content.ReadAsStringAsync(), await denied.Content.ReadAsStringAsync());
        Assert.Equal(before, await SnapshotB());
        using var allowed = await _client.PutAsJsonAsync($"/products/{_fixture.TenantA.ProductoId}", payload);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        await using var verify = _fixture.CatalogDb(_fixture.TenantA);
        var own = await verify.Productos.SingleAsync();
        Assert.Equal("Intento de edición", own.Nombre);
        Assert.Equal(999m, own.PrecioBase);
        Assert.False(own.IsActive);
        Assert.Equal(before, await SnapshotB());
    }

    [Fact]
    public async Task Put_CambioIsActive_SinAdmin403_ConAdminDesactivaYReactiva200()
    {
        var beforeB = await SnapshotB();
        foreach (var requestedState in new[] { false, true })
        {
            var payload = new { nombre = "Producto fixture", codigoBarras = "7790000000001",
                precioBase = 100m, isActive = requestedState, uomBaseId = Guid.NewGuid() };
            var beforeA = await SnapshotProducts(_fixture.TenantA.TenantId);
            Rn01HttpSupport.Authenticate(_client, _fixture.TenantA, "CAJERO");
            using var denied = await _client.PutAsJsonAsync($"/products/{_fixture.TenantA.ProductoId}", payload);
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            Assert.Equal(beforeA, await SnapshotProducts(_fixture.TenantA.TenantId));
            Rn01HttpSupport.Authenticate(_client, _fixture.TenantA, "ADMIN");
            using var allowed = await _client.PutAsJsonAsync($"/products/{_fixture.TenantA.ProductoId}", payload);
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
            await using var verify = _fixture.CatalogDb(_fixture.TenantA);
            Assert.Equal(requestedState, (await verify.Productos.SingleAsync()).IsActive);
            Assert.Equal(beforeB, await SnapshotB());
        }
        using var reactivated = await _client.GetAsync($"/products/{_fixture.TenantA.ProductoId}");
        Assert.Equal(HttpStatusCode.OK, reactivated.StatusCode);
    }

    [Fact]
    public async Task Delete_ProductoB_404YSnapshotIntacto_Propio204()
    {
        var before = await SnapshotB();
        using var denied = await _client.DeleteAsync($"/products/{_fixture.TenantB.ProductoId}");
        Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);
        Assert.Equal(before, await SnapshotB());
        using var allowed = await _client.DeleteAsync($"/products/{_fixture.TenantA.ProductoId}");
        Assert.Equal(HttpStatusCode.NoContent, allowed.StatusCode);
        await using var verify = _fixture.CatalogDb(_fixture.TenantA);
        Assert.False((await verify.Productos.SingleAsync()).IsActive);
        Assert.Equal(before, await SnapshotB());
    }

    [Fact]
    public async Task Post_TenantBEnBodyYHeader_PersisteExclusivamenteEnTenantDelJwt()
    {
        var before = await SnapshotB();
        using var response = await _client.PostAsJsonAsync("/products", new
        {
            nombre = "Producto nuevo A", codigoBarras = "nuevo-alta", precioBase = 321m,
            isActive = true, tenantId = _fixture.TenantB.TenantId, uomBaseId = Guid.NewGuid()
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<Producto>();
        Assert.NotNull(created);
        Assert.Equal(_fixture.TenantA.TenantId, created.TenantId);
        await using var verify = _fixture.CatalogDb(_fixture.TenantA);
        Assert.Equal(_fixture.TenantA.TenantId, (await verify.Productos.SingleAsync(p => p.Id == created.Id)).TenantId);
        Assert.Equal(before, await SnapshotB());
    }

    // Raw SQL bypasses EF tenant filters and compares all persisted columns and row count.
    private Task<string> SnapshotB() => SnapshotProducts(_fixture.TenantB.TenantId);

    private async Task<string> SnapshotProducts(Guid tenantId)
    {
        await using var connection = new NpgsqlConnection(_fixture.Connection("catalog"));
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT COALESCE(jsonb_agg(to_jsonb(p) ORDER BY p.id), '[]'::jsonb)::text FROM productos p WHERE tenant_id = @tenant", connection);
        command.Parameters.AddWithValue("tenant", tenantId);
        return Assert.IsType<string>(await command.ExecuteScalarAsync());
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory is not null) await _factory.DisposeAsync();
        if (_fixture is not null) await _fixture.DisposeAsync();
    }
}

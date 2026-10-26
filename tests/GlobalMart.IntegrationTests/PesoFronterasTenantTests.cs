extern alias POSCartServiceAlias;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using GlobalMart.IntegrationTests.Fixtures;
using Xunit;

namespace GlobalMart.IntegrationTests;

public sealed partial class VentaTenantIsolationTests
{
    private async Task ConfigurarPesable(TenantData tenant, decimal price = 12.3456m)
    {
        await using var db = _fixture.CatalogDb(tenant);
        var product = await db.Productos.SingleAsync(p => p.Id == tenant.ProductoId);
        product.EsPesoVariable = true;
        product.PrecioBase = price;
        await db.SaveChangesAsync();
    }

    private async Task<HttpResponseMessage> EnviarPeso(string operation, Dictionary<string, object?> payload, Guid item)
    {
        var sale = _fixture.TenantA.VentaPendienteId;
        return operation switch
        {
            "create" => await _a.PostAsJsonAsync("/ventas", new { items = new[] { payload }, metodo_pago = "EFECTIVO" }),
            "add" => await _a.PostAsJsonAsync($"/ventas/{sale}/items", payload),
            "edit" => await _a.PutAsJsonAsync($"/ventas/{sale}/items/{item}", payload),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
    }

    [Fact]
    [Trait("Plan", "S2-065")]
    public async Task CrearCanasta_ValidoSeguidoDeInvalido_NoPersisteParcialmente()
    {
        await ConfigurarPesable(_fixture.TenantA);
        var before = await SnapshotAsync();
        using var response = await _a.PostAsJsonAsync("/ventas", new
        {
            items = new[]
            {
                new { producto_id = _fixture.TenantA.ProductoId, peso_kg = .750m },
                new { producto_id = _fixture.TenantA.ProductoId, peso_kg = .009m }
            },
            metodo_pago = "EFECTIVO"
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(before, await SnapshotAsync());
        Assert.Empty(_pos.Kafka.Events);
    }

    [Theory]
    [Trait("Plan", "S2-065")]
    [InlineData("create")]
    [InlineData("add")]
    [InlineData("edit")]
    public async Task FronterasPeso_HTTPRealCatalogo_RechazaSinCambiarAmbosTenants(string operation)
    {
        await ConfigurarPesable(_fixture.TenantA);
        await using var db = _fixture.PosDb(_fixture.TenantA);
        var item = await db.ItemsVenta.SingleAsync(i => i.VentaId == _fixture.TenantA.VentaPendienteId);
        var rejected = new[]
        {
            "{}", "{\"peso_kg\":null,\"cantidad\":1}", "{\"peso_kg\":0}",
            "{\"peso_kg\":-0.500}", "{\"peso_kg\":0.009}", "{\"peso_kg\":0.7505}",
            "{\"peso_kg\":0.750,\"cantidad\":1}", "{\"peso_kg\":0.750,\"cantidad\":0}",
            "{\"peso_kg\":0.750,\"cantidad\":-1}", "{\"peso_g\":750}",
            "{\"peso_kg\":\"750 g\"}", "{\"peso_kg\":\"0,750\"}"
        };
        foreach (var input in rejected)
        {
            var before = await SnapshotAsync();
            var payload = JsonSerializer.Deserialize<Dictionary<string, object?>>(input)!;
            payload["producto_id"] = _fixture.TenantA.ProductoId;
            using var response = await EnviarPeso(operation, payload, item.Id);
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest,
                $"{operation} {input}: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
            Assert.Equal(before, await SnapshotAsync());
            Assert.Empty(_pos.Kafka.Events);
        }
        // El mismo producto pasa a ser no pesable en catálogo: no debe aceptar peso.
        await using var catalogDb = _fixture.CatalogDb(_fixture.TenantA);
        var product = await catalogDb.Productos.SingleAsync(p => p.Id == _fixture.TenantA.ProductoId);
        product.EsPesoVariable = false;
        await catalogDb.SaveChangesAsync();
        var snapshot = await SnapshotAsync();
        using var wrongUnit = await EnviarPeso(operation, new()
        {
            ["producto_id"] = product.Id, ["cantidad"] = 2m, ["peso_kg"] = .750m
        }, item.Id);
        Assert.Equal(HttpStatusCode.BadRequest, wrongUnit.StatusCode);
        Assert.Equal(snapshot, await SnapshotAsync());
    }

    [Theory]
    [Trait("Plan", "S2-065")]
    [InlineData("create")]
    [InlineData("add")]
    [InlineData("edit")]
    public async Task BordesAceptados_ImportesExactos_ConCatalogoReal(string operation)
    {
        await ConfigurarPesable(_fixture.TenantA);
        // Preparar un ítem pesable válido vía POST, incluso para probar la posterior edición.
        using var added = await _a.PostAsJsonAsync($"/ventas/{_fixture.TenantA.VentaPendienteId}/items",
            new { producto_id = _fixture.TenantA.ProductoId, peso_kg = .750m });
        Assert.Equal(HttpStatusCode.Created, added.StatusCode);
        using var addedJson = await JsonAsync(added);
        var itemId = addedJson.RootElement.GetProperty("id").GetGuid();
        var beforeB = await SnapshotAsync(_fixture.TenantB.TenantId);
        foreach (var (peso, subtotal, iva) in new[]
        {
            (.010m, .12m, .02m), (.011m, .14m, .03m), (.7500m, 9.26m, 1.76m),
            (1.234m, 15.23m, 2.89m)
        })
        {
            var payload = new Dictionary<string, object?>
            {
                ["producto_id"] = _fixture.TenantA.ProductoId, ["peso_kg"] = peso,
                ["cantidad"] = peso, ["precio_unitario"] = 99999m, ["total"] = 99999m
            };
            using var response = await EnviarPeso(operation, payload, itemId);
            Assert.Equal(operation == "edit" ? HttpStatusCode.OK : HttpStatusCode.Created, response.StatusCode);
            using var json = await JsonAsync(response);
            var saleId = operation == "create" ? json.RootElement.GetProperty("id").GetGuid() : _fixture.TenantA.VentaPendienteId;
            var result = operation == "create" ? json.RootElement.GetProperty("items")[0] : json.RootElement;
            Assert.Equal(peso, result.GetProperty("cantidad").GetDecimal());
            Assert.Equal(peso, result.GetProperty("peso_kg").GetDecimal());
            Assert.Equal(12.3456m, result.GetProperty("precio_unitario").GetDecimal());
            Assert.Equal(subtotal, result.GetProperty("subtotal").GetDecimal());
            if (operation != "create")
            {
                Assert.Equal(iva, result.GetProperty("iva").GetDecimal());
                Assert.Equal(subtotal + iva, result.GetProperty("total").GetDecimal());
            }
            await using var verify = _fixture.PosDb(_fixture.TenantA);
            var sale = await verify.Ventas.AsNoTracking().Include(v => v.Items).SingleAsync(v => v.Id == saleId);
            var stored = sale.Items.Single(i => i.Id == result.GetProperty("id").GetGuid());
            Assert.Equal(subtotal, stored.Subtotal);
            Assert.Equal(12.3456m, stored.PrecioUnitario);
            Assert.Equal(sale.Items.Sum(i => i.Subtotal), sale.Subtotal);
            Assert.Equal(sale.Items.Sum(i => Math.Round(i.Subtotal * .19m, 2, MidpointRounding.ToEven)), sale.Impuestos);
            Assert.Equal(sale.Subtotal + sale.Impuestos, sale.Total);
            Assert.Equal(beforeB, await SnapshotAsync(_fixture.TenantB.TenantId));
        }
    }

    [Theory]
    [Trait("Plan", "S2-065")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PesableAjeno_CrearAgregarEditar_NoFiltraDatosNiMuta(bool actorB)
    {
        var actor = actorB ? _fixture.TenantB : _fixture.TenantA;
        var other = actorB ? _fixture.TenantA : _fixture.TenantB;
        var client = actorB ? _b : _a;
        var owner = actorB ? _a : _b;
        await ConfigurarPesable(actor);
        await ConfigurarPesable(other);
        // Control positivo del dueño sobre el producto exacto que el atacante utilizará.
        using var valid = await owner.PostAsJsonAsync($"/ventas/{other.VentaPendienteId}/items",
            new { producto_id = other.ProductoId, peso_kg = .750m });
        Assert.Equal(HttpStatusCode.Created, valid.StatusCode);
        using var validJson = await JsonAsync(valid);
        var foreignItem = validJson.RootElement.GetProperty("id").GetGuid();
        client.DefaultRequestHeaders.Add("X-Tenant-ID", other.TenantId.ToString());
        var before = await SnapshotAsync();
        var payload = new { producto_id = other.ProductoId, peso_kg = .750m, tenant_id = other.TenantId };
        using var create = await client.PostAsJsonAsync("/ventas", new { items = new[] { payload }, metodo_pago = "EFECTIVO", tenant_id = other.TenantId });
        using var add = await client.PostAsJsonAsync($"/ventas/{actor.VentaPendienteId}/items", payload);
        using var edit = await client.PutAsJsonAsync($"/ventas/{other.VentaPendienteId}/items/{foreignItem}", payload);
        using var mixed = await client.PutAsJsonAsync($"/ventas/{actor.VentaPendienteId}/items/{foreignItem}", payload);
        foreach (var response in new[] { create, add, edit, mixed })
        {
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.DoesNotContain("Producto fixture", await response.Content.ReadAsStringAsync());
        }
        Assert.Equal(before, await SnapshotAsync());
        Assert.Empty(_pos.Kafka.Events);
        client.DefaultRequestHeaders.Remove("X-Tenant-ID");
        using var own = await client.PostAsJsonAsync($"/ventas/{actor.VentaPendienteId}/items",
            new { producto_id = actor.ProductoId, peso_kg = .750m });
        Assert.Equal(HttpStatusCode.Created, own.StatusCode);
    }
}

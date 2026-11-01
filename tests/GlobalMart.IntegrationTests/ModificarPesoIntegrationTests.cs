extern alias POSCartServiceAlias;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CatalogClient = POSCartServiceAlias::POSCartService.Services.CatalogClient;
using TaxClient = POSCartServiceAlias::POSCartService.Services.TaxClient;
using PosCartDbContext = POSCartServiceAlias::POSCartService.Data.PosCartDbContext;
using Venta = POSCartServiceAlias::POSCartService.Models.Venta;
using Xunit;

namespace GlobalMart.IntegrationTests;

public partial class VentaIntegrationTests
{
    [Theory]
    [Trait("Plan", "S2-064")]
    [InlineData(true, "{\"peso_kg\":0.500}", 200, "ok")]
    [InlineData(true, "{\"peso_kg\":1.234,\"cantidad\":1.234}", 200, "ok")]
    [InlineData(true, "{\"peso_kg\":0.010}", 200, "ok")]
    [InlineData(true, "{\"peso_kg\":0.7500}", 200, "ok")]
    [InlineData(true, "{}", 400, "ok")]
    [InlineData(true, "{\"peso_kg\":null,\"cantidad\":1}", 400, "ok")]
    [InlineData(true, "{\"peso_kg\":0}", 400, "ok")]
    [InlineData(true, "{\"peso_kg\":-0.500}", 400, "ok")]
    [InlineData(true, "{\"peso_kg\":0.009}", 400, "ok")]
    [InlineData(true, "{\"peso_kg\":0.7505}", 400, "ok")]
    [InlineData(true, "{\"peso_kg\":0.750,\"cantidad\":1}", 400, "ok")]
    [InlineData(true, "{\"peso_kg\":0.750,\"cantidad\":0}", 400, "ok")]
    [InlineData(false, "{\"cantidad\":2}", 200, "ok")]
    [InlineData(false, "{\"cantidad\":3,\"peso_kg\":null}", 200, "ok")]
    [InlineData(false, "{\"peso_kg\":0.500,\"cantidad\":2}", 400, "ok")]
    [InlineData(false, "{}", 400, "ok")]
    [InlineData(false, "{\"cantidad\":null}", 400, "ok")]
    [InlineData(false, "{\"cantidad\":0}", 400, "ok")]
    [InlineData(false, "{\"cantidad\":-1}", 400, "ok")]
    [InlineData(true, "{\"peso_kg\":0.500}", 503, "tax")]
    [InlineData(false, "{\"cantidad\":2}", 503, "tax")]
    [InlineData(true, "{\"peso_kg\":0.500}", 502, "catalog")]
    [InlineData(true, "{\"peso_kg\":0.500}", 404, "missing")]
    public async Task PutItem_ReglasPorTipo_RecalculaOSinMutacion(
        bool pesable, string entrada, int status, string failure)
    {
        var tenant = Guid.NewGuid();
        var cajero = Guid.NewGuid();
        var catalog = new EditingCatalogHandler();
        var tax = new EditingTaxHandler();
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddHttpClient(CatalogClient.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => catalog);
            services.AddHttpClient(TaxClient.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => tax);
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
            items = new[] { new { producto_id = ProductOneId, cantidad = 1m } }, metodo_pago = "EFECTIVO"
        });
        Assert.Equal(HttpStatusCode.Created, crear.StatusCode);
        var ventaId = await ReadGuidPropertyAsync(crear, "id");
        var agregar = await client.PostAsJsonAsync($"/ventas/{ventaId}/items",
            new { producto_id = ProductTwoId, peso_kg = .750m });
        Assert.Equal(HttpStatusCode.Created, agregar.StatusCode);

        async Task<Venta> Leer()
        {
            using var scope = factory.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<PosCartDbContext>().Ventas
                .IgnoreQueryFilters().AsNoTracking().Include(v => v.Items)
                .SingleAsync(v => v.Id == ventaId && v.TenantId == tenant);
        }
        static string Snapshot(Venta v) => JsonSerializer.Serialize(new
        {
            v.Id, v.TenantId, v.Estado, v.Subtotal, v.Impuestos, v.Total,
            Items = v.Items.OrderBy(i => i.Id).Select(i => new
            { i.Id, i.ProductoId, i.NombreProducto, i.Cantidad, i.PesoKg, i.PrecioUnitario, i.Subtotal })
        });
        var antes = await Leer();
        var target = antes.Items.Single(i => i.ProductoId == (pesable ? ProductTwoId : ProductOneId));
        var other = antes.Items.Single(i => i.Id != target.Id);
        catalog.Editing = true; // El precio nuevo de catálogo no debe sustituir el snapshot.
        catalog.Failure = failure;
        tax.Fail = failure == "tax";
        var llamadas = tax.Calls;
        var payload = JsonSerializer.Deserialize<Dictionary<string, object?>>(entrada)!;
        payload["precio_unitario"] = 1m;
        payload["total"] = 1m;
        payload["es_peso_variable"] = !pesable;
        var response = await client.PutAsJsonAsync($"/api/ventas/{ventaId}/items/{target.Id}", payload);
        Assert.Equal(status, (int)response.StatusCode);
        var despues = await Leer();
        if (status != 200)
        {
            Assert.Equal(Snapshot(antes), Snapshot(despues));
            Assert.Equal(llamadas + (failure == "tax" ? 1 : 0), tax.Calls);
            return;
        }
        Assert.Equal(2, despues.Items.Count);
        var updated = despues.Items.Single(i => i.Id == target.Id);
        var unchanged = despues.Items.Single(i => i.Id == other.Id);
        Assert.Equal(other.Cantidad, unchanged.Cantidad);
        Assert.Equal(other.PesoKg, unchanged.PesoKg);
        Assert.Equal(other.PrecioUnitario, unchanged.PrecioUnitario);
        Assert.Equal(other.Subtotal, unchanged.Subtotal);
        using var input = JsonDocument.Parse(entrada);
        var cantidad = input.RootElement.GetProperty(pesable ? "peso_kg" : "cantidad").GetDecimal();
        Assert.Equal(cantidad, updated.Cantidad);
        Assert.Equal(pesable ? (decimal?)cantidad : null, updated.PesoKg);
        Assert.Equal(target.PrecioUnitario, updated.PrecioUnitario);
        Assert.Equal(Math.Round(target.PrecioUnitario * cantidad, 2, MidpointRounding.ToEven), updated.Subtotal);
        Assert.Equal(despues.Items.Sum(i => i.Subtotal), despues.Subtotal);
        Assert.Equal(despues.Items.Sum(i => Math.Round(i.Subtotal * .19m, 2, MidpointRounding.ToEven)), despues.Impuestos);
        Assert.Equal(despues.Subtotal + despues.Impuestos, despues.Total);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(updated.Id, body.RootElement.GetProperty("id").GetGuid());
        Assert.Equal(updated.Subtotal, body.RootElement.GetProperty("subtotal").GetDecimal());
        // Repetir el mismo PUT conserva ítem e importes, no agrega otra línea.
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/ventas/{ventaId}/items/{target.Id}", payload)).StatusCode);
        Assert.Equal(Snapshot(despues), Snapshot(await Leer()));
    }

    private sealed class EditingCatalogHandler : HttpMessageHandler
    {
        public bool Editing { get; set; }
        public string Failure { get; set; } = "ok";
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (Failure == "catalog") throw new HttpRequestException("Catálogo no disponible para prueba");
            if (Failure == "missing") return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            var id = Guid.Parse(request.RequestUri!.Segments[^1]);
            return Task.FromResult(JsonResponse(HttpStatusCode.OK, new
            {
                id, nombre = "Producto de prueba", isActive = true, esPesoVariable = id == ProductTwoId,
                precioBase = Editing ? 9999m : id == ProductTwoId ? 12.3456m : 100m
            }));
        }
    }

    private sealed class EditingTaxHandler : HttpMessageHandler
    {
        private readonly HttpMessageInvoker _inner = new(new RealTaxCalculatorHandler(19m));
        public int Calls { get; private set; }
        public bool Fail { get; set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            return Fail ? Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))
                : _inner.SendAsync(request, ct);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) _inner.Dispose();
            base.Dispose(disposing);
        }
    }
}

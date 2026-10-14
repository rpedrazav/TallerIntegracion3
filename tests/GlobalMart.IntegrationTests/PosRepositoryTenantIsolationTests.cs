extern alias POSCartServiceAlias;

using System.Text.Json;
using GlobalMart.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Pos = POSCartServiceAlias::POSCartService;

namespace GlobalMart.IntegrationTests;

/// <summary>Defensa RN-01 directamente en repositorios, sin controles del controller/servicio.</summary>
[Trait("Category", "RN01")]
[Trait("Plan", "S2-023")]
public sealed class PosRepositoryTenantIsolationTests
{
    [Theory]
    [InlineData("sale", "foreign")]
    [InlineData("item", "foreign")]
    [InlineData("delete", "foreign")]
    [InlineData("sale", "missing")]
    [InlineData("item", "missing")]
    [InlineData("delete", "missing")]
    [InlineData("sale", "tracked")]
    [InlineData("item", "tracked")]
    [InlineData("delete", "tracked")]
    [InlineData("sale", "tracked-missing")]
    [InlineData("item", "tracked-missing")]
    [InlineData("delete", "tracked-missing")]
    public async Task MutacionDirecta_AjenaOSinTenant_NoAlteraVentasItemsNiPagos(string operation, string mode)
    {
        await using var fixture = await CreateFixtureAsync();
        var a = fixture.TenantA;
        var b = fixture.TenantB;
        var before = await SnapshotAsync(fixture);
        await using var db = fixture.PosDb(a);
        var target = mode.Contains("missing") ? a : b;
        // A tracked entity must not turn Find/Local into an authorization bypass.
        var track = mode.StartsWith("tracked");
        Pos.Models.Venta sale;
        Pos.Models.ItemVenta item;
        if (track)
        {
            sale = await db.Ventas.IgnoreQueryFilters().SingleAsync(v => v.Id == target.VentaPendienteId);
            item = await db.ItemsVenta.IgnoreQueryFilters().SingleAsync(i => i.VentaId == sale.Id);
        }
        else
        {
            sale = new Pos.Models.Venta { Id = target.VentaPendienteId, TenantId = a.TenantId };
            var itemId = await db.ItemsVenta.IgnoreQueryFilters().Where(i => i.VentaId == sale.Id).Select(i => i.Id).SingleAsync();
            // Forge the parent as well: authorization must resolve the stored Venta, not input.VentaId.
            item = new Pos.Models.ItemVenta { Id = itemId, VentaId = a.VentaPendienteId, Venta = new Pos.Models.Venta { TenantId = a.TenantId } };
        }
        sale.Total = 9000m;
        sale.Estado = Pos.Models.EstadoVenta.COMPLETADA;
        sale.Pagos.Add(new Pos.Models.Pago { Monto = 9000m });
        item.Cantidad = 90m;
        item.Subtotal = 9000m;
        if (mode.Contains("missing")) db.CurrentTenantId = null;

        if (operation == "sale")
            await Assert.ThrowsAsync<KeyNotFoundException>(() => new Pos.Repositories.VentaRepository(db).ActualizarAsync(sale));
        else if (operation == "item")
            await Assert.ThrowsAsync<KeyNotFoundException>(() => new Pos.Repositories.ItemVentaRepository(db).ActualizarAsync(item));
        else
            await new Pos.Repositories.ItemVentaRepository(db).EliminarAsync(item);

        // A fresh context reads committed state, independent of the deliberately dirty identity map.
        Assert.Equal(before, await SnapshotAsync(fixture));
    }

    [Fact]
    public async Task MutacionesDirectas_Propias_ActualizanYEliminanSinAfectarTenantB()
    {
        await using var fixture = await CreateFixtureAsync();
        var a = fixture.TenantA;
        var beforeB = await SnapshotAsync(fixture, fixture.TenantB.TenantId);
        await using (var db = fixture.PosDb(a))
        {
            var repository = new Pos.Repositories.VentaRepository(db);
            var sale = (await repository.GetByIdAsync(a.VentaPendienteId))!;
            // The detached object's tenant is not authoritative, even for a legitimate ID.
            sale.TenantId = fixture.TenantB.TenantId;
            sale.Total = 238m;
            sale.Subtotal = 200m;
            sale.Impuestos = 38m;
            var saved = await repository.ActualizarAsync(sale);
            Assert.Equal(a.TenantId, saved.TenantId);
        }
        Guid itemId;
        await using (var db = fixture.PosDb(a))
        {
            var repository = new Pos.Repositories.ItemVentaRepository(db);
            var item = await db.ItemsVenta.AsNoTracking().SingleAsync(i => i.VentaId == a.VentaPendienteId);
            itemId = item.Id;
            item.Cantidad = 2m;
            item.Subtotal = 200m;
            await repository.ActualizarAsync(item);
        }
        await using (var verify = fixture.PosDb(a))
        {
            Assert.Equal(238m, (await verify.Ventas.SingleAsync(v => v.Id == a.VentaPendienteId)).Total);
            Assert.Equal(2m, (await verify.ItemsVenta.SingleAsync(i => i.Id == itemId)).Cantidad);
        }
        await using (var db = fixture.PosDb(a))
        {
            // A pretracked own entity remains a supported path.
            var item = await db.ItemsVenta.SingleAsync(i => i.Id == itemId);
            await new Pos.Repositories.ItemVentaRepository(db).EliminarAsync(item);
        }
        await using (var verify = fixture.PosDb(a))
            Assert.False(await verify.ItemsVenta.AnyAsync(i => i.Id == itemId));
        Assert.Equal(beforeB, await SnapshotAsync(fixture, fixture.TenantB.TenantId));
    }

    private static async Task<TwoTenantFixture> CreateFixtureAsync()
    {
        var connection = Environment.GetEnvironmentVariable("GLOBALMART_TEST_POSTGRES")
            ?? throw new InvalidOperationException("Configure GLOBALMART_TEST_POSTGRES; ver Fixtures/README.md.");
        var fixture = new TwoTenantFixture(connection);
        await fixture.InitializeAsync();
        return fixture;
    }

    private static async Task<string> SnapshotAsync(TwoTenantFixture fixture, Guid? tenant = null)
    {
        await using var db = fixture.PosDb(fixture.TenantA);
        var sales = await db.Ventas.IgnoreQueryFilters().Where(v => tenant == null || v.TenantId == tenant)
            .OrderBy(v => v.Id).Select(v => new { v.Id, v.TenantId, v.Estado, v.Subtotal, v.Impuestos, v.Total, v.MetodoPago }).ToListAsync();
        var ids = sales.Select(v => v.Id).ToArray();
        var items = await db.ItemsVenta.IgnoreQueryFilters().Where(i => ids.Contains(i.VentaId)).OrderBy(i => i.Id)
            .Select(i => new { i.Id, i.VentaId, i.ProductoId, i.NombreProducto, i.PrecioUnitario, i.Cantidad, i.PesoKg, i.Subtotal }).ToListAsync();
        var payments = await db.Pagos.IgnoreQueryFilters().Where(p => ids.Contains(p.VentaId)).OrderBy(p => p.Id)
            .Select(p => new { p.Id, p.VentaId, p.Metodo, p.Monto, p.Vuelto }).ToListAsync();
        return JsonSerializer.Serialize(new { sales, items, payments });
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WarehouseInventoryService.Data;
using WarehouseInventoryService.Models;
using WarehouseInventoryService.Services;
using Xunit;

namespace GlobalMart.Tests.MS4_Warehouse;

public sealed class RecepcionService_Tests
{
    [Fact]
    public async Task Registrar_CreaLote_SumaStock_YRegistraMovimiento()
    {
        var tenantId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var sucursalId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<WarehouseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        await using var db = new WarehouseDbContext(options) { CurrentTenantId = tenantId };
        db.Stocks.Add(new Stock
        {
            TenantId = tenantId,
            ProductoId = productoId,
            SucursalId = sucursalId,
            CantidadActual = 3
        });
        await db.SaveChangesAsync();

        var lote = await new RecepcionService(db).RegistrarAsync(
            tenantId, productoId, sucursalId, 2.5m, new DateOnly(2027, 2, 15), " Bodega A-03 ", default);

        Assert.Equal(5.5m, (await db.Stocks.SingleAsync()).CantidadActual);
        Assert.Equal("Bodega A-03", lote.Ubicacion);
        Assert.Equal(lote.Id, (await db.Lotes.SingleAsync()).Id);
        var movimiento = await db.Movimientos.SingleAsync();
        Assert.Equal("RECEPCION", movimiento.Tipo);
        Assert.Equal(2.5m, movimiento.Cantidad);
        Assert.Equal(tenantId, movimiento.TenantId);
    }
}

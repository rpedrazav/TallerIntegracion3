using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WarehouseInventoryService.Data;
using WarehouseInventoryService.Messaging;
using WarehouseInventoryService.Models;
using WarehouseInventoryService.Repositories;
using Xunit;

namespace GlobalMart.Tests.MS4_Warehouse;

public sealed class StockSinLotesTests : IDisposable
{
    private readonly Guid _tenant = Guid.NewGuid();
    private readonly Guid _sucursal = Guid.NewGuid();
    private readonly Guid _producto = Guid.NewGuid();
    private readonly DbContextOptions<WarehouseDbContext> _options =
        new DbContextOptionsBuilder<WarehouseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
    private readonly WarehouseDbContext _db;

    public StockSinLotesTests()
    {
        _db = new WarehouseDbContext(_options) { CurrentTenantId = _tenant };
        _db.Stocks.Add(new Stock
        {
            TenantId = _tenant, SucursalId = _sucursal, ProductoId = _producto, CantidadActual = 10
        });
        _db.SaveChanges();
    }

    private SaleEventProcessor Processor(WarehouseDbContext db) =>
        new(db, new StockRepository(db), NullLogger<SaleEventProcessor>.Instance);

    private string Payload(Guid eventId, params (Guid Producto, decimal Cantidad)[] items) =>
        JsonSerializer.Serialize(new
        {
            event_id = eventId, venta_id = Guid.NewGuid(), tenant_id = _tenant, sucursal_id = _sucursal,
            items = items.Select(i => new { producto_id = i.Producto, cantidad = i.Cantidad })
        });

    [Theory]
    [InlineData(2, 8)]
    [InlineData(10, 0)]
    [InlineData(0.75, 9.25)]
    public void SinLotes_PersisteDescuentoMovimientoYEvento_UnaSolaVez(decimal cantidad, decimal esperado)
    {
        var eventId = Guid.NewGuid();
        var payload = Payload(eventId, (_producto, cantidad));
        Processor(_db).ProcesarEvento(payload).Status.Should().Be(SaleEventProcessingStatus.Processed);

        using var verification = new WarehouseDbContext(_options) { CurrentTenantId = _tenant };
        Processor(verification).ProcesarEvento(payload).Status.Should().Be(SaleEventProcessingStatus.Duplicate);
        verification.Stocks.Single().CantidadActual.Should().Be(esperado);
        verification.Lotes.Should().BeEmpty();
        var movimiento = verification.Movimientos.Should().ContainSingle().Subject;
        movimiento.Tipo.Should().Be("VENTA");
        movimiento.Cantidad.Should().Be(cantidad);
        movimiento.TenantId.Should().Be(_tenant);
        movimiento.ProductoId.Should().Be(_producto);
        verification.EventosKafkaProcesados.Should().ContainSingle(e => e.EventId == eventId);
    }

    [Fact]
    public void SegundoItemSinStock_NoPersisteDescuentoParcial_YPermiteReintento()
    {
        var segundo = Guid.NewGuid();
        _db.Stocks.Add(new Stock
        {
            TenantId = _tenant, SucursalId = _sucursal, ProductoId = segundo, CantidadActual = 1
        });
        _db.SaveChanges();
        var eventId = Guid.NewGuid();
        var payload = Payload(eventId, (_producto, 2), (segundo, 3));

        Processor(_db).ProcesarEvento(payload).Status.Should().Be(SaleEventProcessingStatus.TransientFailure);
        // El consumidor crea un scope nuevo por intento; verificar la BD sin cambios rastreados.
        using var retry = new WarehouseDbContext(_options) { CurrentTenantId = _tenant };
        retry.Stocks.Single(s => s.ProductoId == _producto).CantidadActual.Should().Be(10);
        retry.Stocks.Single(s => s.ProductoId == segundo).CantidadActual.Should().Be(1);
        retry.Movimientos.Should().BeEmpty();
        retry.EventosKafkaProcesados.Should().BeEmpty();
        retry.Stocks.Single(s => s.ProductoId == segundo).CantidadActual = 3;
        retry.SaveChanges();
        Processor(retry).ProcesarEvento(payload).Status.Should().Be(SaleEventProcessingStatus.Processed);
        using var verification = new WarehouseDbContext(_options) { CurrentTenantId = _tenant };
        verification.Stocks.Single(s => s.ProductoId == _producto).CantidadActual.Should().Be(8);
        verification.Stocks.Single(s => s.ProductoId == segundo).CantidadActual.Should().Be(0);
        verification.Movimientos.Should().HaveCount(2);
        verification.EventosKafkaProcesados.Should().ContainSingle(e => e.EventId == eventId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LotesDeOtraSucursalOTenant_NoInterfierenNiSeDescuentan(bool otroTenant)
    {
        var sucursalAjena = Guid.NewGuid();
        var tenantAjeno = otroTenant ? Guid.NewGuid() : _tenant;
        _db.Stocks.Add(new Stock
        {
            TenantId = tenantAjeno, SucursalId = sucursalAjena, ProductoId = _producto, CantidadActual = 20
        });
        _db.Lotes.Add(new Lote
        {
            TenantId = tenantAjeno, SucursalId = otroTenant ? _sucursal : sucursalAjena,
            ProductoId = _producto, Cantidad = 1, FechaVencimiento = new DateOnly(2027, 1, 1)
        });
        _db.SaveChanges();

        Processor(_db).ProcesarEvento(Payload(Guid.NewGuid(), (_producto, 2)))
            .Status.Should().Be(SaleEventProcessingStatus.Processed);
        using var verification = new WarehouseDbContext(_options) { CurrentTenantId = _tenant };
        verification.Stocks.Single(s => s.SucursalId == _sucursal).CantidadActual.Should().Be(8);
        verification.Stocks.IgnoreQueryFilters().Single(s => s.SucursalId == sucursalAjena)
            .CantidadActual.Should().Be(20);
        verification.Lotes.IgnoreQueryFilters().Single().Cantidad.Should().Be(1);
    }

    [Fact]
    public void LoteAgotado_NoSeConfundeConProductoSinLotes()
    {
        _db.Lotes.Add(new Lote
        {
            TenantId = _tenant, SucursalId = _sucursal, ProductoId = _producto,
            Cantidad = 0, FechaVencimiento = new DateOnly(2027, 1, 1)
        });
        _db.SaveChanges();
        Processor(_db).ProcesarEvento(Payload(Guid.NewGuid(), (_producto, 2)))
            .Status.Should().Be(SaleEventProcessingStatus.TransientFailure);
        using var verification = new WarehouseDbContext(_options) { CurrentTenantId = _tenant };
        verification.Stocks.Single().CantidadActual.Should().Be(10);
        verification.Lotes.Single().Cantidad.Should().Be(0);
        verification.Movimientos.Should().BeEmpty();
        verification.EventosKafkaProcesados.Should().BeEmpty();
    }

    public void Dispose() => _db.Dispose();
}

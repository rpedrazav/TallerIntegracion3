using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using WarehouseInventoryService.Data;
using WarehouseInventoryService.Messaging;
using WarehouseInventoryService.Models;
using WarehouseInventoryService.Repositories;
using Xunit;

namespace GlobalMart.Tests.MS4_Warehouse;

/// <summary>
/// Tests unitarios para el procesador de eventos de venta de MS-4 (WarehouseInventoryService).
/// Cubre:
/// - TI3-253: Log estructurado y registro de MovimientoStock para auditoría al descontar stock.
/// - S2-098: Rechazo recuperable cuando no existe stock suficiente.
/// </summary>
public class SaleEventProcessor_MovimientoStockTests : IDisposable
{
    private static readonly Guid TenantId   = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid SucursalId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");
    private static readonly Guid ProductoId = Guid.Parse("cccccccc-0000-0000-0000-000000000001");
    private static readonly Guid VentaId    = Guid.Parse("dddddddd-0000-0000-0000-000000000001");
    private static readonly Guid EventId    = Guid.Parse("eeeeeeee-0000-0000-0000-000000000001");

    private readonly WarehouseDbContext _context;
    private readonly Mock<IStockRepository> _stockRepoMock;
    private readonly Mock<ILogger<SaleEventProcessor>> _loggerMock;
    private readonly SaleEventProcessor _processor;

    public SaleEventProcessor_MovimientoStockTests()
    {
        var options = new DbContextOptionsBuilder<WarehouseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new WarehouseDbContext(options);
        _stockRepoMock = new Mock<IStockRepository>();
        _loggerMock = new Mock<ILogger<SaleEventProcessor>>();

        _processor = new SaleEventProcessor(_context, _stockRepoMock.Object, _loggerMock.Object);
    }

    private static string CrearPayload(Guid eventId, Guid productoId, decimal cantidad)
    {
        var evento = new
        {
            event_id = eventId,
            venta_id = VentaId,
            tenant_id = TenantId,
            sucursal_id = SucursalId,
            items = new[]
            {
                new { producto_id = productoId, cantidad }
            },
            total = cantidad * 1500,
            timestamp = DateTime.UtcNow
        };

        return JsonSerializer.Serialize(evento);
    }

    // ────────────────────────────────────────────────────────────────────────
    // TI3-253: Movimientos de stock y persistencia en tabla de auditoría
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ProcesarEvento_ConStockExistente_RegistraMovimientoStockConDetalle()
    {
        // Arrange
        var payload = CrearPayload(EventId, ProductoId, cantidad: 4);

        _stockRepoMock
            .Setup(r => r.Descontar(ProductoId, 4, TenantId, SucursalId))
            .ReturnsAsync(new Stock
            {
                ProductoId = ProductoId,
                SucursalId = SucursalId,
                TenantId = TenantId,
                CantidadActual = 16,
                StockMinimo = 5
            });

        // Act
        var resultado = _processor.ProcesarEvento(payload);

        // Assert
        resultado.Status.Should().Be(SaleEventProcessingStatus.Processed);

        var movimiento = _context.Movimientos.FirstOrDefault(m => m.ProductoId == ProductoId);
        movimiento.Should().NotBeNull();
        movimiento!.Tipo.Should().Be("VENTA");
        movimiento.Cantidad.Should().Be(4);
        movimiento.TenantId.Should().Be(TenantId);
        movimiento.Motivo.Should().Contain("20").And.Contain("16");
    }

    // ────────────────────────────────────────────────────────────────────────
    // S2-098: Stock inexistente o insuficiente -> reintento sin descuento
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ProcesarEvento_SinStockPrevio_RetornaFalloTransitorioSinRegistrar()
    {
        // Arrange
        var payload = CrearPayload(EventId, ProductoId, cantidad: 3);

        _stockRepoMock
            .Setup(r => r.Descontar(ProductoId, 3, TenantId, SucursalId))
            .ReturnsAsync((Stock?)null);

        // Act
        var resultado = _processor.ProcesarEvento(payload);

        // Assert
        resultado.Status.Should().Be(SaleEventProcessingStatus.TransientFailure);
        resultado.Retry.Should().BeTrue();
        _context.Movimientos.Should().BeEmpty();
        _context.EventosKafkaProcesados.Should().BeEmpty();
    }

    [Fact]
    public void ProcesarEvento_StockInsuficiente_NoBajaDeCero()
    {
        _context.CurrentTenantId = TenantId;
        _context.Stocks.Add(new Stock
        {
            ProductoId = ProductoId,
            SucursalId = SucursalId,
            TenantId = TenantId,
            CantidadActual = 2
        });
        _context.SaveChanges();
        var processor = new SaleEventProcessor(
            _context,
            new StockRepository(_context),
            Mock.Of<ILogger<SaleEventProcessor>>());

        var resultado = processor.ProcesarEvento(CrearPayload(EventId, ProductoId, cantidad: 3));

        resultado.Status.Should().Be(SaleEventProcessingStatus.TransientFailure);
        _context.Stocks.Single().CantidadActual.Should().Be(2);
        _context.Movimientos.Should().BeEmpty();
        _context.EventosKafkaProcesados.Should().BeEmpty();
    }

    [Fact]
    public void ProcesarEvento_ConDosLotes_DescuentaPrimeroElQueVenceAntes()
    {
        var loteTemprano = new Lote
        {
            ProductoId = ProductoId,
            SucursalId = SucursalId,
            TenantId = TenantId,
            FechaVencimiento = new DateOnly(2027, 1, 15),
            Cantidad = 5
        };
        var loteTardio = new Lote
        {
            ProductoId = ProductoId,
            SucursalId = SucursalId,
            TenantId = TenantId,
            FechaVencimiento = new DateOnly(2027, 6, 15),
            Cantidad = 5
        };
        _context.CurrentTenantId = TenantId;
        _context.Stocks.Add(new Stock
        {
            ProductoId = ProductoId,
            SucursalId = SucursalId,
            TenantId = TenantId,
            CantidadActual = 10
        });
        _context.Lotes.AddRange(loteTardio, loteTemprano);
        _context.SaveChanges();
        var processor = new SaleEventProcessor(
            _context,
            new StockRepository(_context),
            Mock.Of<ILogger<SaleEventProcessor>>());

        var resultado = processor.ProcesarEvento(CrearPayload(EventId, ProductoId, cantidad: 8));

        resultado.Status.Should().Be(SaleEventProcessingStatus.Processed);
        _context.Lotes.Single(l => l.Id == loteTemprano.Id).Cantidad.Should().Be(0);
        _context.Lotes.Single(l => l.Id == loteTardio.Id).Cantidad.Should().Be(2);
        _context.Stocks.Single().CantidadActual.Should().Be(2);
        _context.EventosKafkaProcesados.Should().ContainSingle(e => e.EventId == EventId);
    }

    [Fact]
    public void ProcesarEvento_MultiplesItems_RegistraTodosLosMovimientos()
    {
        // Arrange
        var producto2 = Guid.Parse("cccccccc-0000-0000-0000-000000000002");
        var evento = new
        {
            event_id = EventId,
            venta_id = VentaId,
            tenant_id = TenantId,
            sucursal_id = SucursalId,
            items = new[]
            {
                new { producto_id = ProductoId, cantidad = 2m },
                new { producto_id = producto2, cantidad = 5m }
            },
            total = 10000,
            timestamp = DateTime.UtcNow
        };
        var payload = JsonSerializer.Serialize(evento);

        _stockRepoMock
            .Setup(r => r.Descontar(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(new Stock { CantidadActual = 10 });

        // Act
        var resultado = _processor.ProcesarEvento(payload);

        // Assert
        resultado.Status.Should().Be(SaleEventProcessingStatus.Processed);
        _context.Movimientos.Count().Should().Be(2);
    }

    [Fact]
    public void ProcesarEvento_SiFallaSegundoItem_NoPersistePrimerDescuentoNiEventId()
    {
        var producto2 = Guid.Parse("cccccccc-0000-0000-0000-000000000002");
        var databaseRoot = new InMemoryDatabaseRoot();
        var options = new DbContextOptionsBuilder<WarehouseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), databaseRoot)
            .Options;

        using (var seed = new WarehouseDbContext(options) { CurrentTenantId = TenantId })
        {
            seed.Stocks.AddRange(
                new Stock { ProductoId = ProductoId, SucursalId = SucursalId, TenantId = TenantId, CantidadActual = 10 },
                new Stock { ProductoId = producto2, SucursalId = SucursalId, TenantId = TenantId, CantidadActual = 10 });
            seed.Lotes.AddRange(
                new Lote
                {
                    ProductoId = ProductoId,
                    SucursalId = SucursalId,
                    TenantId = TenantId,
                    FechaVencimiento = new DateOnly(2027, 1, 15),
                    Cantidad = 10
                },
                new Lote
                {
                    ProductoId = producto2,
                    SucursalId = SucursalId,
                    TenantId = TenantId,
                    FechaVencimiento = new DateOnly(2027, 1, 15),
                    Cantidad = 10
                });
            seed.SaveChanges();
        }

        using var processingContext = new WarehouseDbContext(options) { CurrentTenantId = TenantId };
        var realRepository = new StockRepository(processingContext);
        var repository = new Mock<IStockRepository>();
        var descuentos = 0;
        repository
            .Setup(r => r.Descontar(It.IsAny<Guid>(), It.IsAny<decimal>(), TenantId, SucursalId))
            .Returns((Guid productoId, decimal cantidad, Guid tenantId, Guid sucursalId) =>
            {
                if (++descuentos == 2)
                {
                    throw new InvalidOperationException("Fallo simulado en segundo ítem");
                }

                return realRepository.Descontar(productoId, cantidad, tenantId, sucursalId);
            });

        var payload = JsonSerializer.Serialize(new
        {
            event_id = EventId,
            venta_id = VentaId,
            tenant_id = TenantId,
            sucursal_id = SucursalId,
            items = new[]
            {
                new { producto_id = ProductoId, cantidad = 2m },
                new { producto_id = producto2, cantidad = 3m }
            }
        });
        var processor = new SaleEventProcessor(
            processingContext,
            repository.Object,
            Mock.Of<ILogger<SaleEventProcessor>>());

        var act = () => processor.ProcesarEvento(payload);

        act.Should().Throw<InvalidOperationException>();
        using var verificationContext = new WarehouseDbContext(options) { CurrentTenantId = TenantId };
        verificationContext.Stocks.Single(s => s.ProductoId == ProductoId).CantidadActual.Should().Be(10);
        verificationContext.Lotes.Single(l => l.ProductoId == ProductoId).Cantidad.Should().Be(10);
        verificationContext.Movimientos.Should().BeEmpty();
        verificationContext.EventosKafkaProcesados.Should().BeEmpty();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

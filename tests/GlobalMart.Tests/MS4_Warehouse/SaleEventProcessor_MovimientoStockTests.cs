using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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
/// - TI3-254: Manejo de caso borde cuando un producto no tiene registro en Stock -> CrearYDescontar.
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
            .Setup(r => r.GetByProducto(ProductoId, SucursalId, TenantId))
            .ReturnsAsync(new Stock
            {
                ProductoId = ProductoId,
                SucursalId = SucursalId,
                TenantId = TenantId,
                CantidadActual = 20,
                StockMinimo = 5
            });

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
        resultado.Should().BeTrue();

        var movimiento = _context.Movimientos.FirstOrDefault(m => m.ProductoId == ProductoId);
        movimiento.Should().NotBeNull();
        movimiento!.Tipo.Should().Be("VENTA");
        movimiento.Cantidad.Should().Be(4);
        movimiento.TenantId.Should().Be(TenantId);
        movimiento.Motivo.Should().Contain("20").And.Contain("16");
    }

    // ────────────────────────────────────────────────────────────────────────
    // TI3-254: Caso borde — Producto sin registro en tabla Stock -> CrearYDescontar
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ProcesarEvento_SinStockPrevio_LlamaCrearYDescontarYRegistraMovimiento()
    {
        // Arrange
        var payload = CrearPayload(EventId, ProductoId, cantidad: 3);

        // No existe en BD
        _stockRepoMock
            .Setup(r => r.GetByProducto(ProductoId, SucursalId, TenantId))
            .ReturnsAsync((Stock?)null);

        // Descontar falla (retorna null) porque no existe registro
        _stockRepoMock
            .Setup(r => r.Descontar(ProductoId, 3, TenantId, SucursalId))
            .ReturnsAsync((Stock?)null);

        // CrearYDescontar crea el registro con stock negativo
        _stockRepoMock
            .Setup(r => r.CrearYDescontar(ProductoId, 3, TenantId, SucursalId))
            .ReturnsAsync(new Stock
            {
                ProductoId = ProductoId,
                SucursalId = SucursalId,
                TenantId = TenantId,
                CantidadActual = -3,
                StockMinimo = 0
            });

        // Act
        var resultado = _processor.ProcesarEvento(payload);

        // Assert
        resultado.Should().BeTrue();

        // Se debió invocar CrearYDescontar
        _stockRepoMock.Verify(
            r => r.CrearYDescontar(ProductoId, 3, TenantId, SucursalId),
            Times.Once);

        // Movimiento registrado en tabla de auditoría indicando registro creado
        var movimiento = _context.Movimientos.FirstOrDefault(m => m.ProductoId == ProductoId);
        movimiento.Should().NotBeNull();
        movimiento!.Tipo.Should().Be("VENTA");
        movimiento.Cantidad.Should().Be(3);
        movimiento.Motivo.Should().Contain("registro creado");
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
        resultado.Should().BeTrue();
        _context.Movimientos.Count().Should().Be(2);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

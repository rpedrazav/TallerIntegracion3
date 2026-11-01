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
/// Tests unitarios del consumer Kafka de MS-4 (WarehouseInventoryService).
/// Cubre TI3-252: verificar que si se publica el mismo evento dos veces
/// (mismo event_id), el stock se descuenta UNA sola vez (idempotencia).
///
/// Usa EF Core InMemory para la tabla EventosKafkaProcesados y Moq para IStockRepository.
/// No requiere Kafka ni PostgreSQL reales.
/// </summary>
public class KafkaConsumer_IdempotenciaTests : IDisposable
{
    // ── Datos de prueba ───────────────────────────────────────────────────
    private static readonly Guid TenantId    = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid SucursalId  = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");
    private static readonly Guid ProductoId  = Guid.Parse("cccccccc-0000-0000-0000-000000000001");
    private static readonly Guid VentaId     = Guid.Parse("dddddddd-0000-0000-0000-000000000001");
    private static readonly Guid EventId     = Guid.Parse("eeeeeeee-0000-0000-0000-000000000001");

    private readonly WarehouseDbContext _context;
    private readonly Mock<IStockRepository> _stockRepoMock;
    private readonly SaleEventProcessor _processor;

    public KafkaConsumer_IdempotenciaTests()
    {
        // EF Core InMemory — cada test tiene su propia BD limpia
        var options = new DbContextOptionsBuilder<WarehouseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new WarehouseDbContext(options);

        _stockRepoMock = new Mock<IStockRepository>();

        // Por defecto, Descontar retorna un Stock válido (simula que el stock existía)
        _stockRepoMock
            .Setup(r => r.Descontar(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(new Stock
            {
                ProductoId = ProductoId,
                SucursalId = SucursalId,
                TenantId = TenantId,
                CantidadActual = 90
            });

        var loggerMock = new Mock<ILogger<SaleEventProcessor>>();
        _processor = new SaleEventProcessor(_context, _stockRepoMock.Object, loggerMock.Object);
    }

    /// <summary>
    /// Crea un payload JSON de sale.completed con un event_id y items dados.
    /// </summary>
    private static string CrearPayload(Guid eventId, decimal cantidad = 5)
    {
        var evento = new
        {
            event_id = eventId,
            venta_id = VentaId,
            tenant_id = TenantId,
            sucursal_id = SucursalId,
            items = new[]
            {
                new { producto_id = ProductoId, cantidad }
            },
            total = cantidad * 1000,
            timestamp = DateTime.UtcNow
        };

        return JsonSerializer.Serialize(evento);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  TI3-252: Evento duplicado (mismo event_id) → solo se procesa una vez
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ProcesarEvento_PrimeraVez_DescuentaStockYRegistraEvento()
    {
        // Arrange
        var payload = CrearPayload(EventId, cantidad: 3);

        // Act
        var resultado = _processor.ProcesarEvento(payload);

        // Assert — Se procesó exitosamente
        resultado.Should().BeTrue();

        // Assert — El event_id quedó registrado en la tabla de idempotencia
        _context.EventosKafkaProcesados.Any(e => e.EventId == EventId).Should().BeTrue();

        // Assert — Se llamó a Descontar exactamente UNA vez
        _stockRepoMock.Verify(
            r => r.Descontar(ProductoId, 3, TenantId, SucursalId),
            Times.Once);
    }

    [Fact]
    public void ProcesarEvento_MismoEventIdDosVeces_SegundaVezEsDescartada()
    {
        // Arrange
        var payload = CrearPayload(EventId, cantidad: 5);

        // Act — Primera vez: se procesa
        var primera = _processor.ProcesarEvento(payload);

        // Act — Segunda vez (mismo event_id): se descarta por idempotencia
        var segunda = _processor.ProcesarEvento(payload);

        // Assert
        primera.Should().BeTrue("la primera vez se debe procesar");
        segunda.Should().BeFalse("la segunda vez debe ser descartada (duplicado)");

        // Assert — Solo se registró UNA entrada en la tabla de idempotencia
        _context.EventosKafkaProcesados.Count().Should().Be(1);

        // Assert — Se llamó a Descontar EXACTAMENTE UNA vez (no dos)
        _stockRepoMock.Verify(
            r => r.Descontar(ProductoId, 5, TenantId, SucursalId),
            Times.Once,
            "El stock debe descontarse solo la primera vez, no cuando llega el duplicado");
    }

    [Fact]
    public void ProcesarEvento_MismoEventIdTresVeces_SoloUnaEjecucion()
    {
        // Arrange
        var payload = CrearPayload(EventId, cantidad: 2);

        // Act — Tres envíos del mismo evento
        var r1 = _processor.ProcesarEvento(payload);
        var r2 = _processor.ProcesarEvento(payload);
        var r3 = _processor.ProcesarEvento(payload);

        // Assert
        r1.Should().BeTrue();
        r2.Should().BeFalse();
        r3.Should().BeFalse();

        // Assert — Solo un registro en la tabla de idempotencia
        _context.EventosKafkaProcesados.Count().Should().Be(1);

        // Assert — Descontar se llamó solo una vez
        _stockRepoMock.Verify(
            r => r.Descontar(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<Guid>(), It.IsAny<Guid>()),
            Times.Once);
    }

    [Fact]
    public void ProcesarEvento_DosEventosDistintos_AmbosSeProcesanCorrectamente()
    {
        // Arrange — Dos eventos con event_id DIFERENTE
        var eventId2 = Guid.NewGuid();
        var payload1 = CrearPayload(EventId, cantidad: 3);
        var payload2 = CrearPayload(eventId2, cantidad: 7);

        // Act
        var r1 = _processor.ProcesarEvento(payload1);
        var r2 = _processor.ProcesarEvento(payload2);

        // Assert — Ambos se procesan porque tienen distinto event_id
        r1.Should().BeTrue();
        r2.Should().BeTrue();

        // Assert — Dos registros en la tabla de idempotencia
        _context.EventosKafkaProcesados.Count().Should().Be(2);

        // Assert — Descontar se llamó DOS veces (una por cada evento)
        _stockRepoMock.Verify(
            r => r.Descontar(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<Guid>(), It.IsAny<Guid>()),
            Times.Exactly(2));
    }

    [Fact]
    public void ProcesarEvento_PayloadInvalido_RetornaFalseSinRegistrar()
    {
        // Act
        var resultado = _processor.ProcesarEvento("esto no es JSON válido {{{");

        // Assert
        resultado.Should().BeFalse();
        _context.EventosKafkaProcesados.Count().Should().Be(0);
        _stockRepoMock.Verify(
            r => r.Descontar(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<Guid>(), It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public void ProcesarEvento_EventoSinItems_RetornaFalseSinRegistrar()
    {
        // Arrange — Evento sin items
        var evento = new
        {
            event_id = EventId,
            venta_id = VentaId,
            tenant_id = TenantId,
            sucursal_id = SucursalId,
            items = Array.Empty<object>(),
            total = 0,
            timestamp = DateTime.UtcNow
        };
        var payload = JsonSerializer.Serialize(evento);

        // Act
        var resultado = _processor.ProcesarEvento(payload);

        // Assert
        resultado.Should().BeFalse();
        _context.EventosKafkaProcesados.Count().Should().Be(0);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using POSCartService.Controllers;
using POSCartService.Data;
using POSCartService.DTOs;
using POSCartService.Exceptions;
using POSCartService.Messaging;
using POSCartService.Models;
using POSCartService.Repositories;
using POSCartService.Services;
using Xunit;

namespace GlobalMart.Tests.MS5_POS;

/// <summary>
/// Suite de pruebas para S2-059 (Logs de correlación sin secretos) y S2-060 (Smoke de despliegue de tarjeta).
/// </summary>
public class CobroTarjetaCorrelationTests
{
    private class TestLogger<T> : ILogger<T>
    {
        public List<string> LogEntries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            LogEntries.Add($"[{logLevel}] {message}");
        }
    }

    private class TestKafkaProducer : IKafkaProducerService
    {
        public List<SaleCompletedEvent> PublishedEvents { get; } = new();

        public Task PublicarSaleCompletedAsync(SaleCompletedEvent evento, CancellationToken cancellationToken = default)
        {
            PublishedEvents.Add(evento);
            return Task.CompletedTask;
        }

        public Task PublicarAsync(string topico, string mensaje, string? clave = null, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private static (PosCartDbContext db, Guid tenantId, Guid cajeroId, Guid sucursalId, Turno turno) CrearEntornoEnMemoria()
    {
        var tenantId = Guid.NewGuid();
        var cajeroId = Guid.NewGuid();
        var sucursalId = Guid.NewGuid();

        var options = new DbContextOptionsBuilder<PosCartDbContext>()
            .UseInMemoryDatabase($"Test_CobroTarjeta_{Guid.NewGuid()}")
            .Options;

        var db = new PosCartDbContext(options) { CurrentTenantId = tenantId };

        var turno = new Turno
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CajeroId = cajeroId,
            SucursalId = sucursalId,
            MontoApertura = 1000m,
            Estado = EstadoTurno.ABIERTO,
            AbiertoAt = DateTime.UtcNow
        };
        db.Turnos.Add(turno);
        db.SaveChanges();

        return (db, tenantId, cajeroId, sucursalId, turno);
    }

    private static Venta CrearVentaPendiente(PosCartDbContext db, Guid tenantId, Guid turnoId, Guid cajeroId, Guid sucursalId, decimal total = 1500m)
    {
        var venta = new Venta
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TurnoId = turnoId,
            CajeroId = cajeroId,
            SucursalId = sucursalId,
            Subtotal = total / 1.19m,
            Impuestos = total - (total / 1.19m),
            Total = total,
            MetodoPago = MetodoPagoVenta.TARJETA,
            Estado = EstadoVenta.PENDIENTE,
            CreatedAt = DateTime.UtcNow,
            Items = new List<ItemVenta>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductoId = Guid.NewGuid(),
                    NombreProducto = "Producto de prueba",
                    PrecioUnitario = total,
                    Cantidad = 1,
                    Subtotal = total
                }
            }
        };

        db.Ventas.Add(venta);
        db.SaveChanges();
        return venta;
    }

    private static HttpContext CrearHttpContext(Guid tenantId, Guid cajeroId)
    {
        var context = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim("cajero_id", cajeroId.ToString()),
            new Claim("tenant_id", tenantId.ToString()),
            new Claim(ClaimTypes.Role, "CAJERO")
        };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        return context;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // S2-059: AGREGAR LOGS DE CORRELACIÓN DE COBRO
    // Criterio de Aceptación: Una prueba permite seguir un pago completo sin exponer secretos.
    // ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task S2_059_CobroTarjeta_RegistraLogsDeCorrelacion_ConIDsOpacos_SinSecretos()
    {
        // Arrange
        var (db, tenantId, cajeroId, sucursalId, turno) = CrearEntornoEnMemoria();
        var venta = CrearVentaPendiente(db, tenantId, turno.Id, cajeroId, sucursalId, 2500m);

        var loggerClient = new TestLogger<PasarelaPagoClient>();
        var loggerService = new TestLogger<VentaService>();
        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(f => f.CreateClient("PasarelaPago")).Returns(new HttpClient());

        var pasarelaClient = new PasarelaPagoClient(mockFactory.Object, loggerClient);
        var kafkaProducer = new TestKafkaProducer();
        var ventaRepo = new VentaRepository(db);
        var ventaService = new VentaService(
            ventaRepo,
            itemVentaRepository: null,
            kafkaProducer: kafkaProducer,
            pasarelaClient: pasarelaClient,
            logger: loggerService);

        var customCorrelationId = "corr-test-opaco-" + Guid.NewGuid().ToString("N");
        var request = new CobrarTarjetaRequest
        {
            CorrelationId = customCorrelationId,
            TokenTransaccion = "tok_simulado_opaco_987",
            SimularResultado = "APROBADO"
        };

        // Act
        var resultado = await ventaService.CobrarTarjetaAsync(venta.Id, request);

        // Assert: 1. El pago fue exitoso
        resultado.Exitoso.Should().BeTrue();
        resultado.CorrelationId.Should().Be(customCorrelationId);
        resultado.IntentoId.Should().NotBeNullOrWhiteSpace();
        resultado.CodigoAutorizacion.Should().StartWith("AUTH-");

        // Assert: 2. Trazabilidad completa en logs a través del correlationId y intentoId
        var todosLosLogs = loggerClient.LogEntries.Concat(loggerService.LogEntries).ToList();
        todosLosLogs.Should().NotBeEmpty();

        var logsConCorrelation = todosLosLogs.Where(l => l.Contains(customCorrelationId)).ToList();
        logsConCorrelation.Count.Should().BeGreaterThanOrEqualTo(2,
            "Debe haber logs de inicio y de éxito asociados al CorrelationId");

        var logsConVentaId = todosLosLogs.Where(l => l.Contains(venta.Id.ToString())).ToList();
        logsConVentaId.Should().NotBeEmpty("Los logs deben correlacionar el VentaId");

        var logsConIntentoId = todosLosLogs.Where(l => l.Contains(resultado.IntentoId)).ToList();
        logsConIntentoId.Should().NotBeEmpty("Los logs deben correlacionar el IntentoId único");

        // Assert: 3. Aceptación: NO exponer secretos en ningún log (PAN de tarjeta, CVV, PIN, claves)
        foreach (var log in todosLosLogs)
        {
            // Verificación contra números de tarjeta de crédito (13 a 19 dígitos continuos)
            System.Text.RegularExpressions.Regex.IsMatch(log, @"\b(?:4[0-9]{12}(?:[0-9]{3})?|5[1-5][0-9]{14}|3[47][0-9]{13})\b")
                .Should().BeFalse($"El log no debe contener números de tarjeta de crédito: {log}");

            // Verificación contra palabras secretas o tokens sensibles
            var logLower = log.ToLowerInvariant();
            logLower.Should().NotContain("cvv");
            logLower.Should().NotContain("cvc");
            logLower.Should().NotContain("pin=");
            logLower.Should().NotContain("password");
            logLower.Should().NotContain("secret");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // S2-060: EJECUTAR SMOKE DE DESPLIEGUE DE TARJETA
    // Criterio de Aceptación: Las tres situaciones terminan con estado de venta correcto.
    // Situación 1: Arranque / Aprobación -> Venta pasa a COMPLETADA, Pago registrado, Kafka sale.completed
    // Situación 2: Rechazo -> Venta permanece PENDIENTE, sin pago, sin Kafka
    // Situación 3: Pérdida temporal del simulador -> Venta permanece PENDIENTE, excepción controlada / 503
    // ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task S2_060_Situacion1_Aprobacion_TerminaConVentaCompletada_Y_PagoRegistrado()
    {
        // Arrange
        var (db, tenantId, cajeroId, sucursalId, turno) = CrearEntornoEnMemoria();
        var venta = CrearVentaPendiente(db, tenantId, turno.Id, cajeroId, sucursalId, 3200m);

        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(f => f.CreateClient("PasarelaPago")).Returns(new HttpClient());
        var pasarelaClient = new PasarelaPagoClient(mockFactory.Object, new TestLogger<PasarelaPagoClient>());
        var kafkaProducer = new TestKafkaProducer();
        var ventaRepo = new VentaRepository(db);
        var ventaService = new VentaService(ventaRepo, null, kafkaProducer, pasarelaClient);

        var controller = new VentasController(ventaService, new TurnoService(new TurnoRepository(db)), Mock.Of<ICatalogClient>(), Mock.Of<ITaxClient>())
        {
            ControllerContext = new ControllerContext { HttpContext = CrearHttpContext(tenantId, cajeroId) }
        };

        var request = new CobrarTarjetaRequest
        {
            TokenTransaccion = "tok_aprobado_123",
            SimularResultado = "APROBADO"
        };

        // Act
        var actionResult = await controller.CobrarTarjeta(venta.Id, request, CancellationToken.None);

        // Assert: Endpoint retorna 200 OK con datos de autorización
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)actionResult;
        var response = okResult.Value as CobrarTarjetaResponse;
        response.Should().NotBeNull();
        response!.Estado.Should().Be("APROBADO");
        response.CodigoAutorizacion.Should().StartWith("AUTH-");

        // Assert: Estado de venta correcto: COMPLETADA en base de datos
        var ventaEnDb = await ventaRepo.GetByIdAsync(venta.Id);
        ventaEnDb.Should().NotBeNull();
        ventaEnDb!.Estado.Should().Be(EstadoVenta.COMPLETADA);
        ventaEnDb.MetodoPago.Should().Be(MetodoPagoVenta.TARJETA);

        // Assert: Pago persistido en BD
        ventaEnDb.Pagos.Should().HaveCount(1);
        var pago = ventaEnDb.Pagos.First();
        pago.Metodo.Should().Be(MetodoPago.TARJETA);
        pago.Monto.Should().Be(3200m);
        pago.Vuelto.Should().Be(0m);

        // Assert: Publicación a Kafka
        kafkaProducer.PublishedEvents.Should().HaveCount(1);
        var eventoKafka = kafkaProducer.PublishedEvents.First();
        eventoKafka.VentaId.Should().Be(venta.Id);
        eventoKafka.MetodoPago.Should().Be("TARJETA");
        eventoKafka.Total.Should().Be(3200m);
    }

    [Fact]
    public async Task S2_060_Situacion2_Rechazo_TerminaConVentaPendiente_SinModificarBD()
    {
        // Arrange
        var (db, tenantId, cajeroId, sucursalId, turno) = CrearEntornoEnMemoria();
        var venta = CrearVentaPendiente(db, tenantId, turno.Id, cajeroId, sucursalId, 4500m);

        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(f => f.CreateClient("PasarelaPago")).Returns(new HttpClient());
        var pasarelaClient = new PasarelaPagoClient(mockFactory.Object, new TestLogger<PasarelaPagoClient>());
        var kafkaProducer = new TestKafkaProducer();
        var ventaRepo = new VentaRepository(db);
        var ventaService = new VentaService(ventaRepo, null, kafkaProducer, pasarelaClient);

        var controller = new VentasController(ventaService, new TurnoService(new TurnoRepository(db)), Mock.Of<ICatalogClient>(), Mock.Of<ITaxClient>())
        {
            ControllerContext = new ControllerContext { HttpContext = CrearHttpContext(tenantId, cajeroId) }
        };

        var request = new CobrarTarjetaRequest
        {
            TokenTransaccion = "tok_fondos_insuficientes",
            SimularResultado = "RECHAZADO"
        };

        // Act
        var actionResult = await controller.CobrarTarjeta(venta.Id, request, CancellationToken.None);

        // Assert: Endpoint retorna 400 BadRequest con código y motivo de rechazo
        actionResult.Should().BeOfType<BadRequestObjectResult>();
        var badResult = (BadRequestObjectResult)actionResult;
        var response = badResult.Value as CobroTarjetaRechazadoResponse;
        response.Should().NotBeNull();
        response!.CodigoRespuesta.Should().Be("REC-51");
        response.Motivo.Should().Contain("Fondos insuficientes");

        // Assert: Estado de venta correcto: Permanece PENDIENTE para permitir reintento
        var ventaEnDb = await ventaRepo.GetByIdAsync(venta.Id);
        ventaEnDb.Should().NotBeNull();
        ventaEnDb!.Estado.Should().Be(EstadoVenta.PENDIENTE);
        ventaEnDb.Pagos.Should().BeEmpty();

        // Assert: No se publica evento en Kafka
        kafkaProducer.PublishedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task S2_060_Situacion3_PerdidaTemporalSimulador_TerminaConVentaPendiente_Y_503()
    {
        // Arrange
        var (db, tenantId, cajeroId, sucursalId, turno) = CrearEntornoEnMemoria();
        var venta = CrearVentaPendiente(db, tenantId, turno.Id, cajeroId, sucursalId, 1800m);

        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(f => f.CreateClient("PasarelaPago")).Returns(new HttpClient());
        var pasarelaClient = new PasarelaPagoClient(mockFactory.Object, new TestLogger<PasarelaPagoClient>());
        var kafkaProducer = new TestKafkaProducer();
        var ventaRepo = new VentaRepository(db);
        var ventaService = new VentaService(ventaRepo, null, kafkaProducer, pasarelaClient);

        var controller = new VentasController(ventaService, new TurnoService(new TurnoRepository(db)), Mock.Of<ICatalogClient>(), Mock.Of<ITaxClient>())
        {
            ControllerContext = new ControllerContext { HttpContext = CrearHttpContext(tenantId, cajeroId) }
        };

        var request = new CobrarTarjetaRequest
        {
            TokenTransaccion = "tok_timeout",
            SimularResultado = "TIMEOUT" // o "INDISPONIBLE"
        };

        // Act
        var actionResult = await controller.CobrarTarjeta(venta.Id, request, CancellationToken.None);

        // Assert: Endpoint maneja la pérdida temporal retornando 503 Service Unavailable
        actionResult.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)actionResult;
        objectResult.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);

        // Assert: Estado de venta correcto: Permanece PENDIENTE intacta
        var ventaEnDb = await ventaRepo.GetByIdAsync(venta.Id);
        ventaEnDb.Should().NotBeNull();
        ventaEnDb!.Estado.Should().Be(EstadoVenta.PENDIENTE);
        ventaEnDb.Pagos.Should().BeEmpty();

        // Assert: Sin publicación Kafka
        kafkaProducer.PublishedEvents.Should().BeEmpty();
    }
}

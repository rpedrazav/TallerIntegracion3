extern alias POSCartServiceAlias;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using Xunit;
using Pos = POSCartServiceAlias::POSCartService;

namespace GlobalMart.IntegrationTests;

[Trait("Plan", "S2-094")]
public sealed class SaleReversedOutboxIntegrationTests
{
    [Fact]
    public async Task Anular_YReintentarPublicacion_ConservaIdentidadPayloadYVentaOriginal()
    {
        await using var database = await TestDatabase.CreateAsync();
        var tenant = Guid.NewGuid(); var admin = Guid.NewGuid();
        var turno = new Pos.Models.Turno { TenantId = tenant, CajeroId = Guid.NewGuid(), SucursalId = Guid.NewGuid(), Estado = Pos.Models.EstadoTurno.ABIERTO };
        var venta = new Pos.Models.Venta { TenantId = tenant, Turno = turno, TurnoId = turno.Id,
            CajeroId = turno.CajeroId, SucursalId = turno.SucursalId, Estado = Pos.Models.EstadoVenta.COMPLETADA,
            MetodoPago = Pos.Models.MetodoPagoVenta.EFECTIVO, Subtotal = 750, Impuestos = 142.5m, Total = 892.5m };
        venta.Items.Add(new Pos.Models.ItemVenta { ProductoId = Guid.NewGuid(), NombreProducto = "Pesable",
            Cantidad = .750m, PesoKg = .750m, PrecioUnitario = 1000, Subtotal = 750 });
        venta.Pagos.Add(new Pos.Models.Pago { Metodo = Pos.Models.MetodoPago.EFECTIVO, Monto = 1000, Vuelto = 107.5m });
        var originalId = Guid.NewGuid();
        await using (var db = NewContext(database.Options, tenant)) {
            db.Ventas.Add(venta);
            db.OutboxMessages.Add(new Pos.Models.OutboxMessage { EventId = originalId, TenantId = tenant, VentaId = venta.Id,
                EventType = "sale.completed", Topic = "sale.completed", Payload = "{}", MessageKey = tenant.ToString(), Status = Pos.Models.OutboxStatus.PUBLICADO });
            await db.SaveChangesAsync();
        }
        Guid eventId; string payload;
        await using (var db = NewContext(database.Options, tenant)) {
            var cancelled = await new Pos.Repositories.VentaRepository(db).AnularTotalAsync(venta.Id, tenant, admin, "Error de venta");
            var message = await db.OutboxMessages.SingleAsync(o => o.EventType == "sale.reversed");
            eventId = message.EventId; payload = message.Payload;
            Assert.Equal(cancelled.Anulacion!.Id, eventId); Assert.NotEqual(originalId, eventId);
            Assert.Equal(Pos.Models.OutboxStatus.PENDIENTE, message.Status);
            var json = JsonDocument.Parse(payload).RootElement;
            Assert.Equal(eventId, json.GetProperty("event_id").GetGuid());
            Assert.Equal(eventId, json.GetProperty("anulacion_id").GetGuid());
            Assert.Equal(venta.Id, json.GetProperty("venta_id").GetGuid());
            Assert.Equal(tenant, json.GetProperty("tenant_id").GetGuid());
            Assert.Equal(admin, json.GetProperty("autorizado_por").GetGuid());
            Assert.Equal(turno.SucursalId, json.GetProperty("sucursal_id").GetGuid());
            Assert.Equal(1, json.GetProperty("schema_version").GetInt32());
            Assert.Equal(892.5m, json.GetProperty("total").GetDecimal());
            Assert.Equal(142.5m, json.GetProperty("iva").GetDecimal());
            Assert.Equal(.750m, Assert.Single(json.GetProperty("items").EnumerateArray()).GetProperty("cantidad").GetDecimal());
        }
        await using (var db = NewContext(database.Options, tenant)) {
            await Assert.ThrowsAsync<Pos.Exceptions.AnulacionConflictException>(() => new Pos.Repositories.VentaRepository(db).AnularTotalAsync(venta.Id, tenant, admin, "Repetido"));
        }
        var producer = new FailOnceProducer(); // Captura el envío y simula pérdida de ACK: puede haber dos entregas físicas.
        var clock = new ManualTimeProvider(DateTimeOffset.UtcNow.AddMinutes(1));
        var options = Options.Create(new Pos.Services.OutboxPublisherOptions());
        await using (var db = NewContext(database.Options, null)) {
            Assert.Equal(1, await Publisher(db, producer, options, clock).ProcessPendingAsync(default));
        }
        await using (var db = NewContext(database.Options, tenant)) {
            var pending = await db.OutboxMessages.SingleAsync(o => o.EventId == eventId);
            Assert.Equal(Pos.Models.OutboxStatus.PENDIENTE, pending.Status);
            Assert.Equal(payload, pending.Payload); Assert.Equal(1, pending.AttemptCount);
            Assert.Equal(Pos.Models.EstadoVenta.ANULADA, (await db.Ventas.SingleAsync()).Estado);
        }
        clock.Advance(TimeSpan.FromSeconds(10));
        // Nuevo contexto: recuperación no depende del objeto original ni de memoria del request.
        await using (var db = NewContext(database.Options, null)) {
            var publisher = Publisher(db, producer, options, clock);
            Assert.Equal(1, await publisher.ProcessPendingAsync(default));
            Assert.Equal(0, await publisher.ProcessPendingAsync(default));
        }
        Assert.Equal(2, producer.Messages.Count);
        Assert.All(producer.Messages, m => {
            Assert.Equal("sale.reversed", m.Topic); Assert.Equal(tenant.ToString(), m.Key); Assert.Equal(payload, m.Payload);
        });
        await using (var db = NewContext(database.Options, tenant)) {
            var message = await db.OutboxMessages.SingleAsync(o => o.EventType == "sale.reversed");
            Assert.Equal(eventId, message.EventId); Assert.Equal(payload, message.Payload);
            Assert.Equal(Pos.Models.OutboxStatus.PUBLICADO, message.Status); Assert.Equal(2, message.AttemptCount);
            Assert.Equal(originalId, (await db.OutboxMessages.SingleAsync(o => o.EventType == "sale.completed")).EventId);
            Assert.Single(await db.Anulaciones.ToListAsync());
        }
        await using (var db = NewContext(database.Options, Guid.NewGuid())) Assert.Empty(await db.OutboxMessages.ToListAsync());
    }

    private static Pos.Services.OutboxPublisherService Publisher(
        Pos.Data.PosCartDbContext context,
        Pos.Services.IKafkaProducerService producer,
        IOptions<Pos.Services.OutboxPublisherOptions> options,
        TimeProvider timeProvider) => new(
            new Pos.Repositories.OutboxRepository(context),
            producer,
            options,
            timeProvider,
            NullLogger<Pos.Services.OutboxPublisherService>.Instance);

    private static Pos.Data.PosCartDbContext NewContext(
        DbContextOptions<Pos.Data.PosCartDbContext> options,
        Guid? tenantId) => new(options) { CurrentTenantId = tenantId };

    private sealed class FailOnceProducer : Pos.Services.IKafkaProducerService
    {
        private int _attempt;
        public List<(string Topic, string Payload, string? Key)> Messages { get; } = new();

        public Task PublicarAsync(
            string topico,
            string mensaje,
            string? clave = null,
            CancellationToken cancellationToken = default)
        {
            Messages.Add((topico, mensaje, clave));
            if (Interlocked.Increment(ref _attempt) == 1)
                throw new InvalidOperationException("fallo temporal simulado");
            return Task.CompletedTask;
        }

        public Task PublicarSaleCompletedAsync(
            Pos.Messaging.SaleCompletedEvent evento,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan duration) => _now = _now.Add(duration);
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly NpgsqlConnection _admin;
        private readonly string _schema;
        public DbContextOptions<Pos.Data.PosCartDbContext> Options { get; }

        private TestDatabase(
            NpgsqlConnection admin,
            string schema,
            DbContextOptions<Pos.Data.PosCartDbContext> options)
        {
            _admin = admin;
            _schema = schema;
            Options = options;
        }

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection = Environment.GetEnvironmentVariable("GLOBALMART_TEST_POSTGRES")
                ?? throw new InvalidOperationException(
                    "Defina GLOBALMART_TEST_POSTGRES apuntando a PostgreSQL de pruebas.");
            var schema = "s2094_" + Guid.NewGuid().ToString("N");
            var admin = new NpgsqlConnection(connection);
            await admin.OpenAsync();
            await using (var create = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", admin))
                await create.ExecuteNonQueryAsync();

            var isolated = new NpgsqlConnectionStringBuilder(connection)
            {
                SearchPath = schema,
                Pooling = false
            }.ConnectionString;
            var options = new DbContextOptionsBuilder<Pos.Data.PosCartDbContext>()
                .UseNpgsql(isolated, postgres =>
                    postgres.MigrationsHistoryTable("__EFMigrationsHistory", schema))
                .Options;
            await using (var db = new Pos.Data.PosCartDbContext(options))
                await db.Database.MigrateAsync();

            return new TestDatabase(admin, schema, options);
        }

        public async ValueTask DisposeAsync()
        {
            await using var drop = new NpgsqlCommand($"DROP SCHEMA \"{_schema}\" CASCADE", _admin);
            await drop.ExecuteNonQueryAsync();
            await _admin.DisposeAsync();
        }
    }
}

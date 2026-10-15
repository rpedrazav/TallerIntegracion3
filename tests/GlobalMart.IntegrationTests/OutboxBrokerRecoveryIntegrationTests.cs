extern alias POSCartServiceAlias;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using Xunit;
using Pos = POSCartServiceAlias::POSCartService;

namespace GlobalMart.IntegrationTests;

[Trait("Plan", "S2-010")]
public sealed class OutboxBrokerRecoveryIntegrationTests
{
    [Fact]
    public async Task CobrarConBrokerCaido_ConservaVentaYEntregaUnEventoLogicoAlRecuperarse()
    {
        await using var database = await TestDatabase.CreateAsync();
        var tenantId = Guid.NewGuid();
        var ventaId = await SeedVentaAsync(database.Options, tenantId);
        var clock = new ManualTimeProvider(DateTimeOffset.UtcNow.AddMinutes(1));
        var broker = new RecoverableKafkaProducer();
        var options = Options.Create(new Pos.Services.OutboxPublisherOptions
        {
            BatchSize = 10,
            MaxAttempts = 10,
            BaseRetryDelaySeconds = 5,
            MaxRetryDelaySeconds = 300,
            LeaseDurationSeconds = 60
        });

        await using (var chargeContext = NewContext(database.Options, tenantId))
        {
            var ventaService = new Pos.Services.VentaService(
                new Pos.Repositories.VentaRepository(chargeContext),
                kafkaProducer: broker);

            var completed = await ventaService.CompletarAsync(ventaId, 150m, 31m);
            Assert.Equal(Pos.Models.EstadoVenta.COMPLETADA, completed.Estado);
        }

        Guid eventId;
        string originalPayload;
        await using (var verifyCharge = NewContext(database.Options, tenantId))
        {
            Assert.Equal(1, await verifyCharge.Ventas.CountAsync());
            var sale = await verifyCharge.Ventas
                .Include(candidate => candidate.Pagos)
                .SingleAsync(candidate => candidate.Id == ventaId);
            Assert.Equal(Pos.Models.EstadoVenta.COMPLETADA, sale.Estado);
            Assert.Single(sale.Pagos);

            var outbox = await verifyCharge.OutboxMessages.SingleAsync();
            Assert.Equal(Pos.Models.OutboxStatus.PENDIENTE, outbox.Status);
            Assert.Equal(0, outbox.AttemptCount);
            eventId = outbox.EventId;
            originalPayload = outbox.Payload;
        }
        Assert.Empty(broker.Attempts);

        await using (var unavailableContext = NewContext(database.Options, null))
        {
            var publisher = Publisher(unavailableContext, broker, options, clock);
            Assert.Equal(1, await publisher.ProcessPendingAsync(CancellationToken.None));
        }

        await using (var verifyFailure = NewContext(database.Options, tenantId))
        {
            var sale = await verifyFailure.Ventas
                .Include(candidate => candidate.Pagos)
                .SingleAsync(candidate => candidate.Id == ventaId);
            Assert.Equal(Pos.Models.EstadoVenta.COMPLETADA, sale.Estado);
            Assert.Single(sale.Pagos);

            var pending = await verifyFailure.OutboxMessages.SingleAsync();
            Assert.Equal(Pos.Models.OutboxStatus.PENDIENTE, pending.Status);
            Assert.Equal(eventId, pending.EventId);
            Assert.Equal(originalPayload, pending.Payload);
            Assert.Equal(1, pending.AttemptCount);
            Assert.Contains("broker no disponible", pending.LastError);
        }

        broker.Recover();
        clock.Advance(TimeSpan.FromSeconds(6));

        await using (var recoveredContext = NewContext(database.Options, null))
        {
            var publisher = Publisher(recoveredContext, broker, options, clock);
            Assert.Equal(1, await publisher.ProcessPendingAsync(CancellationToken.None));
            Assert.Equal(0, await publisher.ProcessPendingAsync(CancellationToken.None));
        }

        await using (var verifyRecovery = NewContext(database.Options, tenantId))
        {
            var sale = await verifyRecovery.Ventas
                .Include(candidate => candidate.Pagos)
                .SingleAsync(candidate => candidate.Id == ventaId);
            Assert.Equal(Pos.Models.EstadoVenta.COMPLETADA, sale.Estado);
            Assert.Single(sale.Pagos);

            var published = await verifyRecovery.OutboxMessages.SingleAsync();
            Assert.Equal(Pos.Models.OutboxStatus.PUBLICADO, published.Status);
            Assert.Equal(eventId, published.EventId);
            Assert.Equal(originalPayload, published.Payload);
            Assert.Equal(2, published.AttemptCount);
            Assert.NotNull(published.PublishedAt);
        }

        Assert.Equal(2, broker.Attempts.Count);
        Assert.All(broker.Attempts, attempt =>
        {
            Assert.Equal("sale.completed", attempt.Topic);
            Assert.Equal(tenantId.ToString(), attempt.Key);
            Assert.Equal(originalPayload, attempt.Payload);
        });

        var delivered = Assert.Single(broker.Deliveries);
        Assert.Equal(originalPayload, delivered.Payload);
        var payload = JsonSerializer.Deserialize<Pos.Messaging.SaleCompletedEvent>(delivered.Payload);
        Assert.NotNull(payload);
        Assert.Equal(eventId, payload!.EventId);
        Assert.Equal(1, payload.SchemaVersion);
        Assert.Equal(tenantId, payload.TenantId);
        Assert.Equal(ventaId, payload.VentaId);
        Assert.Equal(119m, payload.Total);
        Assert.Single(payload.Items);
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

    private static async Task<Guid> SeedVentaAsync(
        DbContextOptions<Pos.Data.PosCartDbContext> options,
        Guid tenantId)
    {
        var turnoId = Guid.NewGuid();
        var ventaId = Guid.NewGuid();
        var cajeroId = Guid.NewGuid();
        var sucursalId = Guid.NewGuid();

        await using var db = NewContext(options, tenantId);
        db.Turnos.Add(new Pos.Models.Turno
        {
            Id = turnoId,
            TenantId = tenantId,
            CajeroId = cajeroId,
            SucursalId = sucursalId,
            MontoApertura = 1000m
        });
        db.Ventas.Add(new Pos.Models.Venta
        {
            Id = ventaId,
            TenantId = tenantId,
            TurnoId = turnoId,
            CajeroId = cajeroId,
            SucursalId = sucursalId,
            Subtotal = 100m,
            Impuestos = 19m,
            Total = 119m,
            MetodoPago = Pos.Models.MetodoPagoVenta.EFECTIVO,
            Items =
            {
                new Pos.Models.ItemVenta
                {
                    ProductoId = Guid.NewGuid(),
                    NombreProducto = "Producto de prueba",
                    Cantidad = 1m,
                    PrecioUnitario = 100m,
                    Subtotal = 100m
                }
            }
        });
        await db.SaveChangesAsync();
        return ventaId;
    }

    private static Pos.Data.PosCartDbContext NewContext(
        DbContextOptions<Pos.Data.PosCartDbContext> options,
        Guid? tenantId) => new(options) { CurrentTenantId = tenantId };

    private sealed class RecoverableKafkaProducer : Pos.Services.IKafkaProducerService
    {
        private bool _available;
        public List<PublishedMessage> Attempts { get; } = new();
        public List<PublishedMessage> Deliveries { get; } = new();

        public void Recover() => _available = true;

        public Task PublicarAsync(
            string topico,
            string mensaje,
            string? clave = null,
            CancellationToken cancellationToken = default)
        {
            var message = new PublishedMessage(topico, clave, mensaje);
            Attempts.Add(message);
            if (!_available)
                throw new InvalidOperationException("broker no disponible (simulado)");

            Deliveries.Add(message);
            return Task.CompletedTask;
        }

        public Task PublicarSaleCompletedAsync(
            Pos.Messaging.SaleCompletedEvent evento,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed record PublishedMessage(string Topic, string? Key, string Payload);

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
            var schema = "s2010_" + Guid.NewGuid().ToString("N");
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

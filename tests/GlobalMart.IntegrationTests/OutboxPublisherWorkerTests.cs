extern alias POSCartServiceAlias;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using Xunit;
using Pos = POSCartServiceAlias::POSCartService;

namespace GlobalMart.IntegrationTests;

[Trait("Plan", "S2-009")]
public sealed class OutboxPublisherWorkerTests
{
    [Fact]
    public async Task FalloTemporal_ConservaEventoYElSiguienteIntentoLoPublicaConMismoEventId()
    {
        await using var database = await TestDatabase.CreateAsync();
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var ventaId = Guid.NewGuid();
        var payload = $$"""{"event_id":"{{eventId}}","schema_version":1}""";
        var clock = new ManualTimeProvider(new DateTimeOffset(2026, 10, 15, 15, 0, 0, TimeSpan.Zero));
        var producer = new FailOnceProducer();
        var options = Options.Create(new Pos.Services.OutboxPublisherOptions
        {
            BatchSize = 10,
            MaxAttempts = 10,
            BaseRetryDelaySeconds = 5,
            MaxRetryDelaySeconds = 300,
            LeaseDurationSeconds = 60
        });

        await using (var seed = NewContext(database.Options, tenantId))
        {
            seed.OutboxMessages.Add(new Pos.Models.OutboxMessage
            {
                EventId = eventId,
                TenantId = tenantId,
                VentaId = ventaId,
                EventType = "sale.completed",
                SchemaVersion = 1,
                Topic = "sale.completed",
                MessageKey = tenantId.ToString(),
                Payload = payload,
                Status = Pos.Models.OutboxStatus.PENDIENTE,
                CreatedAt = clock.GetUtcNow().UtcDateTime,
                NextAttemptAt = clock.GetUtcNow().UtcDateTime
            });
            await seed.SaveChangesAsync();
        }

        await using (var firstContext = NewContext(database.Options, null))
        {
            var publisher = Publisher(firstContext, producer, options, clock);
            Assert.Equal(1, await publisher.ProcessPendingAsync(CancellationToken.None));
        }

        DateTime scheduledRetry;
        await using (var verifyFailure = NewContext(database.Options, tenantId))
        {
            var pending = await verifyFailure.OutboxMessages.SingleAsync();
            Assert.Equal(Pos.Models.OutboxStatus.PENDIENTE, pending.Status);
            Assert.Equal(eventId, pending.EventId);
            Assert.Equal(payload, pending.Payload);
            Assert.Equal(1, pending.AttemptCount);
            Assert.Equal(1, pending.TotalAttemptCount);
            Assert.Null(pending.LeaseToken);
            Assert.Null(pending.LockedUntil);
            Assert.NotNull(pending.NextAttemptAt);
            Assert.Contains("temporal", pending.LastError);
            scheduledRetry = pending.NextAttemptAt!.Value;
        }

        Assert.InRange(
            scheduledRetry,
            clock.GetUtcNow().UtcDateTime.AddSeconds(2.5),
            clock.GetUtcNow().UtcDateTime.AddSeconds(5));
        clock.Advance(TimeSpan.FromSeconds(6));

        await using (var secondContext = NewContext(database.Options, null))
        {
            var publisher = Publisher(secondContext, producer, options, clock);
            Assert.Equal(1, await publisher.ProcessPendingAsync(CancellationToken.None));
        }

        await using (var verifySuccess = NewContext(database.Options, tenantId))
        {
            var published = await verifySuccess.OutboxMessages.SingleAsync();
            Assert.Equal(Pos.Models.OutboxStatus.PUBLICADO, published.Status);
            Assert.Equal(eventId, published.EventId);
            Assert.Equal(payload, published.Payload);
            Assert.Equal(2, published.AttemptCount);
            Assert.Equal(2, published.TotalAttemptCount);
            Assert.NotNull(published.PublishedAt);
            Assert.Null(published.NextAttemptAt);
            Assert.Null(published.LastError);
        }

        Assert.Equal(2, producer.Messages.Count);
        Assert.All(producer.Messages, sent =>
        {
            Assert.Equal("sale.completed", sent.Topic);
            Assert.Equal(tenantId.ToString(), sent.Key);
            Assert.Equal(payload, sent.Payload);
        });
    }

    [Fact]
    public async Task StopAsync_CancelaElCicloHospedadoLimpiaYRapidamente()
    {
        var blockingPublisher = new BlockingPublisher();
        var services = new ServiceCollection();
        services.AddSingleton<Pos.Services.IOutboxPublisherService>(blockingPublisher);
        await using var provider = services.BuildServiceProvider();
        var worker = new Pos.Services.OutboxPublisherWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new Pos.Services.OutboxPublisherOptions()),
            NullLogger<Pos.Services.OutboxPublisherWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        await blockingPublisher.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await worker.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2));

        Assert.True(blockingPublisher.CancellationObserved.Task.IsCompletedSuccessfully);
        worker.Dispose();
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

    private sealed class BlockingPublisher : Pos.Services.IOutboxPublisherService
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource CancellationObserved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken)
        {
            Started.TrySetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return 0;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                CancellationObserved.TrySetResult();
                throw;
            }
        }
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
            var schema = "s2009_" + Guid.NewGuid().ToString("N");
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

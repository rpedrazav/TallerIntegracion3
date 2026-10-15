extern alias POSCartServiceAlias;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;
using Pos = POSCartServiceAlias::POSCartService;

namespace GlobalMart.IntegrationTests;

[Trait("Plan", "S2-008")]
public sealed class VentaOutboxTransactionTests
{
    [Fact]
    public async Task CompletarVenta_GuardaEstadoPagoYEventoPendienteEnUnaTransaccion()
    {
        await using var database = await TestDatabase.CreateAsync();
        var tenantId = Guid.NewGuid();
        var ventaId = await SeedVentaAsync(database.Options, tenantId);

        await using (var db = NewContext(database.Options, tenantId))
        {
            var service = new Pos.Services.VentaService(
                new Pos.Repositories.VentaRepository(db),
                kafkaProducer: new UnexpectedKafkaProducer());

            var completed = await service.CompletarAsync(ventaId, 150m, 31m);
            Assert.Equal(Pos.Models.EstadoVenta.COMPLETADA, completed.Estado);
        }

        await using (var verify = NewContext(database.Options, tenantId))
        {
            var sale = await verify.Ventas.Include(v => v.Pagos).SingleAsync(v => v.Id == ventaId);
            Assert.Equal(Pos.Models.EstadoVenta.COMPLETADA, sale.Estado);
            var payment = Assert.Single(sale.Pagos);
            Assert.Equal(150m, payment.Monto);
            Assert.Equal(31m, payment.Vuelto);

            var outbox = await verify.OutboxMessages.SingleAsync(m =>
                m.VentaId == ventaId && m.Status == Pos.Models.OutboxStatus.PENDIENTE);
            Assert.NotEqual(Guid.Empty, outbox.EventId);
            Assert.Equal(tenantId.ToString(), outbox.MessageKey);
            Assert.Equal(1, outbox.SchemaVersion);
            Assert.Equal(outbox.CreatedAt, outbox.NextAttemptAt);

            var payload = JsonSerializer.Deserialize<Pos.Messaging.SaleCompletedEvent>(outbox.Payload);
            Assert.NotNull(payload);
            Assert.Equal(outbox.EventId, payload!.EventId);
            Assert.Equal(outbox.SchemaVersion, payload.SchemaVersion);
            Assert.Equal(tenantId, payload.TenantId);
            Assert.Equal(ventaId, payload.VentaId);
            Assert.Single(payload.Items);
        }
    }

    [Fact]
    public async Task CompletarVenta_SiFallaOutbox_RevierteEstadoYPago()
    {
        await using var database = await TestDatabase.CreateAsync();
        var tenantId = Guid.NewGuid();
        var ventaId = await SeedVentaAsync(database.Options, tenantId);

        await using (var arrange = NewContext(database.Options, tenantId))
        {
            arrange.OutboxMessages.Add(new Pos.Models.OutboxMessage
            {
                EventId = Guid.NewGuid(),
                TenantId = tenantId,
                VentaId = ventaId,
                EventType = "sale.completed",
                MessageKey = tenantId.ToString(),
                Payload = "{}"
            });
            await arrange.SaveChangesAsync();
        }

        await using (var db = NewContext(database.Options, tenantId))
        {
            var service = new Pos.Services.VentaService(new Pos.Repositories.VentaRepository(db));
            var exception = await Assert.ThrowsAsync<DbUpdateException>(
                () => service.CompletarAsync(ventaId, 150m, 31m));
            Assert.Equal(
                PostgresErrorCodes.UniqueViolation,
                Assert.IsType<PostgresException>(exception.InnerException).SqlState);
        }

        await using (var verify = NewContext(database.Options, tenantId))
        {
            var sale = await verify.Ventas.Include(v => v.Pagos).SingleAsync(v => v.Id == ventaId);
            Assert.Equal(Pos.Models.EstadoVenta.PENDIENTE, sale.Estado);
            Assert.Empty(sale.Pagos);
            Assert.Single(await verify.OutboxMessages.Where(m => m.VentaId == ventaId).ToListAsync());
        }
    }

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
        Guid tenantId) => new(options) { CurrentTenantId = tenantId };

    private sealed class UnexpectedKafkaProducer : Pos.Services.IKafkaProducerService
    {
        public Task PublicarAsync(
            string topico,
            string mensaje,
            string? clave = null,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("S2-008 no debe publicar directamente en Kafka.");

        public Task PublicarSaleCompletedAsync(
            Pos.Messaging.SaleCompletedEvent evento,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("S2-008 no debe publicar directamente en Kafka.");
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
            var schema = "s2008_" + Guid.NewGuid().ToString("N");
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

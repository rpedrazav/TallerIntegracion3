extern alias POSCartServiceAlias;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
using Pos = POSCartServiceAlias::POSCartService;

namespace GlobalMart.IntegrationTests;

[Trait("Plan", "S2-007")]
public sealed class OutboxMigrationTests
{
    [Fact]
    public async Task MigracionReal_PermiteConsultarPendientesPorTenant_YRevertirSoloOutbox()
    {
        var connection = Environment.GetEnvironmentVariable("GLOBALMART_TEST_POSTGRES")
            ?? throw new InvalidOperationException("Defina GLOBALMART_TEST_POSTGRES apuntando a PostgreSQL de pruebas.");
        var schema = "s2007_" + Guid.NewGuid().ToString("N");
        await using var admin = new NpgsqlConnection(connection);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", admin))
            await create.ExecuteNonQueryAsync();

        var isolated = new NpgsqlConnectionStringBuilder(connection)
        {
            SearchPath = schema,
            Pooling = false
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<Pos.Data.PosCartDbContext>()
            .UseNpgsql(isolated, postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", schema)).Options;
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var ventaId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var payload = $"{{ \"event_id\": \"{eventId}\", \"schema_version\": 1 }}";

        try
        {
            await using (var db = new Pos.Data.PosCartDbContext(options))
            {
                // Aplica la cadena real; EnsureCreated no demuestra una migración.
                await db.Database.MigrateAsync();
                Assert.False(db.Database.HasPendingModelChanges());
                Assert.Contains(await db.Database.GetAppliedMigrationsAsync(), m => m.EndsWith("_AddOutboxMessages"));
            }

            await using (var db = new Pos.Data.PosCartDbContext(options) { CurrentTenantId = tenantA })
            {
                db.OutboxMessages.Add(Message(tenantA, ventaId, eventId, payload));
                db.OutboxMessages.Add(Message(tenantA, Guid.NewGuid(), Guid.NewGuid(), "{}", Pos.Models.OutboxStatus.PUBLICADO));
                await db.SaveChangesAsync();
            }
            await using (var db = new Pos.Data.PosCartDbContext(options) { CurrentTenantId = tenantB })
            {
                // La unicidad se limita al tenant, aunque coincidan venta y tipo.
                db.OutboxMessages.Add(Message(tenantB, ventaId, Guid.NewGuid(), "{}"));
                await db.SaveChangesAsync();
                Assert.Single(await db.OutboxMessages.Where(m => m.Status == Pos.Models.OutboxStatus.PENDIENTE).ToListAsync());
            }
            await using (var db = new Pos.Data.PosCartDbContext(options) { CurrentTenantId = tenantA })
            {
                var pending = await db.OutboxMessages
                    .Where(m => m.Status == Pos.Models.OutboxStatus.PENDIENTE)
                    .OrderBy(m => m.NextAttemptAt).ToListAsync();
                var row = Assert.Single(pending);
                Assert.Equal(eventId, row.EventId);
                Assert.Equal(payload, row.Payload); // Conserva incluso el formato del texto.
                Assert.Equal(1, row.SchemaVersion);
                Assert.Equal(0, row.AttemptCount);
                Assert.Null(row.PublishedAt);
                row.Status = Pos.Models.OutboxStatus.EN_PROCESO;
                row.AttemptCount = 1;
                row.TotalAttemptCount = 1;
                await db.SaveChangesAsync();
            }
            await using (var db = new Pos.Data.PosCartDbContext(options) { CurrentTenantId = tenantA })
            {
                Assert.Empty(await db.OutboxMessages.Where(m => m.Status == Pos.Models.OutboxStatus.PENDIENTE).ToListAsync());
                var saved = await db.OutboxMessages.SingleAsync(m => m.EventId == eventId);
                Assert.Equal(payload, saved.Payload);
                Assert.Equal(1, saved.TotalAttemptCount);
                db.OutboxMessages.Add(Message(tenantA, ventaId, Guid.NewGuid(), "{}"));
                var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
                Assert.Equal(PostgresErrorCodes.UniqueViolation, Assert.IsType<PostgresException>(error.InnerException).SqlState);
            }
            await using (var db = new Pos.Data.PosCartDbContext(options))
            {
                Assert.Empty(await db.OutboxMessages.ToListAsync()); // Sin tenant: cerrado por defecto.
                await db.GetService<IMigrator>().MigrateAsync("20260912184333_InitialCreate");
            }
            await using (var inspect = new NpgsqlCommand(
                "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = @schema AND table_name = 'OutboxMessages'), " +
                "EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = @schema AND table_name = 'Ventas')", admin))
            {
                inspect.Parameters.AddWithValue("schema", schema);
                await using var reader = await inspect.ExecuteReaderAsync();
                Assert.True(await reader.ReadAsync());
                Assert.False(reader.GetBoolean(0));
                Assert.True(reader.GetBoolean(1));
            }
            await using (var db = new Pos.Data.PosCartDbContext(options))
                await db.Database.MigrateAsync(); // También admite reaplicar después del Down.
        }
        finally
        {
            // Solo el esquema aleatorio creado por esta prueba, nunca una base compartida.
            await using var drop = new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static Pos.Models.OutboxMessage Message(Guid tenant, Guid venta, Guid id, string payload,
        Pos.Models.OutboxStatus status = Pos.Models.OutboxStatus.PENDIENTE) => new()
    {
        EventId = id,
        TenantId = tenant,
        VentaId = venta,
        MessageKey = tenant.ToString(),
        Payload = payload,
        Status = status
    };
}

using Microsoft.EntityFrameworkCore;
using POSCartService.Data;
using POSCartService.Models;

namespace POSCartService.Repositories;

public sealed class OutboxRepository : IOutboxRepository
{
    private readonly PosCartDbContext _context;

    public OutboxRepository(PosCartDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<Guid>> GetActiveTenantIdsAsync(
        CancellationToken cancellationToken)
    {
        // Consulta interna de control: enumera únicamente IDs existentes en la base local
        // de MS-5. Cada lectura o mutación posterior usa CurrentTenantId y el filtro global.
        return await _context.Database.SqlQuery<Guid>($"""
                SELECT DISTINCT "TenantId" AS "Value"
                FROM "OutboxMessages"
                WHERE "Status" IN ({(int)OutboxStatus.PENDIENTE}, {(int)OutboxStatus.EN_PROCESO})
                """)
            .ToListAsync(cancellationToken);
    }

    public async Task<ReservedOutboxMessage?> ReserveNextAsync(
        Guid tenantId,
        DateTime now,
        TimeSpan leaseDuration,
        int maxAttempts,
        CancellationToken cancellationToken)
    {
        _context.CurrentTenantId = tenantId;
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        await _context.OutboxMessages
            .Where(message =>
                message.AttemptCount >= maxAttempts
                && (message.Status == OutboxStatus.PENDIENTE
                    || (message.Status == OutboxStatus.EN_PROCESO
                        && message.LockedUntil <= now)))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(message => message.Status, OutboxStatus.FALLIDO)
                .SetProperty(message => message.NextAttemptAt, (DateTime?)null)
                .SetProperty(message => message.LeaseToken, (Guid?)null)
                .SetProperty(message => message.LockedUntil, (DateTime?)null)
                .SetProperty(message => message.LastError, "Se agotó el límite de intentos de publicación."),
                cancellationToken);

        var message = await _context.OutboxMessages
            .FromSqlInterpolated($"""
                SELECT *
                FROM "OutboxMessages"
                WHERE "TenantId" = {tenantId}
                  AND "AttemptCount" < {maxAttempts}
                  AND (
                    ("Status" = {(int)OutboxStatus.PENDIENTE}
                      AND ("NextAttemptAt" IS NULL OR "NextAttemptAt" <= {now}))
                    OR
                    ("Status" = {(int)OutboxStatus.EN_PROCESO}
                      AND "LockedUntil" <= {now})
                  )
                ORDER BY COALESCE("NextAttemptAt", "CreatedAt"), "CreatedAt"
                FOR UPDATE SKIP LOCKED
                LIMIT 1
                """)
            .SingleOrDefaultAsync(cancellationToken);

        if (message is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        var leaseToken = Guid.NewGuid();
        message.Status = OutboxStatus.EN_PROCESO;
        message.AttemptCount++;
        message.TotalAttemptCount++;
        message.LastAttemptAt = now;
        message.LeaseToken = leaseToken;
        message.LockedUntil = now.Add(leaseDuration);

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new ReservedOutboxMessage(
            message.EventId,
            message.TenantId,
            message.Topic,
            message.MessageKey,
            message.Payload,
            message.AttemptCount,
            leaseToken);
    }

    public async Task<bool> MarkPublishedAsync(
        ReservedOutboxMessage message,
        DateTime publishedAt,
        CancellationToken cancellationToken)
    {
        _context.CurrentTenantId = message.TenantId;
        var updated = await _context.OutboxMessages
            .Where(candidate =>
                candidate.EventId == message.EventId
                && candidate.Status == OutboxStatus.EN_PROCESO
                && candidate.LeaseToken == message.LeaseToken)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(candidate => candidate.Status, OutboxStatus.PUBLICADO)
                .SetProperty(candidate => candidate.PublishedAt, publishedAt)
                .SetProperty(candidate => candidate.NextAttemptAt, (DateTime?)null)
                .SetProperty(candidate => candidate.LeaseToken, (Guid?)null)
                .SetProperty(candidate => candidate.LockedUntil, (DateTime?)null)
                .SetProperty(candidate => candidate.LastError, (string?)null),
                cancellationToken);
        return updated == 1;
    }

    public async Task<bool> MarkFailedAttemptAsync(
        ReservedOutboxMessage message,
        DateTime? nextAttemptAt,
        string error,
        int maxAttempts,
        CancellationToken cancellationToken)
    {
        _context.CurrentTenantId = message.TenantId;
        var exhausted = message.AttemptCount >= maxAttempts;
        var updated = await _context.OutboxMessages
            .Where(candidate =>
                candidate.EventId == message.EventId
                && candidate.Status == OutboxStatus.EN_PROCESO
                && candidate.LeaseToken == message.LeaseToken)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(candidate => candidate.Status,
                    exhausted ? OutboxStatus.FALLIDO : OutboxStatus.PENDIENTE)
                .SetProperty(candidate => candidate.NextAttemptAt,
                    exhausted ? null : nextAttemptAt)
                .SetProperty(candidate => candidate.LeaseToken, (Guid?)null)
                .SetProperty(candidate => candidate.LockedUntil, (DateTime?)null)
                .SetProperty(candidate => candidate.LastError, error),
                cancellationToken);
        return updated == 1;
    }
}

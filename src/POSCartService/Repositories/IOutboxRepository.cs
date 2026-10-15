using POSCartService.Models;

namespace POSCartService.Repositories;

public sealed record ReservedOutboxMessage(
    Guid EventId,
    Guid TenantId,
    string Topic,
    string MessageKey,
    string Payload,
    int AttemptCount,
    Guid LeaseToken);

public interface IOutboxRepository
{
    Task<IReadOnlyList<Guid>> GetActiveTenantIdsAsync(CancellationToken cancellationToken);

    Task<ReservedOutboxMessage?> ReserveNextAsync(
        Guid tenantId,
        DateTime now,
        TimeSpan leaseDuration,
        int maxAttempts,
        CancellationToken cancellationToken);

    Task<bool> MarkPublishedAsync(
        ReservedOutboxMessage message,
        DateTime publishedAt,
        CancellationToken cancellationToken);

    Task<bool> MarkFailedAttemptAsync(
        ReservedOutboxMessage message,
        DateTime? nextAttemptAt,
        string error,
        int maxAttempts,
        CancellationToken cancellationToken);
}

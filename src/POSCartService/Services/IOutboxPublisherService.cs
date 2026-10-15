namespace POSCartService.Services;

public interface IOutboxPublisherService
{
    Task<int> ProcessPendingAsync(CancellationToken cancellationToken);
}

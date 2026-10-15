using Microsoft.Extensions.Options;
using POSCartService.Repositories;

namespace POSCartService.Services;

public sealed class OutboxPublisherService : IOutboxPublisherService
{
    private readonly IOutboxRepository _repository;
    private readonly IKafkaProducerService _producer;
    private readonly OutboxPublisherOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<OutboxPublisherService> _logger;

    public OutboxPublisherService(
        IOutboxRepository repository,
        IKafkaProducerService producer,
        IOptions<OutboxPublisherOptions> options,
        TimeProvider timeProvider,
        ILogger<OutboxPublisherService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _producer = producer ?? throw new ArgumentNullException(nameof(producer));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken)
    {
        var processed = 0;
        var tenantIds = await _repository.GetActiveTenantIdsAsync(cancellationToken);

        foreach (var tenantId in tenantIds)
        {
            while (processed < _options.BatchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var now = UtcNow();
                var message = await _repository.ReserveNextAsync(
                    tenantId,
                    now,
                    TimeSpan.FromSeconds(_options.LeaseDurationSeconds),
                    _options.MaxAttempts,
                    cancellationToken);

                if (message is null)
                    break;

                try
                {
                    await _producer.PublicarAsync(
                        message.Topic,
                        message.Payload,
                        message.MessageKey,
                        cancellationToken);

                    var marked = await _repository.MarkPublishedAsync(
                        message,
                        UtcNow(),
                        cancellationToken);

                    if (marked)
                    {
                        _logger.LogInformation(
                            "Outbox publicado EventId={EventId} TenantId={TenantId} Topic={Topic} Attempt={Attempt}",
                            message.EventId,
                            message.TenantId,
                            message.Topic,
                            message.AttemptCount);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Outbox no pudo marcarse publicado por lease obsoleto EventId={EventId} TenantId={TenantId} LeaseToken={LeaseToken}",
                            message.EventId,
                            message.TenantId,
                            message.LeaseToken);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation(
                        "Publicación outbox cancelada; la reserva expirará EventId={EventId} TenantId={TenantId} LeaseToken={LeaseToken}",
                        message.EventId,
                        message.TenantId,
                        message.LeaseToken);
                    throw;
                }
                catch (Exception exception)
                {
                    DateTime? nextAttemptAt = message.AttemptCount >= _options.MaxAttempts
                        ? null
                        : UtcNow().Add(CalculateRetryDelay(message.AttemptCount));
                    var error = TruncateError(exception);
                    var marked = await _repository.MarkFailedAttemptAsync(
                        message,
                        nextAttemptAt,
                        error,
                        _options.MaxAttempts,
                        cancellationToken);

                    _logger.LogWarning(
                        exception,
                        "Falló publicación outbox EventId={EventId} TenantId={TenantId} Attempt={Attempt} NextAttemptAt={NextAttemptAt} StateUpdated={StateUpdated}",
                        message.EventId,
                        message.TenantId,
                        message.AttemptCount,
                        nextAttemptAt,
                        marked);
                }

                processed++;
            }

            if (processed >= _options.BatchSize)
                break;
        }

        return processed;
    }

    private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

    private TimeSpan CalculateRetryDelay(int attempt)
    {
        var exponent = Math.Max(0, attempt - 1);
        var ceiling = Math.Min(
            _options.MaxRetryDelaySeconds,
            _options.BaseRetryDelaySeconds * Math.Pow(2, exponent));
        var seconds = ceiling * (0.5 + Random.Shared.NextDouble() * 0.5);
        return TimeSpan.FromSeconds(seconds);
    }

    private static string TruncateError(Exception exception)
    {
        var error = $"{exception.GetType().Name}: {exception.Message}";
        return error.Length <= 2000 ? error : error[..2000];
    }
}

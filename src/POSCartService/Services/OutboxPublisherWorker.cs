using Microsoft.Extensions.Options;

namespace POSCartService.Services;

public sealed class OutboxPublisherWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxPublisherOptions _options;
    private readonly ILogger<OutboxPublisherWorker> _logger;

    public OutboxPublisherWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxPublisherOptions> options,
        ILogger<OutboxPublisherWorker> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Worker de outbox deshabilitado por configuración");
            return;
        }

        _logger.LogInformation("Worker de outbox iniciado");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var processed = 0;
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var publisher = scope.ServiceProvider.GetRequiredService<IOutboxPublisherService>();
                    processed = await publisher.ProcessPendingAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Error no controlado en ciclo del worker de outbox");
                }

                if (processed == 0)
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(_options.PollIntervalSeconds),
                        stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Apagado normal del host.
        }
        finally
        {
            _logger.LogInformation("Worker de outbox detenido");
        }
    }
}

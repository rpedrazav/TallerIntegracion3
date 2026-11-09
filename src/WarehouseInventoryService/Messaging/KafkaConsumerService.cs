using Confluent.Kafka;

namespace WarehouseInventoryService.Messaging;

/// <summary>
/// Servicio hospedado que se suscribe al topic Kafka "sale.completed" al iniciar la aplicación.
/// Por cada evento recibido, delega el procesamiento a ISaleEventProcessor que se encarga de:
/// verificar idempotencia (EventosKafkaProcesados), deserializar el payload, iterar los items
/// y descontar stock vía IStockRepository.Descontar.
/// Si el procesamiento de un mensaje falla, se loguea el error y se continúa con el siguiente
/// mensaje sin bloquear el consumer.
/// </summary>
public sealed class KafkaConsumerService : IHostedService, IDisposable
{
    private const string Topic = "sale.completed";

    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private IConsumer<string, string>? _consumer;
    private CancellationTokenSource? _cts;
    private Task? _consumeLoopTask;

    public KafkaConsumerService(
        ILogger<KafkaConsumerService> logger,
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _scopeFactory = scopeFactory;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = _configuration["Kafka:GroupId"] ?? "warehouse-inventory-service",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
        _consumer.Subscribe(Topic);

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _consumeLoopTask = Task.Run(() => ConsumeLoop(_cts.Token), CancellationToken.None);

        _logger.LogInformation("KafkaConsumerService suscrito al topic {Topic}", Topic);
        return Task.CompletedTask;
    }

    private void ConsumeLoop(CancellationToken cancellationToken)
    {
        if (_consumer is null)
        {
            return;
        }

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result;
                try
                {
                    result = _consumer.Consume(cancellationToken);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consumiendo mensaje del topic {Topic}", Topic);
                    continue;
                }

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<ISaleEventProcessor>();
                    processor.ProcesarEvento(result.Message.Value);
                }
                catch (Exception ex)
                {
                    // No bloquear el consumer: se loguea el error y se continúa con el siguiente mensaje.
                    _logger.LogError(
                        ex,
                        "Error procesando evento del topic {Topic} (offset {Offset}); se continúa con el siguiente mensaje",
                        Topic,
                        result.Offset);
                }

                // Se hace commit siempre (procesado, duplicado descartado, o fallido),
                // para no reintentar indefinidamente el mismo mensaje.
                _consumer.Commit(result);
            }
        }
        catch (OperationCanceledException)
        {
            // Esperado al detener el servicio.
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cts?.Cancel();

        if (_consumeLoopTask is not null)
        {
            await Task.WhenAny(_consumeLoopTask, Task.Delay(Timeout.Infinite, cancellationToken));
        }

        _consumer?.Close();
        _logger.LogInformation("KafkaConsumerService detenido");
    }

    public void Dispose()
    {
        _cts?.Dispose();
        _consumer?.Dispose();
    }
}


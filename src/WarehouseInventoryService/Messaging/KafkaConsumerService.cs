using System.Text.Json;
using Confluent.Kafka;

namespace WarehouseInventoryService.Messaging;

/// <summary>
/// Servicio hospedado que se suscribe al topic Kafka "sale.completed" al iniciar la aplicación.
/// Por cada evento recibido, delega el procesamiento a ISaleEventProcessor que se encarga de:
/// verificar idempotencia (EventosKafkaProcesados), deserializar el payload, iterar los items
/// y descontar stock vía IStockRepository.Descontar.
/// Confirma procesados y duplicados; los errores permanentes se envían a sale.completed.error.
/// Los fallos transitorios vuelven al mismo offset sin confirmarlo.
/// </summary>
public sealed class KafkaConsumerService : IHostedService, IDisposable
{
    private string Topic => _configuration["Kafka:Topic"] ?? "sale.completed";
    private string ErrorTopic => _configuration["Kafka:ErrorTopic"] ?? "sale.completed.error";
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private IConsumer<string, string>? _consumer;
    private IProducer<string, string>? _errorProducer;
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
        var bootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = _configuration["Kafka:GroupId"] ?? "warehouse-inventory-service",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
        _errorProducer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All
        }).Build();
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
                    var processingResult = processor.ProcesarEvento(result.Message.Value);

                    if (processingResult.Retry)
                    {
                        Retry(result, processingResult.Error, cancellationToken);
                        continue;
                    }

                    if (processingResult.SendToErrorTopic)
                    {
                        SendToErrorTopic(result, processingResult);
                    }

                    if (processingResult.CommitOffset)
                    {
                        _consumer.Commit(result);
                        _logger.LogInformation(
                            "Offset {Offset} confirmado tras resultado {Status}",
                            result.Offset,
                            processingResult.Status);
                    }
                }
                catch (Exception ex)
                {
                    Retry(result, ex.Message, cancellationToken, ex);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Esperado al detener el servicio.
        }
    }

    private void SendToErrorTopic(
        ConsumeResult<string, string> result,
        SaleEventProcessingResult processingResult)
    {
        if (_errorProducer is null)
        {
            throw new InvalidOperationException("Productor de errores Kafka no inicializado");
        }

        var payload = JsonSerializer.Serialize(new
        {
            source_topic = result.Topic,
            source_partition = result.Partition.Value,
            source_offset = result.Offset.Value,
            error_type = processingResult.Status.ToString(),
            error = processingResult.Error,
            original_payload = result.Message.Value,
            failed_at = DateTime.UtcNow
        });

        _errorProducer.ProduceAsync(ErrorTopic, new Message<string, string>
        {
            Key = result.Message.Key,
            Value = payload
        }).GetAwaiter().GetResult();
    }

    private void Retry(
        ConsumeResult<string, string> result,
        string? error,
        CancellationToken cancellationToken,
        Exception? exception = null)
    {
        _logger.LogWarning(
            exception,
            "Fallo transitorio en {Topic} offset {Offset}: {Error}. Offset no confirmado; se reintenta",
            Topic,
            result.Offset,
            error);

        _consumer!.Seek(result.TopicPartitionOffset);
        Task.Delay(RetryDelay, cancellationToken).GetAwaiter().GetResult();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cts?.Cancel();

        if (_consumeLoopTask is not null)
        {
            await Task.WhenAny(_consumeLoopTask, Task.Delay(Timeout.Infinite, cancellationToken));
        }

        _consumer?.Close();
        _errorProducer?.Flush(TimeSpan.FromSeconds(5));
        _logger.LogInformation("KafkaConsumerService detenido");
    }

    public void Dispose()
    {
        _cts?.Dispose();
        _consumer?.Dispose();
        _errorProducer?.Dispose();
    }
}


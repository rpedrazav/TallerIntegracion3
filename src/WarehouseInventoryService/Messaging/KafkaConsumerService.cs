using Confluent.Kafka;

namespace WarehouseInventoryService.Messaging;

/// <summary>
/// Servicio hospedado que se suscribe al topic Kafka "sale.completed" al iniciar la aplicación.
/// Alcance actual: suscripción y consumo básico con log del payload recibido.
/// La lógica de descuento de stock (regla FEFO) se implementa en un ticket separado.
/// </summary>
public sealed class KafkaConsumerService : IHostedService, IDisposable
{
    private const string Topic = "sale.completed";

    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly IConfiguration _configuration;
    private IConsumer<string, string>? _consumer;
    private CancellationTokenSource? _cts;
    private Task? _consumeLoopTask;

    public KafkaConsumerService(ILogger<KafkaConsumerService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
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
                try
                {
                    var result = _consumer.Consume(cancellationToken);

                    // TODO: aplicar descuento de stock con regla FEFO (ver ticket siguiente).
                    _logger.LogInformation(
                        "Evento recibido en {Topic}: {Payload}",
                        Topic,
                        result.Message.Value);

                    _consumer.Commit(result);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consumiendo mensaje del topic {Topic}", Topic);
                }
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

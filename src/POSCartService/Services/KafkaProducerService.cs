using System.Text.Json;
using Confluent.Kafka;
using POSCartService.Messaging;

namespace POSCartService.Services;

/// <summary>
/// Implementación del producer Kafka para MS-5 POS &amp; Cart Service.
/// Recibe <see cref="IProducer{TKey,TValue}"/> inyectado por DI (Singleton) para reutilizar
/// la conexión al broker entre requests, siguiendo el patrón recomendado por Confluent.
/// </summary>
public sealed class KafkaProducerService : IKafkaProducerService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;

    /// <param name="producer">
    /// Producer de Confluent.Kafka inyectado como Singleton desde el contenedor DI.
    /// Configurado en <c>Program.cs</c> con <c>BootstrapServers</c>, <c>Acks.Leader</c>
    /// y reintentos automáticos.
    /// </param>
    public KafkaProducerService(
        IProducer<string, string> producer,
        ILogger<KafkaProducerService> logger)
    {
        _producer = producer ?? throw new ArgumentNullException(nameof(producer));
        _logger   = logger   ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task PublicarAsync(
        string topico,
        string mensaje,
        string? clave = null,
        CancellationToken cancellationToken = default)
    {
        var message = new Message<string, string>
        {
            Key   = clave ?? string.Empty,
            Value = mensaje
        };

        try
        {
            var result = await _producer.ProduceAsync(topico, message, cancellationToken);

            _logger.LogInformation(
                "Kafka: mensaje publicado en topic={Topic} partition={Partition} offset={Offset} key={Key}",
                topico, result.Partition.Value, result.Offset.Value, clave);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(
                ex,
                "Kafka: error publicando en topic={Topic} key={Key}: {Reason}",
                topico, clave, ex.Error.Reason);
            // No relanzar: el caller decide si el error Kafka debe abortar su flujo.
            // VentaService lo usa fire-and-forget para no revertir la BD ya persistida.
        }
    }

    /// <inheritdoc/>
    public async Task PublicarSaleCompletedAsync(
        SaleCompletedEvent evento,
        CancellationToken cancellationToken = default)
    {
        const string topico = "sale.completed";
        var payload = JsonSerializer.Serialize(evento, JsonOptions);

        // Clave = tenant_id: garantiza orden de eventos por tenant en la misma partición.
        await PublicarAsync(topico, payload, clave: evento.TenantId.ToString(), cancellationToken);

        _logger.LogInformation(
            "sale.completed publicado: venta={VentaId} tenant={TenantId}",
            evento.VentaId, evento.TenantId);
    }
}

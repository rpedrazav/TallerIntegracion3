using System.Text.Json;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using WarehouseInventoryService.Data;
using WarehouseInventoryService.Models;
using WarehouseInventoryService.Repositories;

namespace WarehouseInventoryService.Messaging;

/// <summary>
/// Servicio hospedado que se suscribe al topic Kafka "sale.completed" al iniciar la aplicación.
/// Por cada evento recibido: verifica idempotencia (EventosKafkaProcesados), deserializa el
/// payload, itera los items y descuenta stock vía IStockRepository.Descontar.
/// No implementa selección de lote por FEFO (ver ticket separado).
/// </summary>
public sealed class KafkaConsumerService : IHostedService, IDisposable
{
    private const string Topic = "sale.completed";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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
                try
                {
                    var result = _consumer.Consume(cancellationToken);
                    ProcesarEvento(result.Message.Value);
                    // Se hace commit siempre (evento nuevo procesado o duplicado descartado),
                    // para no reintentar indefinidamente un mensaje ya identificado como duplicado.
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

    private void ProcesarEvento(string payload)
    {
        SaleCompletedEvent? evento;
        try
        {
            evento = JsonSerializer.Deserialize<SaleCompletedEvent>(payload, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Payload inválido en topic {Topic}: {Payload}", Topic, payload);
            return;
        }

        if (evento is null || evento.Items.Count == 0)
        {
            _logger.LogWarning("Evento sale.completed sin items o nulo: {Payload}", payload);
            return;
        }

        // event_id = VentaId (ver decisión de diseño acordada: no hay eventId explícito en el payload).
        var eventId = evento.VentaId;

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();

        var yaProcesado = context.EventosKafkaProcesados
            .AsNoTracking()
            .Any(e => e.EventId == eventId);

        if (yaProcesado)
        {
            _logger.LogInformation(
                "Evento {EventId} ya fue procesado anteriormente, se descarta (idempotencia)",
                eventId);
            return;
        }

        var stockRepository = scope.ServiceProvider.GetRequiredService<IStockRepository>();

        foreach (var item in evento.Items)
        {
            var stockActualizado = stockRepository
                .Descontar(item.ProductoId, item.Cantidad, evento.TenantId, evento.SucursalId)
                .GetAwaiter()
                .GetResult();

            if (stockActualizado is null)
            {
                _logger.LogWarning(
                    "No existe stock para producto {ProductoId} en sucursal {SucursalId} (venta {VentaId})",
                    item.ProductoId, evento.SucursalId, evento.VentaId);
            }
            else
            {
                _logger.LogInformation(
                    "Stock descontado: producto {ProductoId}, sucursal {SucursalId}, cantidad {Cantidad}, venta {VentaId}",
                    item.ProductoId, evento.SucursalId, item.Cantidad, evento.VentaId);
            }
        }

        context.EventosKafkaProcesados.Add(new EventoKafkaProcesado
        {
            EventId = eventId,
            ProcesadoAt = DateTime.UtcNow
        });
        context.SaveChanges();
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

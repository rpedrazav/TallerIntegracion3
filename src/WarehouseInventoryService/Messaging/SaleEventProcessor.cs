using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WarehouseInventoryService.Data;
using WarehouseInventoryService.Models;
using WarehouseInventoryService.Repositories;

namespace WarehouseInventoryService.Messaging;

/// <summary>
/// Procesa eventos sale.completed con garantía de idempotencia (exactly-once).
/// Extraído de KafkaConsumerService para facilitar tests unitarios.
///
/// Flujo:
///   1. Deserializa el JSON del evento
///   2. Verifica si el event_id ya fue procesado (tabla EventosKafkaProcesados)
///   3. Si es duplicado → retorna Duplicate (descartado)
///   4. Si es nuevo → descuenta stock por cada item y registra el evento
/// </summary>
public class SaleEventProcessor : ISaleEventProcessor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly WarehouseDbContext _context;
    private readonly IStockRepository _stockRepository;
    private readonly ILogger<SaleEventProcessor> _logger;

    public SaleEventProcessor(
        WarehouseDbContext context,
        IStockRepository stockRepository,
        ILogger<SaleEventProcessor> logger)
    {
        _context = context;
        _stockRepository = stockRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public SaleEventProcessingResult ProcesarEvento(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return new(SaleEventProcessingStatus.InvalidPayload, "El payload está vacío");
        }

        SaleCompletedEvent? evento;
        try
        {
            evento = JsonSerializer.Deserialize<SaleCompletedEvent>(payload, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Payload inválido: {Payload}", payload);
            return new(SaleEventProcessingStatus.InvalidPayload, ex.Message);
        }

        if (evento is null)
        {
            return new(SaleEventProcessingStatus.InvalidPayload, "El payload no contiene un evento");
        }

        if (evento.Items is null || evento.Items.Count == 0)
        {
            _logger.LogWarning("Evento sale.completed sin items: {Payload}", payload);
            return new(SaleEventProcessingStatus.Unrecoverable, "El evento no contiene items");
        }

        // Idempotencia: usa EventId (GUID único del evento) si está presente, o VentaId como fallback.
        var eventId = evento.EventId != Guid.Empty ? evento.EventId : evento.VentaId;

        // Setear tenant para los filtros globales multi-tenant
        _context.CurrentTenantId = evento.TenantId;

        var yaProcesado = _context.EventosKafkaProcesados
            .AsNoTracking()
            .Any(e => e.EventId == eventId);

        if (yaProcesado)
        {
            _logger.LogInformation(
                "Evento {EventId} ya fue procesado anteriormente, se descarta (idempotencia)",
                eventId);
            return new(SaleEventProcessingStatus.Duplicate);
        }

        foreach (var item in evento.Items)
        {
            var stockActualizado = _stockRepository
                .Descontar(item.ProductoId, item.Cantidad, evento.TenantId, evento.SucursalId)
                .GetAwaiter()
                .GetResult();

            if (stockActualizado is not null)
            {
                var cantidadAnterior = stockActualizado.CantidadActual + item.Cantidad;

                // TI3-253: Log estructurado con todos los campos del movimiento
                _logger.LogInformation(
                    "Movimiento de stock: {Movimiento}",
                    new
                    {
                        tipo = "VENTA",
                        producto_id = item.ProductoId,
                        sucursal_id = evento.SucursalId,
                        tenant_id = evento.TenantId,
                        venta_id = evento.VentaId,
                        cantidad_descontada = item.Cantidad,
                        stock_anterior = cantidadAnterior,
                        stock_nuevo = stockActualizado.CantidadActual,
                        timestamp = DateTime.UtcNow
                    });

                // Persistir movimiento en tabla de auditoría
                _context.Movimientos.Add(new MovimientoStock
                {
                    ProductoId = item.ProductoId,
                    Tipo = "VENTA",
                    Cantidad = item.Cantidad,
                    Motivo = $"Venta {evento.VentaId} — stock {cantidadAnterior} → {stockActualizado.CantidadActual}",
                    TenantId = evento.TenantId
                });
            }
            else
            {
                _logger.LogWarning(
                    "Stock inexistente o insuficiente para producto {ProductoId} en sucursal {SucursalId}; evento {EventId} pendiente",
                    item.ProductoId,
                    evento.SucursalId,
                    eventId);
                return new(
                    SaleEventProcessingStatus.TransientFailure,
                    $"Stock inexistente o insuficiente para producto {item.ProductoId}");
            }
        }

        _context.EventosKafkaProcesados.Add(new EventoKafkaProcesado
        {
            EventId = eventId,
            ProcesadoAt = DateTime.UtcNow
        });

        // Una única llamada agrupa descuentos, movimientos e idempotencia en la transacción de EF Core.
        _context.SaveChanges();

        return new(SaleEventProcessingStatus.Processed);
    }
}

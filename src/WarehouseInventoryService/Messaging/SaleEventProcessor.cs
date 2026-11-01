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
///   3. Si es duplicado → retorna false (descartado)
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
    public bool ProcesarEvento(string payload)
    {
        SaleCompletedEvent? evento;
        try
        {
            evento = JsonSerializer.Deserialize<SaleCompletedEvent>(payload, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Payload inválido: {Payload}", payload);
            return false;
        }

        if (evento is null || evento.Items.Count == 0)
        {
            _logger.LogWarning("Evento sale.completed sin items o nulo: {Payload}", payload);
            return false;
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
            return false;
        }

        foreach (var item in evento.Items)
        {
            // Obtener stock anterior para el log estructurado (TI3-253)
            var stockAnterior = _stockRepository
                .GetByProducto(item.ProductoId, evento.SucursalId, evento.TenantId)
                .GetAwaiter()
                .GetResult();

            decimal cantidadAnterior = stockAnterior?.CantidadActual ?? 0m;

            var stockActualizado = _stockRepository
                .Descontar(item.ProductoId, item.Cantidad, evento.TenantId, evento.SucursalId)
                .GetAwaiter()
                .GetResult();

            if (stockActualizado is not null)
            {
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
                // TI3-254: Caso borde — producto sin registro en tabla Stock.
                // Se crea el registro con stock negativo (discrepancia a resolver).
                _logger.LogWarning(
                    "Producto {ProductoId} sin registro de stock en sucursal {SucursalId}. Creando registro (TI3-254)",
                    item.ProductoId, evento.SucursalId);

                var stockCreado = _stockRepository
                    .CrearYDescontar(item.ProductoId, item.Cantidad, evento.TenantId, evento.SucursalId)
                    .GetAwaiter()
                    .GetResult();

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
                        stock_anterior = 0m,
                        stock_nuevo = stockCreado.CantidadActual,
                        registro_creado = true,
                        timestamp = DateTime.UtcNow
                    });

                _context.Movimientos.Add(new MovimientoStock
                {
                    ProductoId = item.ProductoId,
                    Tipo = "VENTA",
                    Cantidad = item.Cantidad,
                    Motivo = $"Venta {evento.VentaId} — registro creado, stock 0 → {stockCreado.CantidadActual}",
                    TenantId = evento.TenantId
                });
            }
        }

        _context.EventosKafkaProcesados.Add(new EventoKafkaProcesado
        {
            EventId = eventId,
            ProcesadoAt = DateTime.UtcNow
        });
        _context.SaveChanges();

        return true;
    }
}

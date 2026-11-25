using System.Security.Claims;
using System.Text.Json;
using POSCartService.Messaging;
using POSCartService.Models;
using POSCartService.Repositories;

namespace POSCartService.Services;

public class VentaService : IVentaService
{
    private readonly IVentaRepository _ventaRepository;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly ReversaTarjetaService? _reversas;
    private readonly IItemVentaRepository? _itemVentaRepository;

    public VentaService(
        IVentaRepository ventaRepository,
        IItemVentaRepository? itemVentaRepository = null,
        IHttpContextAccessor? httpContextAccessor = null,
        ReversaTarjetaService? reversas = null)
    {
        _ventaRepository     = ventaRepository ?? throw new ArgumentNullException(nameof(ventaRepository));
        _itemVentaRepository = itemVentaRepository;
        _httpContextAccessor = httpContextAccessor;
        _reversas = reversas;
    }


    /// <inheritdoc/>
    public Task<Venta?> GetByIdAsync(Guid ventaId)
        => _ventaRepository.GetByIdAsync(ventaId);

    public async Task<Venta> CambiarMetodoPagoAsync(Guid ventaId, MetodoPagoVenta metodoPago)
    {
        var venta = await _ventaRepository.GetByIdAsync(ventaId)
            ?? throw new KeyNotFoundException($"Venta {ventaId} no encontrada.");
        if (venta.Estado != EstadoVenta.PENDIENTE || venta.Pagos.Count != 0)
            throw new InvalidOperationException("Solo se puede cambiar el método de una venta pendiente sin pagos.");
        return await _ventaRepository.CambiarMetodoPagoPendienteAsync(ventaId, metodoPago);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Venta>> GetByTurnoAsync(Guid turnoId)
        => _ventaRepository.GetByTurnoAsync(turnoId);

    /// <inheritdoc/>
    public async Task<Venta> CrearAsync(
        Guid turnoId,
        Guid cajeroId,
        Guid sucursalId,
        Guid tenantId,
        IEnumerable<ItemVenta> items,
        decimal subtotal,
        decimal impuestos,
        decimal total,
        MetodoPagoVenta metodoPago)
    {
        var listaItems = items.ToList();

        if (listaItems.Count == 0)
            throw new ArgumentException("La venta debe tener al menos un ítem.", nameof(items));

        var venta = new Venta
        {
            TurnoId    = turnoId,
            CajeroId   = cajeroId,
            SucursalId = sucursalId,
            TenantId   = tenantId,
            MetodoPago = metodoPago,
            Estado     = EstadoVenta.PENDIENTE,
            Items      = listaItems
        };
        AplicarTotales(venta, subtotal, impuestos, total);

        return await _ventaRepository.CrearAsync(venta);
    }

    /// <inheritdoc/>
    public async Task<Venta> CompletarAsync(Guid ventaId, decimal montoRecibido, decimal vuelto)
    {
        var venta = await _ventaRepository.GetByIdAsync(ventaId)
            ?? throw new KeyNotFoundException($"Venta {ventaId} no encontrada.");

        if (venta.Estado != EstadoVenta.PENDIENTE)
            throw new InvalidOperationException(
                $"Solo se puede completar una venta PENDIENTE. Estado actual: {venta.Estado}.");

        if (venta.MetodoPago != MetodoPagoVenta.EFECTIVO)
            throw new InvalidOperationException(
                $"No se puede registrar pago en efectivo para una venta con método de pago {venta.MetodoPago}.");

        var pago = new Pago
        {
            Id      = Guid.NewGuid(),
            VentaId = venta.Id,
            Metodo  = MetodoPago.EFECTIVO,
            Monto   = montoRecibido,
            Vuelto  = vuelto
        };

        var outboxMessage = CrearOutboxVentaCompletada(venta);

        return await _ventaRepository.CompletarConOutboxAsync(venta.Id, pago, outboxMessage);
    }

    /// <inheritdoc/>
    public async Task<Venta> AnularAsync(Guid ventaId, Guid cajeroId, string motivo)
    {
        // Defensa también para llamadas internas: no aceptar una identidad enviada por el body.
        var user = _httpContextAccessor?.HttpContext?.User;
        var userId = user?.FindFirst("cajero_id")?.Value
            ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user?.FindFirst("sub")?.Value;
        if (user?.Identity?.IsAuthenticated != true || !user.IsInRole("ADMIN")
            || !Guid.TryParse(userId, out var adminId) || adminId == Guid.Empty || adminId != cajeroId
            || !Guid.TryParse(user.FindFirst("tenant_id")?.Value, out var tenantId) || tenantId == Guid.Empty)
            throw new UnauthorizedAccessException("La anulación requiere un ADMIN autenticado del tenant.");

        motivo = motivo?.Trim() ?? string.Empty;
        if (motivo.Length is < 1 or > 500)
            throw new ArgumentException("motivo debe contener entre 1 y 500 caracteres.", nameof(motivo));

        var venta = await _ventaRepository.GetByIdAsync(ventaId);
        if (venta is null || venta.TenantId != tenantId) throw new KeyNotFoundException("Venta no encontrada.");
        if (venta.MetodoPago == MetodoPagoVenta.TARJETA && _reversas is not null)
            return await _reversas.AnularAsync(ventaId, motivo);
        return await _ventaRepository.AnularTotalAsync(ventaId, tenantId, adminId, motivo);
    }

    /// <inheritdoc/>
    public async Task<(Venta Venta, ItemVenta Item)> AgregarItemAsync(
        Guid ventaId,
        ItemVenta item,
        decimal subtotal,
        decimal impuestos,
        decimal total)
    {
        var venta = await _ventaRepository.GetByIdAsync(ventaId)
            ?? throw new KeyNotFoundException($"Venta {ventaId} no encontrada.");

        if (venta.Estado != EstadoVenta.PENDIENTE)
            throw new InvalidOperationException(
                $"Solo se pueden agregar ítems a una venta PENDIENTE. Estado actual: {venta.Estado}.");

        item.VentaId = ventaId;

        // Si el repositorio de ítems está disponible, persistir el ítem individual
        if (_itemVentaRepository is not null)
        {
            await _itemVentaRepository.CrearAsync(item);
        }

        AplicarTotales(venta, subtotal, impuestos, total);

        await _ventaRepository.ActualizarAsync(venta);

        // Añadir a la colección en memoria si no está ya
        if (!venta.Items.Any(i => i.Id == item.Id))
        {
            venta.Items.Add(item);
        }

        return (venta, item);
    }

    /// <inheritdoc/>
    public async Task<(Venta Venta, ItemVenta Item)> ModificarCantidadItemAsync(
        Guid ventaId,
        Guid itemId,
        decimal nuevaCantidad,
        decimal? nuevoPesoKg,
        decimal nuevoSubtotalItem,
        decimal nuevoSubtotalVenta,
        decimal nuevosImpuestosVenta,
        decimal nuevoTotalVenta)
    {
        var venta = await _ventaRepository.GetByIdAsync(ventaId)
            ?? throw new KeyNotFoundException($"Venta {ventaId} no encontrada.");

        if (venta.Estado != EstadoVenta.PENDIENTE)
            throw new InvalidOperationException(
                $"Solo se pueden modificar ítems de una venta PENDIENTE. Estado actual: {venta.Estado}.");

        var item = venta.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new KeyNotFoundException($"Item {itemId} no encontrado en la venta {ventaId}.");

        item.Cantidad = nuevaCantidad;
        item.PesoKg   = nuevoPesoKg;
        item.Subtotal = nuevoSubtotalItem;

        if (_itemVentaRepository is not null)
        {
            await _itemVentaRepository.ActualizarAsync(item);
        }

        AplicarTotales(venta, nuevoSubtotalVenta, nuevosImpuestosVenta, nuevoTotalVenta);

        await _ventaRepository.ActualizarAsync(venta);

        return (venta, item);
    }

    /// <inheritdoc/>
    public async Task<(Venta Venta, ItemVenta ItemEliminado)> EliminarItemAsync(
        Guid ventaId,
        Guid itemId,
        decimal nuevoSubtotalVenta,
        decimal nuevosImpuestosVenta,
        decimal nuevoTotalVenta)
    {
        var venta = await _ventaRepository.GetByIdAsync(ventaId)
            ?? throw new KeyNotFoundException($"Venta {ventaId} no encontrada.");

        if (venta.Estado != EstadoVenta.PENDIENTE)
            throw new InvalidOperationException(
                $"Solo se pueden eliminar ítems de una venta PENDIENTE. Estado actual: {venta.Estado}.");

        var item = venta.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new KeyNotFoundException($"Item {itemId} no encontrado en la venta {ventaId}.");

        // Eliminar del repositorio de ítems si está disponible
        if (_itemVentaRepository is not null)
        {
            await _itemVentaRepository.EliminarAsync(item);
        }

        // Remover de la colección en memoria
        venta.Items.Remove(item);

        AplicarTotales(venta, nuevoSubtotalVenta, nuevosImpuestosVenta, nuevoTotalVenta);

        await _ventaRepository.ActualizarAsync(venta);

        return (venta, item);
    }

    /// <inheritdoc/>
    public async Task<Venta> CompletarTarjetaAsync(Guid ventaId, IntentoPago intentoPago)
    {
        var venta = await _ventaRepository.GetByIdAsync(ventaId)
            ?? throw new KeyNotFoundException($"Venta {ventaId} no encontrada.");

        if (venta.Estado != EstadoVenta.PENDIENTE)
            throw new InvalidOperationException(
                $"Solo se puede completar una venta PENDIENTE. Estado actual: {venta.Estado}.");

        if (venta.MetodoPago == MetodoPagoVenta.EFECTIVO)
            throw new InvalidOperationException(
                "CompletarTarjetaAsync no puede usarse para ventas con método de pago EFECTIVO.");

        var pago = new Pago
        {
            Id         = Guid.NewGuid(),
            VentaId    = venta.Id,
            Metodo     = MetodoPago.TARJETA,
            Monto      = venta.Total,
            Vuelto     = 0m,
            Referencia = intentoPago.Referencia
        };

        var outboxMessage = CrearOutboxVentaCompletada(venta);

        intentoPago.TenantId  = venta.TenantId;
        intentoPago.VentaId   = venta.Id;
        intentoPago.CreatedAt = intentoPago.CreatedAt == default ? outboxMessage.CreatedAt : intentoPago.CreatedAt;

        return await _ventaRepository.CompletarTarjetaConOutboxAsync(venta.Id, pago, intentoPago, outboxMessage);
    }

    // MS-2 es la autoridad fiscal: conservar sus importes y redondeos sin recalcularlos aquí.
    private static void AplicarTotales(Venta venta, decimal subtotal, decimal impuestos, decimal total)
    {
        venta.Subtotal = subtotal;
        venta.Impuestos = impuestos;
        venta.Total = total;
    }

    private static OutboxMessage CrearOutboxVentaCompletada(Venta venta)
    {
        var eventId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;
        var evento = new SaleCompletedEvent
        {
            EventId       = eventId,
            SchemaVersion = 1,
            TenantId      = venta.TenantId,
            VentaId       = venta.Id,
            CajeroId      = venta.CajeroId,
            SucursalId    = venta.SucursalId,
            Subtotal      = venta.Subtotal,
            Iva           = venta.Impuestos,
            Total         = venta.Total,
            MetodoPago    = venta.MetodoPago.ToString(),
            Timestamp     = timestamp,
            Items         = venta.Items.Select(i => new SaleCompletedItemEvent
            {
                ProductoId     = i.ProductoId,
                Cantidad       = i.Cantidad,
                PrecioUnitario = i.PrecioUnitario
            }).ToList()
        };
        return new OutboxMessage
        {
            EventId          = eventId,
            TenantId         = venta.TenantId,
            VentaId          = venta.Id,
            EventType        = "sale.completed",
            SchemaVersion    = evento.SchemaVersion,
            Topic            = "sale.completed",
            MessageKey       = venta.TenantId.ToString(),
            Payload          = JsonSerializer.Serialize(evento),
            Status           = OutboxStatus.PENDIENTE,
            CreatedAt        = timestamp,
            NextAttemptAt    = timestamp
        };
    }

    public Task<IntentoPago> IniciarIntentoPagoAsync(Guid ventaId, Guid requestId)
        => _ventaRepository.IniciarIntentoPagoAsync(ventaId, requestId);

    public Task<IntentoPago?> GetUltimoIntentoPagoAsync(Guid ventaId)
        => _ventaRepository.GetUltimoIntentoPagoAsync(ventaId);

    public Task ActualizarIntentoPagoAsync(IntentoPago intentoPago)
        => _ventaRepository.ActualizarIntentoPagoAsync(intentoPago);
}

using POSCartService.Models;
using POSCartService.Repositories;

namespace POSCartService.Services;

public class VentaService : IVentaService
{
    private readonly IVentaRepository _ventaRepository;
    private readonly IItemVentaRepository? _itemVentaRepository;

    public VentaService(IVentaRepository ventaRepository, IItemVentaRepository? itemVentaRepository = null)
    {
        _ventaRepository = ventaRepository ?? throw new ArgumentNullException(nameof(ventaRepository));
        _itemVentaRepository = itemVentaRepository;
    }


    /// <inheritdoc/>
    public Task<Venta?> GetByIdAsync(Guid ventaId)
        => _ventaRepository.GetByIdAsync(ventaId);

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
        decimal impuestoPorcentaje,
        MetodoPagoVenta metodoPago)
    {
        var listaItems = items.ToList();

        if (listaItems.Count == 0)
            throw new ArgumentException("La venta debe tener al menos un ítem.", nameof(items));

        if (impuestoPorcentaje < 0)
            throw new ArgumentOutOfRangeException(nameof(impuestoPorcentaje), "El porcentaje de impuesto no puede ser negativo.");

        // RN-01: subtotal = suma de (precio unitario * cantidad)
        var subtotal = listaItems.Sum(i => i.Subtotal);

        // RN-01: impuestos y total
        var impuestos = Math.Round(subtotal * impuestoPorcentaje / 100m, 2);
        var total     = subtotal + impuestos;

        var venta = new Venta
        {
            TurnoId    = turnoId,
            CajeroId   = cajeroId,
            SucursalId = sucursalId,
            TenantId   = tenantId,
            Subtotal   = subtotal,
            Impuestos  = impuestos,
            Total      = total,
            MetodoPago = metodoPago,
            Estado     = EstadoVenta.PENDIENTE,
            Items      = listaItems
        };

        return await _ventaRepository.CrearAsync(venta);
    }

    /// <inheritdoc/>
    public async Task<Venta> CompletarAsync(Guid ventaId)
    {
        var venta = await _ventaRepository.GetByIdAsync(ventaId)
            ?? throw new KeyNotFoundException($"Venta {ventaId} no encontrada.");

        if (venta.Estado != EstadoVenta.PENDIENTE)
            throw new InvalidOperationException(
                $"Solo se puede completar una venta PENDIENTE. Estado actual: {venta.Estado}.");

        venta.Estado = EstadoVenta.COMPLETADA;
        return await _ventaRepository.ActualizarAsync(venta);
    }

    /// <inheritdoc/>
    public async Task<Venta> AnularAsync(Guid ventaId, Guid cajeroId, string motivo)
    {
        var venta = await _ventaRepository.GetByIdAsync(ventaId)
            ?? throw new KeyNotFoundException($"Venta {ventaId} no encontrada.");

        if (venta.Estado is EstadoVenta.ANULADA or EstadoVenta.CANCELADA)
            throw new InvalidOperationException(
                $"La venta ya se encuentra en estado {venta.Estado} y no puede anularse nuevamente.");

        venta.Estado    = EstadoVenta.ANULADA;
        venta.Anulacion = new Anulacion
        {
            VentaId       = venta.Id,
            AutorizadoPor = cajeroId,
            Motivo        = motivo
        };

        return await _ventaRepository.ActualizarAsync(venta);
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

        // Actualizar totales calculados de la venta padre
        venta.Subtotal  = subtotal;
        venta.Impuestos = impuestos;
        venta.Total     = total;

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

        venta.Subtotal  = nuevoSubtotalVenta;
        venta.Impuestos = nuevosImpuestosVenta;
        venta.Total     = nuevoTotalVenta;

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

        // Actualizar totales de la venta padre
        venta.Subtotal  = nuevoSubtotalVenta;
        venta.Impuestos = nuevosImpuestosVenta;
        venta.Total     = nuevoTotalVenta;

        await _ventaRepository.ActualizarAsync(venta);

        return (venta, item);
    }
}



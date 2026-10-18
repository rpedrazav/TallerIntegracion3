using Microsoft.EntityFrameworkCore;
using POSCartService.Data;
using POSCartService.Models;

namespace POSCartService.Repositories;

public class VentaRepository : IVentaRepository
{
    private readonly PosCartDbContext _context;

    public VentaRepository(PosCartDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<Venta?> GetByIdAsync(Guid ventaId)
    {
        return await _context.Ventas
            .AsNoTracking()
            .Include(v => v.Items)
            .Include(v => v.Pagos)
            .Include(v => v.Anulacion)
            .FirstOrDefaultAsync(v => v.Id == ventaId);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Venta>> GetByTurnoAsync(Guid turnoId)
    {
        return await _context.Ventas
            .Include(v => v.Items)
            .Include(v => v.Pagos)
            .Where(v => v.TurnoId == turnoId)
            .OrderBy(v => v.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Venta>> GetByCajeroAsync(Guid cajeroId, DateTime desde, DateTime hasta)
    {
        return await _context.Ventas
            .Include(v => v.Items)
            .Include(v => v.Pagos)
            .Where(v => v.CajeroId == cajeroId
                     && v.CreatedAt >= desde
                     && v.CreatedAt <= hasta)
            .OrderBy(v => v.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<Venta> CrearAsync(Venta venta)
    {
        _context.Ventas.Add(venta);
        await _context.SaveChangesAsync();
        return venta;
    }

    /// <inheritdoc/>
    public async Task<Venta> ActualizarAsync(Venta venta)
    {
        // La pertenencia se comprueba en la base, incluso si el llamador omite el servicio.
        // Nunca se usa el TenantId aportado en venta para autorizar la actualización.
        var tracked = await _context.Ventas
            .Include(v => v.Items)
            .Include(v => v.Pagos)
            .Include(v => v.Anulacion)
            .FirstOrDefaultAsync(v => v.Id == venta.Id
                && _context.CurrentTenantId.HasValue && v.TenantId == _context.CurrentTenantId);
        if (tracked is null)
            throw new KeyNotFoundException($"Venta {venta.Id} no encontrada para actualizar.");

        // Copiar solo las propiedades escalares que pueden cambiar
        tracked.Estado     = venta.Estado;
        tracked.Subtotal   = venta.Subtotal;
        tracked.Impuestos  = venta.Impuestos;
        tracked.Total      = venta.Total;
        tracked.MetodoPago = venta.MetodoPago;

        // Si se adjunta una nueva Anulacion, persistirla por separado
        if (venta.Anulacion is not null && tracked.Anulacion is null)
        {
            venta.Anulacion.VentaId = tracked.Id;
            _context.Anulaciones.Add(venta.Anulacion);
            tracked.Anulacion = venta.Anulacion;
        }

        // Si se adjuntan nuevos Pagos, persistirlos por separado
        var pagosExistentesIds = tracked.Pagos?.Select(p => p.Id).ToHashSet() ?? new HashSet<Guid>();
        var pagosNuevos = venta.Pagos.Where(p => !pagosExistentesIds.Contains(p.Id)).ToList();
        foreach (var pago in pagosNuevos)
        {
            pago.VentaId = tracked.Id;
            _context.Pagos.Add(pago);
        }

        await _context.SaveChangesAsync();
        return tracked;
    }

    public async Task<Venta> CambiarMetodoPagoPendienteAsync(Guid ventaId, MetodoPagoVenta metodoPago)
    {
        var updated = await _context.Ventas
            .Where(v => v.Id == ventaId && _context.CurrentTenantId.HasValue
                && v.TenantId == _context.CurrentTenantId && v.Estado == EstadoVenta.PENDIENTE
                && !v.Pagos.Any()
                && !v.IntentosPago.Any(i => i.Estado != EstadoIntentoPago.RECHAZADO))
            .ExecuteUpdateAsync(setters => setters.SetProperty(v => v.MetodoPago, metodoPago));
        if (updated != 1)
            throw new InvalidOperationException("Solo se puede cambiar el método de una venta pendiente sin pagos.");
        return (await GetByIdAsync(ventaId))!;
    }

    /// <inheritdoc/>
    public async Task<Venta> CompletarConOutboxAsync(
        Guid ventaId,
        Pago pago,
        OutboxMessage outboxMessage)
    {
        var tenantId = _context.CurrentTenantId
            ?? throw new KeyNotFoundException($"Venta {ventaId} no encontrada para completar.");

        if (outboxMessage.EventId == Guid.Empty
            || outboxMessage.TenantId != tenantId
            || outboxMessage.VentaId != ventaId)
        {
            throw new InvalidOperationException("El evento outbox no coincide con la venta y el tenant actuales.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var tracked = await _context.Ventas
            .Include(v => v.Items)
            .Include(v => v.Pagos)
            .FirstOrDefaultAsync(v => v.Id == ventaId && v.TenantId == tenantId)
            ?? throw new KeyNotFoundException($"Venta {ventaId} no encontrada para completar.");

        if (tracked.Estado != EstadoVenta.PENDIENTE)
        {
            throw new InvalidOperationException(
                $"Solo se puede completar una venta PENDIENTE. Estado actual: {tracked.Estado}.");
        }

        tracked.Estado = EstadoVenta.COMPLETADA;
        pago.VentaId = tracked.Id;
        _context.Pagos.Add(pago);
        _context.OutboxMessages.Add(outboxMessage);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return tracked;
    }

    /// <inheritdoc/>
    public async Task<Venta> CompletarTarjetaConOutboxAsync(
        Guid ventaId,
        Pago pago,
        IntentoPago intentoPago,
        OutboxMessage outboxMessage)
    {
        var tenantId = _context.CurrentTenantId
            ?? throw new KeyNotFoundException($"Venta {ventaId} no encontrada para completar.");

        if (outboxMessage.EventId == Guid.Empty
            || outboxMessage.TenantId != tenantId
            || outboxMessage.VentaId != ventaId)
        {
            throw new InvalidOperationException("El evento outbox no coincide con la venta y el tenant actuales.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var tracked = await _context.Ventas
            .Include(v => v.Items)
            .Include(v => v.Pagos)
            .FirstOrDefaultAsync(v => v.Id == ventaId && v.TenantId == tenantId)
            ?? throw new KeyNotFoundException($"Venta {ventaId} no encontrada para completar.");

        if (tracked.Estado != EstadoVenta.PENDIENTE)
        {
            throw new InvalidOperationException(
                $"Solo se puede completar una venta PENDIENTE. Estado actual: {tracked.Estado}.");
        }

        tracked.Estado = EstadoVenta.COMPLETADA;
        pago.VentaId = tracked.Id;
        intentoPago.VentaId = tracked.Id;
        intentoPago.Estado = EstadoIntentoPago.APROBADO;
        intentoPago.UpdatedAt = DateTime.UtcNow;

        _context.Pagos.Add(pago);
        _context.IntentosPago.Add(intentoPago);
        _context.OutboxMessages.Add(outboxMessage);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return tracked;
    }

    /// <inheritdoc/>
    public async Task RegistrarIntentoPagoAsync(IntentoPago intentoPago)
    {
        intentoPago.UpdatedAt = DateTime.UtcNow;
        _context.IntentosPago.Add(intentoPago);
        await _context.SaveChangesAsync();
    }
}

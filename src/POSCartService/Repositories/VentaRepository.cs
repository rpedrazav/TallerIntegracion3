using System.Data;
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

    public async Task<IntentoPago> IniciarIntentoPagoAsync(Guid ventaId, Guid requestId)
    {
        var tenantId = _context.CurrentTenantId
            ?? throw new KeyNotFoundException($"Venta {ventaId} no encontrada.");
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var venta = await _context.Ventas.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == ventaId && v.TenantId == tenantId)
            ?? throw new KeyNotFoundException($"Venta {ventaId} no encontrada.");
        if (venta.Estado != EstadoVenta.PENDIENTE || venta.MetodoPago == MetodoPagoVenta.EFECTIVO ||
            await _context.Pagos.AnyAsync(p => p.VentaId == ventaId) ||
            await _context.IntentosPago.AnyAsync(i => i.IdempotencyKey == requestId.ToString()) ||
            await _context.IntentosPago.AnyAsync(i => i.VentaId == ventaId && i.Estado != EstadoIntentoPago.RECHAZADO))
            throw new InvalidOperationException("La venta ya tiene un cobro en curso o sin resultado definitivo.");

        var intento = new IntentoPago
        {
            TenantId = tenantId,
            VentaId = ventaId,
            IdempotencyKey = requestId.ToString(),
            Estado = EstadoIntentoPago.PENDIENTE,
            Monto = venta.Total,
            Moneda = "CLP"
        };
        _context.IntentosPago.Add(intento);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return intento;
    }

    public Task<IntentoPago?> GetUltimoIntentoPagoAsync(Guid ventaId)
        => _context.IntentosPago.AsNoTracking()
            .Where(i => i.VentaId == ventaId && _context.CurrentTenantId.HasValue && i.TenantId == _context.CurrentTenantId)
            .OrderByDescending(i => i.CreatedAt)
            .ThenByDescending(i => i.Id)
            .FirstOrDefaultAsync();

    public async Task ActualizarIntentoPagoAsync(IntentoPago intentoPago)
    {
        var updated = await _context.IntentosPago
            .Where(i => i.Id == intentoPago.Id && i.VentaId == intentoPago.VentaId &&
                _context.CurrentTenantId.HasValue && i.TenantId == _context.CurrentTenantId &&
                (i.Estado == EstadoIntentoPago.PENDIENTE || i.Estado == EstadoIntentoPago.FALLIDO))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(i => i.Estado, intentoPago.Estado)
                .SetProperty(i => i.Referencia, intentoPago.Referencia)
                .SetProperty(i => i.AuthorizationCode, intentoPago.AuthorizationCode)
                .SetProperty(i => i.DeclineCode, intentoPago.DeclineCode)
                .SetProperty(i => i.Mensaje, intentoPago.Mensaje)
                .SetProperty(i => i.UpdatedAt, DateTime.UtcNow));
        if (updated != 1)
            throw new InvalidOperationException("El intento de pago ya cambió de estado.");
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
        if (tracked.MetodoPago == MetodoPagoVenta.EFECTIVO)
            throw new InvalidOperationException("La venta ya no admite cobro con tarjeta.");

        tracked.Estado = EstadoVenta.COMPLETADA;
        pago.VentaId = tracked.Id;
        intentoPago.VentaId = tracked.Id;

        _context.Pagos.Add(pago);
        var intentoExistente = await _context.IntentosPago.FirstOrDefaultAsync(i =>
            i.VentaId == ventaId && i.TenantId == tenantId && i.IdempotencyKey == intentoPago.IdempotencyKey);
        if (intentoExistente is null)
        {
            intentoPago.Estado = EstadoIntentoPago.APROBADO;
            intentoPago.UpdatedAt = DateTime.UtcNow;
            _context.IntentosPago.Add(intentoPago);
        }
        else if (intentoExistente.Estado is EstadoIntentoPago.PENDIENTE or EstadoIntentoPago.FALLIDO)
        {
            intentoExistente.Estado = EstadoIntentoPago.APROBADO;
            intentoExistente.Referencia = intentoPago.Referencia;
            intentoExistente.AuthorizationCode = intentoPago.AuthorizationCode;
            intentoExistente.Mensaje = intentoPago.Mensaje;
            intentoExistente.UpdatedAt = DateTime.UtcNow;
        }
        else
            throw new InvalidOperationException("El intento de pago ya tiene un resultado definitivo.");
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

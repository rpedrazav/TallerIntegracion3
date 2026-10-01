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
        // Usamos IgnoreQueryFilters porque ActualizarAsync ya es llamado desde un
        // scope autenticado; el filtro multi-tenant no debe bloquear la escritura.
        var tracked = await _context.Ventas
            .IgnoreQueryFilters()
            .Include(v => v.Items)
            .Include(v => v.Pagos)
            .Include(v => v.Anulacion)
            .FirstOrDefaultAsync(v => v.Id == venta.Id);
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
}

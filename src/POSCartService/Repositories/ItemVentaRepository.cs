using Microsoft.EntityFrameworkCore;
using POSCartService.Data;
using POSCartService.Models;

namespace POSCartService.Repositories;

public class ItemVentaRepository : IItemVentaRepository
{
    private readonly PosCartDbContext _context;

    public ItemVentaRepository(PosCartDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ItemVenta>> GetByVentaAsync(Guid ventaId)
    {
        return await _context.ItemsVenta
            .Where(i => i.VentaId == ventaId)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<ItemVenta?> GetByIdAsync(Guid itemId)
    {
        return await _context.ItemsVenta
            .FirstOrDefaultAsync(i => i.Id == itemId);
    }

    /// <inheritdoc/>
    public async Task<ItemVenta> CrearAsync(ItemVenta item)
    {
        _context.ItemsVenta.Add(item);
        await _context.SaveChangesAsync();
        return item;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ItemVenta>> CrearRangoAsync(IEnumerable<ItemVenta> items)
    {
        var lista = items.ToList();
        _context.ItemsVenta.AddRange(lista);
        await _context.SaveChangesAsync();
        return lista;
    }

    /// <inheritdoc/>
    public async Task<ItemVenta> ActualizarAsync(ItemVenta item)
    {
        var tracked = await _context.ItemsVenta
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == item.Id);
        if (tracked is null)
            throw new KeyNotFoundException($"ItemVenta {item.Id} no encontrado para actualizar.");

        tracked.Cantidad       = item.Cantidad;
        tracked.PesoKg         = item.PesoKg;
        tracked.PrecioUnitario = item.PrecioUnitario;
        tracked.Subtotal       = item.Subtotal;

        await _context.SaveChangesAsync();
        return tracked;
    }

    /// <inheritdoc/>
    public async Task EliminarAsync(ItemVenta item)
    {
        _context.ItemsVenta.Remove(item);
        await _context.SaveChangesAsync();
    }
}


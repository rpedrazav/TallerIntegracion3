using Microsoft.EntityFrameworkCore;
using WarehouseInventoryService.Data;
using WarehouseInventoryService.Models;

namespace WarehouseInventoryService.Repositories;

public class StockRepository : IStockRepository
{
    private readonly WarehouseDbContext _context;

    public StockRepository(WarehouseDbContext context)
    {
        _context = context;
    }

    public async Task<Stock?> GetByProducto(Guid productoId, Guid sucursalId, Guid tenantId)
    {
        return await _context.Stocks
            .Where(s => s.ProductoId == productoId
                     && s.SucursalId == sucursalId
                     && s.TenantId == tenantId)
            .FirstOrDefaultAsync();
    }

    public async Task<Stock?> Descontar(Guid productoId, decimal cantidad, Guid tenantId, Guid sucursalId)
    {
        var stock = await GetByProducto(productoId, sucursalId, tenantId);
        if (stock is null || stock.CantidadActual < cantidad)
        {
            return null;
        }

        var lotes = await _context.Lotes
            .Where(l => l.ProductoId == productoId
                     && l.SucursalId == sucursalId
                     && l.TenantId == tenantId
                     && l.Cantidad > 0)
            .OrderBy(l => l.FechaVencimiento)
            .ThenBy(l => l.Id)
            .ToListAsync();

        if (lotes.Sum(l => l.Cantidad) < cantidad)
        {
            return null;
        }

        var restante = cantidad;
        foreach (var lote in lotes)
        {
            var descuento = Math.Min(lote.Cantidad, restante);
            lote.Cantidad -= descuento;
            restante -= descuento;

            if (restante == 0)
            {
                break;
            }
        }

        stock.CantidadActual -= cantidad;
        return stock;
    }
}

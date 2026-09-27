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
        if (stock is null)
        {
            return null;
        }

        stock.CantidadActual -= cantidad;

        await _context.SaveChangesAsync();
        return stock;
    }
}

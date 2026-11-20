using Microsoft.EntityFrameworkCore;
using WarehouseInventoryService.Data;
using WarehouseInventoryService.Models;

namespace WarehouseInventoryService.Services;

public sealed class LoteService
{
    private readonly WarehouseDbContext _db;

    public LoteService(WarehouseDbContext db)
    {
        _db = db;
    }

    public async Task<List<Lote>> ListarPorVencerAsync(
        Guid tenantId,
        Guid sucursalId,
        int dias,
        CancellationToken cancellationToken)
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var limite = hoy.AddDays(dias);
        return await _db.Lotes
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId
                     && l.SucursalId == sucursalId
                     && l.Cantidad > 0
                     && l.FechaVencimiento >= hoy
                     && l.FechaVencimiento <= limite)
            .OrderBy(l => l.FechaVencimiento)
            .ThenBy(l => l.Id)
            .ToListAsync(cancellationToken);
    }
}

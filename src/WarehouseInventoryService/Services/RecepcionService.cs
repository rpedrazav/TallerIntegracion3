using System.Data;
using Microsoft.EntityFrameworkCore;
using WarehouseInventoryService.Data;
using WarehouseInventoryService.Models;

namespace WarehouseInventoryService.Services;

public sealed class RecepcionService
{
    private readonly WarehouseDbContext _db;

    public RecepcionService(WarehouseDbContext db)
    {
        _db = db;
    }

    public async Task<Lote> RegistrarAsync(
        Guid tenantId,
        Guid productoId,
        Guid sucursalId,
        decimal cantidad,
        DateOnly fechaVencimiento,
        string? ubicacion,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var stock = await _db.Stocks.SingleOrDefaultAsync(s =>
            s.TenantId == tenantId && s.ProductoId == productoId && s.SucursalId == sucursalId,
            cancellationToken);

        if (stock is null)
        {
            stock = new Stock
            {
                TenantId = tenantId,
                ProductoId = productoId,
                SucursalId = sucursalId
            };
            _db.Stocks.Add(stock);
        }

        stock.CantidadActual += cantidad;
        var lote = new Lote
        {
            TenantId = tenantId,
            ProductoId = productoId,
            SucursalId = sucursalId,
            Cantidad = cantidad,
            FechaVencimiento = fechaVencimiento,
            Ubicacion = ubicacion?.Trim() ?? string.Empty
        };
        _db.Lotes.Add(lote);
        _db.Movimientos.Add(new MovimientoStock
        {
            TenantId = tenantId,
            ProductoId = productoId,
            Tipo = "RECEPCION",
            Cantidad = cantidad,
            Motivo = $"Lote {lote.Id} recibido en sucursal {sucursalId}"
        });

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return lote;
    }
}

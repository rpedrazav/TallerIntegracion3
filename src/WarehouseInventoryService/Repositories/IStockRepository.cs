using WarehouseInventoryService.Models;

namespace WarehouseInventoryService.Repositories;

/// <summary>
/// Repositorio de acceso a datos para Stock (MS-4).
/// </summary>
public interface IStockRepository
{
    /// <summary>
    /// Obtiene el stock de un producto en una sucursal dentro de un tenant.
    /// </summary>
    Task<Stock?> GetByProducto(Guid productoId, Guid sucursalId, Guid tenantId);

    /// <summary>
    /// Prepara el descuento FEFO de lotes y stock agregado para que el consumidor persista el evento completo.
    /// Retorna null si el stock o los lotes disponibles son insuficientes.
    /// </summary>
    Task<Stock?> Descontar(Guid productoId, decimal cantidad, Guid tenantId, Guid sucursalId);
}

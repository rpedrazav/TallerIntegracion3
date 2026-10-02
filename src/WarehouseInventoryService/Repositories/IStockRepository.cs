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
    /// Descuenta una cantidad del stock de un producto en una sucursal dentro de un tenant.
    /// Retorna null si no existe stock para esa combinación producto/sucursal/tenant.
    /// </summary>
    Task<Stock?> Descontar(Guid productoId, decimal cantidad, Guid tenantId, Guid sucursalId);

    /// <summary>
    /// TI3-254: Crea un registro de stock con cantidad_actual = 0 y luego descuenta.
    /// Se usa cuando el consumer recibe un producto que no tiene registro en la tabla Stock.
    /// El stock quedará negativo, señalando una discrepancia a resolver por reposición.
    /// </summary>
    Task<Stock> CrearYDescontar(Guid productoId, decimal cantidad, Guid tenantId, Guid sucursalId);
}

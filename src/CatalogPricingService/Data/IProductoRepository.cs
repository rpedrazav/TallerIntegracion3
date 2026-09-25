using CatalogPricingService.Models;

namespace CatalogPricingService.Data
{
    public interface IProductoRepository
    {
        Task<(IEnumerable<Producto> Productos, int TotalCount)> GetAllByTenantAsync(Guid tenantId, int page, int pageSize);
        Task<(IEnumerable<Producto> Productos, int TotalCount)> SearchByNameAsync(Guid tenantId, string query, int page, int pageSize);
        Task<Producto?> GetByIdAsync(Guid id, Guid tenantId);
        Task<Producto> CreateAsync(Producto producto);
        Task UpdateAsync(Producto producto);
    }
}
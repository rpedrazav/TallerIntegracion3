using CatalogPricingService.Models;

namespace CatalogPricingService.Services
{
    public interface IProductoService
    {
        Task<Producto?> GetProductoByIdAsync(Guid id, Guid tenantId);
        Task<Producto> CreateProductoAsync(Producto producto);
        Task<Producto> UpdateProductoAsync(Guid id, Producto productoActualizado, Guid userTenantId);
        Task<(IEnumerable<Producto> Productos, int TotalCount)> GetAllProductosAsync(Guid tenantId, int page, int pageSize);
    }
}

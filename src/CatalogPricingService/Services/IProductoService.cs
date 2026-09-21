using CatalogPricingService.Models;

namespace CatalogPricingService.Services
{
    public interface IProductoService
    {
        Task<Producto> UpdateProductoAsync(Guid id, Producto productoActualizado, Guid userTenantId);
    }
}

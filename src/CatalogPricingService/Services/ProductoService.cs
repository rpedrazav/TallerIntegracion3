using CatalogPricingService.Data;
using CatalogPricingService.Models;

namespace CatalogPricingService.Services
{
    public class ProductoService : IProductoService
    {
        private readonly IProductoRepository _repository;

        public ProductoService(IProductoRepository repository)
        {
            _repository = repository;
        }

        public async Task<Producto?> GetProductoByIdAsync(Guid id, Guid tenantId)
        {
            return await _repository.GetByIdAsync(id, tenantId);
        }

        public async Task<Producto> CreateProductoAsync(Producto producto)
        {
            return await _repository.CreateAsync(producto);
        }

        public async Task<Producto> UpdateProductoAsync(Guid id, Producto productoActualizado, Guid userTenantId)
        {
            var productoExistente = await _repository.GetByIdAsync(id, userTenantId);
            
            if (productoExistente == null)
                throw new UnauthorizedAccessException("Acceso denegado: El producto no existe o pertenece a otro Minimarket.");

            productoExistente.Nombre = productoActualizado.Nombre;
            productoExistente.Descripcion = productoActualizado.Descripcion;
            productoExistente.PrecioBase = productoActualizado.PrecioBase;
            productoExistente.UomBaseId = productoActualizado.UomBaseId;
            productoExistente.CategoriaId = productoActualizado.CategoriaId;
            productoExistente.IsActive = productoActualizado.IsActive;
            productoExistente.EsPesoVariable = productoActualizado.EsPesoVariable;

            await _repository.UpdateAsync(productoExistente);
            return productoExistente;
        }

        public async Task<(IEnumerable<Producto> Productos, int TotalCount)> GetAllProductosAsync(Guid tenantId, int page, int pageSize)
        {
            return await _repository.GetAllByTenantAsync(tenantId, page, pageSize);
        }
    }
}

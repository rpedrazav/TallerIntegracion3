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
            return await _repository.GetActiveByIdAsync(id, tenantId);
        }

        public async Task<Producto?> GetProductoByBarcodeAsync(string barcode, Guid tenantId)
        {
            return await _repository.GetByBarcodeAsync(barcode, tenantId);
        }

        public async Task<Producto> CreateProductoAsync(Producto producto)
        {
            return await _repository.CreateAsync(producto);
        }

        public Task<bool> DesactivarProductoAsync(Guid id, Guid tenantId)
            => _repository.DeactivateAsync(id, tenantId);

        public async Task<Producto> UpdateProductoAsync(Guid id, Producto productoActualizado, Guid userTenantId, bool puedeCambiarEstado = false)
        {
            var productoExistente = await _repository.GetByIdAsync(id, userTenantId);
            
            if (productoExistente == null)
                throw new KeyNotFoundException("Producto no encontrado.");

            if (productoExistente.IsActive != productoActualizado.IsActive && !puedeCambiarEstado)
                throw new UnauthorizedAccessException("Solo ADMIN puede cambiar el estado del producto.");

            productoExistente.Nombre = productoActualizado.Nombre;
            productoExistente.Descripcion = productoActualizado.Descripcion;
            productoExistente.PrecioBase = productoActualizado.PrecioBase;
            productoExistente.UomBaseId = productoActualizado.UomBaseId;
            productoExistente.CategoriaId = productoActualizado.CategoriaId;
            productoExistente.IsActive = productoActualizado.IsActive;
            productoExistente.EsPesoVariable = productoActualizado.EsPesoVariable;

            await _repository.UpdateAsync(productoExistente, puedeCambiarEstado);
            return productoExistente;
        }

        public async Task<(IEnumerable<Producto> Productos, int TotalCount)> GetAllProductosAsync(Guid tenantId, int page, int pageSize)
        {
            return await _repository.GetAllByTenantAsync(tenantId, page, pageSize);
        }

        public async Task<(IEnumerable<Producto> Productos, int TotalCount)> SearchProductosAsync(Guid tenantId, string query, int page, int pageSize)
        {
            return await _repository.SearchByNameAsync(tenantId, query, page, pageSize);
        }
    }
}


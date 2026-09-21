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

        public async Task<Producto> UpdateProductoAsync(Guid id, Producto productoActualizado, Guid userTenantId)
        {
            // RN-01: Validar que el producto existe Y pertenece al Tenant del usuario que hace la petición
            var productoExistente = await _repository.GetByIdAsync(id, userTenantId);
            
            if (productoExistente == null)
            {
                throw new UnauthorizedAccessException("Acceso denegado: El producto no existe o pertenece a otro Minimarket.");
            }

            // Actualizar solo los campos mutables permitidos
            productoExistente.Nombre = productoActualizado.Nombre;
            productoExistente.Descripcion = productoActualizado.Descripcion;
            productoExistente.PrecioBase = productoActualizado.PrecioBase;
            productoExistente.UomBaseId = productoActualizado.UomBaseId;
            productoExistente.CategoriaId = productoActualizado.CategoriaId;
            productoExistente.IsActive = productoActualizado.IsActive;
            productoExistente.EsPesoVariable = productoActualizado.EsPesoVariable;

            // Guardar en base de datos a través del repositorio
            await _repository.UpdateAsync(productoExistente);

            return productoExistente;
        }
    }
}

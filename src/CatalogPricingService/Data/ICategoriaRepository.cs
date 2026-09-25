using CatalogPricingService.Models;

namespace CatalogPricingService.Data
{
    public interface ICategoriaRepository
    {
        /// <summary>
        /// Retorna todas las categorias activas del tenant indicado.
        /// El aislamiento multi-tenant se garantiza filtrando siempre por tenantId.
        /// </summary>
        Task<IEnumerable<Categoria>> GetAllAsync(Guid tenantId);

        /// <summary>
        /// Crea una nueva categoria para el tenant indicado y persiste el cambio.
        /// </summary>
        Task<Categoria> CreateAsync(Categoria categoria);
    }
}

using CatalogPricingService.DTOs;

namespace CatalogPricingService.Services;

public interface ICategoriaService
{
    /// <summary>
    /// Retorna todas las categorias activas del tenant indicado.
    /// </summary>
    Task<IEnumerable<CategoriaResponseDto>> GetAllCategoriasAsync(Guid tenantId);

    /// <summary>
    /// Crea una nueva categoria para el tenant indicado.
    /// Si se especifica ParentId, calcula automaticamente el nivel jerarquico.
    /// </summary>
    Task<CategoriaResponseDto> CreateCategoriaAsync(CreateCategoriaDto dto);
}

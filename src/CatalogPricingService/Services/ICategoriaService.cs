using CatalogPricingService.DTOs;

namespace CatalogPricingService.Services;

public interface ICategoriaService
{
    /// <summary>
    /// Crea una nueva categoria para el tenant indicado.
    /// Si se especifica ParentId, calcula automaticamente el nivel jerarquico.
    /// </summary>
    Task<CategoriaResponseDto> CreateCategoriaAsync(CreateCategoriaDto dto);
}

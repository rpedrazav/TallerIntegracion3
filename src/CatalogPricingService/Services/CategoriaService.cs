using CatalogPricingService.Data;
using CatalogPricingService.DTOs;
using CatalogPricingService.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogPricingService.Services;

public class CategoriaService : ICategoriaService
{
    private readonly ICategoriaRepository _repository;
    private readonly CatalogDbContext _context;

    public CategoriaService(ICategoriaRepository repository, CatalogDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    /// <inheritdoc />
    public async Task<CategoriaResponseDto> CreateCategoriaAsync(CreateCategoriaDto dto)
    {
        // Calcular el nivel jerarquico en base al padre
        int level = 0;
        if (dto.ParentId.HasValue)
        {
            var parent = await _context.Categorias
                .FirstOrDefaultAsync(c => c.Id == dto.ParentId.Value && c.TenantId == dto.TenantId);

            // El validador ya garantiza que el padre existe; esto es defensa adicional.
            level = parent != null ? parent.Level + 1 : 0;
        }

        var categoria = new Categoria
        {
            TenantId = dto.TenantId,
            Nombre    = dto.Nombre.Trim(),
            ParentId  = dto.ParentId,
            Level     = level,
            IsActive  = true,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repository.CreateAsync(categoria);

        return MapToResponse(created);
    }

    // --------------- helpers ---------------

    private static CategoriaResponseDto MapToResponse(Categoria c) => new()
    {
        Id        = c.Id,
        Nombre    = c.Nombre,
        ParentId  = c.ParentId,
        Level     = c.Level,
        IsActive  = c.IsActive,
        CreatedAt = c.CreatedAt
    };
}

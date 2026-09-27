using FluentValidation;
using Microsoft.EntityFrameworkCore;
using CatalogPricingService.DTOs;
using CatalogPricingService.Data;

namespace CatalogPricingService.Validators;

public class CreateCategoriaDtoValidator : AbstractValidator<CreateCategoriaDto>
{
    private readonly CatalogDbContext _context;

    public CreateCategoriaDtoValidator(CatalogDbContext context)
    {
        _context = context;

        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la categoria es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede exceder los 100 caracteres.")
            // Unicidad de nombre por tenant (no puede haber dos categorias con el mismo nombre en un tenant)
            .MustAsync(async (dto, nombre, cancellation) =>
            {
                bool exists = await _context.Categorias
                    .AnyAsync(c => c.TenantId == dto.TenantId && c.Nombre == nombre, cancellation);
                return !exists;
            }).WithMessage("Ya existe una categoria con ese nombre en su catalogo.");

        // Si se indica un ParentId, debe existir y pertenecer al mismo tenant
        RuleFor(x => x.ParentId)
            .MustAsync(async (dto, parentId, cancellation) =>
            {
                if (parentId == null) return true; // raiz, siempre valido

                bool exists = await _context.Categorias
                    .AnyAsync(c => c.Id == parentId && c.TenantId == dto.TenantId, cancellation);
                return exists;
            }).WithMessage("La categoria padre no existe o no pertenece a su catalogo.");
    }
}

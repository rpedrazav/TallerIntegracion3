using FluentValidation;
using Microsoft.EntityFrameworkCore;
using CatalogPricingService.DTOs;
using CatalogPricingService.Data;
using System.Threading;
using System.Threading.Tasks;

namespace CatalogPricingService.Validators
{
    public class CreateProductoDtoValidator : AbstractValidator<CreateProductoDto>
    {
        private readonly CatalogDbContext _context;

        public CreateProductoDtoValidator(CatalogDbContext context)
        {
            _context = context;

            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("El nombre es obligatorio.")
                .MaximumLength(200).WithMessage("El nombre no puede exceder los 200 caracteres.");

            RuleFor(x => x.PrecioBase)
                .GreaterThan(0).WithMessage("El precio base debe ser mayor a 0.");

            RuleFor(x => x.CodigoBarras)
                .MaximumLength(50).WithMessage("El código de barras no puede exceder los 50 caracteres.")
                .MustAsync(async (dto, codigoBarras, cancellation) => 
                {
                    if (string.IsNullOrEmpty(codigoBarras)) return true; // Es opcional
                    
                    // Unicidad estricta por tenant (Aislamiento de datos)
                    bool exists = await _context.Productos
                        .AnyAsync(p => p.TenantId == dto.TenantId && p.CodigoBarras == codigoBarras, cancellation);
                    
                    return !exists;
                }).WithMessage("El código de barras ya existe en su catálogo.");
        }
    }
}

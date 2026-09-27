using FluentValidation;
using POSCartService.DTOs;

namespace POSCartService.Validators;

/// <summary>
/// Validador de FluentValidation para el request POST /ventas/{id}/items.
/// </summary>
public sealed class AgregarItemRequestValidator : AbstractValidator<AgregarItemRequest>
{
    public AgregarItemRequestValidator()
    {
        RuleFor(x => x.ProductoId)
            .NotEmpty()
            .WithMessage("producto_id es obligatorio.");

        RuleFor(x => x.Cantidad)
            .GreaterThan(0)
            .When(x => !x.PesoKg.HasValue)
            .WithMessage("cantidad debe ser mayor a 0 cuando no se especifica peso_kg.");

        RuleFor(x => x.PesoKg)
            .GreaterThan(0)
            .When(x => x.PesoKg.HasValue)
            .WithMessage("peso_kg debe ser mayor a 0 si se especifica.");
    }
}

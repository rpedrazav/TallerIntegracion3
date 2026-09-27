using FluentValidation;
using TaxComplianceService.Services;

namespace TaxComplianceService.Models;

/// <summary>
/// Validador FluentValidation para items individuales de cálculo fiscal.
/// Valida que precio > 0 y cantidad > 0.
/// </summary>
public class TaxItemValidator : AbstractValidator<TaxItem>
{
    public TaxItemValidator()
    {
        RuleFor(i => i.Precio)
            .GreaterThan(0)
            .WithMessage("El precio debe ser mayor a 0.");

        RuleFor(i => i.Cantidad)
            .GreaterThan(0)
            .WithMessage("La cantidad debe ser mayor a 0.");
    }
}

/// <summary>
/// Validador FluentValidation para TaxCalculateRequest.
/// Reglas de negocio:
/// 1. La lista de items no puede estar vacía ni ser nula.
/// 2. Cada item debe tener precio > 0.
/// 3. Cada item debe tener cantidad > 0.
/// </summary>
public class TaxCalculateRequestValidator : AbstractValidator<TaxCalculateRequest>
{
    public TaxCalculateRequestValidator()
    {
        RuleFor(x => x.Items)
            .NotNull()
            .WithMessage("La lista de items no puede ser nula.")
            .NotEmpty()
            .WithMessage("La lista de items no puede estar vacía.");

        RuleForEach(x => x.Items)
            .NotNull()
            .WithMessage("El item no puede ser nulo.")
            .SetValidator(new TaxItemValidator());

        When(x => x.PorcentajeIva.HasValue, () =>
        {
            RuleFor(x => x.PorcentajeIva!.Value)
                .GreaterThanOrEqualTo(0)
                .WithMessage("El porcentaje de IVA no puede ser negativo.");
        });
    }
}

/// <summary>
/// Alias alternativo para el validador de solicitud de cálculo fiscal.
/// </summary>
public class TaxCalculateValidator : TaxCalculateRequestValidator { }

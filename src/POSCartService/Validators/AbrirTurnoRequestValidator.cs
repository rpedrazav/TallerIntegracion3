using FluentValidation;
using POSCartService.DTOs;

namespace POSCartService.Validators;

public sealed class AbrirTurnoRequestValidator : AbstractValidator<AbrirTurnoRequest>
{
    public AbrirTurnoRequestValidator()
    {
        RuleFor(request => request.MontoFondoInicial)
            .GreaterThanOrEqualTo(0)
            .WithMessage("El monto de fondo inicial debe ser mayor o igual a 0.");
    }
}

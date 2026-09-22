using FluentValidation;
using TenantIdentityService.DTOs;

namespace TenantIdentityService.Validators;

/// <summary>
/// Valida el request POST /auth/login antes de que llegue al AuthService.
/// Si la validación falla, el controller retorna 400 Bad Request con los errores detallados.
/// Esto evita llamadas innecesarias a la base de datos con datos claramente inválidos.
/// </summary>
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
                .WithMessage("El email es obligatorio.")
            .EmailAddress()
                .WithMessage("El email no tiene un formato válido.")
            .MaximumLength(200)
                .WithMessage("El email no puede superar los 200 caracteres.");

        RuleFor(x => x.Password)
            .NotEmpty()
                .WithMessage("La contraseña es obligatoria.")
            .MinimumLength(6)
                .WithMessage("La contraseña debe tener al menos 6 caracteres.")
            .MaximumLength(100)
                .WithMessage("La contraseña no puede superar los 100 caracteres.");

        RuleFor(x => x.TenantId)
            .NotEmpty()
                .WithMessage("El identificador del tenant es obligatorio.");
    }
}

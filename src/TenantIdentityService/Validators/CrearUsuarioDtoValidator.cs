using FluentValidation;
using TenantIdentityService.DTOs;

namespace TenantIdentityService.Validators;

/// <summary>
/// Valida el body de POST /api/v1/users antes de tocar la base de datos.
/// Si la validación falla, el controller retorna 400 Bad Request con los errores detallados.
///
/// Política de contraseña fuerte (RN-07):
/// - Mínimo 8 caracteres, máximo 100.
/// - Al menos una letra mayúscula.
/// - Al menos una letra minúscula.
/// - Al menos un dígito (0-9).
/// - Al menos un carácter especial (!@#$%^&amp;*()-_=+[]{}|;':",./&lt;&gt;?).
///
/// El proyecto no usa AddFluentValidationAutoValidation(), por lo que
/// el controller invoca ValidateAsync explícitamente, igual que AuthController
/// y SucursalesController.
/// </summary>
public class CrearUsuarioDtoValidator : AbstractValidator<CrearUsuarioDto>
{
    // Conjunto de caracteres especiales aceptados en la contraseña.
    private const string CaracteresEspeciales = @"!@#$%^&*()-_=+[]{}|;':"",./<>?\\";

    public CrearUsuarioDtoValidator()
    {
        // ── Nombre ──────────────────────────────────────────────────────────
        RuleFor(x => x.Nombre)
            .NotEmpty()
                .WithMessage("El nombre es obligatorio.")
            .MinimumLength(2)
                .WithMessage("El nombre debe tener al menos 2 caracteres.")
            .MaximumLength(100)
                .WithMessage("El nombre no puede superar los 100 caracteres.")
            .Matches(@"^[\p{L}\p{M}'\-\s]+$")
                .WithMessage("El nombre solo puede contener letras, espacios, apóstrofes y guiones.");

        // ── Email ────────────────────────────────────────────────────────────
        RuleFor(x => x.Email)
            .NotEmpty()
                .WithMessage("El email es obligatorio.")
            .EmailAddress()
                .WithMessage("El email no tiene un formato válido.")
            .MaximumLength(200)
                .WithMessage("El email no puede superar los 200 caracteres.");

        // ── Password ─────────────────────────────────────────────────────────
        RuleFor(x => x.Password)
            .NotEmpty()
                .WithMessage("La contraseña es obligatoria.")
            .MinimumLength(8)
                .WithMessage("La contraseña debe tener al menos 8 caracteres.")
            .MaximumLength(100)
                .WithMessage("La contraseña no puede superar los 100 caracteres.")
            .Must(p => p.Any(char.IsUpper))
                .WithMessage("La contraseña debe contener al menos una letra mayúscula.")
            .Must(p => p.Any(char.IsLower))
                .WithMessage("La contraseña debe contener al menos una letra minúscula.")
            .Must(p => p.Any(char.IsDigit))
                .WithMessage("La contraseña debe contener al menos un dígito.")
            .Must(p => p.Any(c => CaracteresEspeciales.Contains(c)))
                .WithMessage(
                    "La contraseña debe contener al menos un carácter especial " +
                    $"({CaracteresEspeciales.Replace("\"", "\\\"")}).");
    }
}

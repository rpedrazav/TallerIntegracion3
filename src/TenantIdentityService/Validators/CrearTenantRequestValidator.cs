using FluentValidation;
using TenantIdentityService.DTOs;

namespace TenantIdentityService.Validators;

/// <summary>Valida el alta de un minimarket (POST /api/tenants y --bootstrap-admin).</summary>
public class CrearTenantRequestValidator : AbstractValidator<CrearTenantRequest>
{
    public CrearTenantRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty()
                .WithMessage("El nombre del minimarket es obligatorio.")
            .MaximumLength(200)
                .WithMessage("El nombre no puede superar los 200 caracteres.");

        RuleFor(x => x.Codigo)
            .Matches("^[a-zA-Z0-9-]{3,50}$")
                .WithMessage("El código debe tener entre 3 y 50 letras, números o guiones, sin espacios.");

        RuleFor(x => x.AdminEmail)
            .NotEmpty()
                .WithMessage("El email del administrador es obligatorio.")
            .EmailAddress()
                .WithMessage("El email del administrador no tiene un formato válido.")
            .MaximumLength(200)
                .WithMessage("El email no puede superar los 200 caracteres.");

        RuleFor(x => x.AdminPassword).ContrasenaSegura();
    }
}

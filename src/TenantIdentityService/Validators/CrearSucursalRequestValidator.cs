using FluentValidation;
using TenantIdentityService.DTOs;

namespace TenantIdentityService.Validators;

/// <summary>
/// Valida el request de POST /sucursales antes de tocar la base de datos.
/// Si falla, el controller responde 400 con ValidationProblemDetails.
/// MS-1 no usa AddFluentValidationAutoValidation(), por eso el controller
/// invoca explícitamente ValidateAsync, igual que en AuthController.
/// </summary>
public class CrearSucursalRequestValidator : AbstractValidator<CrearSucursalRequest>
{
    public CrearSucursalRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty()
                .WithMessage("El nombre de la sucursal es obligatorio.")
            .MaximumLength(200)
                .WithMessage("El nombre no puede superar los 200 caracteres.");

        RuleFor(x => x.Direccion)
            .NotEmpty()
                .WithMessage("La dirección de la sucursal es obligatoria.");

        // Zona horaria opcional, pero si viene informada debe ser una zona IANA real.
        // Vacío o null significa "heredar la del tenant".
        RuleFor(x => x.ZonaHoraria)
            .Must(EsZonaHorariaIanaValida)
                .WithMessage(
                    "La zona horaria debe ser un identificador IANA válido " +
                    "(ej: \"America/Santiago\", \"America/Argentina/Buenos_Aires\"). " +
                    "Envíela vacía para heredar la zona horaria del tenant.")
            .When(x => !string.IsNullOrWhiteSpace(x.ZonaHoraria));
    }

    /// <summary>
    /// Áreas válidas del identificador IANA. "UTC" es el único identificador IANA
    /// válido que no lleva barra.
    /// </summary>
    private static readonly string[] PrefijosIana =
    {
        "Africa", "America", "Antarctica", "Arctic", "Asia",
        "Atlantic", "Australia", "Europe", "Indian", "Pacific", "Etc"
    };

    /// <summary>
    /// Verifica que el identificador sea una zona horaria IANA real.
    ///
    /// No basta con <c>TimeZoneInfo.FindSystemTimeZoneById</c>: en .NET 8 con ICU esa llamada
    /// también resuelve identificadores de Windows ("Chile/Continental", "SA Pacific Standard Time"),
    /// que no son IANA. Por eso primero se exige el prefijo de área IANA, que descarta los ids de
    /// Windows, y después se confirma que la zona exista en la base del runtime.
    /// </summary>
    private static bool EsZonaHorariaIanaValida(string? zona)
    {
        if (string.IsNullOrWhiteSpace(zona))
            return true; // ausente o vacía = hereda la del tenant

        var id = zona.Trim();

        // Prefijo: rechaza ids de Windows y cualquier cadena sin forma de zona IANA.
        // La comparación es ordinal porque la base de zonas IANA distingue mayúsculas.
        var tienePrefijoIana = PrefijosIana.Any(p => id.StartsWith(p + "/", StringComparison.Ordinal));
        if (!tienePrefijoIana && !id.Equals("UTC", StringComparison.Ordinal))
            return false;

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(id);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }
}

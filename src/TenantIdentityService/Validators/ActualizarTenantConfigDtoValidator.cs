using FluentValidation;
using TenantIdentityService.DTOs;

namespace TenantIdentityService.Validators;

/// <summary>
/// TI3-459: Validador de FluentValidation para ActualizarTenantConfigDto.
/// Valida los campos Moneda, Idioma, ZonaHoraria y PorcentajeIva.
/// </summary>
public class ActualizarTenantConfigDtoValidator : AbstractValidator<ActualizarTenantConfigDto>
{
    /// <summary>
    /// Monedas ISO 4217 soportadas por el sistema.
    /// </summary>
    private static readonly string[] MonedasValidas = { "CLP", "ARS", "USD", "EUR", "PEN", "COP", "MXN", "BRL", "UYU" };

    /// <summary>
    /// Códigos de idioma soportados.
    /// </summary>
    private static readonly string[] IdiomasValidos = { "es", "en", "pt" };

    /// <summary>
    /// Códigos ISO 3166-1 alpha-2 de países soportados.
    /// </summary>
    private static readonly string[] PaisesValidos = { "CL", "AR", "US", "PE", "CO", "MX", "BR", "UY", "EC", "PY", "BO", "VE" };

    public ActualizarTenantConfigDtoValidator()
    {
        RuleFor(x => x.Pais)
            .NotEmpty().WithMessage("El país es obligatorio.")
            .Length(2).WithMessage("El código de país debe tener exactamente 2 caracteres (ISO 3166-1 alpha-2).")
            .Must(p => PaisesValidos.Contains(p.ToUpperInvariant()))
            .WithMessage($"País no soportado. Valores válidos: {string.Join(", ", PaisesValidos)}.");

        RuleFor(x => x.Moneda)
            .NotEmpty().WithMessage("La moneda es obligatoria.")
            .Length(3).WithMessage("El código de moneda debe tener exactamente 3 caracteres (ISO 4217).")
            .Must(m => MonedasValidas.Contains(m.ToUpperInvariant()))
            .WithMessage($"Moneda no soportada. Valores válidos: {string.Join(", ", MonedasValidas)}.");

        RuleFor(x => x.Idioma)
            .NotEmpty().WithMessage("El idioma es obligatorio.")
            .MaximumLength(5).WithMessage("El código de idioma no puede exceder 5 caracteres.")
            .Must(i => IdiomasValidos.Contains(i.ToLowerInvariant()))
            .WithMessage($"Idioma no soportado. Valores válidos: {string.Join(", ", IdiomasValidos)}.");

        RuleFor(x => x.PorcentajeIva)
            .GreaterThanOrEqualTo(0).WithMessage("El porcentaje de IVA no puede ser negativo.")
            .LessThanOrEqualTo(50).WithMessage("El porcentaje de IVA no puede superar el 50%.");

        RuleFor(x => x.ZonaHoraria)
            .NotEmpty().WithMessage("La zona horaria es obligatoria.")
            .Must(BeValidIanaTimezone)
            .WithMessage("La zona horaria debe ser un identificador IANA válido (ej: America/Santiago).");
    }

    /// <summary>
    /// Valida que la zona horaria sea un identificador IANA reconocido por el sistema.
    /// </summary>
    private static bool BeValidIanaTimezone(string? tz)
    {
        if (string.IsNullOrWhiteSpace(tz)) return false;
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(tz);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
    }
}

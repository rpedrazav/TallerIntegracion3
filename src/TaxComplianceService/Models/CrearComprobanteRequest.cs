using FluentValidation;
using System.Text.Json.Serialization;
using TaxComplianceService.Services;

namespace TaxComplianceService.Models;

/// <summary>
/// Solicitud para emitir un comprobante de venta en MS-2.
/// Los importes (subtotal, IVA y total) NO se reciben del cliente: se calculan en el servidor
/// a partir de <see cref="Items"/> y de la configuración fiscal del tenant en MS-1.
/// </summary>
public class CrearComprobanteRequest
{
    /// <summary>
    /// Identificador de la venta en MS-5 (POSCartService) que originó este comprobante.
    /// Se representa por Guid porque no hay navigation property entre microservicios.
    /// </summary>
    [JsonPropertyName("venta_id")]
    public Guid VentaId { get; set; }

    /// <summary>
    /// Detalle de items vendidos. No puede estar vacío.
    /// </summary>
    [JsonPropertyName("items")]
    public List<TaxItem> Items { get; set; } = new();
}

/// <summary>
/// Validador FluentValidation para <see cref="CrearComprobanteRequest"/>.
/// Reglas: venta_id obligatorio, items no vacíos, cada item con precio y cantidad mayores a 0.
/// </summary>
public class CrearComprobanteRequestValidator : AbstractValidator<CrearComprobanteRequest>
{
    public CrearComprobanteRequestValidator()
    {
        RuleFor(x => x.VentaId)
            .NotEmpty()
            .WithMessage("El venta_id es obligatorio.");

        RuleFor(x => x.Items)
            .NotNull()
            .WithMessage("La lista de items no puede ser nula.")
            .NotEmpty()
            .WithMessage("La lista de items no puede estar vacía.");

        RuleForEach(x => x.Items)
            .NotNull()
            .WithMessage("El item no puede ser nulo.")
            .SetValidator(new TaxItemValidator());
    }
}

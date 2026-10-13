using FluentValidation;
using POSCartService.DTOs;

namespace POSCartService.Validators;

public sealed class CrearVentaRequestValidator : AbstractValidator<CrearVentaRequest>
{
    private static readonly HashSet<string> MetodosPagoValidos =
        new(StringComparer.OrdinalIgnoreCase) { "EFECTIVO", "TARJETA", "MIXTO" };

    public CrearVentaRequestValidator()
    {
        RuleFor(r => r.Items)
            .NotEmpty()
            .WithMessage("La venta debe contener al menos un ítem.");

        RuleForEach(r => r.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductoId)
                .NotEmpty()
                .WithMessage("Cada ítem debe tener un producto_id válido.");

            item.RuleFor(i => i.NombreProducto)
                .MaximumLength(200)
                .When(i => !string.IsNullOrEmpty(i.NombreProducto))
                .WithMessage("El nombre del producto no puede superar 200 caracteres si se proporciona.");

            item.RuleFor(i => i.Cantidad)
                .GreaterThan(0)
                .When(i => !i.PesoKg.HasValue || i.PesoKg.Value <= 0)
                .WithMessage("La cantidad debe ser mayor a 0 cuando no se especifica peso_kg.");

            item.RuleFor(i => i.PesoKg)
                .GreaterThan(0)
                .When(i => i.PesoKg.HasValue)
                .WithMessage("El peso en kg debe ser mayor a 0 si se especifica.");

            item.RuleFor(i => i.PrecioUnitario)
                .GreaterThanOrEqualTo(0)
                .WithMessage("El precio unitario no puede ser negativo.");
        });

        RuleFor(r => r.MetodoPago)
            .NotEmpty()
            .Must(m => MetodosPagoValidos.Contains(m))
            .WithMessage("metodo_pago debe ser EFECTIVO, TARJETA o MIXTO.");
    }
}

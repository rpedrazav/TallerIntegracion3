namespace TaxComplianceService.Services;

/// <summary>
/// Servicio para cálculo de IVA e impuestos sobre ventas.
/// Implementa la lógica de cálculo fiscal según el caso de uso TC-01 de MS-2.
/// Maneja correctamente IVA = 0 (productos exentos: subtotal = total, iva = 0),
/// IVA = 19% (Chile), IVA = 21% (Argentina) y canastas mixtas con productos exentos.
/// </summary>
public class TaxCalculatorService : ITaxCalculatorService
{
    /// <summary>
    /// Calcula subtotal, iva y total para una colección de items dado un porcentaje de IVA general.
    /// Para productos exentos o tasa general 0%: subtotal = total, iva = 0.
    /// Para productos gravados (ej: 19% Chile o 21% Argentina): iva = subtotal * (porcentaje / 100).
    /// </summary>
    /// <param name="items">Colección de items con precio, cantidad y estado de exención opcional.</param>
    /// <param name="porcentajeIva">Porcentaje de IVA a aplicar (ej: 0 para exento, 19 para Chile, 21 para Argentina).</param>
    /// <returns>TaxBreakdown con subtotal, iva, total y el desglose detallado item por item.</returns>
    public static TaxBreakdown Calculate(IEnumerable<TaxItem> items, decimal porcentajeIva)
    {
        if (items == null)
        {
            throw new ArgumentNullException(nameof(items), "La colección de items no puede ser nula.");
        }

        if (porcentajeIva < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(porcentajeIva), "El porcentaje de IVA no puede ser negativo.");
        }

        var itemsList = items.ToList();
        var itemBreakdowns = new List<TaxItemBreakdown>();
        decimal subtotal = 0m;
        decimal totalIva = 0m;

        foreach (var item in itemsList)
        {
            if (item == null) continue;

            decimal itemSubtotal = item.Precio * item.Cantidad;
            bool esExento = item.Exento || item.EsExento || (item.PorcentajeIva.HasValue && item.PorcentajeIva.Value == 0) || porcentajeIva == 0;

            decimal tasaAplicada = esExento ? 0m : (item.PorcentajeIva ?? porcentajeIva);
            decimal itemIva = esExento ? 0m : itemSubtotal * (tasaAplicada / 100m);
            decimal itemTotal = itemSubtotal + itemIva;

            subtotal += itemSubtotal;
            totalIva += itemIva;

            itemBreakdowns.Add(new TaxItemBreakdown
            {
                Nombre = item.Nombre,
                Precio = item.Precio,
                Cantidad = item.Cantidad,
                Subtotal = itemSubtotal,
                PorcentajeIva = tasaAplicada,
                Iva = itemIva,
                Total = itemTotal,
                Exento = esExento
            });
        }

        decimal total = subtotal + totalIva;

        return new TaxBreakdown
        {
            Subtotal = subtotal,
            PorcentajeIva = porcentajeIva,
            Iva = totalIva,
            Total = total,
            Items = itemBreakdowns
        };
    }

    /// <summary>
    /// Implementación de ITaxCalculatorService para uso mediante inyección de dependencias.
    /// </summary>
    TaxBreakdown ITaxCalculatorService.Calculate(IEnumerable<TaxItem> items, decimal porcentajeIva)
    {
        return Calculate(items, porcentajeIva);
    }
}

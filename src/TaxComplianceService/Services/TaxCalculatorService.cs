namespace TaxComplianceService.Services;

/// <summary>
/// Servicio para cálculo de IVA e impuestos sobre ventas.
/// Implementa la lógica de cálculo fiscal según el caso de uso TC-01 de MS-2.
/// </summary>
public class TaxCalculatorService : ITaxCalculatorService
{
    /// <summary>
    /// Calcula subtotal = sum(precio * cantidad), iva = subtotal * (porcentajeIva / 100), total = subtotal + iva.
    /// Retorna el breakdown completo.
    /// Soporta invocación estática directa: TaxCalculatorService.Calculate(items, porcentajeIva)
    /// </summary>
    /// <param name="items">Colección de items con precio y cantidad.</param>
    /// <param name="porcentajeIva">Porcentaje de IVA a aplicar (ej: 19 para 19%).</param>
    /// <returns>TaxBreakdown con el subtotal, iva, total y el desglose item por item.</returns>
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

        foreach (var item in itemsList)
        {
            if (item == null) continue;

            decimal itemSubtotal = item.Precio * item.Cantidad;
            decimal itemIva = itemSubtotal * (porcentajeIva / 100m);
            decimal itemTotal = itemSubtotal + itemIva;

            subtotal += itemSubtotal;

            itemBreakdowns.Add(new TaxItemBreakdown
            {
                Nombre = item.Nombre,
                Precio = item.Precio,
                Cantidad = item.Cantidad,
                Subtotal = itemSubtotal,
                Iva = itemIva,
                Total = itemTotal
            });
        }

        decimal iva = subtotal * (porcentajeIva / 100m);
        decimal total = subtotal + iva;

        return new TaxBreakdown
        {
            Subtotal = subtotal,
            PorcentajeIva = porcentajeIva,
            Iva = iva,
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

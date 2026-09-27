namespace TaxComplianceService.Services;

/// <summary>
/// Contrato del servicio de cálculo fiscal para inyección de dependencias.
/// </summary>
public interface ITaxCalculatorService
{
    /// <summary>
    /// Calcula el subtotal, iva y total de una colección de items con el porcentaje de IVA dado.
    /// subtotal = sum(precio * cantidad)
    /// iva = subtotal * (porcentajeIva / 100)
    /// total = subtotal + iva
    /// </summary>
    /// <param name="items">Colección de items con precio y cantidad.</param>
    /// <param name="porcentajeIva">Porcentaje de IVA a aplicar (ej: 19 para 19%).</param>
    /// <returns>Breakdown completo del cálculo.</returns>
    TaxBreakdown Calculate(IEnumerable<TaxItem> items, decimal porcentajeIva);
}

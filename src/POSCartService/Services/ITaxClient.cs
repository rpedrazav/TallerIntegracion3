using POSCartService.DTOs;

namespace POSCartService.Services;

/// <summary>
/// Cliente para interactuar con MS-2 (Tax &amp; Compliance Service).
/// Permite calcular el desglose de IVA y totales para una lista de ítems.
/// </summary>
public interface ITaxClient
{
    /// <summary>
    /// Envía la lista de ítems a MS-2 para calcular el IVA y los subtotales/totales correspondientes.
    /// </summary>
    Task<TaxCalculationResult?> CalculateTaxAsync(
        IEnumerable<TaxItemDto> items,
        decimal? porcentajeIva = null,
        string? bearerToken = null,
        CancellationToken cancellationToken = default);
}

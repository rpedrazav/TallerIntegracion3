using TaxComplianceService.Services;

namespace TaxComplianceService.Models;

/// <summary>
/// Solicitud para el cálculo de impuestos e IVA sobre una colección de items.
/// Utilizado en el caso de uso TC-01 de MS-2 (TaxComplianceService).
/// </summary>
public class TaxCalculateRequest
{
    /// <summary>
    /// Colección de items/productos para el cálculo. No puede estar vacía.
    /// </summary>
    public List<TaxItem> Items { get; set; } = new();

    /// <summary>
    /// Porcentaje de IVA aplicable (ej: 19 para 19%).
    /// Opcional: si es nulo, el servicio lo puede resolver mediante la configuración fiscal del tenant (MS-1).
    /// </summary>
    public decimal? PorcentajeIva { get; set; }
}

/// <summary>
/// Alias auxiliar para representar un item dentro de una solicitud de cálculo.
/// </summary>
public class TaxItemRequest : TaxItem
{
    public TaxItemRequest() { }
    public TaxItemRequest(decimal precio, decimal cantidad, string? nombre = null) : base(precio, cantidad, nombre) { }
}

/// <summary>
/// Alias auxiliar alternativo para items de cálculo fiscal.
/// </summary>
public class TaxCalculateItemRequest : TaxItem
{
    public TaxCalculateItemRequest() { }
    public TaxCalculateItemRequest(decimal precio, decimal cantidad, string? nombre = null) : base(precio, cantidad, nombre) { }
}

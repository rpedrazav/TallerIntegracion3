namespace TaxComplianceService.Services;

/// <summary>
/// DTO que representa la respuesta del endpoint GET /tenants/{id}/config de MS-1.
/// Solo se mapean los campos necesarios para MS-2 (porcentaje_iva).
/// </summary>
public class TenantConfigResponse
{
    /// <summary>Identificador único del tenant.</summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Porcentaje de IVA configurado para el tenant (ej: 19 para Chile, 21 para Argentina).
    /// </summary>
    public decimal PorcentajeIva { get; set; }
}

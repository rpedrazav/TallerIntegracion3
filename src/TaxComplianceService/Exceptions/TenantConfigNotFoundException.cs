namespace TaxComplianceService.Exceptions;

/// <summary>
/// Se lanza cuando MS-1 responde correctamente pero no tiene configuración fiscal
/// registrada para el tenant solicitado.
/// </summary>
public class TenantConfigNotFoundException : Exception
{
    public TenantConfigNotFoundException(Guid tenantId)
        : base($"No se encontró la configuración fiscal para el tenant {tenantId}.")
    {
        TenantId = tenantId;
    }

    /// <summary>
    /// Tenant consultado que no tiene configuración fiscal.
    /// </summary>
    public Guid TenantId { get; }
}

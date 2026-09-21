namespace TaxComplianceService.Services;

/// <summary>
/// Contrato para el cliente HTTP que consulta la configuración fiscal de un tenant a MS-1.
/// </summary>
public interface ITenantConfigClient
{
    /// <summary>
    /// Obtiene el porcentaje de IVA configurado para el tenant dado consultando MS-1.
    /// </summary>
    /// <param name="tenantId">Identificador del tenant.</param>
    /// <param name="cancellationToken">Token de cancelación opcional.</param>
    /// <returns>
    /// La respuesta con el <c>PorcentajeIva</c> del tenant, o <c>null</c> si no se encuentra.
    /// </returns>
    Task<TenantConfigResponse?> GetTenantConfigAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

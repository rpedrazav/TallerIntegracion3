using TaxComplianceService.Models;

namespace TaxComplianceService.Services;

/// <summary>
/// Servicio de emisión de comprobantes de venta de MS-2.
/// Calcula los importes en el servidor y obtiene el correlativo desde la secuencia
/// por tenant en PostgreSQL.
/// </summary>
public interface IComprobanteService
{
    /// <summary>
    /// Emite un comprobante para la venta indicada: calcula subtotal, IVA y total con
    /// <see cref="ITaxCalculatorService"/>, obtiene el correlativo atómico del tenant y persiste el registro.
    /// </summary>
    /// <param name="request">Venta e items de la operación.</param>
    /// <param name="tenantId">Tenant extraído del JWT.</param>
    /// <param name="cajeroId">Cajero extraído del claim <c>sub</c> del JWT.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El comprobante persistido.</returns>
    Task<Comprobante> EmitirAsync(
        CrearComprobanteRequest request,
        Guid tenantId,
        Guid cajeroId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene un comprobante por su Id para reimpresión.
    /// La lectura pasa por el <c>HasQueryFilter</c> global de <c>TaxDbContext</c>, por lo que
    /// solo devuelve comprobantes del tenant del request. Si no existe o pertenece a otro tenant,
    /// devuelve <c>null</c> ( indistinguible a propósito, para no revelar datos de otros tenants).
    /// </summary>
    /// <param name="id">Identificador del comprobante.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El comprobante del tenant actual, o <c>null</c> si no existe.</returns>
    Task<Comprobante?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

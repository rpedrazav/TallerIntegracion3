using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TenantIdentityService.Data;

namespace TenantIdentityService.Controllers;

/// <summary>
/// Expone la configuración de un tenant para consumo interno entre microservicios.
/// Endpoint: GET /tenants/{id}/config
/// Consumidor principal: MS-2 TaxComplianceService (TenantConfigClient).
/// </summary>
[ApiController]
[Route("tenants")]
public class TenantConfigController : ControllerBase
{
    private readonly TenantDbContext _db;
    private readonly ILogger<TenantConfigController> _logger;

    public TenantConfigController(TenantDbContext db, ILogger<TenantConfigController> logger)
    {
        _db     = db     ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retorna la configuración fiscal del tenant solicitado.
    /// Usado internamente por MS-2 para obtener el <c>porcentaje_iva</c>.
    /// </summary>
    /// <param name="id">Identificador del tenant.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>
    /// 200 con <c>{ tenantId, porcentajeIva }</c> si existe,
    /// 404 si el tenant no existe o está inactivo.
    /// </returns>
    [HttpGet("{id:guid}/config")]
    [ProducesResponseType(typeof(TenantConfigDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConfig(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[TenantConfigController] Solicitando config de tenant {TenantId}", id);

        var tenant = await _db.Tenants
            .AsNoTracking()
            .Where(t => t.Id == id && t.Activo)
            .Select(t => new TenantConfigDto
            {
                TenantId      = t.Id,
                PorcentajeIva = t.PorcentajeIva
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (tenant is null)
        {
            _logger.LogWarning("[TenantConfigController] Tenant {TenantId} no encontrado o inactivo", id);
            return NotFound(new { message = $"Tenant {id} no encontrado o inactivo." });
        }

        return Ok(tenant);
    }
}

/// <summary>DTO de respuesta del endpoint GET /tenants/{id}/config.</summary>
public sealed record TenantConfigDto
{
    public Guid    TenantId      { get; init; }
    public decimal PorcentajeIva { get; init; }
}

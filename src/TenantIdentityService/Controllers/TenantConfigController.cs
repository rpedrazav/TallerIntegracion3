using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantIdentityService.DTOs;
using TenantIdentityService.Repositories;

namespace TenantIdentityService.Controllers;

/// <summary>
/// Expone la configuración de un tenant para consumo interno entre microservicios.
/// Endpoint: GET /tenants/{id}/config
/// Consumidor principal: MS-2 TaxComplianceService (TenantConfigClient).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/tenants")]
[Route("tenants")]
public class TenantConfigController : ControllerBase
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ILogger<TenantConfigController> _logger;

    public TenantConfigController(
        ITenantRepository tenantRepository,
        ILogger<TenantConfigController> logger)
    {
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
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
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConfig(Guid id)
    {
        var tenantClaim = User.FindFirst("tenant_id")?.Value;
        if (!Guid.TryParse(tenantClaim, out var tokenTenantId))
            return Unauthorized(new { message = "El token no contiene un tenant_id válido." });

        var isSuperAdmin = User.IsInRole("SUPER_ADMIN");
        if (!isSuperAdmin && tokenTenantId != id)
            return Forbid();

        _logger.LogInformation("Solicitando config de tenant {TenantId}", id);

        var tenant = await _tenantRepository.GetByIdAsync(id);

        if (tenant is null)
        {
            _logger.LogWarning("Tenant {TenantId} no encontrado o inactivo", id);
            return NotFound(new { message = $"Tenant {id} no encontrado o inactivo." });
        }

        return Ok(new TenantConfigDto
        {
            TenantId = tenant.Id,
            Pais = tenant.Pais,
            Moneda = tenant.Moneda,
            Idioma = tenant.Idioma,
            ZonaHoraria = tenant.ZonaHoraria,
            PorcentajeIva = tenant.PorcentajeIva,
            UmbralStockMinimo = tenant.UmbralStockMinimo,
            UmbralVariacionFx = tenant.UmbralVariacionFx,
            SeparacionFuncionesOc = tenant.SeparacionFuncionesOc
        });
    }
}

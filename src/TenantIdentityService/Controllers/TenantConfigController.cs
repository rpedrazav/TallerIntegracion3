using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantIdentityService.DTOs;
using TenantIdentityService.Repositories;

namespace TenantIdentityService.Controllers;

/// <summary>
/// Expone la configuraciÃ³n regional y fiscal del tenant autenticado.
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
    /// Retorna la configuraciÃ³n del tenant autenticado.
    /// </summary>
    /// <param name="id">Identificador del tenant.</param>
    /// <returns>Configuración del tenant.</returns>
    /// <response code="200">País, moneda, idioma, zona horaria y porcentaje de IVA.</response>
    /// <response code="401">Sin JWT válido.</response>
    /// <response code="404">Si el tenant no existe o está inactivo.</response>
    [HttpGet("{id:guid}/config")]
    [ProducesResponseType(typeof(TenantConfigDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConfig(Guid id)
    {
        var tenantClaim = User.FindFirst("tenant_id")?.Value;
        if (!Guid.TryParse(tenantClaim, out var tokenTenantId))
            return Unauthorized(new { message = "El token no contiene un tenant_id vÃ¡lido." });

        if (tokenTenantId != id)
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
            Pais = tenant.Pais,
            Moneda = tenant.Moneda,
            Idioma = tenant.Idioma,
            ZonaHoraria = tenant.ZonaHoraria,
            PorcentajeIva = tenant.PorcentajeIva
        });
    }

    [HttpPut("{id:guid}/config")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(TenantConfigDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateConfig(
        Guid id,
        [FromBody] ActualizarTenantConfigDto request)
    {
        var tenantClaim = User.FindFirst("tenant_id")?.Value;
        if (!Guid.TryParse(tenantClaim, out var tokenTenantId))
            return Unauthorized(new { message = "El token no contiene un tenant_id vÃ¡lido." });

        if (tokenTenantId != id)
            return Forbid();

        var tenant = await _tenantRepository.GetByIdAsync(id);
        if (tenant is null)
            return NotFound(new { message = $"Tenant {id} no encontrado o inactivo." });

        tenant.Pais = request.Pais;
        tenant.Moneda = request.Moneda;
        tenant.Idioma = request.Idioma;
        tenant.PorcentajeIva = request.PorcentajeIva;
        tenant.ZonaHoraria = request.ZonaHoraria;

        var tenantActualizado = await _tenantRepository.UpdateAsync(tenant);

        return Ok(new TenantConfigDto
        {
            Pais = tenantActualizado.Pais,
            Moneda = tenantActualizado.Moneda,
            Idioma = tenantActualizado.Idioma,
            ZonaHoraria = tenantActualizado.ZonaHoraria,
            PorcentajeIva = tenantActualizado.PorcentajeIva
        });
    }
}


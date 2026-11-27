using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantIdentityService.DTOs;
using TenantIdentityService.Exceptions;
using TenantIdentityService.Repositories;

namespace TenantIdentityService.Controllers;

/// <summary>
/// Minimarkets (tenants): alta y listado para SUPER_ADMIN (GET/POST /tenants) y
/// configuración regional y fiscal del tenant autenticado (GET/PUT /tenants/{id}/config).
/// Consumidor de la configuración: MS-2 TaxComplianceService (TenantConfigClient).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/tenants")]
[Route("api/tenants")]
[Route("tenants")]
public class TenantConfigController : ControllerBase
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IValidator<ActualizarTenantConfigDto> _validator;
    private readonly IValidator<CrearTenantRequest> _crearValidator;
    private readonly ILogger<TenantConfigController> _logger;

    public TenantConfigController(
        ITenantRepository tenantRepository,
        IValidator<ActualizarTenantConfigDto> validator,
        IValidator<CrearTenantRequest> crearValidator,
        ILogger<TenantConfigController> logger)
    {
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _crearValidator = crearValidator ?? throw new ArgumentNullException(nameof(crearValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>S3-094: lista todos los minimarkets. Solo SUPER_ADMIN.</summary>
    [HttpGet]
    [Authorize(Roles = "SUPER_ADMIN")]
    [ProducesResponseType(typeof(IEnumerable<TenantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Listar() =>
        Ok((await _tenantRepository.ListAsync()).Select(TenantDto.From));

    /// <summary>
    /// S3-094: da de alta un minimarket con configuración CL/CLP/IVA 19, una sucursal inicial
    /// y su usuario ADMIN, en una sola transacción. Solo SUPER_ADMIN.
    /// </summary>
    /// <response code="201">Minimarket creado; su ADMIN ya puede iniciar sesión con el código.</response>
    /// <response code="400">Datos inválidos.</response>
    /// <response code="403">El usuario no es SUPER_ADMIN.</response>
    /// <response code="409">Ya existe un minimarket con ese código o nombre.</response>
    [HttpPost]
    [Authorize(Roles = "SUPER_ADMIN")]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear([FromBody] CrearTenantRequest request, CancellationToken cancellationToken)
    {
        var validacion = await _crearValidator.ValidateAsync(request, cancellationToken);
        if (!validacion.IsValid)
        {
            foreach (var error in validacion.Errors)
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            return ValidationProblem(ModelState);
        }

        try
        {
            var tenant = await _tenantRepository.CreateWithAdminAsync(request);
            _logger.LogInformation("Minimarket {Codigo} creado con id {TenantId}", tenant.Codigo, tenant.Id);
            return StatusCode(StatusCodes.Status201Created, TenantDto.From(tenant));
        }
        catch (DuplicateTenantException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Retorna la configuración del tenant autenticado.
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
            return Unauthorized(new { message = "El token no contiene un tenant_id válido." });

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

    /// <summary>
    /// TI3-458/TI3-459: Actualiza la configuración del tenant autenticado.
    /// Solo accesible por usuarios con rol ADMIN del mismo tenant.
    /// Valida campos Moneda, Idioma, ZonaHoraria, PorcentajeIva via FluentValidation.
    /// </summary>
    [HttpPut("{id:guid}/config")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(TenantConfigDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateConfig(
        Guid id,
        [FromBody] ActualizarTenantConfigDto request,
        CancellationToken cancellationToken)
    {
        // TI3-459: Validar el request vía FluentValidation
        var validacion = await _validator.ValidateAsync(request, cancellationToken);
        if (!validacion.IsValid)
        {
            _logger.LogWarning("PUT /tenants/{TenantId}/config rechazado por validación: {Errores}",
                id, string.Join(", ", validacion.Errors.Select(e => e.ErrorMessage)));

            foreach (var error in validacion.Errors)
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);

            return ValidationProblem(ModelState);
        }

        // TI3-458: Validar que el usuario tiene un tenant_id válido en el JWT
        var tenantClaim = User.FindFirst("tenant_id")?.Value;
        if (!Guid.TryParse(tenantClaim, out var tokenTenantId))
            return Unauthorized(new { message = "El token no contiene un tenant_id válido." });

        // TI3-458: Validar que el usuario pertenece al mismo tenant que intenta modificar
        if (tokenTenantId != id)
        {
            _logger.LogWarning(
                "Intento de modificar config de tenant {TargetTenantId} por usuario del tenant {UserTenantId}",
                id, tokenTenantId);
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "No tienes permiso para modificar la configuración de otro tenant."
            });
        }

        // TI3-458: Usar GetByIdForUpdateAsync para obtener entidad con tracking
        var tenant = await _tenantRepository.GetByIdForUpdateAsync(id);
        if (tenant is null)
            return NotFound(new { message = $"Tenant {id} no encontrado o inactivo." });

        // TI3-459: Actualizar los campos de configuración (normalizando)
        tenant.Pais = request.Pais.ToUpperInvariant();
        tenant.Moneda = request.Moneda.ToUpperInvariant();
        tenant.Idioma = request.Idioma.ToLowerInvariant();
        tenant.PorcentajeIva = request.PorcentajeIva;
        tenant.ZonaHoraria = request.ZonaHoraria;

        var tenantActualizado = await _tenantRepository.UpdateAsync(tenant);

        _logger.LogInformation(
            "Config de tenant {TenantId} actualizada: Pais={Pais}, Moneda={Moneda}, Idioma={Idioma}, IVA={Iva}%, ZH={ZH}",
            id, tenantActualizado.Pais, tenantActualizado.Moneda,
            tenantActualizado.Idioma, tenantActualizado.PorcentajeIva,
            tenantActualizado.ZonaHoraria);

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

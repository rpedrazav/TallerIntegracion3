using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantIdentityService.DTOs;
using TenantIdentityService.Exceptions;
using TenantIdentityService.Models;
using TenantIdentityService.Repositories;
using TenantIdentityService.Validators;

namespace TenantIdentityService.Controllers;

/// <summary>
/// Expone las sucursales del tenant autenticado.
/// GET /sucursales y POST /sucursales.
/// El tenant se obtiene del claim tenant_id del JWT, nunca de la ruta ni del body,
/// de modo que no se puedan consultar ni crear sucursales de otro tenant.
/// El aislamiento lo aplica el HasQueryFilter global de TenantDbContext vía TenantMiddleware.
/// </summary>
[ApiController]
[Authorize]
[Route("sucursales")]
public class SucursalesController : ControllerBase
{
    private readonly ISucursalRepository _sucursalRepository;
    private readonly IValidator<CrearSucursalRequest> _crearSucursalValidator;
    private readonly ILogger<SucursalesController> _logger;

    public SucursalesController(
        ISucursalRepository sucursalRepository,
        IValidator<CrearSucursalRequest> crearSucursalValidator,
        ILogger<SucursalesController> logger)
    {
        _sucursalRepository = sucursalRepository ?? throw new ArgumentNullException(nameof(sucursalRepository));
        _crearSucursalValidator = crearSucursalValidator ?? throw new ArgumentNullException(nameof(crearSucursalValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retorna todas las sucursales del tenant del JWT, incluyendo las inactivas.
    /// La zona horaria de cada respuesta es la efectiva: la propia de la sucursal si tiene,
    /// o la del tenant si hereda.
    /// </summary>
    /// <returns>
    /// 200 con la lista de sucursales, que puede estar vacía.
    /// 401 si el JWT es inválido o no trae el claim tenant_id.
    /// </returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SucursalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll()
    {
        // TenantMiddleware ya inyectó CurrentTenantId en el DbContext a partir del claim.
        var sucursales = await _sucursalRepository.GetAllAsync();

        _logger.LogInformation("Listando {Cantidad} sucursales del tenant del token", sucursales.Count);

        var response = sucursales.Select(MapToDto).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Crea una sucursal belonging al tenant del JWT.
    /// Solo accesible para ADMIN: crear una sucursal es una operación de nivel tenant,
    /// igual que crear usuarios (UsuarioController) o editar la configuración del tenant.
    /// </summary>
    /// <param name="request">Nombre, dirección y zona horaria IANA opcional.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>
    /// 201 con la sucursal creada y su zona horaria efectiva ya resuelta.<br/>
    /// 400 si el body es inválido o la zona horaria no es un id IANA real.<br/>
    /// 401 sin JWT válido.<br/>
    /// 403 si el usuario no es ADMIN.<br/>
    /// 409 si ya existe una sucursal con ese nombre en el tenant (sin distinguir mayúsculas).
    /// </returns>
    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(SucursalDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CrearSucursalRequest request,
        CancellationToken cancellationToken)
    {
        // ── 1. Validar el request ────────────────────────────────────────────
        // MS-1 no registra AddFluentValidationAutoValidation(), así que se valida explícitamente.
        var validacion = await _crearSucursalValidator.ValidateAsync(request, cancellationToken);
        if (!validacion.IsValid)
        {
            _logger.LogDebug("POST /sucursales rechazado por validación: {Errores}",
                string.Join(", ", validacion.Errors.Select(e => e.ErrorMessage)));

            foreach (var error in validacion.Errors)
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);

            return ValidationProblem(ModelState);
        }

        // ── 2. Resolver el tenant desde el JWT ──────────────────────────────
        var tenantClaim = User.FindFirst("tenant_id")?.Value;
        if (!Guid.TryParse(tenantClaim, out var tenantId))
            return Unauthorized(new { message = "El token no contiene un tenant_id válido." });

        // ── 3. Construir la entidad ──────────────────────────────────────────
        // Zona vacía o ausente = null = hereda la zona del tenant.
        var zonaHoraria = string.IsNullOrWhiteSpace(request.ZonaHoraria)
            ? null
            : request.ZonaHoraria.Trim();

        var sucursal = new Sucursal
        {
            // TenantId viene solo del token (RN-01): el body no puede elegir de qué tenant es
            TenantId    = tenantId,
            Nombre      = request.Nombre,
            Direccion   = request.Direccion,
            ZonaHoraria = zonaHoraria,
            Activa      = true,
            CreadaEn    = DateTime.UtcNow
        };

        // ── 4. Persistir ─────────────────────────────────────────────────────
        try
        {
            var creada = await _sucursalRepository.CreateAsync(sucursal);

            _logger.LogInformation(
                "Sucursal creada {SucursalId} \"{Nombre}\" en tenant {TenantId} (zona propia: {ZonaPropia})",
                creada.Id, creada.Nombre, creada.TenantId, creada.ZonaHoraria ?? "(hereda del tenant)");

            var dto = MapToDto(creada);

            // 201 sin header Location: no existe GET /sucursales/{id}, solo el listado.
            return StatusCode(StatusCodes.Status201Created, dto);
        }
        catch (DuplicateSucursalNameException ex)
        {
            _logger.LogWarning(
                "Conflicto al crear sucursal \"{Nombre}\" en tenant {TenantId}: {Motivo}",
                ex.NombreDuplicado, tenantId, ex.Message);

            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Proyecta la entidad al DTO de respuesta, resolviendo la zona horaria efectiva.
    /// </summary>
    private static SucursalDto MapToDto(Sucursal sucursal) => new()
    {
        Id          = sucursal.Id,
        TenantId    = sucursal.TenantId,
        Nombre      = sucursal.Nombre,
        Direccion   = sucursal.Direccion,
        Activa      = sucursal.Activa,
        ZonaHoraria = sucursal.ZonaHoraria ?? sucursal.Tenant.ZonaHoraria
    };
}

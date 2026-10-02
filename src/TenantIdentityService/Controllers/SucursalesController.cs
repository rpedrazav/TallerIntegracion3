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
/// El aislamiento lo aplica el HasQueryFilter global de TenantDbContext vÃ­a TenantMiddleware.
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
        /// <returns>Lista de sucursales.</returns>
    /// <response code="200">Retorna la lista de sucursales, que puede estar vacía.</response>
    /// <response code="401">Si el JWT es inválido o no trae el claim tenant_id.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SucursalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll()
    {
        // TenantMiddleware ya inyectÃ³ CurrentTenantId en el DbContext a partir del claim.
        var sucursales = await _sucursalRepository.GetAllAsync();

        _logger.LogInformation("Listando {Cantidad} sucursales del tenant del token", sucursales.Count);

        var response = sucursales.Select(MapToDto).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Crea una sucursal belonging al tenant del JWT.
    /// Solo accesible para ADMIN: crear una sucursal es una operaciÃ³n de nivel tenant,
    /// igual que crear usuarios (UsuarioController) o editar la configuraciÃ³n del tenant.
    /// </summary>
    /// <param name="request">Nombre, dirección y zona horaria IANA opcional.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>La sucursal creada.</returns>
    /// <response code="201">Retorna la sucursal creada y su zona horaria efectiva ya resuelta.</response>
    /// <response code="400">Si el body es inválido o la zona horaria no es un id IANA real.</response>
    /// <response code="401">Sin JWT válido.</response>
    /// <response code="403">Si el usuario no es ADMIN.</response>
    /// <response code="409">Si ya existe una sucursal con ese nombre en el tenant.</response>
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
        // â”€â”€ 1. Validar el request â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // MS-1 no registra AddFluentValidationAutoValidation(), asÃ­ que se valida explÃ­citamente.
        var validacion = await _crearSucursalValidator.ValidateAsync(request, cancellationToken);
        if (!validacion.IsValid)
        {
            _logger.LogDebug("POST /sucursales rechazado por validaciÃ³n: {Errores}",
                string.Join(", ", validacion.Errors.Select(e => e.ErrorMessage)));

            foreach (var error in validacion.Errors)
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);

            return ValidationProblem(ModelState);
        }

        // â”€â”€ 2. Resolver el tenant desde el JWT â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var tenantClaim = User.FindFirst("tenant_id")?.Value;
        if (!Guid.TryParse(tenantClaim, out var tenantId))
            return Unauthorized(new { message = "El token no contiene un tenant_id vÃ¡lido." });

        // â”€â”€ 3. Construir la entidad â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // Zona vacÃ­a o ausente = null = hereda la zona del tenant.
        var zonaHoraria = string.IsNullOrWhiteSpace(request.ZonaHoraria)
            ? null
            : request.ZonaHoraria.Trim();

        var sucursal = new Sucursal
        {
            // TenantId viene solo del token (RN-01): el body no puede elegir de quÃ© tenant es
            TenantId    = tenantId,
            Nombre      = request.Nombre,
            Direccion   = request.Direccion,
            ZonaHoraria = zonaHoraria,
            Activa      = true,
            CreadaEn    = DateTime.UtcNow
        };

        // â”€â”€ 4. Persistir â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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




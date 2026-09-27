using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TenantIdentityService.DTOs;
using TenantIdentityService.Services;

namespace TenantIdentityService.Controllers;

/// <summary>
/// Controlador de autenticación — MS-1 Tenant &amp; Identity Service.
///
/// Endpoint público (sin JWT requerido):
///   POST /auth/login  →  retorna JWT si las credenciales son válidas.
///
/// Este es el ÚNICO endpoint del sistema que no requiere token.
/// Kong está configurado para dejarlo pasar sin validación JWT.
/// </summary>
[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IJwtService  _jwtService;
    private readonly IValidator<LoginRequest> _validator;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService             authService,
        IJwtService              jwtService,
        IValidator<LoginRequest> validator,
        IConfiguration           config,
        ILogger<AuthController>  logger)
    {
        _authService = authService;
        _jwtService  = jwtService;
        _validator   = validator;
        _config      = config;
        _logger      = logger;
    }

    /// <summary>
    /// Autentica a un usuario y retorna un JWT firmado.
    /// </summary>
    /// <param name="request">Credenciales del usuario y tenant al que accede.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>
    /// 200 con <see cref="LoginResponse"/> (JWT + info del usuario) si las credenciales son correctas.<br/>
    /// 400 si el body tiene errores de validación (email inválido, password vacía, etc.).<br/>
    /// 401 si las credenciales son incorrectas (mensaje genérico sin revelar el motivo exacto).
    /// </returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse),          StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest   request,
        CancellationToken         cancellationToken)
    {
        // ── 1. Validar el request con FluentValidation ────────────────────────
        var validacion = await _validator.ValidateAsync(request, cancellationToken);
        if (!validacion.IsValid)
        {
            _logger.LogDebug("Login rechazado por validación: {Errores}",
                string.Join(", ", validacion.Errors.Select(e => e.ErrorMessage)));

            // Retorna 400 con el formato estándar de ASP.NET Core ValidationProblemDetails
            foreach (var error in validacion.Errors)
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);

            return ValidationProblem(ModelState);
        }

        // ── 2. Validar credenciales contra la base de datos ───────────────────
        var usuario = await _authService.ValidateCredentialsAsync(
            request.Email,
            request.Password,
            request.TenantId);

        // ── 3. Si las credenciales son incorrectas → 401 genérico ─────────────
        //    NUNCA se indica si falló el email, la contraseña o si el usuario no existe.
        if (usuario is null)
        {
            _logger.LogWarning(
                "Login fallido para email {Email} en tenant {TenantId}. IP: {IP}",
                request.Email, request.TenantId,
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida");

            return Unauthorized(new { message = "Credenciales incorrectas." });
        }

        // ── 4. Generar el JWT ─────────────────────────────────────────────────
        var token     = _jwtService.GenerateToken(usuario);
        var horasExp  = _config.GetValue<int>("Jwt:ExpirationHours", 8);
        var expiresAt = DateTime.UtcNow.AddHours(horasExp);

        // ── 5. Construir la respuesta ─────────────────────────────────────────
        var roles = usuario.UsuarioRoles.Select(ur => ur.Rol.Nombre).ToList();

        // El active_role en la respuesta debe coincidir con el que se grabó en el JWT.
        // Usamos la misma lógica de prioridad que JwtService.
        string[] prioridadRoles = ["SUPER_ADMIN", "ADMIN", "CAJERO", "REPONEDOR", "CLIENTE_AFILIADO"];
        var activeRole = prioridadRoles.FirstOrDefault(r => roles.Contains(r)) ?? "CAJERO";

        var response = new LoginResponse
        {
            Token     = token,
            ExpiresAt = expiresAt,
            Usuario   = new UsuarioInfoDto
            {
                Id         = usuario.Id,
                Nombre     = usuario.Nombre,
                Email      = usuario.Email,
                ActiveRole = activeRole,
                Roles      = roles
            }
        };

        _logger.LogInformation(
            "Login exitoso. UsuarioId: {UsuarioId} | ActiveRole: {ActiveRole} | TenantId: {TenantId}",
            usuario.Id, activeRole, request.TenantId);

        return Ok(response);
    }
}

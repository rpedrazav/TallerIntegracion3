using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TenantIdentityService.DTOs;
using TenantIdentityService.Services;

namespace TenantIdentityService.Controllers;

/// <summary>
/// Controlador de autenticación — MS-1 Tenant &amp; Identity Service.
///
/// Endpoint público (sin JWT requerido):
///   POST /auth/login  →  retorna JWT si las credenciales y el código del minimarket son válidos.
///
/// Kong está configurado para dejarlo pasar sin validación JWT.
/// </summary>
[ApiController]
[Route("auth")]
[Route("api/auth")]
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
    /// Autentica a un usuario en su minimarket y retorna un JWT firmado.
    /// </summary>
    /// <param name="request">Email, contraseña y código del minimarket.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <response code="200">Retorna el JWT y datos del usuario si las credenciales son válidas.</response>
    /// <response code="400">Si el body tiene errores de validación (email inválido, password o código vacío).</response>
    /// <response code="401">Si las credenciales o el código son incorrectos (mensaje genérico).</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse),          StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest   request,
        CancellationToken         cancellationToken)
    {
        var validacion = await _validator.ValidateAsync(request, cancellationToken);
        if (!validacion.IsValid)
        {
            foreach (var error in validacion.Errors)
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            return ValidationProblem(ModelState);
        }

        var usuario = await _authService.ValidateCredentialsAsync(
            request.Email,
            request.Password,
            request.TenantCodigo);

        // Nunca se indica si falló el código, el email o la contraseña (CWE-204).
        if (usuario is null)
        {
            _logger.LogWarning(
                "Login fallido para email {Email} en minimarket {TenantCodigo}. IP: {IP}",
                request.Email, request.TenantCodigo,
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida");

            return Unauthorized(new { message = "Credenciales incorrectas." });
        }

        var token     = _jwtService.GenerateToken(usuario);
        var expiresAt = DateTime.UtcNow.AddHours(_config.GetValue<int>("Jwt:ExpirationHours", 8));
        var roles     = JwtService.RolesDe(usuario);

        return Ok(new LoginResponse
        {
            Token     = token,
            ExpiresAt = expiresAt,
            Usuario   = new UsuarioInfoDto
            {
                Id         = usuario.Id,
                Nombre     = usuario.Nombre,
                Email      = usuario.Email,
                ActiveRole = JwtService.RolActivo(roles),
                Roles      = roles
            }
        });
    }
}

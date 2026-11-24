using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TenantIdentityService.Controllers;
using TenantIdentityService.DTOs;
using TenantIdentityService.Models;
using TenantIdentityService.Services;
using Xunit;

namespace GlobalMart.Tests.MS1_TenantIdentity;

/// <summary>
/// Tests unitarios del AuthController (POST /auth/login).
/// Cubre TI3-206, TI3-207 y TI3-208.
/// </summary>
public class AuthController_LoginTests
{
    // ── Mocks compartidos ──────────────────────────────────────────────────
    private readonly Mock<IAuthService>             _authServiceMock;
    private readonly Mock<IJwtService>              _jwtServiceMock;
    private readonly Mock<IValidator<LoginRequest>> _validatorMock;
    private readonly Mock<IConfiguration>           _configMock;
    private readonly Mock<ILogger<AuthController>>  _loggerMock;
    private readonly AuthController                 _controller;

    // ── Datos de prueba reutilizables ──────────────────────────────────────
    private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid TenantB = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
    private static readonly Guid UserId  = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public AuthController_LoginTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _jwtServiceMock  = new Mock<IJwtService>();
        _validatorMock   = new Mock<IValidator<LoginRequest>>();
        _configMock      = new Mock<IConfiguration>();
        _loggerMock      = new Mock<ILogger<AuthController>>();

        // Configurar IConfiguration para que devuelva las horas de expiración
        _configMock.Setup(c => c.GetSection("Jwt:ExpirationHours").Value).Returns("8");

        _controller = new AuthController(
            _authServiceMock.Object,
            _jwtServiceMock.Object,
            _validatorMock.Object,
            _configMock.Object,
            _loggerMock.Object
        );

        // Simular HttpContext para que el controller pueda acceder a RemoteIpAddress
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    /// <summary>
    /// Crea un Usuario de prueba con un rol específico, listo para ser
    /// retornado por el mock de IAuthService.
    /// </summary>
    private static Usuario CrearUsuarioDePrueba(
        Guid tenantId,
        string rolNombre = "CAJERO",
        string email     = "cajero@globalmart.cl")
    {
        var rol = new Rol { Id = Guid.NewGuid(), Nombre = rolNombre };
        var usuario = new Usuario
        {
            Id           = UserId,
            TenantId     = tenantId,
            Nombre       = "Cajero Test",
            Email        = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("cajero123"),
            Activo       = true,
            UsuarioRoles = new List<UsuarioRol>
            {
                new() { UsuarioId = UserId, RolId = rol.Id, Rol = rol }
            },
            UsuarioSucursales = new List<UsuarioSucursal>()
        };
        return usuario;
    }

    /// <summary>
    /// Configura el mock del validador para que la validación sea exitosa.
    /// </summary>
    private void SetupValidacionExitosa()
    {
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    [Fact]
    public async Task Login_SolicitudInvalida_Retorna400YNoConsultaCredenciales()
    {
        using var services = new ServiceCollection().AddControllers().Services.BuildServiceProvider();
        _controller.HttpContext.RequestServices = services;
        var request = new LoginRequest { Email = "invalido", Password = "", TenantId = Guid.Empty };
        _validatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult([
                new ValidationFailure(nameof(LoginRequest.Email), "El email no tiene un formato válido.")
            ]));

        var result = await _controller.Login(request, CancellationToken.None);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var problem = badRequest.Value.Should().BeOfType<ValidationProblemDetails>().Subject;
        problem.Status.Should().Be(StatusCodes.Status400BadRequest);
        problem.Errors.Should().ContainKey(nameof(LoginRequest.Email));
        problem.Errors[nameof(LoginRequest.Email)].Should().Contain("El email no tiene un formato válido.");
        _authServiceMock.Verify(
            service => service.ValidateCredentialsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>()),
            Times.Never);
        _jwtServiceMock.Verify(service => service.GenerateToken(It.IsAny<Usuario>()), Times.Never);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  TI3-206: Login exitoso → 200 + JWT con user_id
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Login_ConCredencialesCorrectas_Retorna200ConToken()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email    = "cajero@globalmart.cl",
            Password = "cajero123",
            TenantId = TenantA
        };

        var usuario = CrearUsuarioDePrueba(TenantA);
        const string fakeJwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test.signature";

        SetupValidacionExitosa();

        _authServiceMock
            .Setup(s => s.ValidateCredentialsAsync(request.Email, request.Password, request.TenantId))
            .ReturnsAsync(usuario);

        _jwtServiceMock
            .Setup(s => s.GenerateToken(usuario))
            .Returns(fakeJwt);

        // Act
        var antes = DateTime.UtcNow;
        var result = await _controller.Login(request, CancellationToken.None);
        var despues = DateTime.UtcNow;

        // Assert — HTTP 200
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        // Assert — El body contiene Token no vacío
        var response = okResult.Value as LoginResponse;
        response.Should().NotBeNull();
        response!.Token.Should().NotBeNullOrEmpty();
        response.Token.Should().Be(fakeJwt);

        // Assert — El body contiene el user_id correcto
        response.Usuario.Should().NotBeNull();
        response.Usuario.Id.Should().Be(UserId);
        response.Usuario.Nombre.Should().Be("Cajero Test");
        response.Usuario.Email.Should().Be(request.Email);
        response.ExpiresAt.Should().BeOnOrAfter(antes.AddHours(8));
        response.ExpiresAt.Should().BeOnOrBefore(despues.AddHours(8));

        // Assert — El active_role es coherente
        response.Usuario.ActiveRole.Should().Be("CAJERO");
        response.Usuario.Roles.Should().ContainSingle().Which.Should().Be("CAJERO");
    }

    [Fact]
    public async Task Login_ConCredencialesCorrectas_RetornaUsuarioConEmail()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email    = "admin@globalmart.cl",
            Password = "admin123",
            TenantId = TenantA
        };

        var usuario = CrearUsuarioDePrueba(TenantA, "ADMIN", "admin@globalmart.cl");
        const string fakeJwt = "eyJhbGciOiJIUzI1NiJ9.admin.sig";

        SetupValidacionExitosa();

        _authServiceMock
            .Setup(s => s.ValidateCredentialsAsync(request.Email, request.Password, request.TenantId))
            .ReturnsAsync(usuario);

        _jwtServiceMock
            .Setup(s => s.GenerateToken(usuario))
            .Returns(fakeJwt);

        // Act
        var result = await _controller.Login(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value as LoginResponse;

        response!.Usuario.Email.Should().Be("admin@globalmart.cl");
        response.Usuario.ActiveRole.Should().Be("ADMIN");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  TI3-207: Login con contraseña incorrecta → 401 genérico
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Login_ConContrasenaIncorrecta_Retorna401()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email    = "cajero@globalmart.cl",
            Password = "contraseña_incorrecta",
            TenantId = TenantA
        };

        SetupValidacionExitosa();

        // AuthService retorna null cuando las credenciales son incorrectas
        _authServiceMock
            .Setup(s => s.ValidateCredentialsAsync(request.Email, request.Password, request.TenantId))
            .ReturnsAsync((Usuario?)null);

        // Act
        var result = await _controller.Login(request, CancellationToken.None);

        // Assert — HTTP 401
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Login_ConContrasenaIncorrecta_MensajeNoRevelaDetalles()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email    = "cajero@globalmart.cl",
            Password = "contraseña_incorrecta",
            TenantId = TenantA
        };

        SetupValidacionExitosa();

        _authServiceMock
            .Setup(s => s.ValidateCredentialsAsync(request.Email, request.Password, request.TenantId))
            .ReturnsAsync((Usuario?)null);

        // Act
        var result = await _controller.Login(request, CancellationToken.None);

        // Assert — El mensaje es genérico y NO revela si el usuario existe (CWE-204)
        var unauthorizedResult = result as UnauthorizedObjectResult;
        var body = unauthorizedResult!.Value;
        var messageJson = System.Text.Json.JsonSerializer.Serialize(body);

        // Debe contener "Credenciales incorrectas" (mensaje genérico)
        messageJson.Should().Contain("Credenciales incorrectas");

        // NO debe revelar información sobre el usuario
        messageJson.Should().NotContainEquivalentOf("no existe");
        messageJson.Should().NotContainEquivalentOf("usuario no encontrado");
        messageJson.Should().NotContainEquivalentOf("email incorrecto");
        messageJson.Should().NotContainEquivalentOf("contraseña incorrecta");
    }

    [Fact]
    public async Task Login_ConUsuarioInexistente_Retorna401MensajeGenerico()
    {
        // Arrange — email que no existe en la BD
        var request = new LoginRequest
        {
            Email    = "noexiste@globalmart.cl",
            Password = "cualquierPassword1",
            TenantId = TenantA
        };

        SetupValidacionExitosa();

        _authServiceMock
            .Setup(s => s.ValidateCredentialsAsync(request.Email, request.Password, request.TenantId))
            .ReturnsAsync((Usuario?)null);

        // Act
        var result = await _controller.Login(request, CancellationToken.None);

        // Assert — Mismo 401 genérico (no debe diferenciarse del caso de contraseña incorrecta)
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var messageJson = System.Text.Json.JsonSerializer.Serialize(unauthorizedResult.Value);

        messageJson.Should().Contain("Credenciales incorrectas");
        messageJson.Should().NotContainEquivalentOf("no existe");
    }

    [Fact]
    public async Task Login_UsuarioInactivo_Retorna401SinFiltrarEstado()
    {
        var request = new LoginRequest
        {
            Email = "inactivo@globalmart.cl",
            Password = "password123",
            TenantId = TenantA
        };
        SetupValidacionExitosa();
        _authServiceMock
            .Setup(service => service.ValidateCredentialsAsync(request.Email, request.Password, request.TenantId))
            .ReturnsAsync((Usuario?)null);

        var result = await _controller.Login(request, CancellationToken.None);

        var unauthorized = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var body = System.Text.Json.JsonSerializer.Serialize(unauthorized.Value);
        body.Should().Contain("Credenciales incorrectas");
        body.Should().NotContainEquivalentOf("inactivo");
        body.Should().NotContainEquivalentOf("desactivado");
        _jwtServiceMock.Verify(service => service.GenerateToken(It.IsAny<Usuario>()), Times.Never);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  TI3-208: Login con email de otro tenant → 401
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Login_ConEmailDeOtroTenant_Retorna401()
    {
        // Arrange — El usuario existe en TenantA, pero intenta loguearse en TenantB
        var request = new LoginRequest
        {
            Email    = "cajero@globalmart.cl",
            Password = "cajero123",
            TenantId = TenantB  // Tenant distinto al del usuario
        };

        SetupValidacionExitosa();

        // AuthService retorna null porque el usuario no pertenece a TenantB
        _authServiceMock
            .Setup(s => s.ValidateCredentialsAsync(request.Email, request.Password, TenantB))
            .ReturnsAsync((Usuario?)null);

        // Act
        var result = await _controller.Login(request, CancellationToken.None);

        // Assert — HTTP 401 (aislamiento multi-tenant)
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Login_ConEmailDeOtroTenant_NoRevelaExistenciaEnOtroTenant()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email    = "cajero@globalmart.cl",
            Password = "cajero123",
            TenantId = TenantB
        };

        SetupValidacionExitosa();

        _authServiceMock
            .Setup(s => s.ValidateCredentialsAsync(request.Email, request.Password, TenantB))
            .ReturnsAsync((Usuario?)null);

        // Act
        var result = await _controller.Login(request, CancellationToken.None);

        // Assert — El mensaje NO debe mencionar "otro tenant" ni revelar existencia
        var unauthorizedResult = result as UnauthorizedObjectResult;
        var messageJson = System.Text.Json.JsonSerializer.Serialize(unauthorizedResult!.Value);

        messageJson.Should().Contain("Credenciales incorrectas");
        messageJson.Should().NotContainEquivalentOf("otro tenant");
        messageJson.Should().NotContainEquivalentOf("tenant incorrecto");
        messageJson.Should().NotContainEquivalentOf("no pertenece");
    }
}

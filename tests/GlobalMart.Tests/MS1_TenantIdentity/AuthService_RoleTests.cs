using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TenantIdentityService.Models;
using TenantIdentityService.Repositories;
using TenantIdentityService.Services;
using Xunit;

namespace GlobalMart.Tests.MS1_TenantIdentity;

/// <summary>
/// Tests unitarios para la asignación y recuperación de roles (RBAC).
/// Cubre TI3-209: verificar que el array de roles se guarda y se puede
/// recuperar correctamente a través de AuthService y JwtService.
/// </summary>
public class AuthService_RoleTests
{
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid UserId   = Guid.Parse("22222222-2222-2222-2222-222222222222");

    // ════════════════════════════════════════════════════════════════════════
    //  Test del modelo: asignación de roles al usuario
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Usuario_ConMultiplesRoles_GuardaYRecuperaCorrectamente()
    {
        // Arrange — Crear un usuario con dos roles: CAJERO y ADMIN
        var rolCajero = new Rol { Id = Guid.NewGuid(), Nombre = "CAJERO" };
        var rolAdmin  = new Rol { Id = Guid.NewGuid(), Nombre = "ADMIN" };

        var usuario = new Usuario
        {
            Id       = UserId,
            TenantId = TenantId,
            Nombre   = "Multi-Rol User",
            Email    = "multirol@globalmart.cl",
            Activo   = true,
            UsuarioRoles = new List<UsuarioRol>
            {
                new() { UsuarioId = UserId, RolId = rolCajero.Id, Rol = rolCajero },
                new() { UsuarioId = UserId, RolId = rolAdmin.Id,  Rol = rolAdmin }
            },
            UsuarioSucursales = new List<UsuarioSucursal>()
        };

        // Act — Recuperar los nombres de roles
        var roles = usuario.UsuarioRoles.Select(ur => ur.Rol.Nombre).ToList();

        // Assert
        roles.Should().HaveCount(2);
        roles.Should().Contain("CAJERO");
        roles.Should().Contain("ADMIN");
    }

    [Fact]
    public void Usuario_SinRoles_TieneColeccionVacia()
    {
        // Arrange
        var usuario = new Usuario
        {
            Id       = UserId,
            TenantId = TenantId,
            Nombre   = "Sin Roles",
            Email    = "sinrol@globalmart.cl",
            Activo   = true
        };

        // Act & Assert
        usuario.UsuarioRoles.Should().BeEmpty();
    }

    [Fact]
    public void Usuario_ConUnSoloRol_RecuperaCorrectamente()
    {
        // Arrange
        var rolCajero = new Rol { Id = Guid.NewGuid(), Nombre = "CAJERO" };
        var usuario = new Usuario
        {
            Id       = UserId,
            TenantId = TenantId,
            Nombre   = "Cajero Único",
            Email    = "cajero@globalmart.cl",
            Activo   = true,
            UsuarioRoles = new List<UsuarioRol>
            {
                new() { UsuarioId = UserId, RolId = rolCajero.Id, Rol = rolCajero }
            },
            UsuarioSucursales = new List<UsuarioSucursal>()
        };

        // Act
        var roles = usuario.UsuarioRoles.Select(ur => ur.Rol.Nombre).ToList();

        // Assert
        roles.Should().ContainSingle().Which.Should().Be("CAJERO");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Test de AuthService: ValidateCredentials retorna usuario con roles
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AuthService_ValidateCredentials_RetornaUsuarioConRolesIntactos()
    {
        // Arrange
        var rolCajero = new Rol { Id = Guid.NewGuid(), Nombre = "CAJERO" };
        var rolReponedor = new Rol { Id = Guid.NewGuid(), Nombre = "REPONEDOR" };

        var passwordPlano = "miPassword123";
        var passwordHash  = BCrypt.Net.BCrypt.HashPassword(passwordPlano);

        var usuario = new Usuario
        {
            Id           = UserId,
            TenantId     = TenantId,
            Nombre       = "Cajero Reponedor",
            Email        = "dual@globalmart.cl",
            PasswordHash = passwordHash,
            Activo       = true,
            UsuarioRoles = new List<UsuarioRol>
            {
                new() { UsuarioId = UserId, RolId = rolCajero.Id,    Rol = rolCajero },
                new() { UsuarioId = UserId, RolId = rolReponedor.Id, Rol = rolReponedor }
            },
            UsuarioSucursales = new List<UsuarioSucursal>()
        };

        var repoMock = new Mock<IUserRepository>();
        repoMock
            .Setup(r => r.FindByEmailAsync("dual@globalmart.cl", TenantId))
            .ReturnsAsync(usuario);

        var loggerMock = new Mock<ILogger<AuthService>>();
        var authService = new AuthService(repoMock.Object, loggerMock.Object);

        // Act
        var resultado = await authService.ValidateCredentialsAsync(
            "dual@globalmart.cl", passwordPlano, TenantId);

        // Assert — El usuario retornado tiene los roles intactos
        resultado.Should().NotBeNull();
        resultado!.UsuarioRoles.Should().HaveCount(2);

        var nombresRoles = resultado.UsuarioRoles.Select(ur => ur.Rol.Nombre).ToList();
        nombresRoles.Should().Contain("CAJERO");
        nombresRoles.Should().Contain("REPONEDOR");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Test de JwtService: el JWT generado contiene los roles correctos
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void JwtService_GenerateToken_IncluyeRolesEnElToken()
    {
        // Arrange — Configurar IConfiguration con las claves JWT
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Jwt:Key",             "GlobalMartOS_TestKey_MustBeAtLeast32CharsLong!!" },
            { "Jwt:Issuer",          "GlobalMartOS" },
            { "Jwt:Audience",        "GlobalMartOS_Clients" },
            { "Jwt:ExpirationHours", "8" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var loggerMock = new Mock<ILogger<JwtService>>();
        var jwtService = new JwtService(configuration, loggerMock.Object);

        var rolCajero = new Rol { Id = Guid.NewGuid(), Nombre = "CAJERO" };
        var rolAdmin  = new Rol { Id = Guid.NewGuid(), Nombre = "ADMIN" };

        var usuario = new Usuario
        {
            Id       = UserId,
            TenantId = TenantId,
            Nombre   = "Multi-Rol JWT Test",
            Email    = "jwt@globalmart.cl",
            Activo   = true,
            UsuarioRoles = new List<UsuarioRol>
            {
                new() { UsuarioId = UserId, RolId = rolCajero.Id, Rol = rolCajero },
                new() { UsuarioId = UserId, RolId = rolAdmin.Id,  Rol = rolAdmin }
            },
            UsuarioSucursales = new List<UsuarioSucursal>()
        };

        // Act
        var tokenString = jwtService.GenerateToken(usuario);

        // Assert — El token no está vacío y tiene formato JWT (3 partes)
        tokenString.Should().NotBeNullOrEmpty();
        tokenString.Split('.').Should().HaveCount(3);

        // Decodificar el payload del JWT para verificar los claims
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(tokenString);

        // Assert — Contiene el claim "sub" con el user_id
        jwt.Claims.Should().Contain(c => c.Type == "sub" && c.Value == UserId.ToString());

        // Assert — Contiene el claim "roles" con ambos roles
        var rolesClaim = jwt.Claims.FirstOrDefault(c => c.Type == "roles");
        rolesClaim.Should().NotBeNull();
        rolesClaim!.Value.Should().Contain("CAJERO");
        rolesClaim.Value.Should().Contain("ADMIN");

        // Assert — active_role debe ser ADMIN (mayor prioridad que CAJERO)
        var activeRoleClaim = jwt.Claims.FirstOrDefault(c => c.Type == "active_role");
        activeRoleClaim.Should().NotBeNull();
        activeRoleClaim!.Value.Should().Be("ADMIN");
    }

    [Fact]
    public void JwtService_GenerateToken_ConUnSoloRol_ActiveRoleCorrecto()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Jwt:Key",             "GlobalMartOS_TestKey_MustBeAtLeast32CharsLong!!" },
            { "Jwt:Issuer",          "GlobalMartOS" },
            { "Jwt:Audience",        "GlobalMartOS_Clients" },
            { "Jwt:ExpirationHours", "8" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var loggerMock = new Mock<ILogger<JwtService>>();
        var jwtService = new JwtService(configuration, loggerMock.Object);

        var rolCajero = new Rol { Id = Guid.NewGuid(), Nombre = "CAJERO" };
        var usuario = new Usuario
        {
            Id       = UserId,
            TenantId = TenantId,
            Nombre   = "Solo Cajero",
            Email    = "solcajero@globalmart.cl",
            Activo   = true,
            UsuarioRoles = new List<UsuarioRol>
            {
                new() { UsuarioId = UserId, RolId = rolCajero.Id, Rol = rolCajero }
            },
            UsuarioSucursales = new List<UsuarioSucursal>()
        };

        // Act
        var tokenString = jwtService.GenerateToken(usuario);

        // Assert
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(tokenString);

        var activeRoleClaim = jwt.Claims.FirstOrDefault(c => c.Type == "active_role");
        activeRoleClaim.Should().NotBeNull();
        activeRoleClaim!.Value.Should().Be("CAJERO");
    }
}

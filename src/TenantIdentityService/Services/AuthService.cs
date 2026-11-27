using TenantIdentityService.Models;
using TenantIdentityService.Repositories;

namespace TenantIdentityService.Services;

/// <summary>
/// Implementación del servicio de autenticación.
///
/// Responsabilidad única (SRP):
///   - UsuarioRepository  → "¿existe este usuario en la BD?"
///   - AuthService     → "¿puede este usuario ingresar?" (valida contraseña)
///   - JwtService      → "genera el token para este usuario"
///
/// Principio de seguridad aplicado: este servicio NUNCA revela si el fallo
/// fue por email inexistente, contraseña incorrecta o usuario desactivado.
/// Siempre retorna null en cualquier caso de fallo para que el AuthController
/// devuelva un 401 genérico (previene enumeración de usuarios — CWE-204).
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IUsuarioRepository usuarioRepository, ILogger<AuthService> logger)
    {
        _usuarioRepository = usuarioRepository;
        _logger         = logger;
    }

    /// <inheritdoc />
    public async Task<Usuario?> ValidateCredentialsAsync(string email, string password, string tenantCodigo)
    {
        // 1. Buscar el usuario en la base de datos (incluye roles y sucursales)
        var usuario = await _usuarioRepository.FindByEmailAsync(email, tenantCodigo);

        // 2. Si no existe o está inactivo → retornar null sin revelar el motivo
        if (usuario is null)
        {
            // Log interno con detalle (solo visible en logs del servidor, nunca al cliente)
            _logger.LogWarning(
                "Intento de login fallido: usuario no encontrado. Email: {Email}, TenantCodigo: {TenantCodigo}",
                email, tenantCodigo);
            return null;
        }

        if (!usuario.UsuarioRoles.Any()) return null;

        // 3. Verificar la contraseña con BCrypt
        //    BCrypt.Verify compara el texto plano contra el hash almacenado en la BD.
        //    Es resistente a timing attacks por diseño del algoritmo.
        var passwordValida = BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash);

        if (!passwordValida)
        {
            _logger.LogWarning(
                "Intento de login fallido: contraseña incorrecta. UsuarioId: {UsuarioId}, TenantId: {TenantId}",
                usuario.Id, usuario.TenantId);
            return null;
        }

        // 4. Credenciales válidas → retornar el usuario con sus roles cargados
        _logger.LogInformation(
            "Login exitoso. UsuarioId: {UsuarioId}, Email: {Email}, TenantId: {TenantId}",
            usuario.Id, email, usuario.TenantId);

        return usuario;
    }
}

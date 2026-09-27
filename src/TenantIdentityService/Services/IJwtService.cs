using TenantIdentityService.Models;

namespace TenantIdentityService.Services;

/// <summary>
/// Contrato del servicio de generación de tokens JWT.
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Genera un JWT firmado con todos los claims requeridos por GlobalMart OS.
    /// </summary>
    /// <param name="usuario">
    /// Usuario autenticado. Debe tener <c>UsuarioRoles</c> y <c>UsuarioSucursales</c>
    /// cargados (eager loading) antes de llamar a este método.
    /// </param>
    /// <returns>Token JWT como string listo para enviar al cliente.</returns>
    string GenerateToken(Usuario usuario);
}

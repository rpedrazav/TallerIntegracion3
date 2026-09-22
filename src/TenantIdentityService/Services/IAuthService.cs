using TenantIdentityService.Models;

namespace TenantIdentityService.Services;

/// <summary>
/// Contrato del servicio de autenticación.
/// Abstrae la lógica de validación de credenciales para facilitar tests con Moq.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Valida las credenciales del usuario contra la base de datos.
    /// </summary>
    /// <param name="email">Email ingresado por el usuario.</param>
    /// <param name="password">Contraseña en texto plano ingresada por el usuario.</param>
    /// <param name="tenantId">Tenant al que intenta acceder el usuario.</param>
    /// <returns>
    /// El <see cref="Usuario"/> con sus roles cargados si las credenciales son correctas,
    /// o <c>null</c> si el usuario no existe, está inactivo o la contraseña es incorrecta.
    /// </returns>
    Task<Usuario?> ValidateCredentialsAsync(string email, string password, Guid tenantId);
}

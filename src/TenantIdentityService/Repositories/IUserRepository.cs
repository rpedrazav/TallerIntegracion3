using TenantIdentityService.Models;

namespace TenantIdentityService.Repositories;

/// <summary>
/// Contrato del repositorio de usuarios.
/// Abstrae el acceso a la base de datos para que AuthService
/// no dependa directamente de EF Core (facilita los tests con Moq).
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Busca un usuario por email dentro de un tenant específico.
    /// Carga los roles del usuario en la misma consulta (Include eager loading).
    /// </summary>
    /// <param name="email">Email del usuario (case-insensitive).</param>
    /// <param name="tenantId">Tenant al que debe pertenecer el usuario.</param>
    /// <returns>El usuario con sus roles cargados, o null si no existe o está inactivo.</returns>
    Task<Usuario?> FindByEmailAsync(string email, Guid tenantId);
}

using TenantIdentityService.Models;

namespace TenantIdentityService.Repositories;

public interface ISucursalRepository
{
    /// <summary>
    /// Retorna las sucursales activas del tenant del request actual.
    /// El aislamiento lo aplica el filtro global de <c>TenantDbContext</c>
    /// (alimentado por <c>TenantMiddleware</c>), no un filtro manual.
    /// </summary>
    Task<IReadOnlyList<Sucursal>> GetAllAsync();

    /// <summary>
    /// Crea una sucursal para el tenant del request actual.
    /// El <c>TenantId</c> se asigna desde el contexto ya filtrado, nunca desde el body.
    /// </summary>
    /// <exception cref="Exceptions.DuplicateSucursalNameException">
    /// Si el tenant ya tiene una sucursal con ese nombre (comparación case-insensitive).
    /// </exception>
    Task<Sucursal> CreateAsync(Sucursal sucursal);

    /// <summary>Actualiza nombre y zona horaria, restringiendo la escritura por id y tenant.</summary>
    /// <returns>La sucursal actualizada o <c>null</c> si no pertenece al tenant.</returns>
    Task<Sucursal?> UpdateAsync(
        Guid id,
        Guid tenantId,
        string nombre,
        string? zonaHoraria,
        CancellationToken cancellationToken = default);

    /// <summary>Desactiva una sucursal sin eliminarla, restringiendo por id y tenant.</summary>
    /// <returns><c>true</c> si la sucursal existe en el tenant; en otro caso, <c>false</c>.</returns>
    Task<bool> DeactivateAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

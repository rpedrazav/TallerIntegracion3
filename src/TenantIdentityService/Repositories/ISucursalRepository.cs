using TenantIdentityService.Models;

namespace TenantIdentityService.Repositories;

public interface ISucursalRepository
{
    /// <summary>
    /// Retorna todas las sucursales del tenant del request actual.
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
}

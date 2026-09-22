using Microsoft.EntityFrameworkCore;
using TenantIdentityService.Data;
using TenantIdentityService.Models;

namespace TenantIdentityService.Repositories;

/// <summary>
/// Implementación de IUserRepository usando EF Core + PostgreSQL.
/// 
/// IMPORTANTE: Este repositorio NO usa el filtro global de tenant_id del DbContext
/// (CurrentTenantId) porque el login ocurre ANTES de que el TenantMiddleware pueda
/// establecer ese valor. En cambio, recibe tenantId como parámetro explícito
/// y filtra manualmente con IgnoreQueryFilters() para evitar un filtro sobre null.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly TenantDbContext _db;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(TenantDbContext db, ILogger<UserRepository> logger)
    {
        _db     = db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Usuario?> FindByEmailAsync(string email, Guid tenantId)
    {
        // IgnoreQueryFilters porque durante el login CurrentTenantId aún es null
        // y el filtro global HasQueryFilter(u => u.TenantId == CurrentTenantId) retornaría vacío.
        var usuario = await _db.Usuarios
            .IgnoreQueryFilters()
            .Include(u => u.UsuarioRoles)
                .ThenInclude(ur => ur.Rol)
            .Include(u => u.UsuarioSucursales)
            .Where(u => u.TenantId == tenantId
                     && u.Email.ToLower() == email.ToLower()
                     && u.Activo)
            .FirstOrDefaultAsync();

        if (usuario is null)
            _logger.LogDebug("FindByEmail: no se encontró usuario activo con email {Email} en tenant {TenantId}", email, tenantId);

        return usuario;
    }
}

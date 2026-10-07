using Microsoft.EntityFrameworkCore;
using Npgsql;
using TenantIdentityService.Data;
using TenantIdentityService.DTOs;
using TenantIdentityService.Exceptions;
using TenantIdentityService.Models;

namespace TenantIdentityService.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private const string UniqueViolationSqlState = "23505";
    private readonly TenantDbContext _db;

    public UsuarioRepository(TenantDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Usuario>> GetAllAsync(Guid tenantId)
    {
        return await _db.Usuarios
            .IgnoreQueryFilters()
            .Where(usuario => usuario.TenantId == tenantId)
            .ToListAsync();
    }

    public async Task<PagedResult<Usuario>> GetActivePagedAsync(
        Guid tenantId,
        int page,
        int pageSize)
    {
        var query = _db.Usuarios
            .IgnoreQueryFilters()
            .Include(usuario => usuario.UsuarioRoles)
                .ThenInclude(usuarioRol => usuarioRol.Rol)
            .Where(usuario => usuario.TenantId == tenantId && usuario.Activo);

        var totalItems = await query.CountAsync();
        var items = await query
            .OrderBy(usuario => usuario.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Usuario>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
        };
    }

    public async Task<Usuario?> GetByIdAsync(Guid id, Guid tenantId)
    {
        return await _db.Usuarios
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(usuario => usuario.Id == id && usuario.TenantId == tenantId);
    }

    public async Task<Usuario> CreateAsync(Usuario usuario)
    {
        var email = usuario.Email.Trim().ToLowerInvariant();
        usuario.Email = email;

        var yaExiste = await _db.Usuarios
            .IgnoreQueryFilters()
            .AnyAsync(u => u.TenantId == usuario.TenantId && u.Email.ToLower() == email);

        if (yaExiste)
            throw new DuplicateUserEmailException(email);

        _db.Usuarios.Add(usuario);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (EsViolacionDeUnicidad(ex))
        {
            throw new DuplicateUserEmailException(email);
        }

        return usuario;
    }

    private static bool EsViolacionDeUnicidad(DbUpdateException ex)
        => ex.InnerException is PostgresException { SqlState: UniqueViolationSqlState };

    public async Task<Usuario> UpdateAsync(Usuario usuario)
    {
        var usuarioExistente = await _db.Usuarios
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(existing => existing.Id == usuario.Id
                                          && existing.TenantId == usuario.TenantId);

        if (usuarioExistente is null)
            throw new KeyNotFoundException("El usuario no existe en el tenant indicado.");

        _db.Entry(usuarioExistente).CurrentValues.SetValues(usuario);
        await _db.SaveChangesAsync();
        return usuarioExistente;
    }

    public async Task<bool> DeactivateAsync(Guid id, Guid tenantId)
    {
        var usuario = await _db.Usuarios
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(existing => existing.Id == id && existing.TenantId == tenantId);

        if (usuario is null)
            return false;

        usuario.Activo = false;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<AssignRolesResult> AssignRolesAsync(
        Guid userId,
        IEnumerable<string> roleNames,
        Guid tenantId)
    {
        var usuario = await _db.Usuarios
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(existing => existing.Id == userId
                                          && existing.TenantId == tenantId);

        if (usuario is null)
        {
            return new AssignRolesResult
            {
                UserFound = false
            };
        }

        var normalizedRoleNames = roleNames
            .Select(roleName => roleName.Trim().ToUpperInvariant())
            .Distinct()
            .ToArray();

        var roles = await _db.Roles
            .Where(role => normalizedRoleNames.Contains(role.Nombre.ToUpper()))
            .ToListAsync();

        var validRoleNames = roles
            .Select(role => role.Nombre.ToUpperInvariant())
            .ToHashSet();
        var invalidRoles = normalizedRoleNames
            .Where(roleName => !validRoleNames.Contains(roleName))
            .ToArray();

        if (invalidRoles.Length > 0)
        {
            return new AssignRolesResult
            {
                UserFound = true,
                InvalidRoles = invalidRoles,
                Usuario = usuario
            };
        }

        var currentAssignments = await _db.UsuarioRoles
            .Where(usuarioRol => usuarioRol.UsuarioId == userId)
            .ToListAsync();

        _db.UsuarioRoles.RemoveRange(currentAssignments);
        _db.UsuarioRoles.AddRange(roles.Select(role => new UsuarioRol
        {
            UsuarioId = userId,
            RolId = role.Id
        }));

        await _db.SaveChangesAsync();

        return new AssignRolesResult
        {
            UserFound = true,
            Usuario = usuario
        };
    }

    public async Task<IReadOnlyList<string>> ValidateRoleNamesAsync(IEnumerable<string> roleNames)
    {
        var normalizedRoleNames = roleNames
            .Select(roleName => roleName.Trim().ToUpperInvariant())
            .Distinct()
            .ToArray();

        var existingRoles = await _db.Roles
            .Where(role => normalizedRoleNames.Contains(role.Nombre.ToUpper()))
            .Select(role => role.Nombre.ToUpperInvariant())
            .ToListAsync();

        return normalizedRoleNames
            .Where(roleName => !existingRoles.Contains(roleName))
            .ToArray();
    }

    public async Task<bool> DeletePermanentlyAsync(Guid id, Guid tenantId)
    {
        var usuario = await _db.Usuarios
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);

        if (usuario is null)
            return false;

        _db.Usuarios.Remove(usuario);
        await _db.SaveChangesAsync();
        return true;
    }
}
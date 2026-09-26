using Microsoft.EntityFrameworkCore;
using TenantIdentityService.Data;
using TenantIdentityService.Models;

namespace TenantIdentityService.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
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

    public async Task<Usuario?> GetByIdAsync(Guid id, Guid tenantId)
    {
        return await _db.Usuarios
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(usuario => usuario.Id == id && usuario.TenantId == tenantId);
    }

    public async Task<Usuario> CreateAsync(Usuario usuario)
    {
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();
        return usuario;
    }

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
}
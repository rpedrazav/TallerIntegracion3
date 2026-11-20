using Microsoft.EntityFrameworkCore;
using Npgsql;
using TenantIdentityService.Data;
using TenantIdentityService.Exceptions;
using TenantIdentityService.Models;

namespace TenantIdentityService.Repositories;

public class SucursalRepository : ISucursalRepository
{
    /// <summary>
    /// SQLSTATE de PostgreSQL para violación de restricción única (unique_violation).
    /// Es la garantía de que dos peticiones simultáneas no creen la misma sucursal.
    /// </summary>
    private const string UniqueViolationSqlState = "23505";

    private readonly TenantDbContext _db;

    public SucursalRepository(TenantDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Sucursal>> GetAllAsync()
    {
        // El HasQueryFilter global de TenantDbContext ya restringe al tenant del request
        // (RN-01), por eso no se filtra manualmente. Se incluye Tenant para poder resolver
        // la zona horaria efectiva: Sucursal.ZonaHoraria ?? Tenant.ZonaHoraria.
        return await _db.Sucursales
            .AsNoTracking()
            .Include(s => s.Tenant)
            .OrderBy(s => s.Nombre)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<Sucursal> CreateAsync(Sucursal sucursal)
    {
        // Normaliza el nombre para que "Centro" y "centro " colisionen con el índice único
        var nombre = sucursal.Nombre.Trim();
        sucursal.Nombre = nombre;

        await EnsureNameAvailableAsync(nombre, sucursal.TenantId);

        _db.Sucursales.Add(sucursal);
        await SaveChangesAsync(nombre);

        // Se relee con Include para devolver la navegación Tenant cargada y poder
        // resolver la zona horaria efectiva en el controller sin una consulta extra allí.
        return await _db.Sucursales
            .AsNoTracking()
            .Include(s => s.Tenant)
            .FirstAsync(s => s.Id == sucursal.Id);
    }

    /// <inheritdoc/>
    public async Task<Sucursal?> UpdateAsync(
        Guid id,
        Guid tenantId,
        string nombre,
        string? zonaHoraria,
        CancellationToken cancellationToken = default)
    {
        var sucursal = await _db.Sucursales
            .Include(s => s.Tenant)
            .FirstOrDefaultAsync(
                s => s.Id == id && s.TenantId == tenantId,
                cancellationToken);

        if (sucursal is null)
            return null;

        nombre = nombre.Trim();
        await EnsureNameAvailableAsync(nombre, tenantId, id, cancellationToken);

        sucursal.Nombre = nombre;
        sucursal.ZonaHoraria = zonaHoraria;

        await SaveChangesAsync(nombre, cancellationToken);

        return sucursal;
    }

    private async Task EnsureNameAvailableAsync(
        string nombre,
        Guid tenantId,
        Guid? excludedId = null,
        CancellationToken cancellationToken = default)
    {
        var yaExiste = await _db.Sucursales.AnyAsync(
            s => s.TenantId == tenantId &&
                 (!excludedId.HasValue || s.Id != excludedId.Value) &&
                 s.Nombre.ToLower() == nombre.ToLower(),
            cancellationToken);

        if (yaExiste)
            throw new DuplicateSucursalNameException(nombre);
    }

    private async Task SaveChangesAsync(
        string nombre,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (EsViolacionDeUnicidad(ex))
        {
            // El índice único resuelve carreras entre el chequeo y la escritura.
            throw new DuplicateSucursalNameException(nombre);
        }
    }

    /// <summary>
    /// Detecta si el fallo de EF corresponde a una violación de índice único en PostgreSQL.
    /// </summary>
    private static bool EsViolacionDeUnicidad(DbUpdateException ex)
        => ex.InnerException is PostgresException { SqlState: UniqueViolationSqlState };
}

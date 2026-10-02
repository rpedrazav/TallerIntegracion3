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

        // Chequeo previo para el caso común: devuelve un mensaje claro sin tocar el índice.
        // No sustituye al índice único, solo evita el viaje de ida y vuelta de un fallo.
        var yaExiste = await _db.Sucursales
            .AnyAsync(s => s.Nombre.ToLower() == nombre.ToLower());

        if (yaExiste)
            throw new DuplicateSucursalNameException(nombre);

        _db.Sucursales.Add(sucursal);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (EsViolacionDeUnicidad(ex))
        {
            // Carrera: otra petición insertó el mismo nombre entre el chequeo y el INSERT.
            // El índice único es la garantía real; aquí solo se traduce a una excepción de dominio.
            throw new DuplicateSucursalNameException(nombre);
        }

        // Se relee con Include para devolver la navegación Tenant cargada y poder
        // resolver la zona horaria efectiva en el controller sin una consulta extra allí.
        return await _db.Sucursales
            .AsNoTracking()
            .Include(s => s.Tenant)
            .FirstAsync(s => s.Id == sucursal.Id);
    }

    /// <summary>
    /// Detecta si el fallo de EF corresponde a una violación de índice único en PostgreSQL.
    /// </summary>
    private static bool EsViolacionDeUnicidad(DbUpdateException ex)
        => ex.InnerException is PostgresException { SqlState: UniqueViolationSqlState };
}

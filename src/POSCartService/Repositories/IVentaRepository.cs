using POSCartService.Models;

namespace POSCartService.Repositories;

/// <summary>
/// Repositorio de acceso a datos para Venta (MS-5).
/// Todas las operaciones están filtradas por tenant mediante el QueryFilter
/// global configurado en <see cref="Data.PosCartDbContext"/>.
/// </summary>
public interface IVentaRepository
{
    /// <summary>
    /// Obtiene una venta por su Id, incluyendo sus items, pagos y anulación.
    /// Devuelve null si no existe o no pertenece al tenant actual.
    /// </summary>
    Task<Venta?> GetByIdAsync(Guid ventaId);

    /// <summary>
    /// Obtiene todas las ventas de un turno de caja.
    /// </summary>
    Task<IReadOnlyList<Venta>> GetByTurnoAsync(Guid turnoId);

    /// <summary>
    /// Obtiene todas las ventas de un cajero para un rango de fechas dado.
    /// </summary>
    Task<IReadOnlyList<Venta>> GetByCajeroAsync(Guid cajeroId, DateTime desde, DateTime hasta);

    /// <summary>
    /// Persiste una nueva venta (con sus items incluidos) en la base de datos.
    /// </summary>
    Task<Venta> CrearAsync(Venta venta);

    /// <summary>
    /// Actualiza el estado y los totales de una venta existente.
    /// </summary>
    Task<Venta> ActualizarAsync(Venta venta);
}

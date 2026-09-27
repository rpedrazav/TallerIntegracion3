using POSCartService.Models;

namespace POSCartService.Repositories;

/// <summary>
/// Repositorio de acceso a datos para ItemVenta (MS-5).
/// Los items heredan el filtro multi-tenant de su Venta padre
/// (configurado en <see cref="Data.PosCartDbContext"/> vía QueryFilter).
/// </summary>
public interface IItemVentaRepository
{
    /// <summary>
    /// Obtiene todos los items de una venta específica.
    /// </summary>
    Task<IReadOnlyList<ItemVenta>> GetByVentaAsync(Guid ventaId);

    /// <summary>
    /// Obtiene un item específico por su Id.
    /// Devuelve null si no existe o no pertenece al tenant actual.
    /// </summary>
    Task<ItemVenta?> GetByIdAsync(Guid itemId);

    /// <summary>
    /// Persiste un nuevo item en la base de datos.
    /// </summary>
    Task<ItemVenta> CrearAsync(ItemVenta item);

    /// <summary>
    /// Persiste una lista de items en una sola operación (bulk insert).
    /// </summary>
    Task<IReadOnlyList<ItemVenta>> CrearRangoAsync(IEnumerable<ItemVenta> items);

    /// <summary>
    /// Elimina un item de la base de datos.
    /// </summary>
    Task EliminarAsync(ItemVenta item);
}

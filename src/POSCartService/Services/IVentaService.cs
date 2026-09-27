using POSCartService.Models;

namespace POSCartService.Services;

/// <summary>
/// Servicio de dominio para la gestión del ciclo de vida de una Venta (MS-5).
/// Coordina el repositorio de ventas, el de items y las reglas de negocio
/// (RN-01: subtotal + IVA = total, RN-04: venta solo en turno activo, etc.).
/// </summary>
public interface IVentaService
{
    /// <summary>
    /// Obtiene una venta por su Id (con items, pagos y anulación incluidos).
    /// </summary>
    Task<Venta?> GetByIdAsync(Guid ventaId);

    /// <summary>
    /// Obtiene todas las ventas de un turno de caja.
    /// </summary>
    Task<IReadOnlyList<Venta>> GetByTurnoAsync(Guid turnoId);

    /// <summary>
    /// Crea una nueva venta en estado PENDIENTE con los items dados.
    /// Calcula subtotal, impuestos y total a partir de la lista de items.
    /// </summary>
    /// <param name="turnoId">Turno de caja activo al que pertenece la venta.</param>
    /// <param name="cajeroId">Cajero que registra la venta (denormalizado).</param>
    /// <param name="sucursalId">Sucursal (denormalizado desde el Turno).</param>
    /// <param name="tenantId">Tenant actual extraído del JWT.</param>
    /// <param name="items">Lista de items del carrito (al menos 1).</param>
    /// <param name="impuestoPorcentaje">Porcentaje de IVA (0, 19 o 21).</param>
    /// <param name="metodoPago">Método de pago seleccionado.</param>
    /// <exception cref="ArgumentException">Si la lista de items está vacía.</exception>
    Task<Venta> CrearAsync(
        Guid turnoId,
        Guid cajeroId,
        Guid sucursalId,
        Guid tenantId,
        IEnumerable<ItemVenta> items,
        decimal impuestoPorcentaje,
        MetodoPagoVenta metodoPago);

    /// <summary>
    /// Marca una venta PENDIENTE como COMPLETADA.
    /// </summary>
    /// <exception cref="InvalidOperationException">Si la venta no está en estado PENDIENTE.</exception>
    Task<Venta> CompletarAsync(Guid ventaId);

    /// <summary>
    /// Marca una venta como ANULADA y registra el motivo de anulación.
    /// </summary>
    /// <exception cref="InvalidOperationException">Si la venta ya está ANULADA o CANCELADA.</exception>
    Task<Venta> AnularAsync(Guid ventaId, Guid cajeroId, string motivo);
}

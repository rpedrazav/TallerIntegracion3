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
    /// Marca una venta PENDIENTE como COMPLETADA, registra el pago en efectivo
    /// y publica el evento <c>sale.completed</c> a Kafka.
    /// </summary>
    /// <param name="ventaId">Id de la venta a completar.</param>
    /// <param name="montoRecibido">Monto entregado por el cliente (≥ venta.Total).</param>
    /// <param name="vuelto">Diferencia monto_recibido − total de la venta.</param>
    /// <exception cref="InvalidOperationException">Si la venta no está en estado PENDIENTE.</exception>
    Task<Venta> CompletarAsync(Guid ventaId, decimal montoRecibido, decimal vuelto);

    /// <summary>
    /// Marca una venta como ANULADA y registra el motivo de anulación.
    /// </summary>
    /// <exception cref="InvalidOperationException">Si la venta ya está ANULADA o CANCELADA.</exception>
    Task<Venta> AnularAsync(Guid ventaId, Guid cajeroId, string motivo);

    /// <summary>
    /// Agrega un ítem a una venta PENDIENTE y actualiza sus totales (subtotal, impuestos, total).
    /// </summary>
    /// <param name="ventaId">Id de la venta / carrito.</param>
    /// <param name="item">Instancia del ítem a agregar.</param>
    /// <param name="subtotal">Nuevo subtotal consolidado de la venta.</param>
    /// <param name="impuestos">Nuevo total de impuestos (IVA) calculado.</param>
    /// <param name="total">Nuevo total general de la venta.</param>
    /// <exception cref="KeyNotFoundException">Si la venta no existe.</exception>
    /// <exception cref="InvalidOperationException">Si la venta no está en estado PENDIENTE.</exception>
    Task<(Venta Venta, ItemVenta Item)> AgregarItemAsync(
        Guid ventaId,
        ItemVenta item,
        decimal subtotal,
        decimal impuestos,
        decimal total);

    /// <summary>
    /// Modifica la cantidad de un ítem existente en una venta PENDIENTE y actualiza los totales consolidados.
    /// </summary>
    /// <param name="ventaId">Id de la venta / carrito.</param>
    /// <param name="itemId">Id del ítem a modificar.</param>
    /// <param name="nuevaCantidad">Nueva cantidad a asignar.</param>
    /// <param name="nuevoPesoKg">Nuevo peso (opcional, balanza).</param>
    /// <param name="nuevoSubtotalItem">Subtotal recalculado del ítem (precioUnitario * nuevaCantidad).</param>
    /// <param name="nuevoSubtotalVenta">Subtotal consolidado recalculado de la venta.</param>
    /// <param name="nuevosImpuestosVenta">Total de impuestos consolidado recalculado de la venta.</param>
    /// <param name="nuevoTotalVenta">Total consolidado recalculado de la venta.</param>
    /// <exception cref="KeyNotFoundException">Si la venta o el ítem no existen.</exception>
    /// <exception cref="InvalidOperationException">Si la venta no está en estado PENDIENTE.</exception>
    Task<(Venta Venta, ItemVenta Item)> ModificarCantidadItemAsync(
        Guid ventaId,
        Guid itemId,
        decimal nuevaCantidad,
        decimal? nuevoPesoKg,
        decimal nuevoSubtotalItem,
        decimal nuevoSubtotalVenta,
        decimal nuevosImpuestosVenta,
        decimal nuevoTotalVenta);

    /// <summary>
    /// Elimina un ítem de una venta PENDIENTE y actualiza los totales consolidados (subtotal, impuestos, total).
    /// </summary>
    /// <param name="ventaId">Id de la venta / carrito.</param>
    /// <param name="itemId">Id del ítem a eliminar.</param>
    /// <param name="nuevoSubtotalVenta">Subtotal consolidado recalculado sin el ítem eliminado.</param>
    /// <param name="nuevosImpuestosVenta">Total de impuestos recalculado sin el ítem eliminado.</param>
    /// <param name="nuevoTotalVenta">Total consolidado recalculado sin el ítem eliminado.</param>
    /// <returns>La venta actualizada y el ítem que fue eliminado.</returns>
    /// <exception cref="KeyNotFoundException">Si la venta o el ítem no existen.</exception>
    /// <exception cref="InvalidOperationException">Si la venta no está en estado PENDIENTE.</exception>
    Task<(Venta Venta, ItemVenta ItemEliminado)> EliminarItemAsync(
        Guid ventaId,
        Guid itemId,
        decimal nuevoSubtotalVenta,
        decimal nuevosImpuestosVenta,
        decimal nuevoTotalVenta);
}



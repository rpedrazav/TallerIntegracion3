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

    /// <summary>
    /// Completa una venta y guarda su pago y evento outbox en una sola transacción.
    /// </summary>
    Task<Venta> CompletarConOutboxAsync(
        Guid ventaId,
        Pago pago,
        OutboxMessage outboxMessage);

    /// <summary>
    /// Completa una venta TARJETA marcando el IntentoPago como APROBADO,
    /// creando el Pago y el OutboxMessage en una sola transacción atómica.
    /// </summary>
    Task<Venta> CompletarTarjetaConOutboxAsync(
        Guid ventaId,
        Pago pago,
        IntentoPago intentoPago,
        OutboxMessage outboxMessage);

    /// <summary>
    /// Persiste un IntentoPago sin modificar el estado de la venta.
    /// Se usa ante rechazo o timeout de la pasarela.
    /// </summary>
    Task RegistrarIntentoPagoAsync(IntentoPago intentoPago);
}

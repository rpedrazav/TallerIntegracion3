using Confluent.Kafka;
using POSCartService.Messaging;

namespace POSCartService.Services;

/// <summary>
/// Contrato para el servicio de publicación de mensajes Kafka desde MS-5.
/// </summary>
public interface IKafkaProducerService
{
    /// <summary>
    /// Publica un mensaje en cualquier topic Kafka.
    /// Método genérico reutilizable por cualquier evento del servicio.
    /// </summary>
    /// <param name="topico">Nombre del topic Kafka de destino (ej. "sale.completed").</param>
    /// <param name="mensaje">Payload del mensaje serializado como string (normalmente JSON).</param>
    /// <param name="clave">
    /// Clave de particionado del mensaje. Si es <c>null</c> Kafka asigna partición por round-robin.
    /// Usar <c>tenant_id</c> para garantizar orden de eventos por tenant.
    /// </param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task PublicarAsync(
        string topico,
        string mensaje,
        string? clave = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publica el evento <c>sale.completed</c> en el topic Kafka correspondiente.
    /// La clave del mensaje es <c>tenant_id</c> para garantizar orden por tenant en la misma partición.
    /// Internamente delega a <see cref="PublicarAsync"/>.
    /// </summary>
    /// <param name="evento">Payload del evento construido en VentaService.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task PublicarSaleCompletedAsync(SaleCompletedEvent evento, CancellationToken cancellationToken = default);
}

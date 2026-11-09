namespace WarehouseInventoryService.Messaging;

/// <summary>
/// Contrato del servicio que procesa eventos sale.completed con idempotencia.
/// Extraído de KafkaConsumerService para facilitar tests unitarios sin Kafka real.
/// </summary>
public interface ISaleEventProcessor
{
    /// <summary>
    /// Procesa un evento sale.completed: verifica idempotencia por event_id,
    /// descuenta stock por cada item y registra el evento como procesado.
    /// </summary>
    /// <param name="payload">JSON del evento sale.completed.</param>
    /// <returns>
    /// true si el evento fue procesado exitosamente.
    /// false si el evento ya había sido procesado (duplicado descartado).
    /// </returns>
    bool ProcesarEvento(string payload);
}

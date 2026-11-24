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
    /// Resultado que distingue procesamiento, duplicado y errores permanentes.
    /// </returns>
    SaleEventProcessingResult ProcesarEvento(string? payload);
}

public enum SaleEventProcessingStatus
{
    Processed,
    Duplicate,
    InvalidPayload,
    Unrecoverable,
    TransientFailure
}

public sealed record SaleEventProcessingResult(
    SaleEventProcessingStatus Status,
    string? Error = null)
{
    public bool CommitOffset => Status is
        SaleEventProcessingStatus.Processed or
        SaleEventProcessingStatus.Duplicate or
        SaleEventProcessingStatus.InvalidPayload or
        SaleEventProcessingStatus.Unrecoverable;

    public bool Retry => !CommitOffset;

    public bool SendToErrorTopic => Status is
        SaleEventProcessingStatus.InvalidPayload or
        SaleEventProcessingStatus.Unrecoverable;
}

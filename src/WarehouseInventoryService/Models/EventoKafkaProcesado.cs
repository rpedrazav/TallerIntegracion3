using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseInventoryService.Models;

/// <summary>
/// Registro de idempotencia para eventos Kafka ya procesados por este microservicio.
/// event_id corresponde al VentaId del evento sale.completed (ver KafkaConsumerService).
/// </summary>
[Table("eventos_kafka_procesados")]
public class EventoKafkaProcesado
{
    [Key]
    [Column("event_id")]
    public Guid EventId { get; set; }

    [Column("procesado_at")]
    public DateTime ProcesadoAt { get; set; } = DateTime.UtcNow;
}

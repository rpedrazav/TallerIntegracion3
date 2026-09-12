using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnalyticsNotificationService.Models;

[Table("historial_envios")]
public class HistorialEnvio
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    [Column("tipo")]
    public string Tipo { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("destinatario")]
    public string Destinatario { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("estado")]
    public string Estado { get; set; } = string.Empty;

    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("tenant_id")]
    public Guid TenantId { get; set; }
}

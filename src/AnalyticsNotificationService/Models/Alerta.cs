using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnalyticsNotificationService.Models;

[Table("alertas")]
public class Alerta
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    [Column("tipo")]
    public string Tipo { get; set; } = string.Empty;

    [Column("producto_id")]
    public Guid? ProductoId { get; set; }

    [Column("valor_actual", TypeName = "decimal(18,4)")]
    public decimal ValorActual { get; set; }

    [Column("umbral", TypeName = "decimal(18,4)")]
    public decimal Umbral { get; set; }

    [Required]
    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("estado")]
    public string Estado { get; set; } = "ACTIVA";
}

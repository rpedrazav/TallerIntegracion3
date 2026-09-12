using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseInventoryService.Models;

[Table("lotes")]
public class Lote
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("producto_id")]
    public Guid ProductoId { get; set; }

    [Required]
    [Column("fecha_vencimiento")]
    public DateOnly FechaVencimiento { get; set; }

    [Column("cantidad", TypeName = "decimal(12,4)")]
    public decimal Cantidad { get; set; }

    [MaxLength(100)]
    [Column("ubicacion")]
    public string Ubicacion { get; set; } = string.Empty;

    [Required]
    [Column("tenant_id")]
    public Guid TenantId { get; set; }
}

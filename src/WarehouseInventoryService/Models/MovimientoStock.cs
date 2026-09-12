using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseInventoryService.Models;

[Table("movimientos_stock")]
public class MovimientoStock
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("producto_id")]
    public Guid ProductoId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("tipo")]
    public string Tipo { get; set; } = string.Empty;

    [Column("cantidad", TypeName = "decimal(12,4)")]
    public decimal Cantidad { get; set; }

    [MaxLength(255)]
    [Column("motivo")]
    public string Motivo { get; set; } = string.Empty;

    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("tenant_id")]
    public Guid TenantId { get; set; }
}

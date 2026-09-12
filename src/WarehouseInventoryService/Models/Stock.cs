using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseInventoryService.Models;

[Table("stock")]
public class Stock
{
    [Required]
    [Column("producto_id")]
    public Guid ProductoId { get; set; }

    [Required]
    [Column("sucursal_id")]
    public Guid SucursalId { get; set; }

    [Column("cantidad_actual", TypeName = "decimal(12,4)")]
    public decimal CantidadActual { get; set; }

    [Column("stock_minimo", TypeName = "decimal(12,4)")]
    public decimal StockMinimo { get; set; }

    [Required]
    [Column("tenant_id")]
    public Guid TenantId { get; set; }
}

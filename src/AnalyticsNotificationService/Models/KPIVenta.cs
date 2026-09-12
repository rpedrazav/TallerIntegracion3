using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnalyticsNotificationService.Models;

[Table("kpi_ventas")]
public class KPIVenta
{
    [Required]
    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [Required]
    [Column("sucursal_id")]
    public Guid SucursalId { get; set; }

    [Required]
    [Column("fecha")]
    public DateOnly Fecha { get; set; }

    [Column("total_ventas", TypeName = "decimal(18,2)")]
    public decimal TotalVentas { get; set; }

    [Column("cant_transacciones")]
    public int CantTransacciones { get; set; }

    [Column("ticket_promedio", TypeName = "decimal(18,2)")]
    public decimal TicketPromedio { get; set; }
}

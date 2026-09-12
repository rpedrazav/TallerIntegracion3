using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CatalogPricingService.Models;

[Table("precios")]
public class Precio
{
    [Required]
    [Column("producto_id")]
    public Guid ProductoId { get; set; }

    [Required]
    [Column("sucursal_id")]
    public Guid SucursalId { get; set; }

    [Column("precio_local", TypeName = "decimal(12,4)")]
    public decimal PrecioLocal { get; set; }

    [Required]
    [MaxLength(3)]
    [Column("moneda_fx")]
    public string MonedaFx { get; set; } = string.Empty;

    [Column("precio_fx", TypeName = "decimal(12,4)")]
    public decimal PrecioFx { get; set; }

    [Required]
    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    // Propiedad de navegacion hacia producto
    [ForeignKey(nameof(ProductoId))]
    public Producto? Producto { get; set; }
}

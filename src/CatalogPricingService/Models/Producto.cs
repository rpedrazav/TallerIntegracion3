using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CatalogPricingService.Models;

[Table("productos")]
public class Producto
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [Required]
    [MaxLength(150)]
    [Column("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Column("descripcion")]
    public string? Descripcion { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("codigo_barras")]
    public string CodigoBarras { get; set; } = string.Empty;

    [Column("codigo_qr_url")]
    public string? CodigoQrUrl { get; set; }

    [Column("categoria_id")]
    public Guid? CategoriaId { get; set; }

    [Required]
    [Column("uom_base_id")]
    public Guid UomBaseId { get; set; }

    [Required]
    [Column("precio_base", TypeName = "decimal(12,4)")]
    public decimal PrecioBase { get; set; }

    [Required]
    [Column("es_peso_variable")]
    public bool EsPesoVariable { get; set; }

    [Required]
    [Column("is_active")]
    public bool IsActive { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}

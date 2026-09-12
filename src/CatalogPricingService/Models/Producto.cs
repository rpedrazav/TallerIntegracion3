using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CatalogPricingService.Models;

[Table("productos")]
public class Producto
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    [Column("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("codigo_barras")]
    public string CodigoBarras { get; set; } = string.Empty;

    [Column("precio", TypeName = "decimal(12,4)")]
    public decimal Precio { get; set; }

    [Required]
    [Column("categoria_id")]
    public Guid CategoriaId { get; set; }

    [Required]
    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [Column("activo")]
    public bool Activo { get; set; } = true;

    // Propiedades de navegacion
    [ForeignKey(nameof(CategoriaId))]
    public Categoria? Categoria { get; set; }

    public ICollection<Precio> Precios { get; set; } = new List<Precio>();
}

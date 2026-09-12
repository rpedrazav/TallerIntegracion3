using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CatalogPricingService.Models;

[Table("categorias")]
public class Categoria
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    [Column("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    // Propiedad de navegacion hacia productos
    public ICollection<Producto> Productos { get; set; } = new List<Producto>();
}

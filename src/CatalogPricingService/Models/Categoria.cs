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

    /// <summary>FK auto-referenciado para categorias jerarquicas (nullable = raiz).</summary>
    [Column("parent_id")]
    public Guid? ParentId { get; set; }

    /// <summary>Nivel jerarquico: 0 = raiz, 1 = subcategoria, etc.</summary>
    [Column("level")]
    public int Level { get; set; } = 0;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navegacion: categoria padre (auto-referenciado)
    public Categoria? Parent { get; set; }

    // Navegacion: subcategorias hijas
    public ICollection<Categoria> Children { get; set; } = new List<Categoria>();

    // Propiedad de navegacion hacia productos
    public ICollection<Producto> Productos { get; set; } = new List<Producto>();
}


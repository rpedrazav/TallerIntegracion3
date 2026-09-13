using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoyaltyCustomerService.Models;

[Table("tiers_membresia")]
public class TierMembresia
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    [Column("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Column("puntos_minimos")]
    public int PuntosMinimos { get; set; } = 0;

    [Column("beneficios_json", TypeName = "jsonb")]
    public string? BeneficiosJson { get; set; }

    [Required]
    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    // Propiedades de navegación
    public ICollection<ClienteAfiliado> ClientesAfiliados { get; set; } = new List<ClienteAfiliado>();
}

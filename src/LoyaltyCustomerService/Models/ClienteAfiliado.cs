using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoyaltyCustomerService.Models;

[Table("clientes_afiliados")]
public class ClienteAfiliado
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(150)]
    [Column("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [MaxLength(30)]
    [Column("telefono")]
    public string? Telefono { get; set; }

    [MaxLength(255)]
    [Column("qr_code")]
    public string? QrCode { get; set; }

    [Column("tier_id")]
    public Guid? TierId { get; set; }

    [Required]
    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    // Propiedades de navegación
    [ForeignKey(nameof(TierId))]
    public TierMembresia? Tier { get; set; }

    public SaldoPuntos? SaldoPuntos { get; set; }

    public ICollection<MovimientoPuntos> Movimientos { get; set; } = new List<MovimientoPuntos>();
}

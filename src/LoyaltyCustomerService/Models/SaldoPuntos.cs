using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoyaltyCustomerService.Models;

[Table("saldos_puntos")]
public class SaldoPuntos
{
    [Key]
    [Column("cliente_id")]
    public Guid ClienteId { get; set; }

    [Column("puntos_disponibles")]
    public int PuntosDisponibles { get; set; } = 0;

    [Column("puntos_historicos")]
    public int PuntosHistoricos { get; set; } = 0;

    // Propiedad de navegación (1 a 1 con ClienteAfiliado)
    [ForeignKey(nameof(ClienteId))]
    public ClienteAfiliado? Cliente { get; set; }
}

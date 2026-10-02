using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace POSCartService.DTOs;

/// <summary>
/// Cuerpo del request para POST /ventas/{id}/cobrar.
/// El cajero informa el monto recibido del cliente; el sistema verifica que
/// sea suficiente para cubrir el total de la venta antes de completarla.
/// </summary>
public sealed record CobrarVentaRequest
{
    /// <summary>
    /// Monto entregado por el cliente. Debe ser mayor o igual al total de la venta.
    /// </summary>
    [JsonPropertyName("monto_recibido")]
    [Required]
    [Range(typeof(decimal), "0.01", "999999999.99", ErrorMessage = "monto_recibido debe ser mayor a 0.")]
    public decimal MontoRecibido { get; init; }
}

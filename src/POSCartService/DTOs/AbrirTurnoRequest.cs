using System.Text.Json.Serialization;

namespace POSCartService.DTOs;

public sealed record AbrirTurnoRequest
{
    [JsonPropertyName("sucursal_id")]
    public Guid SucursalId { get; init; }

    [JsonPropertyName("monto_fondo_inicial")]
    public decimal MontoFondoInicial { get; init; }
}
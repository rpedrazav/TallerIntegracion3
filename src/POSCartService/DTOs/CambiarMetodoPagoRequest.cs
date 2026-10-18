using System.Text.Json.Serialization;

namespace POSCartService.DTOs;

public sealed record CambiarMetodoPagoRequest(
    [property: JsonPropertyName("metodo_pago")] string MetodoPago);

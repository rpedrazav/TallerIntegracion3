using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace POSCartService.DTOs;

/// <summary>
/// Cuerpo del request para POST /turnos/cuadre.
/// El cajero declara el efectivo que contó físicamente en la caja.
/// </summary>
public sealed record CuadreRequest
{
    /// <summary>
    /// Monto de efectivo contado físicamente por el cajero al hacer el cuadre.
    /// </summary>
    [JsonPropertyName("monto_declarado")]
    [Required]
    [Range(typeof(decimal), "0", "999999999.99", ErrorMessage = "monto_declarado no puede ser negativo.")]
    public decimal MontoDeclarado { get; init; }
}

/// <summary>
/// Respuesta de POST /turnos/cuadre.
/// Muestra el efectivo esperado (suma de pagos EFECTIVO del turno), el monto declarado
/// por el cajero y la diferencia entre ambos.
/// </summary>
public sealed record CuadreResponse
{
    [JsonPropertyName("turno_id")]
    public Guid TurnoId { get; init; }

    /// <summary>
    /// Suma de Pago.Monto de todas las ventas COMPLETADAS con MetodoPago EFECTIVO del turno activo.
    /// </summary>
    [JsonPropertyName("efectivo_esperado")]
    public decimal EfectivoEsperado { get; init; }

    /// <summary>
    /// Monto de efectivo contado físicamente por el cajero (parámetro del request).
    /// </summary>
    [JsonPropertyName("monto_declarado")]
    public decimal MontoDeclarado { get; init; }

    /// <summary>
    /// Diferencia = monto_declarado − efectivo_esperado.
    /// Positivo → sobrante; negativo → faltante.
    /// </summary>
    [JsonPropertyName("diferencia")]
    public decimal Diferencia { get; init; }
}

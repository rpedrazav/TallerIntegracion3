namespace POSCartService.Models;

/// <summary>
/// Estado de un intento de cobro con tarjeta contra la pasarela simulada o real.
/// </summary>
public enum EstadoIntentoPago
{
    /// <summary>El intento fue creado pero la pasarela aún no respondió (o hubo timeout incierto).</summary>
    PENDIENTE,

    /// <summary>La pasarela aprobó el cobro. La venta puede completarse.</summary>
    APROBADO,

    /// <summary>La pasarela rechazó el cobro (resultado de negocio, HTTP 200 con status DECLINED).</summary>
    RECHAZADO,

    /// <summary>Falló la comunicación con la pasarela (timeout, error HTTP, payload inválido).</summary>
    FALLIDO
}

/// <summary>
/// Intento de cobro con tarjeta para una venta PENDIENTE.
/// <para>
/// Cumple RN-02 y PCI DSS: no almacena ni transporta PAN, CVV, PIN, fecha de expiración ni datos de tarjeta.
/// Solo persiste referencias opacas devueltas por la pasarela.
/// </para>
/// <para>
/// El índice único (TenantId, IdempotencyKey) evita reservas duplicadas.
/// La reserva se confirma antes de llamar a la pasarela (S2-035); el índice por
/// sí solo no garantiza ausencia de cargos externos duplicados.
/// </para>
/// </summary>
public class IntentoPago
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Identificador del tenant. Requerido para aislamiento multi-tenant (RN-01)
    /// y para el índice único de idempotencia (TenantId, IdempotencyKey).
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>FK hacia la Venta PENDIENTE asociada a este intento.</summary>
    public Guid VentaId { get; set; }

    /// <summary>
    /// Clave de idempotencia generada por MS-5 al iniciar el intento.
    /// Coincide con el request_id enviado a la pasarela y el Idempotency-Key HTTP.
    /// Forma el índice único (TenantId, IdempotencyKey).
    /// </summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    public EstadoIntentoPago Estado { get; set; } = EstadoIntentoPago.PENDIENTE;

    /// <summary>Monto del cobro en la unidad principal de Moneda.</summary>
    public decimal Monto { get; set; }

    /// <summary>Código ISO 4217 de la moneda (ej. CLP).</summary>
    public string Moneda { get; set; } = string.Empty;

    /// <summary>Referencia opaca de la transacción devuelta por la pasarela (gateway_reference / RRN). Nulo si no hubo respuesta.</summary>
    public string? Referencia { get; set; }

    /// <summary>Código de autorización opaco devuelto por la pasarela. Solo presente si APROBADO.</summary>
    public string? AuthorizationCode { get; set; }

    /// <summary>Código de rechazo devuelto por la pasarela (ej. DO_NOT_HONOR). Solo presente si RECHAZADO.</summary>
    public string? DeclineCode { get; set; }

    /// <summary>Mensaje descriptivo adicional de la pasarela. Puede ser nulo.</summary>
    public string? Mensaje { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Relación de navegación
    public Venta Venta { get; set; } = null!;
}

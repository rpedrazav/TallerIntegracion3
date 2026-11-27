namespace TenantIdentityService.DTOs;

/// <summary>
/// Cuerpo del request POST /auth/login.
/// Validado por <see cref="TenantIdentityService.Validators.LoginRequestValidator"/>.
/// </summary>
public sealed record LoginRequest
{
    /// <summary>Email del usuario que intenta iniciar sesión.</summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>Contraseña en texto plano (nunca se almacena, solo se verifica con BCrypt).</summary>
    public string Password { get; init; } = string.Empty;

    /// <summary>Código del minimarket (ej: "demo"). El tenant_id del JWT sale de este código.</summary>
    public string TenantCodigo { get; init; } = string.Empty;
}

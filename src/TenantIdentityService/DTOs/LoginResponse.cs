namespace TenantIdentityService.DTOs;

/// <summary>
/// Cuerpo de la respuesta exitosa de POST /auth/login.
/// El cliente Electron guarda este objeto en electron-store para sesiones futuras.
/// </summary>
public sealed record LoginResponse
{
    /// <summary>JWT firmado listo para enviar en el header Authorization: Bearer {Token}.</summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>Fecha y hora UTC en que expira el token (por defecto 8 horas desde el login).</summary>
    public DateTime ExpiresAt { get; init; }

    /// <summary>Información básica del usuario para que el frontend muestre nombre y rol sin decodificar el JWT.</summary>
    public UsuarioInfoDto Usuario { get; init; } = new();
}

/// <summary>Subconjunto de datos del usuario incluido en la respuesta de login.</summary>
public sealed record UsuarioInfoDto
{
    public Guid   Id         { get; init; }
    public string Nombre     { get; init; } = string.Empty;
    public string Email      { get; init; } = string.Empty;
    public string ActiveRole { get; init; } = string.Empty;

    /// <summary>Todos los roles asignados al usuario (puede tener más de uno).</summary>
    public IReadOnlyList<string> Roles { get; init; } = [];
}

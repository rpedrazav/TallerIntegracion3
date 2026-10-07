namespace TenantIdentityService.DTOs;

/// <summary>
/// Datos requeridos para crear un nuevo usuario en el tenant.
/// La validación de formato y restricciones se realiza en <see cref="TenantIdentityService.Validators.CrearUsuarioDtoValidator"/>.
/// </summary>
public class CrearUsuarioDto
{
    /// <summary>Nombre completo del usuario (2–100 caracteres).</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Correo electrónico único dentro del tenant.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Contraseña en texto plano. Debe cumplir la política de contraseñas fuertes:
    /// mínimo 8 caracteres, al menos una mayúscula, una minúscula, un dígito y un carácter especial.
    /// Se almacenará hasheada con BCrypt; nunca se persiste en texto plano.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Roles iniciales opcionales a asignar al crear el usuario.</summary>
    public List<string>? Roles { get; set; }
}
using System.ComponentModel.DataAnnotations;

namespace TenantIdentityService.DTOs;

public class CrearUsuarioDto
{
    [Required]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
}
using System.ComponentModel.DataAnnotations;

namespace TenantIdentityService.DTOs;

public class ActualizarUsuarioDto
{
    [Required]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
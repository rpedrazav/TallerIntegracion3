using System.ComponentModel.DataAnnotations;

namespace TenantIdentityService.DTOs;

public class ActualizarUsuarioDto
{
    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    public Guid? SucursalId { get; set; }
}

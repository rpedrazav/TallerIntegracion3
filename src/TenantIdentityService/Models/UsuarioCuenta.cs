namespace TenantIdentityService.Models;

/// <summary>Proyección editable de la tabla users existente, sin credenciales ni roles.</summary>
public class UsuarioCuenta
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? SucursalId { get; set; }
    public string Email { get; set; } = string.Empty;
}

namespace TenantIdentityService.Models;

/// <summary>
/// Tabla de unión M:N entre Usuario y Sucursal.
/// Un usuario puede estar asignado a múltiples sucursales del mismo tenant.
/// </summary>
public class UsuarioSucursal
{
    public Guid UsuarioId { get; set; }
    public Guid SucursalId { get; set; }

    // Relaciones de navegación
    public Usuario Usuario { get; set; } = null!;
    public Sucursal Sucursal { get; set; } = null!;
}

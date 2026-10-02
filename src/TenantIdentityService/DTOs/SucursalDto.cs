namespace TenantIdentityService.DTOs;

/// <summary>
/// Respuesta de GET /sucursales.
/// <see cref="ZonaHoraria"/> es la zona efectiva de la sucursal:
/// <c>Sucursal.ZonaHoraria ?? Tenant.ZonaHoraria</c>. Si la sucursal no define zona propia,
/// hereda la del tenant.
/// </summary>
public class SucursalDto
{
    public Guid Id { get; init; }

    public Guid TenantId { get; init; }

    public string Nombre { get; init; } = string.Empty;

    public string Direccion { get; init; } = string.Empty;

    /// <summary>
    /// Permite al consumidor distinguir sucursales inactivas. El endpoint las lista todas.
    /// </summary>
    public bool Activa { get; init; }

    /// <summary>Zona horaria IANA efectiva de la sucursal.</summary>
    public string ZonaHoraria { get; init; } = string.Empty;
}

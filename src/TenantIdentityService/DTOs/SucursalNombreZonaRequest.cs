namespace TenantIdentityService.DTOs;

/// <summary>Campos editables comunes de una sucursal.</summary>
public abstract class SucursalNombreZonaRequest
{
    /// <summary>Nombre único por tenant, comparado sin distinguir mayúsculas.</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Zona horaria IANA propia. Si es nula o vacía, hereda la zona del tenant.</summary>
    public string? ZonaHoraria { get; set; }
}

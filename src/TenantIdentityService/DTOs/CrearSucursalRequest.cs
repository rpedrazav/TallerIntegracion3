namespace TenantIdentityService.DTOs;

/// <summary>
/// Request de POST /sucursales.
/// El <c>tenant_id</c> NO se recibe aquí: se toma exclusivamente del claim del JWT.
/// La serialización usa camelCase por defecto (System.Text.Json), igual que el resto de MS-1.
/// </summary>
public class CrearSucursalRequest
{
    /// <summary>Nombre de la sucursal. Único por tenant, comparado sin distinguir mayúsculas.</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Dirección física de la sucursal.</summary>
    public string Direccion { get; set; } = string.Empty;

    /// <summary>
    /// Zona horaria IANA específica de la sucursal (ej: "America/Santiago",
    /// "America/Argentina/Buenos_Aires").
    /// Opcional: si es <c>null</c> o vacía, la sucursal hereda <c>Tenant.ZonaHoraria</c>
    /// y la zona efectiva se resuelve como <c>Sucursal.ZonaHoraria ?? Tenant.ZonaHoraria</c>.
    /// </summary>
    public string? ZonaHoraria { get; set; }
}

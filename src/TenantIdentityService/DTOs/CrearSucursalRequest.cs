namespace TenantIdentityService.DTOs;

/// <summary>
/// Request de POST /sucursales.
/// El <c>tenant_id</c> NO se recibe aquí: se toma exclusivamente del claim del JWT.
/// La serialización usa camelCase por defecto (System.Text.Json), igual que el resto de MS-1.
/// </summary>
public class CrearSucursalRequest : SucursalNombreZonaRequest
{
    /// <summary>Dirección física de la sucursal.</summary>
    public string Direccion { get; set; } = string.Empty;
}

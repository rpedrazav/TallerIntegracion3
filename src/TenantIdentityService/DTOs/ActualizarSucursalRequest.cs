namespace TenantIdentityService.DTOs;

/// <summary>Request de PUT /sucursales/{id}; el tenant se obtiene exclusivamente del JWT.</summary>
public sealed class ActualizarSucursalRequest : SucursalNombreZonaRequest;

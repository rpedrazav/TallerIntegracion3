using TenantIdentityService.Models;

namespace TenantIdentityService.DTOs;

/// <summary>Minimarket en el listado de SUPER_ADMIN.</summary>
public sealed record TenantDto(Guid Id, string Nombre, string? Codigo, bool Activo, DateTime CreadoEn)
{
    public static TenantDto From(Tenant t) => new(t.Id, t.Nombre, t.Codigo, t.Activo, t.CreadoEn);
}

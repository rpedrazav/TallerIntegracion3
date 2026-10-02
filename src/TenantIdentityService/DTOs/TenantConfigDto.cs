namespace TenantIdentityService.DTOs;

public sealed record TenantConfigDto
{
    public string Pais { get; init; } = string.Empty;

    public string Moneda { get; init; } = string.Empty;

    public string Idioma { get; init; } = string.Empty;

    public string ZonaHoraria { get; init; } = string.Empty;

    public decimal PorcentajeIva { get; init; }
}
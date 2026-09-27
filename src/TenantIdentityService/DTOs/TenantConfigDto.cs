namespace TenantIdentityService.DTOs;

public sealed record TenantConfigDto
{
    public Guid TenantId { get; init; }

    public string Pais { get; init; } = string.Empty;

    public string Moneda { get; init; } = string.Empty;

    public string Idioma { get; init; } = string.Empty;

    public string ZonaHoraria { get; init; } = string.Empty;

    public decimal PorcentajeIva { get; init; }

    public decimal UmbralStockMinimo { get; init; }

    public decimal UmbralVariacionFx { get; init; }

    public bool SeparacionFuncionesOc { get; init; }
}
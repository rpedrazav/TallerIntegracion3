namespace TenantIdentityService.DTOs;

public class ActualizarTenantConfigDto
{
    public string Pais { get; set; } = string.Empty;

    public string Moneda { get; set; } = string.Empty;

    public string Idioma { get; set; } = string.Empty;

    public decimal PorcentajeIva { get; set; }

    public string ZonaHoraria { get; set; } = string.Empty;
}
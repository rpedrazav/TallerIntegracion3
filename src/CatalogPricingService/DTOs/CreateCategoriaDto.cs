namespace CatalogPricingService.DTOs;

/// <summary>
/// Payload para crear una nueva categoria.
/// TenantId se inyecta desde el JWT en el controlador, no viene del cliente.
/// </summary>
public class CreateCategoriaDto
{
    /// <summary>Nombre de la categoria (obligatorio, max 100 caracteres).</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Id de la categoria padre (opcional).
    /// Si es null, se crea como categoria raiz (Level = 0).
    /// </summary>
    public Guid? ParentId { get; set; }

    // Inyectado en el controlador desde el JWT
    public Guid TenantId { get; set; }
}

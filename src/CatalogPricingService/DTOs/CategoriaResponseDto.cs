namespace CatalogPricingService.DTOs;

/// <summary>
/// Representacion de respuesta de una Categoria.
/// No expone el TenantId al cliente (informacion interna de aislamiento).
/// </summary>
public class CategoriaResponseDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public int Level { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

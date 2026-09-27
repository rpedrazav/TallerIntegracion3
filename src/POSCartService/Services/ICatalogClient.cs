using POSCartService.DTOs;

namespace POSCartService.Services;

/// <summary>
/// Cliente para interactuar con MS-3 (Catalog &amp; Pricing Service).
/// Permite consultar información actualizada de productos como precio actual y estado.
/// </summary>
public interface ICatalogClient
{
    /// <summary>
    /// Consulta el producto por su Id en MS-3 (GET /api/products/{id}).
    /// </summary>
    Task<ProductoCatalogDto?> GetProductAsync(
        Guid productId,
        string? bearerToken = null,
        CancellationToken cancellationToken = default);
}

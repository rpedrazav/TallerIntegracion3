namespace SupplyChainService.Models;

public class Proveedor
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }

    public string Nombre { get; set; } = string.Empty;

    /// <summary>Código ISO de 2 letras (ej. CL, AR, US).</summary>
    public string PaisOrigen { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Telefono { get; set; }

    /// <summary>Código ISO de 3 letras (ej. USD, CLP).</summary>
    public string MonedaFacturacion { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Relaciones de navegación (misma base de datos: MS-6)
    public ICollection<OrdenCompra> OrdenesCompra { get; set; } = new List<OrdenCompra>();
}
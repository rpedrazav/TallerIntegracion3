namespace SupplyChainService.Models;

public enum EstadoOrdenCompra
{
    BORRADOR,
    ENVIADA,
    EN_TRANSITO,
    RECIBIDA,
    CANCELADA
}

public class OrdenCompra
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }

    public Guid ProveedorId { get; set; }

    /// <summary>Referencia a la Sucursal en Tenant & Identity Service (MS-1). Sin navigation property: bases de datos separadas.</summary>
    public Guid SucursalDestinoId { get; set; }

    /// <summary>Usuario (ADMIN) que creó la orden de compra. Sin navigation property: base de datos externa (MS-1).</summary>
    public Guid CreadorId { get; set; }

    /// <summary>Usuario (ADMIN) que aprobó la orden de compra. Null hasta que se aprueba. RN-13: debe ser distinto de CreadorId.</summary>
    public Guid? AprobadorId { get; set; }

    public EstadoOrdenCompra Estado { get; set; } = EstadoOrdenCompra.BORRADOR;

    /// <summary>Código ISO de 3 letras de la moneda de la orden.</summary>
    public string Moneda { get; set; } = string.Empty;

    public decimal SubtotalProveedor { get; set; }

    public decimal Flete { get; set; } = 0;

    public decimal Seguro { get; set; } = 0;

    public decimal Aranceles { get; set; } = 0;

    /// <summary>Costo Landed = SubtotalProveedor + Flete + Seguro + Aranceles (RN-09).</summary>
    public decimal CostoLanded { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Relaciones de navegación (misma base de datos: MS-6)
    public Proveedor Proveedor { get; set; } = null!;
    public ICollection<OrdenCompraItem> Items { get; set; } = new List<OrdenCompraItem>();
    public ICollection<Envio> Envios { get; set; } = new List<Envio>();
}
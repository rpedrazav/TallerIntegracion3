namespace POSCartService.Models;

/// <summary>
/// Estado del turno de caja, según ERD (M3). 
/// NOTA: el diagrama de estados de Daniel (DA4) define además EN_USO,
/// PENDIENTE_CIERRE y EN_REVISION como estados intermedios de negocio.
/// Pendiente de alinear con el equipo si esos estados se persisten o son solo transitorios en memoria.
/// </summary>
public enum EstadoTurno
{
    ABIERTO,
    CERRADO
}

public class Turno
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }

    /// <summary>Referencia al Usuario (Cajero) en Tenant & Identity Service (MS-1). Sin navigation property: bases de datos separadas.</summary>
    public Guid CajeroId { get; set; }

    /// <summary>Referencia a la Sucursal en Tenant & Identity Service (MS-1). Sin navigation property: bases de datos separadas.</summary>
    public Guid SucursalId { get; set; }

    public decimal MontoApertura { get; set; }

    public decimal? MontoCierre { get; set; }

    public EstadoTurno Estado { get; set; } = EstadoTurno.ABIERTO;

    public DateTime AbiertoAt { get; set; } = DateTime.UtcNow;

    public DateTime? CerradoAt { get; set; }

    // Relaciones de navegación (misma base de datos: MS-5)
    public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
}
namespace SupplyChainService.Models;

public enum EstadoEnvio
{
    EN_PREPARACION,
    EN_TRANSITO,
    ADUANAS,
    ENTREGADO
}

public class Envio
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrdenCompraId { get; set; }

    public string? NumeroTracking { get; set; }

    public string EmpresaLogistica { get; set; } = string.Empty;

    public DateOnly? FechaSalida { get; set; }

    public DateOnly? FechaEstimadaLlegada { get; set; }

    public DateOnly? FechaLlegadaReal { get; set; }

    public EstadoEnvio Estado { get; set; } = EstadoEnvio.EN_PREPARACION;

    // Relación de navegación (misma base de datos: MS-6)
    public OrdenCompra OrdenCompra { get; set; } = null!;
}
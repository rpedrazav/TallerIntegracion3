namespace POSCartService.Exceptions;

/// <summary>
/// Se lanza cuando se intenta abrir un turno para un cajero que ya tiene
/// uno activo (ABIERTO o EN_USO). RN-06: solo un turno activo por cajero.
/// </summary>
public class TurnoYaAbiertoException : Exception
{
    public Guid CajeroId { get; }
    public Guid TurnoActivoId { get; }

    public TurnoYaAbiertoException(Guid cajeroId, Guid turnoActivoId)
        : base($"El cajero {cajeroId} ya tiene un turno activo ({turnoActivoId}). Debe cerrarlo antes de abrir uno nuevo.")
    {
        CajeroId = cajeroId;
        TurnoActivoId = turnoActivoId;
    }
}

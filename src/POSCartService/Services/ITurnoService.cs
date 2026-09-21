using POSCartService.Models;

namespace POSCartService.Services;

public interface ITurnoService
{
    /// <summary>
    /// Abre un nuevo turno de caja para un cajero, validando que no tenga
    /// otro turno activo (RN-06).
    /// </summary>
    /// <exception cref="Exceptions.TurnoYaAbiertoException">
    /// Si el cajero ya tiene un turno ABIERTO o EN_USO.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Si montoFondoInicial es negativo.
    /// </exception>
    Task<Turno> Abrir(Guid cajeroId, Guid tenantId, Guid sucursalId, decimal montoFondoInicial);
}

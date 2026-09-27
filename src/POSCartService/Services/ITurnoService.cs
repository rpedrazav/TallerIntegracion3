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

    /// <summary>
    /// Cierra el turno activo del cajero dentro del tenant.
    /// </summary>
    Task<Turno?> Cerrar(Guid cajeroId, Guid tenantId);

    /// <summary>
    /// Obtiene el turno activo del cajero dentro del tenant.
    /// </summary>
    Task<Turno?> GetActivo(Guid cajeroId, Guid tenantId);
}

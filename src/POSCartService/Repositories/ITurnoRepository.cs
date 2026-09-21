using POSCartService.Models;

namespace POSCartService.Repositories;

/// <summary>
/// Repositorio de acceso a datos para Turno de Caja (MS-5).
/// </summary>
public interface ITurnoRepository
{
    /// <summary>
    /// Obtiene el turno activo de un cajero dentro de un tenant.
    /// </summary>
    Task<Turno?> GetActivo(Guid cajeroId, Guid tenantId);

    /// <summary>
    /// Registra la apertura de un nuevo turno de caja.
    /// </summary>
    Task<Turno> Abrir(Turno turno);

    /// <summary>
    /// Cierra un turno existente.
    /// </summary>
    Task<Turno?> Cerrar(Guid turnoId);
}
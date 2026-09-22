using POSCartService.Exceptions;
using POSCartService.Models;
using POSCartService.Repositories;

namespace POSCartService.Services;

public class TurnoService : ITurnoService
{
    private readonly ITurnoRepository _turnoRepository;

    public TurnoService(ITurnoRepository turnoRepository)
    {
        _turnoRepository = turnoRepository;
    }

    public async Task<Turno> Abrir(Guid cajeroId, Guid tenantId, Guid sucursalId, decimal montoFondoInicial)
    {
        if (montoFondoInicial < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(montoFondoInicial),
                "El monto de fondo inicial no puede ser negativo.");
        }

        var turnoActivo = await _turnoRepository.GetActivo(cajeroId, tenantId);
        if (turnoActivo is not null)
        {
            throw new TurnoYaAbiertoException(cajeroId, turnoActivo.Id);
        }

        var turno = new Turno
        {
            TenantId = tenantId,
            CajeroId = cajeroId,
            SucursalId = sucursalId,
            MontoApertura = montoFondoInicial,
            Estado = EstadoTurno.ABIERTO
        };

        return await _turnoRepository.Abrir(turno);
    }

    public async Task<Turno?> Cerrar(Guid cajeroId, Guid tenantId)
    {
        var turnoActivo = await _turnoRepository.GetActivo(cajeroId, tenantId);
        if (turnoActivo is null)
        {
            return null;
        }

        return await _turnoRepository.Cerrar(turnoActivo.Id);
    }
}

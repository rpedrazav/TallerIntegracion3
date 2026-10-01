using Microsoft.EntityFrameworkCore;
using POSCartService.Data;
using POSCartService.Models;

namespace POSCartService.Repositories;

public class TurnoRepository : ITurnoRepository
{
    private readonly PosCartDbContext _context;

    public TurnoRepository(PosCartDbContext context)
    {
        _context = context;
    }

    public async Task<Turno?> GetActivo(Guid cajeroId, Guid tenantId)
    {
        return await _context.Turnos
            .Where(t => t.CajeroId == cajeroId
                     && t.TenantId == tenantId
                     && (t.Estado == EstadoTurno.ABIERTO || t.Estado == EstadoTurno.EN_USO))
            .FirstOrDefaultAsync();
    }

    public async Task<Turno> Abrir(Turno turno)
    {
        _context.Turnos.Add(turno);
        await _context.SaveChangesAsync();
        return turno;
    }

    public async Task<Turno?> Cerrar(Guid turnoId)
    {
        var turno = await _context.Turnos.FindAsync(turnoId);
        if (turno is null)
        {
            return null;
        }

        turno.Estado = EstadoTurno.CERRADO;
        turno.CerradoAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return turno;
    }
    public async Task<decimal> GetEfectivoTurnoAsync(Guid turnoId)
    {
        // Suma Pago.Monto de ventas COMPLETADAS con método EFECTIVO del turno.
        // IgnoreQueryFilters: el filtro multi-tenant ya fue validado en el controller
        // al verificar el turno activo del cajero autenticado.
        return await _context.Pagos
            .IgnoreQueryFilters()
            .Where(p => p.Venta.TurnoId == turnoId
                     && p.Venta.Estado   == EstadoVenta.COMPLETADA
                     && p.Metodo         == MetodoPago.EFECTIVO)
            .SumAsync(p => p.Monto);
    }
}
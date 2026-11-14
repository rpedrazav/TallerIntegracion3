using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using POSCartService.Data;
using POSCartService.DTOs;
using POSCartService.Exceptions;
using POSCartService.Models;

namespace POSCartService.Services;

/// <summary>Reserva durable → petición externa sin locks → confirmación local atómica.</summary>
public sealed class ReversaTarjetaService(PosCartDbContext db, IReversalGatewayClient gateway,
    IHttpContextAccessor accessor, IConfiguration config, ILogger<ReversaTarjetaService> logger)
{
    public async Task<Venta> AnularAsync(Guid ventaId, string motivo)
    {
        var (tenant, admin) = RequireAdmin();
        motivo = motivo.Trim();
        if (motivo.Length is < 1 or > 500) throw new ArgumentException("Motivo inválido.");
        var scenario = config["PasarelaPago:ReversalScenarioId"] ?? "sim-reversal-approved-v1";
        if (scenario is not ("sim-reversal-approved-v1" or "sim-reversal-declined-v1" or "sim-reversal-timeout-v1"))
            throw new InvalidOperationException("Escenario de reversa no configurado correctamente.");
        var (reversa, nueva) = await ReservarAsync(ventaId, tenant, admin, motivo);
        if (nueva)
        {
            var result = await LlamarPasarelaAsync(async () => await gateway.ReverseAsync(Request(reversa) with { ScenarioId = scenario }), reversa);
            if (result is not null) await ConfirmarAsync(reversa, result);
        }
        var actual = await BuscarAsync(ventaId, tenant);
        if (actual.Estado == EstadoReversa.RECHAZADA)
            throw new AnulacionConflictException("REVERSA_RECHAZADA", "La reversa fue rechazada. El pago no se ha reembolsado; consulte el caso.");
        if (actual.Estado == EstadoReversa.PENDIENTE)
            throw new AnulacionConflictException("REVERSA_PENDIENTE", "Resultado incierto. Consulte la reversa existente sin solicitar otra devolución.");
        return await db.Ventas.AsNoTracking().Include(v => v.Anulacion).SingleAsync(v => v.Id == ventaId && v.TenantId == tenant);
    }

    public async Task<ReversaPago> ConsultarAsync(Guid ventaId)
    {
        var (tenant, _) = RequireAdmin();
        var reversa = await BuscarAsync(ventaId, tenant);
        if (reversa.Estado == EstadoReversa.PENDIENTE)
        {
            var result = await LlamarPasarelaAsync(() => gateway.GetStatusAsync(tenant, reversa.Id), reversa);
            if (result is not null) await ConfirmarAsync(reversa, result);
        }
        return await BuscarAsync(ventaId, tenant);
    }

    private async Task<ReversaPago> BuscarAsync(Guid ventaId, Guid tenant) =>
        await db.ReversasPago.AsNoTracking().SingleOrDefaultAsync(r => r.VentaId == ventaId && r.TenantId == tenant)
        ?? throw new KeyNotFoundException("Reversa no encontrada.");

    private (Guid Tenant, Guid Admin) RequireAdmin()
    {
        var user = accessor.HttpContext?.User;
        var identity = user?.FindFirst("cajero_id")?.Value ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user?.FindFirst("sub")?.Value;
        if (user?.Identity?.IsAuthenticated != true || !user.IsInRole("ADMIN")
            || !Guid.TryParse(identity, out var admin) || admin == Guid.Empty
            || !Guid.TryParse(user.FindFirst("tenant_id")?.Value, out var tenant) || tenant == Guid.Empty || db.CurrentTenantId != tenant)
            throw new UnauthorizedAccessException("Se requiere ADMIN del tenant autenticado.");
        return (tenant, admin);
    }

    private async Task<(ReversaPago Reversa, bool Nueva)> ReservarAsync(Guid ventaId, Guid tenant, Guid admin, string motivo)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        var turnoId = await db.Ventas.Where(v => v.Id == ventaId && v.TenantId == tenant).Select(v => (Guid?)v.TurnoId).SingleOrDefaultAsync()
            ?? throw new KeyNotFoundException("Venta no encontrada.");
        // Mismo orden de locks que la anulación de efectivo. No se mantienen durante HTTP.
        var turnos = await db.Turnos.FromSqlInterpolated(
            $"SELECT * FROM \"Turnos\" WHERE \"Id\" = {turnoId} AND \"TenantId\" = {tenant} FOR UPDATE").AsNoTracking().ToListAsync();
        var ventas = await db.Ventas.FromSqlInterpolated(
            $"SELECT * FROM \"Ventas\" WHERE \"Id\" = {ventaId} AND \"TenantId\" = {tenant} FOR UPDATE").AsNoTracking().ToListAsync();
        var venta = ventas.Single();
        var existente = await db.ReversasPago.AsNoTracking().SingleOrDefaultAsync(r => r.VentaId == ventaId && r.TenantId == tenant);
        if (existente is not null) return (existente, false); // Nunca reenviar un POST, ni tras un rechazo.
        if (venta.Estado != EstadoVenta.COMPLETADA)
            throw new AnulacionConflictException("ESTADO_NO_ANULABLE", "Solo se puede reversar una venta completada.");
        var intentos = await db.IntentosPago.AsNoTracking().Where(i => i.VentaId == ventaId && i.TenantId == tenant).ToListAsync();
        if (intentos.Any(i => i.Estado is EstadoIntentoPago.PENDIENTE or EstadoIntentoPago.FALLIDO))
            throw new AnulacionConflictException("PAGO_INCIERTO", "Resuelva el cobro antes de solicitar la reversa.");
        var pagos = await db.Pagos.AsNoTracking().Where(p => p.VentaId == ventaId && p.Venta.TenantId == tenant).ToListAsync();
        var aprobados = intentos.Where(i => i.Estado == EstadoIntentoPago.APROBADO).ToList();
        if (venta.MetodoPago != MetodoPagoVenta.TARJETA || pagos.Count != 1 || pagos[0].Metodo != MetodoPago.TARJETA
            || pagos[0].Monto != venta.Total || venta.Total <= 0 || pagos[0].Vuelto != 0
            || string.IsNullOrWhiteSpace(pagos[0].Referencia) || pagos[0].Referencia!.Length > 200
            || aprobados.Count != 1 || aprobados[0].Referencia != pagos[0].Referencia || aprobados[0].Monto != venta.Total
            || aprobados[0].Moneda.Length != 3 || aprobados[0].Moneda.Any(c => c is < 'A' or > 'Z'))
            throw new AnulacionConflictException("PAGO_INCONSISTENTE", "Se requiere pago aprobado con referencia, monto y moneda coincidentes.");
        if (turnos.SingleOrDefault()?.Estado != EstadoTurno.ABIERTO)
            throw new AnulacionConflictException("TURNO_NO_ANULABLE", "El turno original debe estar abierto al iniciar la reversa.");
        var reversa = new ReversaPago { TenantId = tenant, VentaId = ventaId, ReferenciaPago = pagos[0].Referencia!,
            Monto = venta.Total, Moneda = aprobados[0].Moneda, AutorizadoPor = admin, Motivo = motivo };
        db.ReversasPago.Add(reversa);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        db.Entry(reversa).State = EntityState.Detached;
        return (reversa, true);
    }

    private async Task<ReversalResponse?> LlamarPasarelaAsync(Func<Task<ReversalResponse?>> call, ReversaPago reversa)
    {
        try
        {
            var result = await call();
            if (result is not null) ReversalGatewayClient.Validate(result, Request(reversa));
            return result;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException or ExternalServiceException)
        {
            // Un error técnico no es un rechazo. No registrar cuerpos ni datos externos arbitrarios.
            logger.LogWarning("Reversa incierta TenantId={TenantId} VentaId={VentaId} ReversaId={ReversaId} ErrorType={ErrorType}",
                reversa.TenantId, reversa.VentaId, reversa.Id, ex.GetType().Name);
            return null;
        }
    }

    private async Task ConfirmarAsync(ReversaPago snapshot, ReversalResponse result)
    {
        ReversalGatewayClient.Validate(result, Request(snapshot));
        await using var tx = await db.Database.BeginTransactionAsync();
        var ventas = await db.Ventas.FromSqlInterpolated(
            $"SELECT * FROM \"Ventas\" WHERE \"Id\" = {snapshot.VentaId} AND \"TenantId\" = {snapshot.TenantId} FOR UPDATE").ToListAsync();
        var venta = ventas.Single();
        var reversa = await db.ReversasPago.SingleAsync(r => r.Id == snapshot.Id && r.TenantId == snapshot.TenantId);
        if (reversa.Estado != EstadoReversa.PENDIENTE) return; // Otra consulta ya confirmó el resultado.
        if (result.Status == "REVERSED")
        {
            if (venta.Estado != EstadoVenta.COMPLETADA)
                throw new AnulacionConflictException("ESTADO_NO_ANULABLE", "Reversa externa confirmada; el estado local requiere revisión.");
            venta.Estado = EstadoVenta.ANULADA;
            venta.Anulacion = new Anulacion { VentaId = venta.Id, Motivo = reversa.Motivo, AutorizadoPor = reversa.AutorizadoPor };
            db.Anulaciones.Add(venta.Anulacion);
            reversa.Estado = EstadoReversa.APROBADA;
            reversa.ReferenciaReversa = result.ReversalReference;
        }
        else
        {
            reversa.Estado = EstadoReversa.RECHAZADA;
            reversa.CodigoRechazo = result.DeclineCode;
        }
        reversa.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    private static ReversalRequest Request(ReversaPago r) => new(r.Id, r.TenantId, r.VentaId,
        r.ReferenciaPago, r.Monto, r.Moneda, "sim-reversal-approved-v1");
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POSCartService.DTOs;
using POSCartService.Exceptions;
using POSCartService.Models;
using POSCartService.Services;

namespace POSCartService.Controllers;

[ApiController]
[Route("turnos")]
[Route("api/turnos")]
[Authorize]
public sealed class TurnosController : ControllerBase
{
    private readonly ITurnoService _turnoService;

    public TurnosController(ITurnoService turnoService)
    {
        _turnoService = turnoService;
    }

    [HttpPost("abrir")]
    [ProducesResponseType(typeof(Turno), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Abrir([FromBody] AbrirTurnoRequest request)
    {
        var cajeroClaim = User.FindFirst("cajero_id")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        var tenantClaim = User.FindFirst("tenant_id")?.Value;

        if (!Guid.TryParse(cajeroClaim, out var cajeroId)
            || !Guid.TryParse(tenantClaim, out var tenantId))
        {
            return Unauthorized(new { error = "Token inválido: faltan cajero_id/sub o tenant_id" });
        }

        try
        {
            var turno = await _turnoService.Abrir(
                cajeroId,
                tenantId,
                request.SucursalId,
                request.MontoFondoInicial);

            return Ok(turno);
        }
        catch (TurnoYaAbiertoException exception)
        {
            return Conflict(new
            {
                error = "El cajero ya tiene un turno activo.",
                cajeroId = exception.CajeroId,
                turnoActivoId = exception.TurnoActivoId
            });
        }
    }

    [HttpGet("activo")]
    [ProducesResponseType(typeof(Turno), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activo()
    {
        var cajeroClaim = User.FindFirst("cajero_id")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        var tenantClaim = User.FindFirst("tenant_id")?.Value;

        if (!Guid.TryParse(cajeroClaim, out var cajeroId)
            || !Guid.TryParse(tenantClaim, out var tenantId))
        {
            return Unauthorized(new { error = "Token inválido: faltan cajero_id/sub o tenant_id" });
        }

        var turno = await _turnoService.GetActivo(cajeroId, tenantId);
        return turno is null
            ? NotFound(new { error = "No existe un turno abierto para el cajero autenticado." })
            : Ok(turno);
    }

    [HttpPost("cerrar")]
    [ProducesResponseType(typeof(Turno), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cerrar()
    {
        var cajeroClaim = User.FindFirst("cajero_id")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        var tenantClaim = User.FindFirst("tenant_id")?.Value;

        if (!Guid.TryParse(cajeroClaim, out var cajeroId)
            || !Guid.TryParse(tenantClaim, out var tenantId))
        {
            return Unauthorized(new { error = "Token inválido: faltan cajero_id/sub o tenant_id" });
        }

        var turno = await _turnoService.Cerrar(cajeroId, tenantId);
        return turno is null
            ? NotFound(new { error = "No existe un turno abierto para el cajero autenticado." })
            : Ok(turno);
    }

    // ── POST /turnos/cuadre ────────────────────────────────────────────────────

    /// <summary>
    /// Calcula el cuadre de caja del turno activo del cajero autenticado.
    /// Suma todos los pagos en EFECTIVO de ventas COMPLETADAS del turno
    /// y lo compara con el monto declarado por el cajero.
    /// </summary>
    /// <remarks>
    /// - efectivo_esperado = suma de Pago.Monto (Metodo=EFECTIVO) de ventas COMPLETADAS del turno activo.
    /// - diferencia = monto_declarado − efectivo_esperado (positivo = sobrante, negativo = faltante).
    /// - No cierra el turno; es sólo consulta de cuadre.
    /// </remarks>
    /// <response code="200">Cuadre calculado correctamente.</response>
    /// <response code="400">monto_declarado inválido (negativo).</response>
    /// <response code="401">Token JWT ausente o inválido.</response>
    /// <response code="404">El cajero no tiene un turno ABIERTO.</response>
    [HttpPost("cuadre")]
    [ProducesResponseType(typeof(CuadreResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cuadre([FromBody] CuadreRequest request)
    {
        // 1. Extraer claims del JWT
        var cajeroClaim = User.FindFirst("cajero_id")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        var tenantClaim = User.FindFirst("tenant_id")?.Value;

        if (!Guid.TryParse(cajeroClaim, out var cajeroId)
            || !Guid.TryParse(tenantClaim, out var tenantId))
        {
            return Unauthorized(new { error = "Token inválido: faltan cajero_id/sub o tenant_id." });
        }

        // 2. Obtener turno activo y sumar efectivo esperado
        Turno turno;
        decimal efectivoEsperado;
        try
        {
            (turno, efectivoEsperado) = await _turnoService.CalcularCuadreAsync(cajeroId, tenantId);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "El cajero no tiene un turno ABIERTO." });
        }

        // 3. Calcular diferencia (positivo = sobrante, negativo = faltante)
        var diferencia = Math.Round(request.MontoDeclarado - efectivoEsperado, 2);

        return Ok(new CuadreResponse
        {
            TurnoId          = turno.Id,
            EfectivoEsperado = efectivoEsperado,
            MontoDeclarado   = request.MontoDeclarado,
            Diferencia       = diferencia
        });
    }
}
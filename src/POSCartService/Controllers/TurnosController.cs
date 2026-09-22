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
}
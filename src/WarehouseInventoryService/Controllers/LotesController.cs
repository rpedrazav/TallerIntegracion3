using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseInventoryService.Services;

namespace WarehouseInventoryService.Controllers;

[ApiController]
[Route("lotes")]
[Route("api/lotes")]
[Authorize(Roles = "ADMIN,REPONEDOR")]
public sealed class LotesController : ControllerBase
{
    private readonly LoteService _lotes;
    private readonly RecepcionValidator _validator;

    public LotesController(LoteService lotes, RecepcionValidator validator)
    {
        _lotes = lotes;
        _validator = validator;
    }

    public sealed record LoteResponse(
        Guid Id,
        Guid ProductoId,
        Guid SucursalId,
        decimal Cantidad,
        DateOnly FechaVencimiento,
        string Ubicacion);

    [HttpGet]
    [ProducesResponseType<List<LoteResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> List(
        [FromQuery] int? porVencer,
        [FromQuery(Name = "sucursal_id")] Guid sucursalId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst("tenant_id")?.Value, out var tenantId))
            return Unauthorized();
        if (porVencer is null or < 0 or > 365 || sucursalId == Guid.Empty)
            return BadRequest(new { message = "porVencer debe estar entre 0 y 365 y la sucursal es obligatoria." });

        var bearer = Request.Headers.Authorization.ToString();
        if (!bearer.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Unauthorized();
        var failure = await _validator.ValidateSucursalAsync(sucursalId, bearer, cancellationToken);
        if (failure is not null)
            return StatusCode(failure.StatusCode, new { message = failure.Message });

        var lotes = await _lotes.ListarPorVencerAsync(tenantId, sucursalId, porVencer.Value, cancellationToken);
        return Ok(lotes.Select(l => new LoteResponse(
            l.Id,
            l.ProductoId,
            l.SucursalId,
            l.Cantidad,
            l.FechaVencimiento,
            l.Ubicacion)).ToList());
    }
}

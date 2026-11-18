using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseInventoryService.Services;

namespace WarehouseInventoryService.Controllers;

[ApiController]
[Route("recepciones")]
[Authorize(Roles = "ADMIN,REPONEDOR")]
public sealed class RecepcionesController : ControllerBase
{
    private readonly RecepcionService _recepciones;

    public RecepcionesController(RecepcionService recepciones)
    {
        _recepciones = recepciones;
    }

    public sealed record RecepcionRequest(
        Guid ProductoId,
        Guid SucursalId,
        decimal Cantidad,
        DateOnly FechaVencimiento,
        string? Ubicacion);

    public sealed record RecepcionResponse(
        Guid Id,
        Guid ProductoId,
        Guid SucursalId,
        decimal Cantidad,
        DateOnly FechaVencimiento,
        string Ubicacion);

    [HttpPost]
    [ProducesResponseType<RecepcionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] RecepcionRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst("tenant_id")?.Value, out var tenantId))
            return Unauthorized();

        var lote = await _recepciones.RegistrarAsync(
            tenantId,
            request.ProductoId,
            request.SucursalId,
            request.Cantidad,
            request.FechaVencimiento,
            request.Ubicacion,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new RecepcionResponse(
            lote.Id,
            lote.ProductoId,
            lote.SucursalId,
            lote.Cantidad,
            lote.FechaVencimiento,
            lote.Ubicacion));
    }
}

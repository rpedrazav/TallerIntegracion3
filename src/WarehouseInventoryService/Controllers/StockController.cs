using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseInventoryService.Repositories;

namespace WarehouseInventoryService.Controllers;

[ApiController]
[Route("stock")]
[Authorize]
public sealed class StockController : ControllerBase
{
    private readonly IStockRepository _stockRepository;

    public StockController(IStockRepository stockRepository)
    {
        _stockRepository = stockRepository;
    }

    [HttpGet("{productId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByProducto(
        Guid productId,
        [FromQuery(Name = "sucursal_id")] Guid sucursalId)
    {
        var tenantClaim = User.FindFirst("tenant_id")?.Value;
        if (!Guid.TryParse(tenantClaim, out var tenantId))
            return Unauthorized(new { error = "Token inválido: falta tenant_id" });

        var stock = await _stockRepository.GetByProducto(productId, sucursalId, tenantId);
        return stock is null
            ? NotFound(new { error = "No existe stock para ese producto en la sucursal indicada." })
            : Ok(stock);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseInventoryService.Data;
using WarehouseInventoryService.Models;
using WarehouseInventoryService.Repositories;

namespace WarehouseInventoryService.Controllers;

/// <summary>
/// Controller de Stock para MS-4 (WarehouseInventoryService).
/// Expone consulta de stock por producto y sucursal, aislado por tenant_id del JWT.
/// TI3-255: Revisado y finalizado — respuestas HTTP correctas, documentación Swagger,
/// endpoint de listado general agregado.
/// </summary>
[ApiController]
[Route("stock")]
[Authorize]
public sealed class StockController : ControllerBase
{
    private readonly IStockRepository _stockRepository;
    private readonly WarehouseDbContext _context;
    private readonly ILogger<StockController> _logger;

    public StockController(
        IStockRepository stockRepository,
        WarehouseDbContext context,
        ILogger<StockController> logger)
    {
        _stockRepository = stockRepository;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene el stock de un producto en una sucursal específica.
    /// </summary>
    /// <param name="productId">GUID del producto.</param>
    /// <param name="sucursalId">GUID de la sucursal (query param).</param>
    /// <returns>Objeto Stock con cantidad_actual y stock_minimo.</returns>
    [HttpGet("{productId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByProducto(
        Guid productId,
        [FromQuery(Name = "sucursal_id")] Guid sucursalId)
    {
        var tenantId = GetTenantId();
        if (tenantId is null) return Unauthorized(new { error = "Token inválido: falta tenant_id" });

        if (sucursalId == Guid.Empty)
            return BadRequest(new { error = "El parámetro sucursal_id es obligatorio." });

        var stock = await _stockRepository.GetByProducto(productId, sucursalId, tenantId.Value);
        return stock is null
            ? NotFound(new { error = "No existe stock para ese producto en la sucursal indicada." })
            : Ok(stock);
    }

    /// <summary>
    /// Lista todos los registros de stock del tenant (filtrado automático por tenant_id del JWT).
    /// Opcionalmente filtra por sucursal_id.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(
        [FromQuery(Name = "sucursal_id")] Guid? sucursalId)
    {
        var tenantId = GetTenantId();
        if (tenantId is null) return Unauthorized(new { error = "Token inválido: falta tenant_id" });

        _context.CurrentTenantId = tenantId.Value;

        IQueryable<Stock> query = _context.Stocks;

        if (sucursalId.HasValue && sucursalId.Value != Guid.Empty)
        {
            query = query.Where(s => s.SucursalId == sucursalId.Value);
        }

        var stocks = await query.ToListAsync();

        return Ok(stocks);
    }

    /// <summary>
    /// Extrae el tenant_id del JWT del usuario autenticado.
    /// </summary>
    private Guid? GetTenantId()
    {
        var tenantClaim = User.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(tenantClaim, out var tenantId) ? tenantId : null;
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseInventoryService.Data;
using WarehouseInventoryService.Models;
using WarehouseInventoryService.Repositories;
using System.Net.Http.Headers;
using System.Text.Json;

namespace WarehouseInventoryService.Controllers;

[ApiController]
[Route("stock")]
[Route("api/stock")]
[Authorize]
public sealed class StockController : ControllerBase
{
    private readonly IStockRepository _stockRepository;
    private readonly WarehouseDbContext _db;
    private readonly IHttpClientFactory _httpClients;
    private readonly IConfiguration _configuration;

    public StockController(IStockRepository stockRepository, WarehouseDbContext db, IHttpClientFactory httpClients, IConfiguration configuration)
    {
        _stockRepository = stockRepository;
        _db = db;
        _httpClients = httpClients;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery(Name = "sucursal_id")] Guid? sucursalId)
    {
        if (!Guid.TryParse(User.FindFirst("tenant_id")?.Value, out var tenantId)) return Unauthorized();
        var query = _db.Stocks.AsNoTracking().Where(s => s.TenantId == tenantId);
        if (sucursalId.HasValue) query = query.Where(s => s.SucursalId == sucursalId.Value);
        return Ok(await query.OrderBy(s => s.SucursalId).ThenBy(s => s.ProductoId).ToListAsync());
    }

    public sealed record AjusteStockRequest(Guid ProductoId, Guid SucursalId, decimal CantidadActual, decimal? StockMinimo, string Motivo);

    [HttpPost("ajuste")]
    [Authorize(Roles = "ADMIN,REPONEDOR")]
    public async Task<IActionResult> Ajustar([FromBody] AjusteStockRequest request)
    {
        if (!Guid.TryParse(User.FindFirst("tenant_id")?.Value, out var tenantId)) return Unauthorized();
        if (request.ProductoId == Guid.Empty || request.SucursalId == Guid.Empty ||
            request.CantidadActual < 0 || request.CantidadActual > 99_999_999 ||
            decimal.Round(request.CantidadActual, 4) != request.CantidadActual ||
            request.StockMinimo is < 0 or > 99_999_999 ||
            (request.StockMinimo.HasValue && decimal.Round(request.StockMinimo.Value, 4) != request.StockMinimo.Value) ||
            string.IsNullOrWhiteSpace(request.Motivo) || request.Motivo.Trim().Length > 180)
            return BadRequest(new { message = "Producto, sucursal, cantidad, mínimo o motivo inválidos." });

        var bearer = Request.Headers.Authorization.ToString();
        if (!bearer.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return Unauthorized();
        try
        {
            var client = _httpClients.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            using var productRequest = new HttpRequestMessage(HttpMethod.Get,
                $"{_configuration["Services:CatalogUrl"]?.TrimEnd('/') ?? "http://localhost:5203"}/products/{request.ProductoId}");
            productRequest.Headers.Authorization = AuthenticationHeaderValue.Parse(bearer);
            using var productResponse = await client.SendAsync(productRequest);
            if (productResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                return BadRequest(new { message = "El producto no pertenece al catálogo de este tenant." });
            if (!productResponse.IsSuccessStatusCode)
                return StatusCode(503, new { message = "No se pudo validar el producto." });

            using var branchRequest = new HttpRequestMessage(HttpMethod.Get,
                $"{_configuration["Services:IdentityUrl"]?.TrimEnd('/') ?? "http://localhost:5124"}/sucursales");
            branchRequest.Headers.Authorization = AuthenticationHeaderValue.Parse(bearer);
            using var branchResponse = await client.SendAsync(branchRequest);
            if (!branchResponse.IsSuccessStatusCode)
                return StatusCode(503, new { message = "No se pudo validar la sucursal." });
            using var branches = JsonDocument.Parse(await branchResponse.Content.ReadAsStringAsync());
            var validBranch = branches.RootElement.EnumerateArray().Any(branch =>
                branch.TryGetProperty("id", out var id) && id.GetGuid() == request.SucursalId &&
                branch.TryGetProperty("activa", out var active) && active.GetBoolean());
            if (!validBranch)
                return BadRequest(new { message = "La sucursal no existe o no está activa en este tenant." });
        }
        catch (HttpRequestException)
        {
            return StatusCode(503, new { message = "No se pudo contactar identidad o catálogo." });
        }
        catch (TaskCanceledException)
        {
            return StatusCode(503, new { message = "La validación de identidad o catálogo agotó el tiempo." });
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var stock = await _db.Stocks.SingleOrDefaultAsync(s => s.TenantId == tenantId && s.ProductoId == request.ProductoId && s.SucursalId == request.SucursalId);
        var anterior = stock?.CantidadActual ?? 0;
        if (stock is null)
        {
            stock = new Stock { TenantId = tenantId, ProductoId = request.ProductoId, SucursalId = request.SucursalId };
            _db.Stocks.Add(stock);
        }
        stock.CantidadActual = request.CantidadActual;
        if (request.StockMinimo.HasValue) stock.StockMinimo = request.StockMinimo.Value;
        _db.Movimientos.Add(new MovimientoStock
        {
            TenantId = tenantId, ProductoId = request.ProductoId, Tipo = "AJUSTE",
            Cantidad = request.CantidadActual - anterior,
            Motivo = $"Sucursal {request.SucursalId}: {request.Motivo.Trim()}",
        });
        try
        {
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Ok(stock);
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "El stock cambió durante el ajuste. Vuelve a consultar." });
        }
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

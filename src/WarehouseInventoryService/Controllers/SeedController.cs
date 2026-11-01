using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseInventoryService.Data;

namespace WarehouseInventoryService.Controllers;

/// <summary>
/// TI3-258: Controlador de seed de datos para MS-4 (WarehouseInventoryService).
/// Solo disponible en entorno de desarrollo (IsDevelopment).
/// Permite cargar datos iniciales de stock a través de un endpoint HTTP.
/// </summary>
[ApiController]
[Route("seed")]
public sealed class SeedController : ControllerBase
{
    private readonly WarehouseDbContext _context;
    private readonly ILogger<SeedController> _logger;

    public SeedController(WarehouseDbContext context, ILogger<SeedController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Carga el seed de datos en la base de datos.
    /// Idempotente: si los datos del tenant demo ya existen, no se duplican.
    /// Solo disponible en modo Development.
    /// </summary>
    /// <returns>Resumen del seed ejecutado con cantidad de registros.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult RunSeed()
    {
        var tenantId = SeedData.DemoTenantId;

        // Verificar si ya existen datos del seed
        var existeAntes = _context.Stocks
            .IgnoreQueryFilters()
            .Any(s => s.TenantId == tenantId);

        if (existeAntes)
        {
            var registrosExistentes = _context.Stocks
                .IgnoreQueryFilters()
                .Count(s => s.TenantId == tenantId);

            _logger.LogInformation(
                "Seed ya ejecutado anteriormente. {Count} registros de stock del tenant demo ya existen.",
                registrosExistentes);

            return Conflict(new
            {
                mensaje = "El seed ya fue ejecutado previamente. Los datos no se duplican.",
                tenant_id = tenantId,
                registros_existentes = registrosExistentes
            });
        }

        // Ejecutar el seed
        SeedData.Initialize(_context);

        var registrosCreados = _context.Stocks
            .IgnoreQueryFilters()
            .Count(s => s.TenantId == tenantId);

        _logger.LogInformation(
            "Seed ejecutado exitosamente. {Count} registros de stock creados para tenant {TenantId}.",
            registrosCreados, tenantId);

        // Obtener resumen de cantidades para el response
        var resumenStock = _context.Stocks
            .IgnoreQueryFilters()
            .Where(s => s.TenantId == tenantId)
            .Select(s => new
            {
                producto_id = s.ProductoId,
                cantidad_actual = s.CantidadActual,
                stock_minimo = s.StockMinimo,
                sucursal_id = s.SucursalId
            })
            .ToList();

        return Ok(new
        {
            mensaje = "Seed ejecutado exitosamente.",
            tenant_id = tenantId,
            sucursal_id = SeedData.DemoSucursalId,
            registros_creados = registrosCreados,
            stock_minimo_default = SeedData.StockMinimoDefault,
            detalle = resumenStock
        });
    }
}

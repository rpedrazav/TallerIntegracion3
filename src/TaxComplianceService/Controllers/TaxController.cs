using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxComplianceService.Data;
using TaxComplianceService.Models;
using TaxComplianceService.Services;

namespace TaxComplianceService.Controllers;

/// <summary>
/// Controlador para operaciones tributarias y de cÃ¡lculo fiscal de MS-2.
/// Endpoint principal: POST /tax/calculate
/// </summary>
[ApiController]
[Route("tax")]
[Authorize]
public class TaxController : ControllerBase
{
    private readonly ITenantConfigClient _tenantConfigClient;
    private readonly ITaxCalculatorService _taxCalculatorService;
    private readonly TaxDbContext _db;
    private readonly ILogger<TaxController> _logger;

    public TaxController(
        ITenantConfigClient tenantConfigClient,
        ITaxCalculatorService taxCalculatorService,
        TaxDbContext db,
        ILogger<TaxController> logger)
    {
        _tenantConfigClient   = tenantConfigClient   ?? throw new ArgumentNullException(nameof(tenantConfigClient));
        _taxCalculatorService = taxCalculatorService ?? throw new ArgumentNullException(nameof(taxCalculatorService));
        _db                   = db                   ?? throw new ArgumentNullException(nameof(db));
        _logger               = logger               ?? throw new ArgumentNullException(nameof(logger));
    }

        /// <summary>
    /// Calcula impuestos (IVA) y desglose para una lista de items de una venta o carrito.
    /// Extrae el <c>tenant_id</c> del token JWT, consulta el IVA del tenant mediante MS-1,
    /// ejecuta el cálculo impositivo y retorna el desglose detallado.
    /// </summary>
    /// <remarks>
    /// **Ejemplo de Request:**
    /// 
    ///     POST /tax/calculate
    ///     {
    ///       "items": [
    ///         {
    ///           "precio": 1000,
    ///           "cantidad": 2,
    ///           "esExento": false
    ///         }
    ///       ],
    ///       "porcentajeIva": null
    ///     }
    /// 
    /// **Ejemplo de Response (200 OK):**
    /// 
    ///     {
    ///       "subtotal": 2000,
    ///       "iva": 380,
    ///       "total": 2380,
    ///       "porcentajeIva": 19,
    ///       "montoExento": 0
    ///     }
    /// </remarks>
    /// <param name="request">Lista de items con precio y cantidad, y opcionalmente porcentaje de IVA.</param>
    /// <param name="cancellationToken">Token de cancelaciÃ³n de la operaciÃ³n HTTP.</param>
        /// <returns>Desglose impositivo de la transacción.</returns>
    /// <response code="200">Retorna el desglose impositivo (Subtotal, IVA, Total).</response>
    /// <response code="400">Si los datos enviados son inválidos o la lista de items está vacía.</response>
    /// <response code="401">Si el token JWT carece del claim tenant_id válido.</response>
    /// <response code="404">Si la configuración fiscal del tenant no fue encontrada en MS-1.</response>
    [HttpPost("calculate")]
    [ProducesResponseType(typeof(TaxBreakdown), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Calculate(
        [FromBody] TaxCalculateRequest request,
        CancellationToken cancellationToken)
    {
        // 1. tenant_id ya validado por TenantMiddleware (401 si faltaba o era inválido)
        var tenantId = _db.CurrentTenantId
            ?? throw new InvalidOperationException("TenantMiddleware no estableció CurrentTenantId.");

        // 2. ValidaciÃ³n defensiva del cuerpo de la solicitud
        if (request == null || request.Items == null || request.Items.Count == 0)
        {
            return BadRequest(new { error = "La lista de items no puede estar vacÃ­a." });
        }

        _logger.LogInformation(
            "[TaxController] Calculando impuestos para tenant {TenantId} con {ItemCount} items",
            tenantId, request.Items.Count);

        // 3. Consultar configuraciÃ³n fiscal del tenant en MS-1
        var tenantConfig = await _tenantConfigClient.GetTenantConfigAsync(tenantId, cancellationToken);
        if (tenantConfig is null)
        {
            _logger.LogWarning(
                "[TaxController] No se encontrÃ³ la configuraciÃ³n fiscal para el tenant {TenantId}",
                tenantId);
            return NotFound(new { error = $"No se encontrÃ³ la configuraciÃ³n fiscal para el tenant {tenantId}." });
        }

        // 4. Determinar porcentaje de IVA aplicable (respeta override si viene en request)
        decimal porcentajeIva = request.PorcentajeIva ?? tenantConfig.PorcentajeIva;

        // 5. Ejecutar cÃ¡lculo fiscal
        var breakdown = _taxCalculatorService.Calculate(request.Items, porcentajeIva);

        _logger.LogInformation(
            "[TaxController] CÃ¡lculo exitoso para tenant {TenantId}. Subtotal: {Subtotal}, IVA ({Porcentaje}%): {Iva}, Total: {Total}",
            tenantId, breakdown.Subtotal, breakdown.PorcentajeIva, breakdown.Iva, breakdown.Total);

        return Ok(breakdown);
    }
}


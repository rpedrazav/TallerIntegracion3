using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxComplianceService.Models;
using TaxComplianceService.Services;

namespace TaxComplianceService.Controllers;

/// <summary>
/// Controlador para operaciones tributarias y de cálculo fiscal de MS-2.
/// Endpoint principal: POST /tax/calculate
/// </summary>
[ApiController]
[Route("tax")]
[Authorize]
public class TaxController : ControllerBase
{
    private readonly ITenantConfigClient _tenantConfigClient;
    private readonly ITaxCalculatorService _taxCalculatorService;
    private readonly ILogger<TaxController> _logger;

    public TaxController(
        ITenantConfigClient tenantConfigClient,
        ITaxCalculatorService taxCalculatorService,
        ILogger<TaxController> logger)
    {
        _tenantConfigClient   = tenantConfigClient   ?? throw new ArgumentNullException(nameof(tenantConfigClient));
        _taxCalculatorService = taxCalculatorService ?? throw new ArgumentNullException(nameof(taxCalculatorService));
        _logger               = logger               ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Calcula impuestos (IVA) y desglose para una lista de items de una venta o carrito.
    /// Extrae el <c>tenant_id</c> del token JWT, consulta el IVA del tenant mediante <see cref="ITenantConfigClient"/>,
    /// ejecuta el cálculo impositivo con <see cref="ITaxCalculatorService"/> y retorna el desglose detallado.
    /// </summary>
    /// <param name="request">Lista de items con precio y cantidad, y opcionalmente porcentaje de IVA.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación HTTP.</param>
    /// <returns>
    /// 200 OK con el desglose impositivo (<see cref="TaxBreakdown"/>).
    /// 400 Bad Request si los datos enviados son inválidos.
    /// 401 Unauthorized si el token JWT carece del claim tenant_id válido.
    /// 404 Not Found si la configuración fiscal del tenant no fue encontrada en MS-1.
    /// </returns>
    [HttpPost("calculate")]
    [ProducesResponseType(typeof(TaxBreakdown), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Calculate(
        [FromBody] TaxCalculateRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Extraer tenant_id del claim del JWT
        var tenantClaim = User.FindFirst("tenant_id")?.Value
            ?? User.FindFirst("TenantId")?.Value;

        if (string.IsNullOrWhiteSpace(tenantClaim) || !Guid.TryParse(tenantClaim, out var tenantId))
        {
            _logger.LogWarning("[TaxController] Solicitud rechazada: falta claim tenant_id válido en el token JWT");
            return Unauthorized(new { error = "Token inválido: falta claim tenant_id válido" });
        }

        // 2. Validación defensiva del cuerpo de la solicitud
        if (request == null || request.Items == null || request.Items.Count == 0)
        {
            return BadRequest(new { error = "La lista de items no puede estar vacía." });
        }

        _logger.LogInformation(
            "[TaxController] Calculando impuestos para tenant {TenantId} con {ItemCount} items",
            tenantId, request.Items.Count);

        // 3. Consultar configuración fiscal del tenant en MS-1
        var tenantConfig = await _tenantConfigClient.GetTenantConfigAsync(tenantId, cancellationToken);
        if (tenantConfig is null)
        {
            _logger.LogWarning(
                "[TaxController] No se encontró la configuración fiscal para el tenant {TenantId}",
                tenantId);
            return NotFound(new { error = $"No se encontró la configuración fiscal para el tenant {tenantId}." });
        }

        // 4. Determinar porcentaje de IVA aplicable (respeta override si viene en request)
        decimal porcentajeIva = request.PorcentajeIva ?? tenantConfig.PorcentajeIva;

        // 5. Ejecutar cálculo fiscal
        var breakdown = _taxCalculatorService.Calculate(request.Items, porcentajeIva);

        _logger.LogInformation(
            "[TaxController] Cálculo exitoso para tenant {TenantId}. Subtotal: {Subtotal}, IVA ({Porcentaje}%): {Iva}, Total: {Total}",
            tenantId, breakdown.Subtotal, breakdown.PorcentajeIva, breakdown.Iva, breakdown.Total);

        return Ok(breakdown);
    }
}

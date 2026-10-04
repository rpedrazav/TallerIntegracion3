using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxComplianceService.Exceptions;
using TaxComplianceService.Models;
using TaxComplianceService.Services;

namespace TaxComplianceService.Controllers;

/// <summary>
/// Controlador para la emisiÃ³n de comprobantes de venta de MS-2.
/// Requiere JWT vÃ¡lido con claims tenant_id y sub.
/// </summary>
[ApiController]
[Route("comprobantes")]
[Authorize]
public class ComprobantesController : ControllerBase
{
    private readonly IComprobanteService _comprobanteService;
    private readonly ILogger<ComprobantesController> _logger;

    public ComprobantesController(
        IComprobanteService comprobanteService,
        ILogger<ComprobantesController> logger)
    {
        _comprobanteService = comprobanteService ?? throw new ArgumentNullException(nameof(comprobanteService));
        _logger            = logger            ?? throw new ArgumentNullException(nameof(logger));
    }

        /// <summary>
    /// Emite un comprobante de venta. El correlativo se genera con la secuencia atómica por tenant.
    /// Los importes se calculan en el servidor a partir de los items y del IVA configurado en MS-1.
    /// </summary>
    /// <remarks>
    /// **Ejemplo de Request:**
    /// 
    ///     POST /comprobantes
    ///     {
    ///       "ventaId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///       "sucursalId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///       "items": [
    ///         {
    ///           "precio": 1000,
    ///           "cantidad": 2,
    ///           "esExento": false
    ///         }
    ///       ]
    ///     }
    /// 
    /// **Ejemplo de Response (201 Created):**
    /// 
    ///     {
    ///       "id": "11111111-2222-3333-4444-555555555555",
    ///       "ventaId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///       "tenantId": "aaaaaaaa-0000-0000-0000-000000000001",
    ///       "correlativo": 1004,
    ///       "fechaEmision": "2026-10-02T15:30:00Z",
    ///       "subtotal": 2000,
    ///       "iva": 380,
    ///       "total": 2380
    ///     }
    /// </remarks>
    /// <param name="request">Venta e items de la operaciÃ³n.</param>
    /// <param name="cancellationToken">Token de cancelaciÃ³n de la operaciÃ³n HTTP.</param>
        /// <returns>El comprobante emitido.</returns>
    /// <response code="201">Retorna el comprobante persistido con su correlativo único.</response>
    /// <response code="400">Si la solicitud es inválida o faltan items.</response>
    /// <response code="401">Si el token JWT es inválido.</response>
    /// <response code="404">Si no se encuentra la configuración fiscal en MS-1.</response>
    [HttpPost]
    [ProducesResponseType(typeof(Comprobante), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Emitir(
        [FromBody] CrearComprobanteRequest request,
        CancellationToken cancellationToken)
    {
        // 1. tenant_id desde el claim del JWT (mismo patrÃ³n que TaxController)
        var tenantClaim = User.FindFirst("tenant_id")?.Value
            ?? User.FindFirst("TenantId")?.Value;

        if (string.IsNullOrWhiteSpace(tenantClaim) || !Guid.TryParse(tenantClaim, out var tenantId))
        {
            _logger.LogWarning("[ComprobantesController] Solicitud rechazada: falta claim tenant_id vÃ¡lido en el token JWT");
            return Unauthorized(new { error = "Token invÃ¡lido: falta claim tenant_id vÃ¡lido" });
        }

        // 2. cajero_id desde el claim sub
        var cajeroClaim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(cajeroClaim) || !Guid.TryParse(cajeroClaim, out var cajeroId))
        {
            _logger.LogWarning("[ComprobantesController] Solicitud rechazada: falta claim sub vÃ¡lido en el token JWT");
            return Unauthorized(new { error = "Token invÃ¡lido: falta claim sub vÃ¡lido" });
        }

        // 3. ValidaciÃ³n defensiva del cuerpo
        if (request is null || request.VentaId == Guid.Empty || request.Items is null || request.Items.Count == 0)
        {
            return BadRequest(new { error = "Se requiere venta_id y una lista de items no vacÃ­a." });
        }

        _logger.LogInformation(
            "[ComprobantesController] Emitiendo comprobante para tenant {TenantId}, venta {VentaId}",
            tenantId, request.VentaId);

        // 4. Calcular importes en el servidor, generar correlativo y persistir
        Comprobante comprobante;
        try
        {
            comprobante = await _comprobanteService.EmitirAsync(request, tenantId, cajeroId, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (TenantConfigNotFoundException ex)
        {
            _logger.LogWarning(ex, "[ComprobantesController] No se pudo emitir el comprobante para el tenant {TenantId}", tenantId);
            return NotFound(new { error = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            // TenantConfigClient relanza HttpRequestException cuando MS-1 no responde
            // o devuelve un status no exitoso tras los reintentos de Polly
            _logger.LogError(ex, "[ComprobantesController] MS-1 no disponible al emitir comprobante para el tenant {TenantId}", tenantId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "MS-1 no responde",
                error   = "El servicio de identidad de tenant (MS-1) no responde o no estÃ¡ disponible."
            });
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Timeout del HttpClient hacia MS-1 (Polly ya reintentÃ³ 2 veces)
            _logger.LogError("[ComprobantesController] Timeout consultando MS-1 para el tenant {TenantId}", tenantId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "MS-1 no responde",
                error   = "El servicio de identidad de tenant (MS-1) no respondiÃ³ a tiempo."
            });
        }

        return StatusCode(StatusCodes.Status201Created, comprobante);
    }

    /// <summary>
    /// Obtiene un comprobante por su Id para reimprimirlo.
    /// El aislamiento por tenant lo garantiza el <c>HasQueryFilter</c> global de <c>TaxDbContext</c>,
    /// alimentado por <c>TenantMiddleware</c>.
    /// </summary>
    /// <param name="id">Identificador del comprobante.</param>
    /// <param name="cancellationToken">Token de cancelaciÃ³n de la operaciÃ³n HTTP.</param>
        /// <returns>El comprobante emitido.</returns>
    /// <response code="201">Retorna el comprobante persistido con su correlativo único.</response>
    /// <response code="400">Si la solicitud es inválida o faltan items.</response>
    /// <response code="401">Si el token JWT es inválido.</response>
    /// <response code="404">Si no se encuentra la configuración fiscal en MS-1.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Comprobante), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        // TenantMiddleware ya inyectÃ³ CurrentTenantId en el DbContext a partir del claim tenant_id.
        // La lectura queda aislada por tenant sin filtrar manualmente (RN-01).
        var comprobante = await _comprobanteService.GetByIdAsync(id, cancellationToken);

        if (comprobante is null)
        {
            _logger.LogInformation(
                "[ComprobantesController] Comprobante {ComprobanteId} no encontrado para el tenant del token", id);
            return NotFound(new { error = $"Comprobante {id} no encontrado." });
        }

        return Ok(comprobante);
    }
}



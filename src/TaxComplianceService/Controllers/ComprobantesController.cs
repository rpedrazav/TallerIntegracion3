using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxComplianceService.Exceptions;
using TaxComplianceService.Models;
using TaxComplianceService.Services;

namespace TaxComplianceService.Controllers;

/// <summary>
/// Controlador para la emisión de comprobantes de venta de MS-2.
/// Requiere JWT válido con claims tenant_id y sub.
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
    /// Emite un comprobante de venta. El correlativo se genera con la secuencia atómica por tenant
    /// (<c>obtener_correlativo_comprobante</c>), por lo que es único incluso con requests concurrentes.
    /// Los importes se calculan en el servidor a partir de los items y del IVA configurado en MS-1.
    /// </summary>
    /// <param name="request">Venta e items de la operación.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación HTTP.</param>
    /// <returns>
    /// 201 Created con el comprobante persistido.
    /// 400 Bad Request si la solicitud es inválida.
    /// 401 Unauthorized si el token JWT carece de los claims tenant_id o sub válidos.
    /// 404 Not Found si MS-1 no tiene configuración fiscal para el tenant.
    /// 503 Service Unavailable si MS-1 no responde.
    /// </returns>
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
        // 1. tenant_id desde el claim del JWT (mismo patrón que TaxController)
        var tenantClaim = User.FindFirst("tenant_id")?.Value
            ?? User.FindFirst("TenantId")?.Value;

        if (string.IsNullOrWhiteSpace(tenantClaim) || !Guid.TryParse(tenantClaim, out var tenantId))
        {
            _logger.LogWarning("[ComprobantesController] Solicitud rechazada: falta claim tenant_id válido en el token JWT");
            return Unauthorized(new { error = "Token inválido: falta claim tenant_id válido" });
        }

        // 2. cajero_id desde el claim sub
        var cajeroClaim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(cajeroClaim) || !Guid.TryParse(cajeroClaim, out var cajeroId))
        {
            _logger.LogWarning("[ComprobantesController] Solicitud rechazada: falta claim sub válido en el token JWT");
            return Unauthorized(new { error = "Token inválido: falta claim sub válido" });
        }

        // 3. Validación defensiva del cuerpo
        if (request is null || request.VentaId == Guid.Empty || request.Items is null || request.Items.Count == 0)
        {
            return BadRequest(new { error = "Se requiere venta_id y una lista de items no vacía." });
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
                error   = "El servicio de identidad de tenant (MS-1) no responde o no está disponible."
            });
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Timeout del HttpClient hacia MS-1 (Polly ya reintentó 2 veces)
            _logger.LogError("[ComprobantesController] Timeout consultando MS-1 para el tenant {TenantId}", tenantId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "MS-1 no responde",
                error   = "El servicio de identidad de tenant (MS-1) no respondió a tiempo."
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
    /// <param name="cancellationToken">Token de cancelación de la operación HTTP.</param>
    /// <returns>
    /// 200 OK con el comprobante.
    /// 401 Unauthorized si el JWT es inválido o no trae el claim tenant_id.
    /// 404 Not Found si el comprobante no existe o no pertenece al tenant del token.
    /// </returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Comprobante), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        // TenantMiddleware ya inyectó CurrentTenantId en el DbContext a partir del claim tenant_id.
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

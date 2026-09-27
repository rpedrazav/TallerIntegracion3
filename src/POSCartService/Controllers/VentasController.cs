using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POSCartService.DTOs;
using POSCartService.Models;
using POSCartService.Services;

namespace POSCartService.Controllers;

/// <summary>
/// Controlador para la gestión de Ventas (MS-5).
/// Requiere JWT válido con claims cajero_id (o sub) y tenant_id.
/// </summary>
[ApiController]
[Route("ventas")]
[Authorize]
public sealed class VentasController : ControllerBase
{
    private readonly IVentaService _ventaService;
    private readonly ITurnoService _turnoService;

    public VentasController(IVentaService ventaService, ITurnoService turnoService)
    {
        _ventaService = ventaService;
        _turnoService = turnoService;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private bool TryGetClaims(out Guid cajeroId, out Guid tenantId)
    {
        var cajeroClaim = User.FindFirst("cajero_id")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        var tenantClaim = User.FindFirst("tenant_id")?.Value;

        cajeroId = default;
        tenantId = default;

        return Guid.TryParse(cajeroClaim, out cajeroId)
            && Guid.TryParse(tenantClaim, out tenantId);
    }

    // ── POST /ventas ───────────────────────────────────────────────────────────

    /// <summary>
    /// Crea una nueva venta en estado PENDIENTE.
    /// Verifica que el cajero autenticado tenga un turno ABIERTO antes de crear la venta.
    /// </summary>
    /// <response code="201">Venta creada exitosamente con su breakdown de ítems.</response>
    /// <response code="400">Request inválido (ítems vacíos, precios negativos, método de pago desconocido).</response>
    /// <response code="401">Token JWT ausente o inválido.</response>
    /// <response code="409">El cajero no tiene un turno ABIERTO — debe abrir turno primero.</response>
    [HttpPost]
    [ProducesResponseType(typeof(VentaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear([FromBody] CrearVentaRequest request)
    {
        // 1. Extraer claims del JWT
        if (!TryGetClaims(out var cajeroId, out var tenantId))
            return Unauthorized(new { error = "Token inválido: faltan cajero_id/sub o tenant_id." });

        // 2. Verificar turno activo — RN-04: solo se puede vender en turno ABIERTO
        var turno = await _turnoService.GetActivo(cajeroId, tenantId);
        if (turno is null)
        {
            return Conflict(new
            {
                error   = "El cajero no tiene un turno abierto. Debe abrir un turno antes de registrar ventas.",
                cajeroId
            });
        }

        // 3. Parsear método de pago
        if (!Enum.TryParse<MetodoPagoVenta>(request.MetodoPago, ignoreCase: true, out var metodoPago))
        {
            return BadRequest(new { error = $"metodo_pago '{request.MetodoPago}' no reconocido. Use EFECTIVO, TARJETA o MIXTO." });
        }

        // 4. Mapear ítems del DTO al modelo de dominio
        // El subtotal de cada ítem = precio_unitario × cantidad (el servicio no recalcula esto)
        var itemsDominio = request.Items.Select(i => new ItemVenta
        {
            ProductoId     = i.ProductoId,
            NombreProducto = i.NombreProducto,
            Cantidad       = i.Cantidad,
            PesoKg         = i.PesoKg,
            PrecioUnitario = i.PrecioUnitario,
            Subtotal       = Math.Round(i.PrecioUnitario * i.Cantidad, 2)
        }).ToList();

        // 5. Crear la venta a través del servicio de dominio
        // IVA: el porcentaje se obtiene del tenant (por ahora 0 — será extendido cuando
        // se integre con MS-2 TaxComplianceService; el endpoint queda preparado para ello).
        // TODO: llamar a TaxComplianceService para obtener el IVA real del tenant.
        const decimal impuestoPorcentaje = 0m;

        var venta = await _ventaService.CrearAsync(
            turnoId:             turno.Id,
            cajeroId:            cajeroId,
            sucursalId:          turno.SucursalId,
            tenantId:            tenantId,
            items:               itemsDominio,
            impuestoPorcentaje:  impuestoPorcentaje,
            metodoPago:          metodoPago);

        // 6. Retornar 201 Created con la URI del recurso creado
        return CreatedAtAction(
            nameof(GetById),
            new { id = venta.Id },
            VentaResponse.FromModel(venta));
    }

    // ── GET /ventas/{id} ───────────────────────────────────────────────────────

    /// <summary>
    /// Obtiene una venta por su Id.
    /// </summary>
    /// <response code="200">Venta encontrada.</response>
    /// <response code="401">Token JWT ausente o inválido.</response>
    /// <response code="404">Venta no encontrada o no pertenece al tenant actual.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VentaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        if (!TryGetClaims(out _, out _))
            return Unauthorized(new { error = "Token inválido: faltan cajero_id/sub o tenant_id." });

        var venta = await _ventaService.GetByIdAsync(id);
        return venta is null
            ? NotFound(new { error = $"Venta {id} no encontrada." })
            : Ok(VentaResponse.FromModel(venta));
    }

    // ── GET /ventas/turno/{turnoId} ────────────────────────────────────────────

    /// <summary>
    /// Obtiene todas las ventas de un turno de caja específico.
    /// </summary>
    /// <response code="200">Lista de ventas del turno (puede ser vacía).</response>
    /// <response code="401">Token JWT ausente o inválido.</response>
    [HttpGet("turno/{turnoId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<VentaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetByTurno(Guid turnoId)
    {
        if (!TryGetClaims(out _, out _))
            return Unauthorized(new { error = "Token inválido: faltan cajero_id/sub o tenant_id." });

        var ventas = await _ventaService.GetByTurnoAsync(turnoId);
        return Ok(ventas.Select(VentaResponse.FromModel));
    }
}

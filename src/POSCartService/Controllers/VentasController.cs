using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POSCartService.DTOs;
using POSCartService.Exceptions;
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
    private readonly ICatalogClient _catalogClient;
    private readonly ITaxClient _taxClient;
    private readonly ILogger<VentasController>? _logger;

    public VentasController(
        IVentaService ventaService,
        ITurnoService turnoService,
        ICatalogClient catalogClient,
        ITaxClient taxClient,
        ILogger<VentasController>? logger = null)
    {
        _ventaService  = ventaService  ?? throw new ArgumentNullException(nameof(ventaService));
        _turnoService  = turnoService  ?? throw new ArgumentNullException(nameof(turnoService));
        _catalogClient = catalogClient ?? throw new ArgumentNullException(nameof(catalogClient));
        _taxClient     = taxClient     ?? throw new ArgumentNullException(nameof(taxClient));
        _logger        = logger;
    }

    public VentasController(IVentaService ventaService, ITurnoService turnoService)
        : this(ventaService, turnoService, null!, null!, null)
    {
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

    // ── POST /ventas/{id}/items ───────────────────────────────────────────────

    /// <summary>
    /// Agrega un ítem al carrito de compras / venta pendiente.
    /// Flujo:
    /// 1. Consulta GET /api/products/{id} en MS-3 para obtener el precio base y nombre actualizado del producto.
    /// 2. Consulta POST /api/tax/calculate en MS-2 para calcular el IVA y los nuevos totales.
    /// 3. Agrega el ítem con todos los datos calculados y actualiza la venta.
    /// </summary>
    /// <response code="201">Ítem agregado exitosamente con sus datos calculados.</response>
    /// <response code="400">Request inválido o producto inactivo para venta.</response>
    /// <response code="401">Token JWT ausente o inválido.</response>
    /// <response code="404">Venta o producto no encontrado ("Producto no encontrado").</response>
    /// <response code="409">La venta no está en estado PENDIENTE.</response>
    /// <response code="502">Error de comunicación con MS-3.</response>
    /// <response code="503">MS-2 no responde (servicio tributario no disponible).</response>
    [HttpPost("{id:guid}/items")]
    [ProducesResponseType(typeof(ItemVentaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> AgregarItem(
        [FromRoute] Guid id,
        [FromBody] AgregarItemRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Extraer claims del JWT
        if (!TryGetClaims(out _, out _))
            return Unauthorized(new { error = "Token inválido: faltan cajero_id/sub o tenant_id." });

        // 2. Verificar que la venta exista
        var venta = await _ventaService.GetByIdAsync(id);
        if (venta is null)
            return NotFound(new { error = $"Venta {id} no encontrada." });

        // 3. RN-04: Solo se pueden agregar ítems si la venta está en estado PENDIENTE
        if (venta.Estado != EstadoVenta.PENDIENTE)
        {
            return Conflict(new
            {
                error = $"Solo se pueden agregar ítems a una venta en estado PENDIENTE. Estado actual: {venta.Estado}."
            });
        }

        // 4. Token Bearer para propagación a microservicios MS-3 y MS-2
        var authHeader = Request.Headers.Authorization.ToString();

        // 5. Llamar a MS-3 GET /api/products/{id} para obtener el precio actual del producto
        ProductoCatalogDto? producto;
        try
        {
            producto = await _catalogClient.GetProductAsync(request.ProductoId, authHeader, cancellationToken);
        }
        catch (ExternalServiceException ex)
        {
            _logger?.LogError(ex, "Error llamando a MS-3 CatalogPricingService");
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
        }

        if (producto is null)
        {
            return NotFound(new { message = "Producto no encontrado", error = "Producto no encontrado" });
        }

        if (!producto.IsActive)
        {
            return BadRequest(new { error = $"El producto '{producto.Nombre}' está inactivo y no se puede vender." });
        }

        // 6. Determinar cantidad y peso
        decimal cantidad = request.Cantidad;
        decimal? pesoKg = request.PesoKg;

        if (producto.EsPesoVariable && pesoKg.HasValue && pesoKg.Value > 0)
        {
            cantidad = pesoKg.Value;
        }
        else if (cantidad <= 0)
        {
            cantidad = 1;
        }

        decimal precioUnitario = producto.PrecioBase;
        decimal subtotalItem = Math.Round(precioUnitario * cantidad, 2);

        var nuevoItem = new ItemVenta
        {
            Id             = Guid.NewGuid(),
            VentaId        = id,
            ProductoId     = producto.Id,
            NombreProducto = producto.Nombre,
            Cantidad       = cantidad,
            PesoKg         = pesoKg,
            PrecioUnitario = precioUnitario,
            Subtotal       = subtotalItem
        };

        // 7. Llamar a MS-2 POST /api/tax/calculate para calcular el IVA consolidado
        var itemsParaCalculo = venta.Items.Select(i => new TaxItemDto
        {
            Nombre   = i.NombreProducto,
            Precio   = i.PrecioUnitario,
            Cantidad = i.Cantidad
        }).Append(new TaxItemDto
        {
            Nombre   = nuevoItem.NombreProducto,
            Precio   = nuevoItem.PrecioUnitario,
            Cantidad = nuevoItem.Cantidad
        }).ToList();

        TaxCalculationResult? taxResult;
        try
        {
            taxResult = await _taxClient.CalculateTaxAsync(itemsParaCalculo, null, authHeader, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "MS-2 TaxComplianceService no responde");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "MS-2 no responde",
                error   = "El servicio de cálculo de impuestos (MS-2) no responde o no está disponible."
            });
        }

        if (taxResult is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "MS-2 no responde",
                error   = "MS-2 no respondió o no retornó resultados de cálculo fiscal."
            });
        }

        // 8. Persistir ítem y actualizar venta con totales calculados
        var (_, itemPersistido) = await _ventaService.AgregarItemAsync(
            id,
            nuevoItem,
            subtotal:  taxResult.Subtotal,

            impuestos: taxResult.Iva,
            total:     taxResult.Total);

        var itemTax = taxResult.Items.LastOrDefault();
        var response = ItemVentaResponse.FromModel(itemPersistido, itemTax?.Iva, itemTax?.Total);

        return CreatedAtAction(
            nameof(GetById),
            new { id = venta.Id },
            response);
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

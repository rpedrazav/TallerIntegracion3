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
[Route("api/ventas")]
[Authorize]
public sealed class VentasController : ControllerBase
{
    private readonly IVentaService _ventaService;
    private readonly ITurnoService _turnoService;
    private readonly ICatalogClient _catalogClient;
    private readonly ITaxClient _taxClient;
    private readonly ILogger<VentasController>? _logger;
    private readonly IPaymentGatewayClient? _paymentGatewayClient;

    public VentasController(
        IVentaService ventaService,
        ITurnoService turnoService,
        ICatalogClient catalogClient,
        ITaxClient taxClient,
        ILogger<VentasController>? logger = null,
        IPaymentGatewayClient? paymentGatewayClient = null)
    {
        _ventaService          = ventaService  ?? throw new ArgumentNullException(nameof(ventaService));
        _turnoService          = turnoService  ?? throw new ArgumentNullException(nameof(turnoService));
        _catalogClient         = catalogClient ?? throw new ArgumentNullException(nameof(catalogClient));
        _taxClient             = taxClient     ?? throw new ArgumentNullException(nameof(taxClient));
        _logger                = logger;
        _paymentGatewayClient = paymentGatewayClient;
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
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Crear(
        [FromBody] CrearVentaRequest request,
        CancellationToken cancellationToken)
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

        // 4. Token Bearer para propagación a microservicios MS-3 y MS-2
        var authHeader = Request.Headers.Authorization.ToString();

        // 5. Validar y resolver cada producto consultando MS-3 (CatalogPricingService)
        // NOTA CONTRATO S2-001 / S2-002: Ningún precio o impuesto aportado por el renderer es autoridad.
        // Se resuelven autoritativamente desde MS-3 y MS-2.
        var itemsDominio = new List<ItemVenta>();
        var itemsParaCalculoTax = new List<TaxItemDto>();

        foreach (var itemRequest in request.Items)
        {
            ProductoCatalogDto? producto;
            try
            {
                producto = await _catalogClient.GetProductAsync(itemRequest.ProductoId, authHeader, cancellationToken);
            }
            catch (ExternalServiceException ex)
            {
                _logger?.LogError(ex, "Error llamando a MS-3 CatalogPricingService para producto {ProductoId}", itemRequest.ProductoId);
                return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
            }

            if (producto is null)
            {
                return NotFound(new
                {
                    message = $"Producto {itemRequest.ProductoId} no encontrado",
                    error = $"Producto {itemRequest.ProductoId} no encontrado en el catálogo."
                });
            }

            if (!producto.IsActive)
            {
                return BadRequest(new
                {
                    error = $"El producto '{producto.Nombre}' está inactivo y no se puede vender."
                });
            }

            // Determinar cantidad y peso
            decimal cantidad = itemRequest.Cantidad;
            decimal? pesoKg = itemRequest.PesoKg;

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

            itemsDominio.Add(new ItemVenta
            {
                ProductoId     = producto.Id,
                NombreProducto = producto.Nombre,
                Cantidad       = cantidad,
                PesoKg         = pesoKg,
                PrecioUnitario = precioUnitario,
                Subtotal       = subtotalItem
            });

            itemsParaCalculoTax.Add(new TaxItemDto
            {
                Nombre   = producto.Nombre,
                Precio   = precioUnitario,
                Cantidad = cantidad
            });
        }

        // 6. Consultar MS-2 POST /api/tax/calculate para calcular IVA y totales consolidados
        TaxCalculationResult? taxResult;
        try
        {
            taxResult = await _taxClient.CalculateTaxAsync(itemsParaCalculoTax, null, authHeader, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "MS-2 TaxComplianceService no responde al crear venta");
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

        // 7. Crear la venta a través del servicio de dominio con totales autoritativos de MS-2
        var venta = await _ventaService.CrearAsync(
            turnoId:    turno.Id,
            cajeroId:   cajeroId,
            sucursalId: turno.SucursalId,
            tenantId:   tenantId,
            items:      itemsDominio,
            subtotal:   taxResult.Subtotal,
            impuestos:  taxResult.Iva,
            total:      taxResult.Total,
            metodoPago: metodoPago);

        // 8. Retornar 201 Created con la URI del recurso creado
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

    // ── PUT /ventas/{id}/items/{itemId} ───────────────────────────────────────

    /// <summary>
    /// Modifica la cantidad de un ítem en el carrito y recalcula el subtotal de ese ítem y el total de la venta (IVA incluido vía MS-2).
    /// </summary>
    /// <response code="200">Ítem modificado exitosamente con subtotal y total recalculados.</response>
    /// <response code="400">Cantidad o peso inválido (debe ser mayor a 0).</response>
    /// <response code="401">Token JWT ausente o inválido.</response>
    /// <response code="404">Venta o ítem no encontrado.</response>
    /// <response code="409">La venta no está en estado PENDIENTE.</response>
    /// <response code="503">MS-2 no responde (servicio tributario no disponible).</response>
    [HttpPut("{id:guid}/items/{itemId:guid}")]
    [ProducesResponseType(typeof(ItemVentaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ModificarCantidadItem(
        [FromRoute] Guid id,
        [FromRoute] Guid itemId,
        [FromBody] ModificarCantidadItemRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Extraer claims del JWT
        if (!TryGetClaims(out _, out _))
            return Unauthorized(new { error = "Token inválido: faltan cajero_id/sub o tenant_id." });

        // 2. Verificar que la venta exista
        var venta = await _ventaService.GetByIdAsync(id);
        if (venta is null)
            return NotFound(new { error = $"Venta {id} no encontrada." });

        // 3. RN-04: Solo se pueden modificar ítems si la venta está en estado PENDIENTE
        if (venta.Estado != EstadoVenta.PENDIENTE)
        {
            return Conflict(new
            {
                error = $"Solo se pueden modificar ítems de una venta en estado PENDIENTE. Estado actual: {venta.Estado}."
            });
        }

        // 4. Verificar que el ítem exista en la venta
        var item = venta.Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
        {
            return NotFound(new { error = $"Ítem {itemId} no encontrado en la venta {id}." });
        }

        // 5. Determinar nueva cantidad y peso
        decimal nuevaCantidad = request.Cantidad;
        decimal? nuevoPesoKg = request.PesoKg;

        if (nuevoPesoKg.HasValue && nuevoPesoKg.Value > 0)
        {
            nuevaCantidad = nuevoPesoKg.Value;
        }
        else if (nuevaCantidad <= 0)
        {
            return BadRequest(new { error = "La cantidad debe ser mayor a 0." });
        }

        decimal nuevoSubtotalItem = Math.Round(item.PrecioUnitario * nuevaCantidad, 2);

        // 6. Recalcular IVA y totales consolidados de la venta consultando MS-2
        var itemsParaCalculo = venta.Items.Select(i => new TaxItemDto
        {
            Nombre   = i.NombreProducto,
            Precio   = i.PrecioUnitario,
            Cantidad = (i.Id == itemId) ? nuevaCantidad : i.Cantidad
        }).ToList();

        var authHeader = Request.Headers.Authorization.ToString();
        TaxCalculationResult? taxResult;
        try
        {
            taxResult = await _taxClient.CalculateTaxAsync(itemsParaCalculo, null, authHeader, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "MS-2 TaxComplianceService no responde al recalcular impuestos");
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

        // 7. Persistir modificación del ítem y actualizar totales de la venta
        var (_, itemActualizado) = await _ventaService.ModificarCantidadItemAsync(
            id,
            itemId,
            nuevaCantidad:        nuevaCantidad,
            nuevoPesoKg:          nuevoPesoKg,
            nuevoSubtotalItem:    nuevoSubtotalItem,
            nuevoSubtotalVenta:   taxResult.Subtotal,
            nuevosImpuestosVenta: taxResult.Iva,
            nuevoTotalVenta:      taxResult.Total);

        var indexItem = venta.Items.ToList().FindIndex(i => i.Id == itemId);
        var itemTax = (indexItem >= 0 && indexItem < taxResult.Items.Count)
            ? taxResult.Items[indexItem]
            : null;

        var response = ItemVentaResponse.FromModel(itemActualizado, itemTax?.Iva, itemTax?.Total);
        return Ok(response);
    }

    // ── DELETE /ventas/{id}/items/{itemId} ────────────────────────────────────

    /// <summary>
    /// Elimina un ítem del carrito / venta pendiente y recalcula los totales de la venta (IVA incluido vía MS-2).
    /// Si la venta queda sin ítems, subtotal, impuestos y total se ponen a 0.
    /// </summary>
    /// <response code="200">Ítem eliminado y totales recalculados correctamente.</response>
    /// <response code="401">Token JWT ausente o inválido.</response>
    /// <response code="404">Venta o ítem no encontrado.</response>
    /// <response code="409">La venta no está en estado PENDIENTE.</response>
    /// <response code="503">MS-2 no responde (servicio tributario no disponible).</response>
    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> EliminarItem(
        [FromRoute] Guid id,
        [FromRoute] Guid itemId,
        CancellationToken cancellationToken)
    {
        // 1. Extraer claims del JWT
        if (!TryGetClaims(out _, out _))
            return Unauthorized(new { error = "Token inválido: faltan cajero_id/sub o tenant_id." });

        // 2. Verificar que la venta exista
        var venta = await _ventaService.GetByIdAsync(id);
        if (venta is null)
            return NotFound(new { error = $"Venta {id} no encontrada." });

        // 3. RN-04: Solo se pueden eliminar ítems si la venta está en estado PENDIENTE
        if (venta.Estado != EstadoVenta.PENDIENTE)
        {
            return Conflict(new
            {
                error = $"Solo se pueden eliminar ítems de una venta en estado PENDIENTE. Estado actual: {venta.Estado}."
            });
        }

        // 4. Verificar que el ítem exista en la venta
        var item = venta.Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
            return NotFound(new { error = $"Ítem {itemId} no encontrado en la venta {id}." });

        // 5. Calcular los ítems restantes (sin el ítem eliminado) para recalcular IVA
        var itemsRestantes = venta.Items
            .Where(i => i.Id != itemId)
            .Select(i => new TaxItemDto
            {
                Nombre   = i.NombreProducto,
                Precio   = i.PrecioUnitario,
                Cantidad = i.Cantidad
            }).ToList();

        decimal nuevoSubtotal  = 0m;
        decimal nuevosImpuestos = 0m;
        decimal nuevoTotal     = 0m;

        // 6. Si quedan ítems, recalcular IVA y totales via MS-2
        if (itemsRestantes.Count > 0)
        {
            var authHeader = Request.Headers.Authorization.ToString();
            TaxCalculationResult? taxResult;
            try
            {
                taxResult = await _taxClient.CalculateTaxAsync(itemsRestantes, null, authHeader, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "MS-2 TaxComplianceService no responde al eliminar ítem");
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

            nuevoSubtotal   = taxResult.Subtotal;
            nuevosImpuestos = taxResult.Iva;
            nuevoTotal      = taxResult.Total;
        }

        // 7. Persistir eliminación del ítem y actualizar totales de la venta
        var (ventaActualizada, itemEliminado) = await _ventaService.EliminarItemAsync(
            id,
            itemId,
            nuevoSubtotalVenta:    nuevoSubtotal,
            nuevosImpuestosVenta:  nuevosImpuestos,
            nuevoTotalVenta:       nuevoTotal);

        return Ok(new
        {
            message          = $"Ítem '{itemEliminado.NombreProducto}' eliminado exitosamente.",
            itemEliminadoId  = itemEliminado.Id,
            ventaId          = ventaActualizada.Id,
            itemsRestantes   = ventaActualizada.Items.Count,
            nuevoSubtotal    = ventaActualizada.Subtotal,
            nuevosImpuestos  = ventaActualizada.Impuestos,
            nuevoTotal       = ventaActualizada.Total
        });
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

    // ── POST /ventas/{id}/cobrar ───────────────────────────────────────────────

    /// <summary>
    /// Cobra una venta PENDIENTE: valida que el monto recibido cubra el total
    /// y marca la venta como COMPLETADA.
    /// </summary>
    /// <remarks>
    /// Reglas aplicadas:
    /// - RN-06: el cajero debe tener un turno ABIERTO (validado al crear la venta).
    /// - monto_recibido debe ser ≥ venta.Total; de lo contrario se retorna 400 "Monto insuficiente".
    /// - La venta debe estar en estado PENDIENTE; de lo contrario 409.
    /// </remarks>
    /// <response code="200">Venta completada. Incluye vuelto calculado.</response>
    /// <response code="400">monto_recibido es menor al total de la venta ("Monto insuficiente").</response>
    /// <response code="401">Token JWT ausente o inválido.</response>
    /// <response code="404">Venta no encontrada.</response>
    /// <response code="409">La venta no está en estado PENDIENTE.</response>
    [HttpPost("{id:guid}/cobrar")]
    [ProducesResponseType(typeof(CobrarVentaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cobrar(
        [FromRoute] Guid id,
        [FromBody] CobrarVentaRequest request)
    {
        // 1. Extraer claims del JWT
        if (!TryGetClaims(out _, out _))
            return Unauthorized(new { error = "Token inválido: faltan cajero_id/sub o tenant_id." });

        // 2. Verificar que la venta exista
        var venta = await _ventaService.GetByIdAsync(id);
        if (venta is null)
            return NotFound(new { error = $"Venta {id} no encontrada." });

        // 3. Verificar que la venta esté PENDIENTE antes de cobrar
        if (venta.Estado != EstadoVenta.PENDIENTE)
        {
            return Conflict(new
            {
                error = $"Solo se puede cobrar una venta en estado PENDIENTE. Estado actual: {venta.Estado}."
            });
        }

        // 4. S2-004: Restringir endpoint de cobro a ventas con método de pago EFECTIVO
        if (venta.MetodoPago != MetodoPagoVenta.EFECTIVO)
        {
            return BadRequest(new
            {
                error = $"El método de pago de esta venta es '{venta.MetodoPago}'. Este endpoint solo procesa cobros en EFECTIVO.",
                metodo_pago = venta.MetodoPago.ToString()
            });
        }

        // 5. Validar monto recibido ≥ total de la venta (regla de negocio del enunciado)
        if (request.MontoRecibido < venta.Total)
        {
            return BadRequest(new
            {
                error          = "Monto insuficiente",
                total_venta    = venta.Total,
                monto_recibido = request.MontoRecibido,
                diferencia     = venta.Total - request.MontoRecibido
            });
        }

        // 5. Calcular vuelto antes de completar (usamos venta.Total que aún no cambió)
        var vuelto = Math.Round(request.MontoRecibido - venta.Total, 2);

        // 6. Completar la venta y persistir el Pago EFECTIVO en una sola operación
        var ventaCompletada = await _ventaService.CompletarAsync(id, request.MontoRecibido, vuelto);

        _logger?.LogInformation(
            "Venta {VentaId} cobrada. Total: {Total}, Recibido: {Recibido}, Vuelto: {Vuelto}",
            ventaCompletada.Id, ventaCompletada.Total, request.MontoRecibido, vuelto);

        return Ok(new CobrarVentaResponse(VentaResponse.FromModel(ventaCompletada), request.MontoRecibido, vuelto));
    }

    // ── POST /ventas/{id}/cobrar-tarjeta ───────────────────────────────────────

    [HttpPatch("{id:guid}/metodo-pago")]
    public async Task<IActionResult> CambiarMetodoPago([FromRoute] Guid id, [FromBody] CambiarMetodoPagoRequest request)
    {
        if (!TryGetClaims(out var cajeroId, out var tenantId))
            return Unauthorized(new { error = "Token inválido: faltan cajero_id/sub o tenant_id." });
        var turno = await _turnoService.GetActivo(cajeroId, tenantId);
        if (turno is null)
            return Conflict(new { error = "El cajero no tiene un turno abierto." });
        var venta = await _ventaService.GetByIdAsync(id);
        if (venta is null || venta.TenantId != tenantId || venta.TurnoId != turno.Id || venta.CajeroId != cajeroId)
            return NotFound(new { error = $"Venta {id} no encontrada." });
        if (venta.Estado != EstadoVenta.PENDIENTE || venta.Pagos.Count != 0)
            return Conflict(new { error = "Solo se puede cambiar el método de una venta pendiente sin pagos." });
        if (!Enum.TryParse<MetodoPagoVenta>(request.MetodoPago, true, out var metodo) ||
            metodo is not (MetodoPagoVenta.EFECTIVO or MetodoPagoVenta.TARJETA))
            return BadRequest(new { error = "Use EFECTIVO o TARJETA." });
        try
        {
            return Ok(VentaResponse.FromModel(await _ventaService.CambiarMetodoPagoAsync(id, metodo)));
        }
        catch (InvalidOperationException)
        {
            return Conflict(new { error = "La venta ya no permite cambiar el método de pago." });
        }
    }

    /// <summary>
    /// Procesa el cobro con tarjeta para una venta PENDIENTE mediante la pasarela simulada/real.
    /// Valida JWT, tenant, turno activo, estado PENDIENTE y total antes de contactar a la pasarela.
    /// </summary>
    /// <response code="200">Cobro procesado exitosamente por la pasarela.</response>
    /// <response code="400">Request inválido, total no válido o método de pago no compatible con TARJETA.</response>
    /// <response code="401">Token JWT ausente o inválido.</response>
    /// <response code="404">Venta no encontrada (o perteneciente a otro tenant).</response>
    /// <response code="409">La venta no está en estado PENDIENTE o el cajero no tiene turno ABIERTO.</response>
    /// <response code="502">Error de comunicación o payload inválido de la pasarela.</response>
    /// <response code="504">Timeout en la comunicación con la pasarela.</response>
    [HttpPost("{id:guid}/cobrar-tarjeta")]
    [ProducesResponseType(typeof(CobrarTarjetaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> CobrarTarjeta(
        [FromRoute] Guid id,
        [FromBody] CobrarTarjetaRequest? request,
        CancellationToken cancellationToken)
    {
        // 1. Extraer y validar claims del JWT (RN-07 / RN-01)
        if (!TryGetClaims(out var cajeroId, out var tenantId))
            return Unauthorized(new { error = "Token inválido: faltan cajero_id/sub o tenant_id." });

        // 2. Verificar turno activo del cajero en el tenant (RN-06)
        var turno = await _turnoService.GetActivo(cajeroId, tenantId);
        if (turno is null)
        {
            return Conflict(new
            {
                error = "El cajero no tiene un turno abierto. Debe abrir un turno antes de cobrar ventas.",
                cajeroId
            });
        }

        // 3. Verificar que la venta exista en el tenant actual (RN-01)
        var venta = await _ventaService.GetByIdAsync(id);
        if (venta is null || venta.TenantId != tenantId)
            return NotFound(new { error = $"Venta {id} no encontrada." });

        // 4. Verificar que la venta esté en estado PENDIENTE
        if (venta.Estado != EstadoVenta.PENDIENTE)
        {
            return Conflict(new
            {
                error = $"Solo se puede cobrar una venta en estado PENDIENTE. Estado actual: {venta.Estado}."
            });
        }

        // 5. Restringir método de cobro: no admite ventas EFECTIVO
        if (venta.MetodoPago == MetodoPagoVenta.EFECTIVO)
        {
            return BadRequest(new
            {
                error = $"El método de pago de esta venta es 'EFECTIVO'. Este endpoint solo procesa cobros con TARJETA.",
                metodo_pago = venta.MetodoPago.ToString()
            });
        }

        // 6. Validar total positivo de la venta
        if (venta.Total <= 0)
        {
            return BadRequest(new
            {
                error = "El total de la venta debe ser mayor a 0 para cobrar con tarjeta.",
                total = venta.Total
            });
        }

        // 7. Si se envió monto en el request, verificar coincidencia con el total
        if (request?.Monto.HasValue == true && request.Monto.Value != venta.Total)
        {
            return BadRequest(new
            {
                error = "El monto especificado en el request no coincide con el total de la venta.",
                monto_request = request.Monto.Value,
                total_venta = venta.Total
            });
        }

        // 8. Invocar cliente de pasarela (solo una vez superadas todas las guardias)
        if (_paymentGatewayClient is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "Servicio de pasarela de pago no configurado en el servidor."
            });
        }

        // Resolver requestId e IdempotencyKey
        var headerKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        var rawKey = !string.IsNullOrWhiteSpace(request?.IdempotencyKey) ? request.IdempotencyKey : headerKey;
        var requestId = Guid.TryParse(rawKey, out var parsedGuid) ? parsedGuid : Guid.NewGuid();

        IntentoPago intentoPago;
        try
        {
            intentoPago = await _ventaService.IniciarIntentoPagoAsync(id, requestId);
        }
        catch (InvalidOperationException)
        {
            var actual = await _ventaService.GetUltimoIntentoPagoAsync(id);
            return Conflict(new
            {
                error = "La venta ya tiene una autorización en curso o sin resultado confirmado. Consulte el intento antes de cobrar de nuevo.",
                request_id = actual?.IdempotencyKey
            });
        }

        var scenarioId = string.IsNullOrWhiteSpace(request?.ScenarioId)
            ? "sim-approved-v1"
            : request.ScenarioId;

        var gatewayRequest = new PaymentGatewayRequest
        {
            RequestId = requestId,
            TenantId = tenantId,
            SaleId = venta.Id,
            Amount = venta.Total,
            Currency = "CLP",
            ScenarioId = scenarioId
        };

        PaymentGatewayResponse gatewayResponse;
        try
        {
            gatewayResponse = await _paymentGatewayClient.ProcessPaymentAsync(gatewayRequest, cancellationToken);
        }
        catch (TimeoutException ex)
        {
            _logger?.LogWarning(ex, "Timeout al comunicarse con la pasarela para venta {VentaId}", venta.Id);

            return StatusCode(StatusCodes.Status504GatewayTimeout, new
            {
                error      = "La pasarela de pago no respondió dentro del tiempo límite.",
                sale_id    = venta.Id,
                request_id = requestId
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error al comunicarse con la pasarela para venta {VentaId}", venta.Id);

            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                error   = "Error al comunicarse con la pasarela de pago.",
                request_id = requestId
            });
        }

        // ── Ramificar según resultado de pasarela ──────────────────────────────

        if (gatewayResponse.RequestId != requestId)
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                error = "La pasarela devolvió una identidad de intento distinta. Consulte el intento antes de cobrar de nuevo.",
                request_id = requestId
            });

        if (gatewayResponse.Status == "APPROVED")
        {
            intentoPago.Referencia = gatewayResponse.GatewayReference;
            intentoPago.AuthorizationCode = gatewayResponse.AuthorizationCode;
            intentoPago.Mensaje = gatewayResponse.Message;

            var ventaCompletada = await _ventaService.CompletarTarjetaAsync(venta.Id, intentoPago);

            _logger?.LogInformation(
                "Venta {VentaId} cobrada con tarjeta. Ref: {Ref}, Auth: {Auth}",
                ventaCompletada.Id, gatewayResponse.GatewayReference, gatewayResponse.AuthorizationCode);

            return Ok(new CobrarTarjetaResponse(
                VentaResponse.FromModel(ventaCompletada),
                gatewayResponse.Status,
                gatewayResponse.TransactionId,
                gatewayResponse.AuthorizationCode,
                gatewayResponse.GatewayReference,
                gatewayResponse.Message
            ));
        }
        else if (gatewayResponse.Status == "DECLINED")
        {
            // Rechazo definitivo; solo este resultado permite otro intento.
            intentoPago.Estado = EstadoIntentoPago.RECHAZADO;
            intentoPago.Referencia = gatewayResponse.GatewayReference;
            intentoPago.DeclineCode = gatewayResponse.DeclineCode;
            intentoPago.Mensaje = gatewayResponse.Message;
            await _ventaService.ActualizarIntentoPagoAsync(intentoPago);

            _logger?.LogWarning(
                "Pago rechazado para venta {VentaId}. Status: {Status}, Decline: {DeclineCode}",
                venta.Id, gatewayResponse.Status, gatewayResponse.DeclineCode);

            return UnprocessableEntity(new
            {
                error        = "La pasarela rechazó el pago.",
                status       = gatewayResponse.Status,
                decline_code = gatewayResponse.DeclineCode,
                mensaje      = gatewayResponse.Message,
                sale_id      = venta.Id,
                request_id   = requestId
            });
        }
        return StatusCode(StatusCodes.Status502BadGateway, new
        {
            error = "La pasarela devolvió un estado no reconocido. Consulte el intento antes de cobrar de nuevo.",
            request_id = requestId
        });
    }

    [HttpGet("{id:guid}/intento-tarjeta")]
    public async Task<IActionResult> ConsultarIntentoTarjeta([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetClaims(out var cajeroId, out var tenantId))
            return Unauthorized(new { error = "Token inválido: faltan cajero_id/sub o tenant_id." });
        var venta = await _ventaService.GetByIdAsync(id);
        if (venta is null || venta.TenantId != tenantId || venta.CajeroId != cajeroId)
            return NotFound(new { error = $"Venta {id} no encontrada." });

        var intento = await _ventaService.GetUltimoIntentoPagoAsync(id);
        if (intento is null)
            return Ok(new { status = "NONE", venta = VentaResponse.FromModel(venta) });

        if (venta.Estado == EstadoVenta.COMPLETADA)
            return Ok(new { status = "APPROVED", venta = VentaResponse.FromModel(venta),
                gateway_reference = venta.Pagos.FirstOrDefault(p => p.Metodo == MetodoPago.TARJETA)?.Referencia ?? intento.Referencia,
                request_id = intento.IdempotencyKey });
        if (intento.Estado == EstadoIntentoPago.APROBADO)
            return Ok(new { status = "UNCERTAIN", venta = VentaResponse.FromModel(venta), request_id = intento.IdempotencyKey });
        if (intento.Estado == EstadoIntentoPago.RECHAZADO)
            return Ok(new { status = "DECLINED", venta = VentaResponse.FromModel(venta),
                message = intento.Mensaje, request_id = intento.IdempotencyKey });

        if (_paymentGatewayClient is null || !Guid.TryParse(intento.IdempotencyKey, out var requestId))
            return Ok(new { status = "UNCERTAIN", venta = VentaResponse.FromModel(venta), request_id = intento.IdempotencyKey });

        PaymentGatewayResponse? resultado;
        try
        {
            resultado = await _paymentGatewayClient.GetPaymentStatusAsync(tenantId, requestId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "No se pudo consultar intento de tarjeta {RequestId}", requestId);
            return Ok(new { status = "UNCERTAIN", venta = VentaResponse.FromModel(venta), request_id = intento.IdempotencyKey });
        }

        if (resultado?.RequestId != requestId)
            return Ok(new { status = "UNCERTAIN", venta = VentaResponse.FromModel(venta), request_id = intento.IdempotencyKey });

        if (resultado?.Status == "APPROVED")
        {
            intento.Referencia = resultado.GatewayReference;
            intento.AuthorizationCode = resultado.AuthorizationCode;
            intento.Mensaje = resultado.Message;
            try
            {
                var completada = await _ventaService.CompletarTarjetaAsync(id, intento);
                return Ok(new { status = "APPROVED", venta = VentaResponse.FromModel(completada),
                    gateway_reference = resultado.GatewayReference, request_id = intento.IdempotencyKey });
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "No se pudo completar la venta al reconciliar el intento {RequestId}", requestId);
                var actual = await _ventaService.GetByIdAsync(id);
                if (actual?.Estado == EstadoVenta.COMPLETADA)
                    return Ok(new { status = "APPROVED", venta = VentaResponse.FromModel(actual),
                        gateway_reference = resultado.GatewayReference, request_id = intento.IdempotencyKey });
                return Ok(new { status = "UNCERTAIN", venta = VentaResponse.FromModel(venta), request_id = intento.IdempotencyKey });
            }
        }
        if (resultado?.Status == "DECLINED")
        {
            intento.Estado = EstadoIntentoPago.RECHAZADO;
            intento.Referencia = resultado.GatewayReference;
            intento.DeclineCode = resultado.DeclineCode;
            intento.Mensaje = resultado.Message;
            try
            {
                await _ventaService.ActualizarIntentoPagoAsync(intento);
            }
            catch (InvalidOperationException)
            {
                var actual = await _ventaService.GetUltimoIntentoPagoAsync(id);
                if (actual?.Estado != EstadoIntentoPago.RECHAZADO)
                    return Ok(new { status = "UNCERTAIN", venta = VentaResponse.FromModel(venta), request_id = intento.IdempotencyKey });
            }
            return Ok(new { status = "DECLINED", venta = VentaResponse.FromModel(venta),
                message = resultado.Message, request_id = intento.IdempotencyKey });
        }
        return Ok(new { status = "UNCERTAIN", venta = VentaResponse.FromModel(venta), request_id = intento.IdempotencyKey });
    }
}

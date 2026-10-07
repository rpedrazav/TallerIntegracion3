using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CatalogPricingService.Services;
using CatalogPricingService.DTOs;
using CatalogPricingService.Models;
using FluentValidation;
using System;
using System.Threading.Tasks;
using System.Text.Json;
using System.Security.Claims;
using System.Linq;

namespace CatalogPricingService.Controllers
{
    [ApiController]
    [Route("products")]
    [Authorize]
    public class ProductoController : ControllerBase
    {
        private readonly IProductoService _service;
        private readonly IValidator<CreateProductoDto> _createValidator;
        private readonly IValidator<UpdateProductoDto> _updateValidator;
        private readonly IWebHostEnvironment _environment;

        public ProductoController(
            IProductoService service,
            IValidator<CreateProductoDto> createValidator,
            IValidator<UpdateProductoDto> updateValidator,
            IWebHostEnvironment environment)
        {
            _service = service;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _environment = environment;
        }

        private Guid? GetTenantIdFromToken()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == "tenant_id")?.Value;
            if (Guid.TryParse(claim, out var tenantId)) return tenantId;

            if (_environment.IsDevelopment() &&
                Guid.TryParse(Request.Headers["X-Tenant-ID"].FirstOrDefault(), out var headerTenantId))
            {
                return headerTenantId;
            }

            return null;
        }

        /// <summary>

        /// Obtiene el catálogo paginado de productos del tenant.

        /// </summary>

        /// <param name="page">Página actual.</param>

        /// <param name="pageSize">Tamaño de la página.</param>

        /// <returns>Paginación de productos.</returns>

        /// <response code="200">Retorna la página solicitada de productos.</response>

        /// <response code="401">No autorizado.</response>

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var tenantId = GetTenantIdFromToken();
            if (tenantId == null) return StatusCode(401, new { message = "Token invÃ¡lido." });

            var result = await _service.GetAllProductosAsync(tenantId.Value, page, pageSize);
            return Ok(new { data = result.Productos, totalCount = result.TotalCount, page, pageSize });
        }

        /// <summary>

        /// Busca productos por nombre o descripción.

        /// </summary>

        /// <param name="q">Término de búsqueda.</param>

        /// <param name="page">Página actual.</param>

        /// <param name="pageSize">Tamaño de la página.</param>

        /// <returns>Resultados de búsqueda.</returns>

        /// <response code="200">Retorna los productos que coinciden.</response>

        /// <response code="401">No autorizado.</response>

        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string q = "",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var tenantId = GetTenantIdFromToken();
            if (tenantId == null) return StatusCode(401, new { message = "Token invÃ¡lido." });

            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { message = "El parÃ¡metro 'q' es requerido." });

            var result = await _service.SearchProductosAsync(tenantId.Value, q.Trim(), page, pageSize);
            return Ok(new { data = result.Productos, totalCount = result.TotalCount, page, pageSize, query = q });
        }

        /// <summary>

        /// Busca un producto por código de barras (POS).

        /// </summary>

        /// <param name="barcode">Código de barras exacto.</param>

        /// <returns>El producto correspondiente.</returns>

        /// <response code="200">Retorna el producto encontrado.</response>

        /// <response code="401">No autorizado.</response>

        /// <response code="404">Si no existe ningún producto activo con ese código de barras.</response>

        [HttpGet("lookup")]
        public async Task<IActionResult> Lookup([FromQuery] string barcode)
        {
            var tenantId = GetTenantIdFromToken();
            if (tenantId == null) return StatusCode(401, new { message = "Token invÃ¡lido." });

            if (string.IsNullOrWhiteSpace(barcode))
                return BadRequest(new { message = "El parÃ¡metro 'barcode' es requerido." });

            var producto = await _service.GetProductoByBarcodeAsync(barcode.Trim(), tenantId.Value);
            if (producto == null)
                return NotFound(new { message = "Producto no encontrado para el cÃ³digo de barras indicado." });

            return Ok(producto);
        }

        /// <summary>

        /// Obtiene un producto por su ID.

        /// </summary>

        /// <param name="id">ID del producto.</param>

        /// <returns>El producto solicitado.</returns>

        /// <response code="200">Retorna el producto.</response>

        /// <response code="401">No autorizado.</response>

        /// <response code="404">Producto no encontrado.</response>

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var tenantId = GetTenantIdFromToken();
            if (tenantId == null) return StatusCode(401, new { message = "Token invÃ¡lido." });

            var producto = await _service.GetProductoByIdAsync(id, tenantId.Value);
            if (producto == null) return NotFound(new { message = "Producto no encontrado o no pertenece a su catÃ¡logo." });

            return Ok(producto);
        }

        /// <summary>

        /// Crea un nuevo producto en el catálogo.

        /// </summary>

        /// <param name="rawDto">Datos del nuevo producto.</param>

        /// <returns>El producto creado.</returns>

        /// <response code="201">Retorna el producto recién creado.</response>

        /// <response code="400">Errores de validación o datos faltantes.</response>

        /// <response code="401">No autorizado.</response>

        /// <response code="409">Si ya existe un producto con el mismo código de barras (SKU duplicado).</response>

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] JsonElement rawDto)
        {
            var tenantId = GetTenantIdFromToken();
            if (tenantId == null) return StatusCode(401, new { message = "Token invÃ¡lido." });

            var dto = JsonSerializer.Deserialize<CreateProductoDto>(rawDto.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (dto == null) return BadRequest(new { message = "Cuerpo JSON invÃ¡lido." });

            dto.TenantId = tenantId.Value;
            var validationResult = await _createValidator.ValidateAsync(dto);
            if (!validationResult.IsValid) return BadRequest(validationResult.Errors);

            var producto = new Producto
            {
                TenantId = tenantId.Value,
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                CodigoBarras = dto.CodigoBarras ?? string.Empty,
                CodigoQrUrl = dto.CodigoQrUrl,
                CategoriaId = dto.CategoriaId,
                UomBaseId = dto.UomBaseId,
                PrecioBase = dto.PrecioBase,
                EsPesoVariable = dto.EsPesoVariable,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            var createdProducto = await _service.CreateProductoAsync(producto);
            return StatusCode(201, createdProducto);
        }

        /// <summary>

        /// Actualiza los datos de un producto (incluyendo precio y estado de venta).

        /// </summary>

        /// <param name="id">ID del producto a actualizar.</param>

        /// <param name="rawDto">Nuevos datos del producto.</param>

        /// <returns>El producto actualizado.</returns>

        /// <response code="200">Retorna el producto ya actualizado.</response>

        /// <response code="400">Errores de validación.</response>

        /// <response code="401">No autorizado.</response>

        /// <response code="404">Producto no encontrado.</response>

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] JsonElement rawDto)
        {
            var tenantId = GetTenantIdFromToken();
            if (tenantId == null) return StatusCode(401, new { message = "Token invÃ¡lido." });

            var dto = JsonSerializer.Deserialize<UpdateProductoDto>(rawDto.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (dto == null) return BadRequest(new { message = "Cuerpo JSON invÃ¡lido." });

            dto.Id = id;
            dto.TenantId = tenantId.Value;

            var validationResult = await _updateValidator.ValidateAsync(dto);
            if (!validationResult.IsValid) return BadRequest(validationResult.Errors);

            var productoActualizado = new Producto
            {
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                CodigoBarras = dto.CodigoBarras ?? string.Empty,
                CodigoQrUrl = dto.CodigoQrUrl,
                CategoriaId = dto.CategoriaId,
                UomBaseId = dto.UomBaseId,
                PrecioBase = dto.PrecioBase,
                EsPesoVariable = dto.EsPesoVariable,
                IsActive = dto.IsActive
            };

            try
            {
                var updatedProducto = await _service.UpdateProductoAsync(id, productoActualizado, tenantId.Value);
                return Ok(updatedProducto);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
        }
    }
}



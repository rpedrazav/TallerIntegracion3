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

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var tenantId = GetTenantIdFromToken();
            if (tenantId == null) return StatusCode(401, new { message = "Token inválido." });

            var result = await _service.GetAllProductosAsync(tenantId.Value, page, pageSize);
            return Ok(new { data = result.Productos, totalCount = result.TotalCount, page, pageSize });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var tenantId = GetTenantIdFromToken();
            if (tenantId == null) return StatusCode(401, new { message = "Token inválido." });

            var producto = await _service.GetProductoByIdAsync(id, tenantId.Value);
            if (producto == null) return NotFound(new { message = "Producto no encontrado o no pertenece a su catálogo." });

            return Ok(producto);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] JsonElement rawDto)
        {
            var tenantId = GetTenantIdFromToken();
            if (tenantId == null) return StatusCode(401, new { message = "Token inválido." });

            var dto = JsonSerializer.Deserialize<CreateProductoDto>(rawDto.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (dto == null) return BadRequest(new { message = "Cuerpo JSON inválido." });

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

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] JsonElement rawDto)
        {
            var tenantId = GetTenantIdFromToken();
            if (tenantId == null) return StatusCode(401, new { message = "Token inválido." });

            var dto = JsonSerializer.Deserialize<UpdateProductoDto>(rawDto.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (dto == null) return BadRequest(new { message = "Cuerpo JSON inválido." });

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

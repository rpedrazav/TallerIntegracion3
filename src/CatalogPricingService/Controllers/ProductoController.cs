using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CatalogPricingService.Services;
using CatalogPricingService.DTOs;
using CatalogPricingService.Models;
using FluentValidation;
using System;
using System.Threading.Tasks;

namespace CatalogPricingService.Controllers
{
    [ApiController]
    [Route("products")]
    [Authorize] // RN-07: Obligatorio JWT
    public class ProductoController : ControllerBase
    {
        private readonly IProductoService _service;
        private readonly IValidator<CreateProductoDto> _createValidator;
        private readonly IValidator<UpdateProductoDto> _updateValidator;

        public ProductoController(
            IProductoService service, 
            IValidator<CreateProductoDto> createValidator, 
            IValidator<UpdateProductoDto> updateValidator)
        {
            _service = service;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var tenantClaim = User.FindFirst("tenant_id")?.Value;
            if (string.IsNullOrEmpty(tenantClaim) || !Guid.TryParse(tenantClaim, out Guid tenantId))
                return Unauthorized(new { message = "Token inválido." });

            var result = await _service.GetAllProductosAsync(tenantId, page, pageSize);
            return Ok(new { data = result.Productos, totalCount = result.TotalCount, page, pageSize });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductoDto dto)
        {
            var tenantClaim = User.FindFirst("tenant_id")?.Value;
            if (string.IsNullOrEmpty(tenantClaim) || !Guid.TryParse(tenantClaim, out Guid tenantId))
                return Unauthorized(new { message = "Token inválido." });

            dto.TenantId = tenantId;
            var validationResult = await _createValidator.ValidateAsync(dto);
            if (!validationResult.IsValid) return BadRequest(validationResult.Errors);

            var producto = new Producto
            {
                TenantId = tenantId,
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
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductoDto dto)
        {
            var tenantClaim = User.FindFirst("tenant_id")?.Value;
            if (string.IsNullOrEmpty(tenantClaim) || !Guid.TryParse(tenantClaim, out Guid tenantId))
                return Unauthorized(new { message = "Token inválido." });

            // RN-01: Inyectamos ID y TenantId directo del sistema para validar con seguridad
            dto.Id = id;
            dto.TenantId = tenantId;

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
                // El servicio valida que el producto exista y pertenezca al tenant del JWT
                var updatedProducto = await _service.UpdateProductoAsync(id, productoActualizado, tenantId);
                return Ok(updatedProducto);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CatalogPricingService.Services;
using CatalogPricingService.DTOs;
using CatalogPricingService.Models;
using FluentValidation;

namespace CatalogPricingService.Controllers
{
    [ApiController]
    [Route("products")]
    [Authorize] // RN-07: JWT obligatorio
    public class ProductoController : ControllerBase
    {
        private readonly IProductoService _service;
        private readonly IValidator<CreateProductoDto> _validator;

        public ProductoController(IProductoService service, IValidator<CreateProductoDto> validator)
        {
            _service = service;
            _validator = validator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var tenantClaim = User.FindFirst("tenant_id")?.Value;
            
            if (string.IsNullOrEmpty(tenantClaim) || !Guid.TryParse(tenantClaim, out Guid tenantId))
            {
                return Unauthorized(new { message = "Token inválido o sin tenant_id activo." });
            }

            var result = await _service.GetAllProductosAsync(tenantId, page, pageSize);

            return Ok(new 
            {
                data = result.Productos,
                totalCount = result.TotalCount,
                page = page,
                pageSize = pageSize
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductoDto dto)
        {
            // RN-08: Extraer tenant_id directamente del JWT
            var tenantClaim = User.FindFirst("tenant_id")?.Value;
            
            if (string.IsNullOrEmpty(tenantClaim) || !Guid.TryParse(tenantClaim, out Guid tenantId))
            {
                return Unauthorized(new { message = "Token inválido o sin tenant_id activo." });
            }

            // Inyectar el tenant al DTO para que FluentValidation evalúe la unicidad del código de barras
            dto.TenantId = tenantId;

            // Ejecutar validación
            var validationResult = await _validator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            // Mapeo seguro de DTO a Entidad de base de datos
            var producto = new Producto
            {
                TenantId = tenantId, // RN-01: Aislamiento estricto
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
    }
}

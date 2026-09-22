using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CatalogPricingService.Services;

namespace CatalogPricingService.Controllers
{
    [ApiController]
    [Route("products")]
    [Authorize] // RN-07: Obliga a que la petición traiga un JWT válido
    public class ProductoController : ControllerBase
    {
        private readonly IProductoService _service;

        public ProductoController(IProductoService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            // RN-01 y RN-08: Extraer tenant_id directamente del JWT para aislamiento total
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
    }
}

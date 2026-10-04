using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FluentValidation;
using CatalogPricingService.DTOs;
using CatalogPricingService.Services;
using System.Text.Json;

namespace CatalogPricingService.Controllers;

[ApiController]
[Route("categories")]
[Authorize]
public class CategoriaController : ControllerBase
{
    private readonly ICategoriaService _service;
    private readonly IValidator<CreateCategoriaDto> _validator;
    private readonly IWebHostEnvironment _environment;

    public CategoriaController(
        ICategoriaService service,
        IValidator<CreateCategoriaDto> validator,
        IWebHostEnvironment environment)
    {
        _service     = service;
        _validator   = validator;
        _environment = environment;
    }

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        /// <summary>
    /// Lista todas las categorias activas del tenant autenticado.
    /// </summary>
    /// <returns>Lista de categorias.</returns>
    /// <response code="200">Retorna un arreglo de categorias (puede estar vacío).</response>
    /// <response code="401">No autorizado.</response>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tenantId = GetTenantIdFromToken();
        if (tenantId == null)
            return StatusCode(401, new { message = "Token invÃ¡lido o tenant_id ausente." });

        var categorias = await _service.GetAllCategoriasAsync(tenantId.Value);
        return Ok(categorias);
    }

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        /// <summary>
    /// Crea una nueva categoria para el tenant autenticado.
    /// </summary>
    /// <param name="rawBody">Datos de la categoría.</param>
    /// <returns>La categoría creada.</returns>
    /// <response code="201">Categoría creada con éxito.</response>
    /// <response code="400">Validación fallida (nombre duplicado, padre inválido).</response>
    /// <response code="401">No autorizado.</response>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] JsonElement rawBody)
    {
        // 1. Extraer tenant del JWT (o del header X-Tenant-ID en Development)
        var tenantId = GetTenantIdFromToken();
        if (tenantId == null)
            return StatusCode(401, new { message = "Token invÃ¡lido o tenant_id ausente." });

        // 2. Deserializar y asignar tenant
        var dto = JsonSerializer.Deserialize<CreateCategoriaDto>(
            rawBody.GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (dto == null)
            return BadRequest(new { message = "Cuerpo JSON invÃ¡lido." });

        dto.TenantId = tenantId.Value;

        // 3. Validar con FluentValidation
        var validationResult = await _validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        // 4. Delegar creacion al servicio
        var created = await _service.CreateCategoriaAsync(dto);

        return StatusCode(201, created);
    }

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    // helpers
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private Guid? GetTenantIdFromToken()
    {
        var claim = User.Claims.FirstOrDefault(c => c.Type == "tenant_id")?.Value;
        if (Guid.TryParse(claim, out var tenantId)) return tenantId;

        // Fallback solo en entorno Development (util para pruebas manuales)
        if (_environment.IsDevelopment() &&
            Guid.TryParse(Request.Headers["X-Tenant-ID"].FirstOrDefault(), out var headerTenantId))
        {
            return headerTenantId;
        }

        return null;
    }
}


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

    // ──────────────────────────────────────────────────────────────
    // POST /categories
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Crea una nueva categoria para el tenant autenticado.
    /// 
    /// Request body:
    ///   { "nombre": "Lacteos", "parentId": null }
    ///
    /// Responses:
    ///   201 Created  – categoria creada con exito.
    ///   400 Bad Request – validacion fallida (nombre duplicado, padre invalido, etc.).
    ///   401 Unauthorized – JWT ausente o tenant_id invalido.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] JsonElement rawBody)
    {
        // 1. Extraer tenant del JWT (o del header X-Tenant-ID en Development)
        var tenantId = GetTenantIdFromToken();
        if (tenantId == null)
            return StatusCode(401, new { message = "Token inválido o tenant_id ausente." });

        // 2. Deserializar y asignar tenant
        var dto = JsonSerializer.Deserialize<CreateCategoriaDto>(
            rawBody.GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (dto == null)
            return BadRequest(new { message = "Cuerpo JSON inválido." });

        dto.TenantId = tenantId.Value;

        // 3. Validar con FluentValidation
        var validationResult = await _validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        // 4. Delegar creacion al servicio
        var created = await _service.CreateCategoriaAsync(dto);

        return StatusCode(201, created);
    }

    // ──────────────────────────────────────────────────────────────
    // helpers
    // ──────────────────────────────────────────────────────────────

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

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantIdentityService.DTOs;
using TenantIdentityService.Models;
using TenantIdentityService.Repositories;

namespace TenantIdentityService.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize(Roles = "ADMIN")] // Cumple el requisito ADMINISTRADOR del backlog con el rol vigente del dominio.
public class UsuarioController : ControllerBase
{
    private readonly IUsuarioRepository _usuarioRepository;

    public UsuarioController(IUsuarioRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UsuarioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<UsuarioDto>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
            return BadRequest(new { message = "page debe ser mayor o igual a 1 y pageSize debe estar entre 1 y 100." });

        var tenantClaim = User.FindFirst("tenant_id")?.Value;
        if (!Guid.TryParse(tenantClaim, out var tenantId))
            return Unauthorized(new { message = "El token no contiene un tenant_id válido." });

        var result = await _usuarioRepository.GetActivePagedAsync(tenantId, page, pageSize);

        return Ok(new PagedResult<UsuarioDto>
        {
            Items = result.Items.Select(MapToDto).ToList(),
            Page = result.Page,
            PageSize = result.PageSize,
            TotalItems = result.TotalItems,
            TotalPages = result.TotalPages
        });
    }

    [HttpPost]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UsuarioDto>> CreateUser([FromBody] CrearUsuarioDto request)
    {
        var tenantClaim = User.FindFirst("tenant_id")?.Value;
        if (!Guid.TryParse(tenantClaim, out var tenantId))
            return Unauthorized(new { message = "El token no contiene un tenant_id válido." });

        var usuario = new Usuario
        {
            TenantId = tenantId,
            Nombre = request.Nombre,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Activo = true,
            CreadoEn = DateTime.UtcNow
        };

        var usuarioCreado = await _usuarioRepository.CreateAsync(usuario);
        var usuarioDto = MapToDto(usuarioCreado);

        return Created($"/api/v1/users/{usuarioCreado.Id}", usuarioDto);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UsuarioDto>> UpdateUser(
        Guid id,
        [FromBody] ActualizarUsuarioDto request)
    {
        var tenantClaim = User.FindFirst("tenant_id")?.Value;
        if (!Guid.TryParse(tenantClaim, out var tenantId))
            return Unauthorized(new { message = "El token no contiene un tenant_id válido." });

        var usuario = await _usuarioRepository.GetByIdAsync(id, tenantId);
        if (usuario is null)
            return NotFound(new { message = "Usuario no encontrado." });

        usuario.Nombre = request.Nombre;
        usuario.Email = request.Email;

        var usuarioActualizado = await _usuarioRepository.UpdateAsync(usuario);

        return Ok(MapToDto(usuarioActualizado));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var tenantClaim = User.FindFirst("tenant_id")?.Value;
        if (!Guid.TryParse(tenantClaim, out var tenantId))
            return Unauthorized(new { message = "El token no contiene un tenant_id válido." });

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(userIdClaim, out var authenticatedUserId))
            return Unauthorized(new { message = "El token no contiene un identificador de usuario válido." });

        if (id == authenticatedUserId)
            return BadRequest(new { message = "Un administrador no puede desactivarse a sí mismo." });

        var deactivated = await _usuarioRepository.DeactivateAsync(id, tenantId);
        if (!deactivated)
            return NotFound(new { message = "Usuario no encontrado." });

        return NoContent();
    }

    private static UsuarioDto MapToDto(Usuario usuario)
    {
        return new UsuarioDto
        {
            Id = usuario.Id,
            TenantId = usuario.TenantId,
            Nombre = usuario.Nombre,
            Email = usuario.Email,
            Activo = usuario.Activo,
            CreadoEn = usuario.CreadoEn,
            UltimoLogin = usuario.UltimoLogin
        };
    }
}
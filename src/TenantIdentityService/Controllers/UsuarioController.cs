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
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using TenantIdentityService.DTOs;
using TenantIdentityService.Exceptions;
using TenantIdentityService.Models;
using TenantIdentityService.Repositories;

namespace TenantIdentityService.Controllers;

[ApiController]
[Route("api/v1/users")]
[Route("api/users")]
[Route("users")]
[Authorize(Roles = "ADMIN")] // Cumple el requisito ADMINISTRADOR del backlog con el rol vigente del dominio.
public class UsuarioController : ControllerBase
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IValidator<CrearUsuarioDto> _crearUsuarioValidator;

    public UsuarioController(
        IUsuarioRepository usuarioRepository,
        IValidator<CrearUsuarioDto> crearUsuarioValidator)
    {
        _usuarioRepository = usuarioRepository;
        _crearUsuarioValidator = crearUsuarioValidator;
    }

    /// <summary>
    /// Obtiene todos los usuarios del tenant.
    /// </summary>
    /// <returns>Lista de usuarios.</returns>
    /// <response code="200">Retorna la lista de usuarios.</response>
    /// <response code="401">No autorizado.</response>
    /// <response code="403">No tienes permisos.</response>
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

    /// <summary>
    /// Crea un nuevo usuario en el tenant.
    /// </summary>
    /// <param name="request">Datos del nuevo usuario.</param>
    /// <returns>El usuario creado.</returns>
    /// <response code="201">Usuario creado exitosamente.</response>
    /// <response code="400">Datos inválidos.</response>
    /// <response code="401">No autorizado.</response>
    /// <response code="403">No tienes permisos.</response>
    /// <response code="409">Email ya registrado en el tenant.</response>
    [HttpPost]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UsuarioDto>> CreateUser([FromBody] CrearUsuarioDto request)
    {
        // 1. Validación con FluentValidation antes de tocar el JWT o la base de datos.
        var validacion = await _crearUsuarioValidator.ValidateAsync(request);
        if (!validacion.IsValid)
            return ValidationProblem(new ValidationProblemDetails(validacion.ToDictionary()));

        // 2. Extraer tenant_id desde los claims del JWT (Aislamiento Multi-Tenant RN-01).
        var tenantClaim = User.FindFirst("tenant_id")?.Value;
        if (!Guid.TryParse(tenantClaim, out var tenantId))
            return Unauthorized(new { message = "El token no contiene un tenant_id válido." });

        // 3. Validar roles solicitados antes de crear el usuario
        List<string>? roleNames = null;
        if (request.Roles is { Count: > 0 })
        {
            roleNames = request.Roles
                .Select(r => r.Trim())
                .Where(r => r.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (roleNames.Any(r => string.Equals(r, "SUPER_ADMIN", StringComparison.OrdinalIgnoreCase)))
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "Un administrador de tenant no puede asignar SUPER_ADMIN." });
            }

            var invalidRoles = await _usuarioRepository.ValidateRoleNamesAsync(roleNames);
            if (invalidRoles.Count > 0)
            {
                return BadRequest(new
                {
                    message = "Uno o más roles no existen.",
                    invalidRoles = invalidRoles
                });
            }
        }

        // 4. Hashear password con BCrypt y construir entidad asociada al tenant.
        var usuario = new Usuario
        {
            TenantId = tenantId,
            Nombre = request.Nombre.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Activo = true,
            CreadoEn = DateTime.UtcNow
        };

        // 5. Guardar en BD y asignar roles
        try
        {
            var usuarioCreado = await _usuarioRepository.CreateAsync(usuario);

            if (roleNames is { Count: > 0 })
            {
                var assignResult = await _usuarioRepository.AssignRolesAsync(usuarioCreado.Id, roleNames, tenantId);
                if (assignResult.InvalidRoles.Count > 0)
                {
                    await _usuarioRepository.DeletePermanentlyAsync(usuarioCreado.Id, tenantId);
                    return BadRequest(new
                    {
                        message = "Uno o más roles no existen.",
                        invalidRoles = assignResult.InvalidRoles
                    });
                }

                if (assignResult.Usuario != null)
                {
                    return Created($"/users/{usuarioCreado.Id}", MapToDto(assignResult.Usuario));
                }
            }

            var usuarioDto = MapToDto(usuarioCreado);
            return Created($"/users/{usuarioCreado.Id}", usuarioDto);
        }
        catch (DuplicateUserEmailException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza un usuario existente.
    /// </summary>
    /// <param name="id">ID del usuario.</param>
    /// <param name="request">Nuevos datos.</param>
    /// <returns>El usuario actualizado.</returns>
    /// <response code="200">Usuario actualizado.</response>
    /// <response code="400">Datos inválidos.</response>
    /// <response code="401">No autorizado.</response>
    /// <response code="403">No tienes permisos.</response>
    /// <response code="404">Usuario no encontrado.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UsuarioActualizadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UsuarioActualizadoDto>> UpdateUser(
        Guid id,
        [FromBody] ActualizarUsuarioDto request)
    {
        var tenantClaim = User.FindFirst("tenant_id")?.Value;
        if (!Guid.TryParse(tenantClaim, out var tenantId))
            return Unauthorized(new { message = "El token no contiene un tenant_id válido." });

        try
        {
            var usuario = await _usuarioRepository.UpdateBasicAsync(id, tenantId, request);
            return usuario is null
                ? NotFound(new { message = "Usuario no encontrado." })
                : Ok(usuario);
        }
        catch (PostgresException ex) when (
            ex.SqlState == PostgresErrorCodes.UniqueViolation &&
            ex.ConstraintName == "uq_users_tenant_email")
        {
            return Conflict(new { message = "El email ya pertenece a otro usuario del tenant." });
        }
    }

    /// <summary>
    /// Elimina (lógicamente) un usuario.
    /// </summary>
    /// <param name="id">ID del usuario.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Usuario eliminado.</response>
    /// <response code="401">No autorizado.</response>
    /// <response code="403">No tienes permisos.</response>
    /// <response code="404">Usuario no encontrado.</response>
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

    /// <summary>
    /// Asigna roles a un usuario.
    /// </summary>
    /// <param name="id">ID del usuario.</param>
    /// <param name="request">Lista de nombres de roles.</param>
    /// <returns>El usuario actualizado.</returns>
    /// <response code="200">Roles asignados.</response>
    /// <response code="400">Datos inválidos.</response>
    /// <response code="401">No autorizado.</response>
    /// <response code="403">No tienes permisos.</response>
    /// <response code="404">Usuario no encontrado.</response>
    [HttpPost("{id:guid}/roles")]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UsuarioDto>> AssignRoles(
        Guid id,
        [FromBody] AsignarRolesDto request)
    {
        if (request.Roles is null || request.Roles.Count == 0)
            return BadRequest(new { message = "Debe enviar al menos un rol." });

        var requestedRoles = request.Roles
            .Select(role => role.Trim())
            .Where(role => role.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (requestedRoles.Count == 0)
            return BadRequest(new { message = "Debe enviar al menos un rol válido." });

        if (requestedRoles.Any(role =>
                string.Equals(role, "SUPER_ADMIN", StringComparison.OrdinalIgnoreCase)))
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "Un administrador de tenant no puede asignar SUPER_ADMIN." });
        }

        var tenantClaim = User.FindFirst("tenant_id")?.Value;
        if (!Guid.TryParse(tenantClaim, out var tenantId))
            return Unauthorized(new { message = "El token no contiene un tenant_id válido." });

        var usuario = await _usuarioRepository.GetByIdAsync(id, tenantId);
        if (usuario is null)
            return NotFound(new { message = "Usuario no encontrado." });

        var result = await _usuarioRepository.AssignRolesAsync(id, requestedRoles, tenantId);
        if (result.InvalidRoles.Count > 0)
        {
            return BadRequest(new
            {
                message = "Uno o más roles no existen.",
                invalidRoles = result.InvalidRoles
            });
        }

        return Ok(MapToDto(result.Usuario!));
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
            UltimoLogin = usuario.UltimoLogin,
            Roles = usuario.UsuarioRoles
                .Where(ur => ur.Rol != null)
                .Select(ur => ur.Rol.Nombre)
                .OrderBy(nombre => nombre)
                .ToList()
        };
    }
}

namespace TenantIdentityService.DTOs;

public record UsuarioActualizadoDto(Guid Id, Guid TenantId, string Email, Guid? SucursalId);

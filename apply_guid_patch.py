import os
import re

base_dir = r"c:\Users\martinxoo\Desktop\Integra_III\TallerIntegracion3"

def patch_file(rel_path, old_content_or_regex, new_content, is_regex=False):
    path = os.path.join(base_dir, rel_path)
    with open(path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    if is_regex:
        content = re.sub(old_content_or_regex, new_content, content, flags=re.DOTALL)
    else:
        content = content.replace(old_content_or_regex, new_content)
        
    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)

# 1. AsignarRolesDto.cs
with open(os.path.join(base_dir, "src/TenantIdentityService/DTOs/AsignarRolesDto.cs"), 'w', encoding='utf-8') as f:
    f.write("""namespace TenantIdentityService.DTOs;

public class AsignarRolesDto
{
    public List<Guid> RoleIds { get; set; } = new();
}
""")

# 2. AssignRolesResult.cs
with open(os.path.join(base_dir, "src/TenantIdentityService/DTOs/AssignRolesResult.cs"), 'w', encoding='utf-8') as f:
    f.write("""using TenantIdentityService.Models;

namespace TenantIdentityService.DTOs;

public class AssignRolesResult
{
    public bool UserFound { get; init; }
    public IReadOnlyCollection<Guid> InvalidRoles { get; init; } = [];
    public Usuario? Usuario { get; init; }
}
""")

# 3. IUsuarioRepository.cs
with open(os.path.join(base_dir, "src/TenantIdentityService/Repositories/IUsuarioRepository.cs"), 'w', encoding='utf-8') as f:
    f.write("""using TenantIdentityService.DTOs;
using TenantIdentityService.Models;

namespace TenantIdentityService.Repositories;

public interface IUsuarioRepository
{
    Task<IEnumerable<Usuario>> GetAllAsync(Guid tenantId);
    Task<PagedResult<Usuario>> GetActivePagedAsync(Guid tenantId, int page, int pageSize);
    Task<Usuario?> GetByIdAsync(Guid id, Guid tenantId);
    Task<Usuario> CreateAsync(Usuario usuario);
    Task<Usuario> UpdateAsync(Usuario usuario);
    Task<UsuarioActualizadoDto?> UpdateBasicAsync(Guid id, Guid tenantId, ActualizarUsuarioDto request);
    Task<bool> DeactivateAsync(Guid id, Guid tenantId);
    Task<AssignRolesResult> AssignRolesAsync(Guid userId, Guid tenantId, IEnumerable<Guid> roleIds);
    Task<IReadOnlyList<string>> ValidateRoleNamesAsync(IEnumerable<string> roleNames);
    Task<bool> DeletePermanentlyAsync(Guid id, Guid tenantId);
}
""")

# 4. UsuarioRepository.cs (Need to fix ValidateRoleNamesAsync back to string, and update AssignRolesAsync)
repo_path = os.path.join(base_dir, "src/TenantIdentityService/Repositories/UsuarioRepository.cs")
with open(repo_path, 'r', encoding='utf-8') as f:
    repo_content = f.read()

# Replace AssignRolesAsync entirely
assign_roles_match = re.search(r"public async Task<AssignRolesResult> AssignRolesAsync\(.*?(?=public async Task<IReadOnlyList)", repo_content, re.DOTALL)
if assign_roles_match:
    new_assign = """public async Task<AssignRolesResult> AssignRolesAsync(Guid userId, Guid tenantId, IEnumerable<Guid> roleIds)
    {
        var usuario = await _db.Usuarios
            .IgnoreQueryFilters()
            .Include(u => u.UsuarioRoles)
            .FirstOrDefaultAsync(existing => existing.Id == userId
                                          && existing.TenantId == tenantId);

        if (usuario is null)
            return new AssignRolesResult { UserFound = false };

        var distinctRoleIds = roleIds.Distinct().ToArray();
        
        var roles = await _db.Roles
            .Where(role => distinctRoleIds.Contains(role.Id))
            .ToListAsync();

        var validRoleIds = roles.Select(role => role.Id).ToHashSet();
        var invalidRoles = distinctRoleIds.Except(validRoleIds).ToArray();

        if (invalidRoles.Length > 0)
        {
            return new AssignRolesResult
            {
                UserFound = true,
                InvalidRoles = invalidRoles,
                Usuario = usuario
            };
        }

        usuario.UsuarioRoles.Clear();
        foreach (var role in roles)
        {
            usuario.UsuarioRoles.Add(new UsuarioRol
            {
                UsuarioId = userId,
                RolId = role.Id
            });
        }

        await _db.SaveChangesAsync();

        return new AssignRolesResult
        {
            UserFound = true,
            Usuario = usuario
        };
    }

    """
    repo_content = repo_content.replace(assign_roles_match.group(0), new_assign)

# Fix ValidateRoleNamesAsync if it was messed up
repo_content = re.sub(r"public async Task<IReadOnlyList<string>> ValidateRoleNamesAsync\(IEnumerable<Guid> roleIds\)", "public async Task<IReadOnlyList<string>> ValidateRoleNamesAsync(IEnumerable<string> roleNames)", repo_content)
repo_content = re.sub(r"var normalizedRoleNames = roleIds", "var normalizedRoleNames = roleNames", repo_content)

with open(repo_path, 'w', encoding='utf-8') as f:
    f.write(repo_content)


# 5. UsuarioController.cs
ctrl_path = os.path.join(base_dir, "src/TenantIdentityService/Controllers/UsuarioController.cs")
with open(ctrl_path, 'r', encoding='utf-8') as f:
    ctrl_content = f.read()

assign_match = re.search(r"public async Task<ActionResult<UsuarioDto>> AssignRoles\([^)]+\).*?(?=private static UsuarioDto MapToDto)", ctrl_content, re.DOTALL)
if assign_match:
    new_ctrl = """public async Task<ActionResult<UsuarioDto>> AssignRoles(
        Guid id,
        [FromBody] AsignarRolesDto request)
    {
        if (request.RoleIds is null || request.RoleIds.Count == 0)
            return BadRequest(new { message = "Debe enviar al menos un rol." });

        var requestedRoles = request.RoleIds
            .Where(r => r != Guid.Empty)
            .Distinct()
            .ToList();

        if (requestedRoles.Count == 0)
            return BadRequest(new { message = "Debe enviar al menos un rol válido." });

        var superAdminId = Guid.Parse("11111111-0000-0000-0000-000000000004");
        if (requestedRoles.Contains(superAdminId))
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

        var result = await _usuarioRepository.AssignRolesAsync(id, tenantId, requestedRoles);
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

    """
    ctrl_content = ctrl_content.replace(assign_match.group(0), new_ctrl)
    
with open(ctrl_path, 'w', encoding='utf-8') as f:
    f.write(ctrl_content)


# 6. UserIntegrationTests.cs
test_path = os.path.join(base_dir, "tests/GlobalMart.IntegrationTests/UserIntegrationTests.cs")
with open(test_path, 'r', encoding='utf-8') as f:
    test_content = f.read()

# Delete my previous broken tests if they exist
test_content = re.sub(r"\[Fact\]\s*public async Task PostUserRoles_.*?\}\s*(?=\[Fact\]|private static string CreateAdminToken)", "", test_content, flags=re.DOTALL)

# Add the proper tests
proper_tests = """
    [Fact]
    public async Task PostUserRoles_RoleIdsValidos_ActualizaAtomicamenteYRetorna200OK()
    {
        var tenantId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        Guid rolCajeroId;
        Guid rolReponedorId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
            db.Database.Migrate();

            // Setup Tenant & User
            db.Tenants.Add(new Tenant { Id = tenantId, Nombre = $"Tenant GuidRoles {tenantId:N}", Pais = "CL", Moneda = "CLP" });
            var usuario = new Usuario { Id = userId, TenantId = tenantId, Nombre = "User Guids", Email = $"g_{Guid.NewGuid():N}@t.cl", PasswordHash = "h", Activo = true };
            db.Usuarios.Add(usuario);

            // Obtener Guids reales de roles desde la base efímera
            rolCajeroId = await db.Roles.Where(r => r.Nombre == "CAJERO").Select(r => r.Id).SingleAsync();
            rolReponedorId = await db.Roles.Where(r => r.Nombre == "REPONEDOR").Select(r => r.Id).SingleAsync();

            // Asignar CAJERO
            db.UsuarioRoles.Add(new UsuarioRol { UsuarioId = userId, RolId = rolCajeroId });
            await db.SaveChangesAsync();
        }

        var configuration = _factory.Services.GetRequiredService<IConfiguration>();
        var token = CreateAdminToken(configuration, tenantId, adminId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act: Enviar ID de REPONEDOR
        var payload = new { RoleIds = new[] { rolReponedorId } };
        var response = await _client.PostAsJsonAsync($"/api/v1/users/{userId}/roles", payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
            var rolesEnDb = await db.UsuarioRoles.Where(ur => ur.UsuarioId == userId).ToListAsync();
            
            Assert.Single(rolesEnDb);
            Assert.Equal(rolReponedorId, rolesEnDb.First().RolId);
        }
    }

    [Fact]
    public async Task PostUserRoles_GuidFalso_Retorna400BadRequest()
    {
        var tenantId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
            db.Database.Migrate();

            db.Tenants.Add(new Tenant { Id = tenantId, Nombre = $"Tenant BadGuid {tenantId:N}", Pais = "CL", Moneda = "CLP" });
            db.Usuarios.Add(new Usuario { Id = userId, TenantId = tenantId, Nombre = "User Bad", Email = $"b_{Guid.NewGuid():N}@t.cl", PasswordHash = "h", Activo = true });
            await db.SaveChangesAsync();
        }

        var configuration = _factory.Services.GetRequiredService<IConfiguration>();
        var token = CreateAdminToken(configuration, tenantId, adminId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act: Enviar Guid aleatorio inexistente
        var invalidGuid = Guid.NewGuid();
        var payload = new { RoleIds = new[] { invalidGuid } };
        var response = await _client.PostAsJsonAsync($"/api/v1/users/{userId}/roles", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostUserRoles_UsuarioOtroTenant_Retorna404NotFound()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var adminA = Guid.NewGuid();
        var userIdEnB = Guid.NewGuid();
        Guid rolCajeroId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
            db.Database.Migrate();

            db.Tenants.Add(new Tenant { Id = tenantA, Nombre = $"Tenant A {tenantA:N}", Pais = "CL", Moneda = "CLP" });
            db.Tenants.Add(new Tenant { Id = tenantB, Nombre = $"Tenant B {tenantB:N}", Pais = "CL", Moneda = "CLP" });
            
            db.Usuarios.Add(new Usuario { Id = userIdEnB, TenantId = tenantB, Nombre = "User B", Email = $"b_{Guid.NewGuid():N}@t.cl", PasswordHash = "h", Activo = true });
            
            rolCajeroId = await db.Roles.Where(r => r.Nombre == "CAJERO").Select(r => r.Id).SingleAsync();
            await db.SaveChangesAsync();
        }

        var configuration = _factory.Services.GetRequiredService<IConfiguration>();
        var tokenA = CreateAdminToken(configuration, tenantA, adminA);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);

        var payload = new { RoleIds = new[] { rolCajeroId } };
        var response = await _client.PostAsJsonAsync($"/api/v1/users/{userIdEnB}/roles", payload);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static string CreateAdminToken"""

test_content = test_content.replace("    private static string CreateAdminToken", proper_tests)

with open(test_path, 'w', encoding='utf-8') as f:
    f.write(test_content)

print("Patch applied successfully")

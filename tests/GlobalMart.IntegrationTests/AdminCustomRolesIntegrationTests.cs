using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Xunit;

namespace GlobalMart.IntegrationTests;

public sealed class AdminCustomRolesIntegrationTests : IClassFixture<Ms1WebApplicationFactory>, IAsyncLifetime
{
    private readonly AdminUsersTestData _data;

    public AdminCustomRolesIntegrationTests(Ms1WebApplicationFactory factory) => _data = new(factory);
    public Task InitializeAsync() => _data.InitializeAsync();
    public Task DisposeAsync() => _data.DisposeAsync().AsTask();

    [Fact]
    public async Task RolPersonalizado_SePuedeCrearAsignarYUsarConPermisosBase()
    {
        var created = await _data.Client.PostAsJsonAsync("/users/roles",
            new { nombre = "SUPERVISOR_CAJA", descripcion = "Supervisa caja", rolBase = "ADMIN" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var role = await created.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(_data.TenantA, role.GetProperty("tenantId").GetGuid());
        Assert.Equal("ADMIN", role.GetProperty("rolBase").GetString());

        var user = await _data.Client.PostAsJsonAsync("/users", new
        {
            tenantId = _data.TenantA, nombre = "Supervisor Prueba", email = "supervisor@example.test",
            password = "Gm7!Test1234", roles = new[] { "SUPERVISOR_CAJA" }
        });
        Assert.Equal(HttpStatusCode.Created, user.StatusCode);

        var login = await _data.Client.PostAsJsonAsync("/auth/login", new
        {
            tenantId = _data.TenantA, email = "supervisor@example.test", password = "Gm7!Test1234"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var session = await login.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ADMIN", session.GetProperty("usuario").GetProperty("activeRole").GetString());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(session.GetProperty("token").GetString());
        Assert.Contains(jwt.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == "ADMIN");
        Assert.Contains(jwt.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == "SUPERVISOR_CAJA");

        _data.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.GetProperty("token").GetString());
        Assert.Equal(HttpStatusCode.OK, (await _data.Client.GetAsync("/users")).StatusCode);
    }

    [Fact]
    public async Task RolDeOtroTenant_NoSeListaNiSePuedeAsignar()
    {
        var created = await _data.Client.PostAsJsonAsync("/users/roles",
            new { nombre = "BODEGA", descripcion = "Bodega", rolBase = "REPONEDOR" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var role = await created.Content.ReadFromJsonAsync<JsonElement>();
        var roleId = role.GetProperty("id").GetGuid();

        _data.Authenticate(_data.UserB, "ADMIN", _data.TenantB);
        var listed = await _data.Client.GetFromJsonAsync<JsonElement>("/users/roles");
        Assert.DoesNotContain(listed.EnumerateArray(), r => r.GetProperty("id").GetGuid() == roleId);
        var assign = await _data.Client.PostAsJsonAsync($"/users/{_data.UserB}/roles", new { roleIds = new[] { roleId } });
        Assert.Equal(HttpStatusCode.BadRequest, assign.StatusCode);
    }

    [Fact]
    public async Task RolBaseNoSeElimina_YRolPropioAsignadoExigeDesasignar()
    {
        var baseRoleId = Guid.Parse("11111111-0000-0000-0000-000000000001");
        Assert.Equal(HttpStatusCode.NotFound, (await _data.Client.DeleteAsync($"/users/roles/{baseRoleId}")).StatusCode);

        var created = await _data.Client.PostAsJsonAsync("/users/roles",
            new { nombre = "AUXILIAR", descripcion = "Apoyo", rolBase = "CAJERO" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var roleId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await _data.Client.PostAsJsonAsync(
            $"/users/{_data.UserA}/roles", new { roleIds = new[] { roleId, baseRoleId } })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await _data.Client.DeleteAsync($"/users/roles/{roleId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _data.Client.PostAsJsonAsync(
            $"/users/{_data.UserA}/roles", new { roleIds = new[] { baseRoleId } })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await _data.Client.DeleteAsync($"/users/roles/{roleId}")).StatusCode);
    }
}

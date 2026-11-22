using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace GlobalMart.IntegrationTests;

public class ReactivarUsuarioIntegrationTests : IClassFixture<Ms1WebApplicationFactory>, IAsyncLifetime
{
    private const string Password = "Password123!";
    private readonly AdminUsersTestData _data;

    public ReactivarUsuarioIntegrationTests(Ms1WebApplicationFactory factory) => _data = new(factory);
    public Task InitializeAsync() => _data.InitializeAsync();
    public Task DisposeAsync() => _data.DisposeAsync().AsTask();

    [Fact]
    public async Task Reactivate_Admin_Retorna204YConservaDatosRolesYSucursal()
    {
        await _data.ConfigureLoginAsync(_data.UserA, Password);
        await _data.SetActiveAsync(_data.UserA, false);
        var before = await _data.SnapshotAsync(_data.UserA);
        var rolesBefore = await _data.RoleIdsAsync(_data.UserA);

        var response = await ReactivateAsync(_data.UserA);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(before with { Activo = true }, await _data.SnapshotAsync(_data.UserA));
        Assert.Equal(rolesBefore, await _data.RoleIdsAsync(_data.UserA));
    }

    [Fact]
    public async Task Reactivate_Cajero_Retorna403SinCambios()
    {
        await _data.SetActiveAsync(_data.OtherUserA, false);
        _data.Authenticate(_data.AdminId, "CAJERO");

        Assert.Equal(HttpStatusCode.Forbidden, (await ReactivateAsync(_data.OtherUserA)).StatusCode);
        Assert.False((await _data.SnapshotAsync(_data.OtherUserA)).Activo);
    }

    [Fact]
    public async Task Reactivate_SinJwt_Retorna401SinCambios()
    {
        await _data.SetActiveAsync(_data.OtherUserA, false);
        _data.Client.DefaultRequestHeaders.Authorization = null;

        Assert.Equal(HttpStatusCode.Unauthorized, (await ReactivateAsync(_data.OtherUserA)).StatusCode);
        Assert.False((await _data.SnapshotAsync(_data.OtherUserA)).Activo);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Reactivate_OtroTenantOInexistente_Retorna404SinCambios(bool otroTenant)
    {
        var foreignBefore = await _data.SnapshotAsync(_data.UserB);
        var id = otroTenant ? _data.UserB : Guid.NewGuid();

        Assert.Equal(HttpStatusCode.NotFound, (await ReactivateAsync(id)).StatusCode);
        Assert.Equal(foreignBefore, await _data.SnapshotAsync(_data.UserB));
    }

    [Fact]
    public async Task Reactivate_Repetida_Retorna204()
    {
        await _data.SetActiveAsync(_data.OtherUserA, false);

        Assert.Equal(HttpStatusCode.NoContent, (await ReactivateAsync(_data.OtherUserA)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await ReactivateAsync(_data.OtherUserA)).StatusCode);
        Assert.True((await _data.SnapshotAsync(_data.OtherUserA)).Activo);
    }

    [Fact]
    public async Task Reactivate_AdminDesactivadoConJwtVigente_Retorna403()
    {
        await _data.SetActiveAsync(_data.OtherUserA, false);
        await _data.SetActiveAsync(_data.AdminId, false);

        Assert.Equal(HttpStatusCode.Forbidden, (await ReactivateAsync(_data.OtherUserA)).StatusCode);
        Assert.False((await _data.SnapshotAsync(_data.OtherUserA)).Activo);
    }

    [Fact]
    public async Task Reactivate_CicloLogin200DeleteLogin401ReactivateLogin200()
    {
        await _data.ConfigureLoginAsync(_data.UserA, Password);
        var before = await _data.SnapshotAsync(_data.UserA);
        var rolesBefore = await _data.RoleIdsAsync(_data.UserA);
        var login = new { email = before.Email, password = Password, tenantId = _data.TenantA };

        Assert.Equal(HttpStatusCode.OK, (await _data.Client.PostAsJsonAsync("/auth/login", login)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await _data.Client.DeleteAsync($"/api/v1/users/{_data.UserA}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _data.Client.PostAsJsonAsync("/auth/login", login)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await ReactivateAsync(_data.UserA)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _data.Client.PostAsJsonAsync("/auth/login", login)).StatusCode);
        Assert.Equal(before, await _data.SnapshotAsync(_data.UserA));
        Assert.Equal(rolesBefore, await _data.RoleIdsAsync(_data.UserA));
    }

    private Task<HttpResponseMessage> ReactivateAsync(Guid id) =>
        _data.Client.SendAsync(new HttpRequestMessage(HttpMethod.Post, $"/api/v1/users/{id}/reactivate"));
}

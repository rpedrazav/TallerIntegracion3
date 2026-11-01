using System.Net;
using Xunit;

namespace GlobalMart.IntegrationTests;

public class DesactivarUsuarioIntegrationTests : IClassFixture<Ms1WebApplicationFactory>, IAsyncLifetime
{
    private readonly AdminUsersTestData _data;

    public DesactivarUsuarioIntegrationTests(Ms1WebApplicationFactory factory) => _data = new(factory);
    public Task InitializeAsync() => _data.InitializeAsync();
    public Task DisposeAsync() => _data.DisposeAsync().AsTask();

    [Fact]
    public async Task Delete_MismoTenant_Returns204YConservaFilaYDatos()
    {
        var before = await _data.SnapshotAsync(_data.OtherUserA);
        var adminBefore = await _data.SnapshotAsync(_data.AdminId);
        var foreignBefore = await _data.SnapshotAsync(_data.UserB);
        var response = await _data.Client.DeleteAsync($"/api/v1/users/{_data.OtherUserA}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
        Assert.Equal(before with { Activo = false }, await _data.SnapshotAsync(_data.OtherUserA));
        Assert.Equal(adminBefore, await _data.SnapshotAsync(_data.AdminId));
        Assert.Equal(foreignBefore, await _data.SnapshotAsync(_data.UserB));
    }

    [Fact]
    public async Task Delete_PropioAdmin_Returns400SinCambios()
    {
        var before = await _data.SnapshotAsync(_data.AdminId);
        var response = await _data.Client.DeleteAsync($"/api/v1/users/{_data.AdminId}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(before, await _data.SnapshotAsync(_data.AdminId));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Delete_OtroTenantOInexistente_Returns404SinCambios(bool otroTenant)
    {
        var ownBefore = await _data.SnapshotAsync(_data.OtherUserA);
        var foreignBefore = await _data.SnapshotAsync(_data.UserB);
        var id = otroTenant ? _data.UserB : Guid.NewGuid();
        var response = await _data.Client.DeleteAsync($"/api/v1/users/{id}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ownBefore, await _data.SnapshotAsync(_data.OtherUserA));
        Assert.Equal(foreignBefore, await _data.SnapshotAsync(_data.UserB));
    }

    [Fact]
    public async Task Delete_YaInactivo_Returns204SinEliminarFila()
    {
        var before = await _data.SnapshotAsync(_data.OtherUserA);
        Assert.Equal(HttpStatusCode.NoContent, (await _data.Client.DeleteAsync($"/api/v1/users/{_data.OtherUserA}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await _data.Client.DeleteAsync($"/api/v1/users/{_data.OtherUserA}")).StatusCode);
        Assert.Equal(before with { Activo = false }, await _data.SnapshotAsync(_data.OtherUserA));
    }

    [Fact]
    public async Task Delete_SinJwt_Returns401SinCambios()
    {
        var before = await _data.SnapshotAsync(_data.OtherUserA);
        _data.Client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, (await _data.Client.DeleteAsync($"/api/v1/users/{_data.OtherUserA}")).StatusCode);
        Assert.Equal(before, await _data.SnapshotAsync(_data.OtherUserA));
    }

    [Fact]
    public async Task Delete_Cajero_Returns403SinCambios()
    {
        var before = await _data.SnapshotAsync(_data.OtherUserA);
        _data.Authenticate(_data.AdminId, "CAJERO");
        Assert.Equal(HttpStatusCode.Forbidden, (await _data.Client.DeleteAsync($"/api/v1/users/{_data.OtherUserA}")).StatusCode);
        Assert.Equal(before, await _data.SnapshotAsync(_data.OtherUserA));
    }
}

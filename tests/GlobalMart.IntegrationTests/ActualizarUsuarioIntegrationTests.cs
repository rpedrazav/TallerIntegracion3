using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace GlobalMart.IntegrationTests;

public class ActualizarUsuarioIntegrationTests : IClassFixture<Ms1WebApplicationFactory>, IAsyncLifetime
{
    private readonly AdminUsersTestData _data;

    public ActualizarUsuarioIntegrationTests(Ms1WebApplicationFactory factory) => _data = new(factory);
    public Task InitializeAsync() => _data.InitializeAsync();
    public Task DisposeAsync() => _data.DisposeAsync().AsTask();

    [Fact]
    public async Task Put_UsuarioDelMismoTenant_Returns200YPersisteSoloDatosBasicos()
    {
        var before = await _data.SnapshotAsync(_data.UserA);
        var response = await _data.Client.PutAsJsonAsync($"/api/v1/users/{_data.UserA}",
            new { Email = "otro-tenant@example.test" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(_data.UserA, body.GetProperty("id").GetGuid());
        Assert.Equal(_data.TenantA, body.GetProperty("tenantId").GetGuid());
        Assert.Equal("otro-tenant@example.test", body.GetProperty("email").GetString());
        Assert.Equal(_data.BranchA, body.GetProperty("sucursalId").GetGuid());
        Assert.False(body.TryGetProperty("passwordHash", out _));
        Assert.Equal(before with { Email = "otro-tenant@example.test" }, await _data.SnapshotAsync(_data.UserA));
    }

    [Fact]
    public async Task Put_EmailDuplicadoEnMismoTenant_Returns409SinCambios()
    {
        var before = await _data.SnapshotAsync(_data.UserA);
        var other = await _data.SnapshotAsync(_data.OtherUserA);
        var response = await _data.Client.PutAsJsonAsync($"/api/v1/users/{_data.UserA}",
            new { Email = "ocupado@example.test" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(before, await _data.SnapshotAsync(_data.UserA));
        Assert.Equal(other, await _data.SnapshotAsync(_data.OtherUserA));
    }

    [Fact]
    public async Task Put_UsuarioDeOtroTenant_Returns404SinCambios()
    {
        var before = await _data.SnapshotAsync(_data.UserB);
        var response = await _data.Client.PutAsJsonAsync($"/api/v1/users/{_data.UserB}",
            new { Email = "intrusion@example.test", SucursalId = _data.BranchA });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(before, await _data.SnapshotAsync(_data.UserB));
    }

    [Fact]
    public async Task Put_SucursalAjenaOInexistente_Returns400SinCambios()
    {
        var before = await _data.SnapshotAsync(_data.UserA);
        var response = await _data.Client.PutAsJsonAsync($"/api/v1/users/{_data.UserA}",
            new { Email = before.Email, SucursalId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(before, await _data.SnapshotAsync(_data.UserA));
    }
}

using GlobalMart.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GlobalMart.IntegrationTests;

public class TwoTenantFixtureTests
{
    [Fact]
    public async Task Fixture_IdsUnicos_RechazaTenantAjeno()
    {
        await using var first = new TwoTenantFixture("Host=localhost;Database=fixture_tests");
        await using var second = new TwoTenantFixture("Host=localhost;Database=fixture_tests");
        Assert.NotEqual(first.TenantA.TenantId, first.TenantB.TenantId);
        Assert.NotEqual(first.TenantA.CajeroId, second.TenantA.CajeroId);
        Assert.NotEqual(first.Connection("pos"), second.Connection("pos"));
        await Assert.ThrowsAsync<ArgumentException>(() => first.CleanTenantAsync(second.TenantA));
    }

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task Fixture_FiltrosYLimpieza_ConservanSegundoTenant()
    {
        var connection = Environment.GetEnvironmentVariable("GLOBALMART_TEST_POSTGRES")
            ?? throw new InvalidOperationException("Configure GLOBALMART_TEST_POSTGRES; ver Fixtures/README.md.");
        await using var fixture = new TwoTenantFixture(connection);
        await fixture.InitializeAsync();
        var a = fixture.TenantA;
        var b = fixture.TenantB;
        await using (var identity = fixture.IdentityDb(a))
        {
            Assert.Equal(a.CajeroId, (await identity.Usuarios.SingleAsync()).Id);
            Assert.True(BCrypt.Net.BCrypt.Verify(TwoTenantFixture.Password,
                (await identity.Usuarios.SingleAsync()).PasswordHash));
            Assert.False(await identity.Usuarios.AnyAsync(u => u.Id == b.CajeroId));
        }
        await using (var catalog = fixture.CatalogDb(a))
        {
            Assert.Equal(a.ProductoId, (await catalog.Productos.SingleAsync()).Id);
            Assert.False(await catalog.Productos.AnyAsync(p => p.Id == b.ProductoId));
        }
        await using (var pos = fixture.PosDb(a))
        {
            Assert.Equal(2, await pos.Turnos.CountAsync());
            Assert.Equal(2, await pos.Ventas.CountAsync());
            Assert.Equal(2, await pos.ItemsVenta.CountAsync());
            Assert.Single(await pos.Pagos.ToListAsync());
            Assert.False(await pos.Ventas.AnyAsync(v => v.Id == b.VentaPendienteId));
        }
        await fixture.CleanTenantAsync(a);
        await fixture.CleanTenantAsync(a);
        await using (var pos = fixture.PosDb(a))
        {
            Assert.Empty(await pos.Turnos.ToListAsync());
            Assert.Empty(await pos.Ventas.ToListAsync());
            Assert.Empty(await pos.ItemsVenta.ToListAsync());
            Assert.Empty(await pos.Pagos.ToListAsync());
        }
        await using (var identity = fixture.IdentityDb(a))
            Assert.Empty(await identity.Usuarios.ToListAsync());
        await using (var catalog = fixture.CatalogDb(a))
            Assert.Empty(await catalog.Productos.ToListAsync());
        await using (var identity = fixture.IdentityDb(b))
            Assert.Equal(b.CajeroId, (await identity.Usuarios.SingleAsync()).Id);
        await using (var catalog = fixture.CatalogDb(b))
            Assert.Equal(b.ProductoId, (await catalog.Productos.SingleAsync()).Id);
        await using (var pos = fixture.PosDb(b))
        {
            Assert.Equal(2, await pos.Ventas.CountAsync());
            Assert.Equal(2, await pos.ItemsVenta.CountAsync());
            Assert.Single(await pos.Pagos.ToListAsync());
        }
        await fixture.CleanTenantAsync(b);
    }
}

extern alias CatalogPricingServiceAlias;
extern alias POSCartServiceAlias;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Xunit;
using CatalogDbContext = CatalogPricingServiceAlias::CatalogPricingService.Data.CatalogDbContext;
using Producto = CatalogPricingServiceAlias::CatalogPricingService.Models.Producto;
using Pos = POSCartServiceAlias::POSCartService;

namespace GlobalMart.IntegrationTests;

public class DesactivarProductoIntegrationTests : IAsyncLifetime
{
    private const string Key = "TI3-642-integration-tests-signing-key-2026";
    private readonly string _schema = "ti3642_" + Guid.NewGuid().ToString("N");
    private readonly Guid _tenantA = Guid.NewGuid();
    private readonly Guid _tenantB = Guid.NewGuid();
    private readonly Guid _productA = Guid.NewGuid();
    private readonly Guid _productB = Guid.NewGuid();
    private readonly Ms3WebApplicationFactory _factory = new();
    private WebApplicationFactory<CatalogPricingServiceAlias::Program>? _host;
    private HttpClient _client = null!;
    private string _connection = null!;

    public async Task InitializeAsync()
    {
        // Configurar antes de CreateClient: Program migra únicamente nuestro esquema vacío.
        _host = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices((context, services) =>
            {
                // IConfiguration del host: mismos appsettings y overrides que MS-3.
                // No arrancar primero la factory base: migraría el esquema compartido.
                var connection = context.Configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("Falta ConnectionStrings:DefaultConnection en la configuración de MS-3.");
                _connection = new NpgsqlConnectionStringBuilder(connection)
                    { SearchPath = _schema, Pooling = false }.ConnectionString;
                using var db = new NpgsqlConnection(_connection);
                db.Open();
                using var create = new NpgsqlCommand($"CREATE SCHEMA \"{_schema}\"", db);
                create.ExecuteNonQuery();
                services.RemoveAll<DbContextOptions<CatalogDbContext>>();
                services.AddSingleton(new DbContextOptionsBuilder<CatalogDbContext>()
                    .UseNpgsql(_connection, options => options.MigrationsHistoryTable("__EFMigrationsHistory", _schema)).Options);
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.TokenValidationParameters.ValidIssuer = "TI3642Tests";
                    options.TokenValidationParameters.ValidAudience = "TI3642Tests";
                    options.TokenValidationParameters.IssuerSigningKey =
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key));
                });
            });
        });
        _client = _host.CreateClient();
        await using var context = Context(_tenantA);
        context.Productos.AddRange(Product(_productA, _tenantA), Product(_productB, _tenantB));
        await context.SaveChangesAsync();
        Authenticate("ADMIN");
    }

    private CatalogDbContext Context(Guid tenant) => new(
        new DbContextOptionsBuilder<CatalogDbContext>().UseNpgsql(_connection).Options)
        { CurrentTenantId = tenant };

    private static Producto Product(Guid id, Guid tenant) => new()
    {
        Id = id, TenantId = tenant, Nombre = "Producto original", Descripcion = "Conservar",
        CodigoBarras = "123456", UomBaseId = Guid.NewGuid(), PrecioBase = 100,
        IsActive = true, CreatedAt = DateTime.UtcNow
    };

    private void Authenticate(string role)
    {
        var jwt = new JwtSecurityToken("TI3642Tests", "TI3642Tests", new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim("tenant_id", _tenantA.ToString()), new Claim(ClaimTypes.Role, role)
        }, expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)), SecurityAlgorithms.HmacSha256));
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", new JwtSecurityTokenHandler().WriteToken(jwt));
    }

    private async Task<string> Snapshot(Guid id)
    {
        await using var db = new NpgsqlConnection(_connection);
        await db.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT (to_jsonb(p) - 'is_active')::text FROM productos p WHERE id = @id", db);
        command.Parameters.AddWithValue("id", id);
        return Assert.IsType<string>(await command.ExecuteScalarAsync());
    }

    private async Task<bool> Active(Guid id, Guid tenant)
    {
        await using var db = Context(tenant);
        return (await db.Productos.SingleAsync(p => p.Id == id)).IsActive;
    }

    [Fact]
    public async Task Delete_Admin_204ConservaFilaYDatos_SegundaBaja204()
    {
        var before = await Snapshot(_productA);
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var response = await _client.DeleteAsync($"/products/{_productA}");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
            Assert.False(await Active(_productA, _tenantA));
            Assert.Equal(before, await Snapshot(_productA));
            Assert.True(await Active(_productB, _tenantB));
        }
    }

    [Fact]
    public async Task GetById_ProductoDesactivado_Retorna404YConservaFila()
    {
        var before = await Snapshot(_productA);

        using var activeResponse = await _client.GetAsync($"/products/{_productA}");
        Assert.Equal(HttpStatusCode.OK, activeResponse.StatusCode);

        using var deleteResponse = await _client.DeleteAsync($"/products/{_productA}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var inactiveResponse = await _client.GetAsync($"/products/{_productA}");
        Assert.Equal(HttpStatusCode.NotFound, inactiveResponse.StatusCode);
        Assert.False(await Active(_productA, _tenantA));
        Assert.Equal(before, await Snapshot(_productA));
    }

    [Fact]
    public async Task Delete_InexistenteYOtroTenant_Mismo404SinCambios()
    {
        var before = await Snapshot(_productB);
        var missing = await _client.DeleteAsync($"/products/{Guid.NewGuid()}");
        var foreign = await _client.DeleteAsync($"/products/{_productB}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal(await missing.Content.ReadAsStringAsync(), await foreign.Content.ReadAsStringAsync());
        Assert.True(await Active(_productB, _tenantB));
        Assert.Equal(before, await Snapshot(_productB));
        Assert.True(await Active(_productA, _tenantA));
    }

    [Theory]
    [InlineData("CAJERO", HttpStatusCode.Forbidden)]
    [InlineData("REPONEDOR", HttpStatusCode.Forbidden)]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    public async Task Delete_SinPermiso_NoModifica(string? role, HttpStatusCode expected)
    {
        var before = await Snapshot(_productA);
        if (role is null) _client.DefaultRequestHeaders.Authorization = null;
        else Authenticate(role);
        var response = await _client.DeleteAsync($"/products/{_productA}");
        Assert.Equal(expected, response.StatusCode);
        Assert.True(await Active(_productA, _tenantA));
        Assert.Equal(before, await Snapshot(_productA));
    }

    [Theory]
    [InlineData(true, false, "CAJERO", HttpStatusCode.Forbidden)]
    [InlineData(false, true, "CAJERO", HttpStatusCode.Forbidden)]
    [InlineData(true, true, "CAJERO", HttpStatusCode.OK)]
    [InlineData(false, false, "CAJERO", HttpStatusCode.OK)]
    [InlineData(true, false, "ADMIN", HttpStatusCode.OK)]
    [InlineData(false, true, "ADMIN", HttpStatusCode.OK)]
    public async Task Put_CambioEstado_SoloAdmin(bool initial, bool requested, string role, HttpStatusCode expected)
    {
        await using var db = Context(_tenantA);
        var product = await db.Productos.SingleAsync(p => p.Id == _productA);
        product.IsActive = initial;
        await db.SaveChangesAsync();
        var before = await Snapshot(_productA);
        Authenticate(role);
        var response = await _client.PutAsJsonAsync($"/products/{_productA}", new
        {
            nombre = "Nombre editado", product.Descripcion, product.CodigoBarras,
            product.CategoriaId, product.UomBaseId, product.PrecioBase,
            product.EsPesoVariable, isActive = requested
        });
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal(expected == HttpStatusCode.OK ? requested : initial, await Active(_productA, _tenantA));
        if (expected == HttpStatusCode.Forbidden)
            Assert.Equal(before, await Snapshot(_productA));
        else
        {
            await using var verify = Context(_tenantA);
            Assert.Equal("Nombre editado", (await verify.Productos.SingleAsync(p => p.Id == _productA)).Nombre);
        }
        Assert.True(await Active(_productB, _tenantB));
    }

    [Fact]
    public async Task Delete_ProductoVendido_ConservaVentaHistoricaConsultableYDatosIntactos()
    {
        var posSchema = _schema + "_pos";
        var posConnection = new NpgsqlConnectionStringBuilder(_connection)
            { SearchPath = posSchema }.ConnectionString;
        await using var connection = new NpgsqlConnection(posConnection);
        await connection.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE SCHEMA \"{posSchema}\"", connection))
            await create.ExecuteNonQueryAsync();

        try
        {
            await using var factory = new Ms5WebApplicationFactory();
            await using var host = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<DbContextOptions<Pos.Data.PosCartDbContext>>();
                    services.AddSingleton(new DbContextOptionsBuilder<Pos.Data.PosCartDbContext>()
                        .UseNpgsql(posConnection, options => options.MigrationsHistoryTable("__EFMigrationsHistory", posSchema)).Options);
                    services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                    {
                        options.TokenValidationParameters.ValidIssuer = "TI3642Tests";
                        options.TokenValidationParameters.ValidAudience = "TI3642Tests";
                        options.TokenValidationParameters.IssuerSigningKey =
                            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key));
                    });
                });
            });
            using var client = host.CreateClient(new WebApplicationFactoryClientOptions
                { BaseAddress = new Uri("https://localhost") });
            Authenticate("ADMIN");
            client.DefaultRequestHeaders.Authorization = _client.DefaultRequestHeaders.Authorization;

            await using var catalog = Context(_tenantA);
            var product = await catalog.Productos.SingleAsync(p => p.Id == _productA);
            Assert.True(product.IsActive);
            var catalogBefore = await Snapshot(_productA);
            var turno = new Pos.Models.Turno
            {
                TenantId = _tenantA, CajeroId = Guid.NewGuid(), SucursalId = Guid.NewGuid(),
                Estado = Pos.Models.EstadoTurno.CERRADO,
                AbiertoAt = DateTime.UtcNow.AddDays(-2), CerradoAt = DateTime.UtcNow.AddDays(-1)
            };
            var item = new Pos.Models.ItemVenta
            {
                ProductoId = product.Id, NombreProducto = product.Nombre,
                PrecioUnitario = product.PrecioBase, Cantidad = 2, Subtotal = product.PrecioBase * 2
            };
            var venta = new Pos.Models.Venta
            {
                TenantId = _tenantA, Turno = turno, CajeroId = turno.CajeroId,
                SucursalId = turno.SucursalId, Estado = Pos.Models.EstadoVenta.COMPLETADA,
                MetodoPago = Pos.Models.MetodoPagoVenta.EFECTIVO,
                CreatedAt = DateTime.UtcNow.AddDays(-1), Subtotal = item.Subtotal,
                Impuestos = 38, Total = 238, Items = new List<Pos.Models.ItemVenta> { item }
            };
            await using (var db = new Pos.Data.PosCartDbContext(
                new DbContextOptionsBuilder<Pos.Data.PosCartDbContext>().UseNpgsql(posConnection).Options)
                { CurrentTenantId = _tenantA })
            {
                db.Ventas.Add(venta);
                await db.SaveChangesAsync();
            }

            using var beforeResponse = await client.GetAsync($"/ventas/{venta.Id}");
            Assert.Equal(HttpStatusCode.OK, beforeResponse.StatusCode);
            var before = await beforeResponse.Content.ReadFromJsonAsync<Pos.DTOs.VentaResponse>();
            Assert.NotNull(before);
            Assert.Equal("COMPLETADA", before.Estado);
            var beforeItem = Assert.Single(before.Items);
            Assert.Equal(item.Id, beforeItem.Id);
            Assert.Equal(_productA, beforeItem.ProductoId);
            Assert.Equal(item.NombreProducto, beforeItem.NombreProducto);
            Assert.Equal(item.PrecioUnitario, beforeItem.PrecioUnitario);
            Assert.Equal(item.Cantidad, beforeItem.Cantidad);
            Assert.Equal(item.Subtotal, beforeItem.Subtotal);
            Assert.Equal(venta.Impuestos, before.Impuestos);
            Assert.Equal(venta.Total, before.Total);

            using var deletion = await _client.DeleteAsync($"/products/{_productA}");
            Assert.Equal(HttpStatusCode.NoContent, deletion.StatusCode);

            using var afterResponse = await client.GetAsync($"/ventas/{venta.Id}");
            Assert.Equal(HttpStatusCode.OK, afterResponse.StatusCode);
            var after = await afterResponse.Content.ReadFromJsonAsync<Pos.DTOs.VentaResponse>();
            Assert.NotNull(after);
            var afterItem = Assert.Single(after.Items);
            Assert.Equal(before.Id, after.Id);
            Assert.Equal(before.Estado, after.Estado);
            Assert.Equal(beforeItem.Id, afterItem.Id);
            Assert.Equal(beforeItem.ProductoId, afterItem.ProductoId);
            Assert.Equal(beforeItem.NombreProducto, afterItem.NombreProducto);
            Assert.Equal(beforeItem.PrecioUnitario, afterItem.PrecioUnitario);
            Assert.Equal(beforeItem.Cantidad, afterItem.Cantidad);
            Assert.Equal(beforeItem.Subtotal, afterItem.Subtotal);
            Assert.Equal(before.Subtotal, after.Subtotal);
            Assert.Equal(before.Impuestos, after.Impuestos);
            Assert.Equal(before.Total, after.Total);
            Assert.False(await Active(_productA, _tenantA));
            Assert.Equal(catalogBefore, await Snapshot(_productA));
        }
        finally
        {
            await using var cleanup = new NpgsqlCommand($"DROP SCHEMA IF EXISTS \"{posSchema}\" CASCADE", connection);
            await cleanup.ExecuteNonQueryAsync();
        }
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_host is not null) await _host.DisposeAsync();
        await _factory.DisposeAsync();
        if (_connection is null) return;
        await using var db = new NpgsqlConnection(_connection);
        await db.OpenAsync();
        await using var cleanup = new NpgsqlCommand($"DROP SCHEMA IF EXISTS \"{_schema}\" CASCADE", db);
        await cleanup.ExecuteNonQueryAsync();
    }
}

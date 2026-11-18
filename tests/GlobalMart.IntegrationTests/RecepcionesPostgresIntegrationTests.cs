extern alias WarehouseInventoryServiceAlias;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using WarehouseDbContext = WarehouseInventoryServiceAlias::WarehouseInventoryService.Data.WarehouseDbContext;
using Xunit;

namespace GlobalMart.IntegrationTests;

public sealed class RecepcionesPostgresIntegrationTests : IClassFixture<RecepcionesPostgresFixture>
{
    private readonly RecepcionesPostgresFixture _fixture;

    public RecepcionesPostgresIntegrationTests(RecepcionesPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task RecepcionValida_CreaLote_SumaStock_YRegistraMovimiento()
    {
        _fixture.Authenticate(_fixture.TenantA);
        await using var beforeDb = _fixture.OpenDb(_fixture.TenantA);
        var stockBefore = await beforeDb.Stocks.Where(s => s.ProductoId == _fixture.ProductoId)
            .Select(s => (decimal?)s.CantidadActual).SingleOrDefaultAsync() ?? 0;
        var lotsBefore = await beforeDb.Lotes.CountAsync();
        var movementsBefore = await beforeDb.Movimientos.CountAsync();

        var response = await _fixture.Client.PostAsJsonAsync("/recepciones", _fixture.Request(3.5m));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await using var db = _fixture.OpenDb(_fixture.TenantA);
        Assert.Equal(stockBefore + 3.5m, await db.Stocks.Where(s => s.ProductoId == _fixture.ProductoId)
            .Select(s => s.CantidadActual).SingleAsync());
        Assert.Equal(lotsBefore + 1, await db.Lotes.CountAsync());
        Assert.Equal(movementsBefore + 1, await db.Movimientos.CountAsync());
        var movement = await db.Movimientos.OrderByDescending(m => m.Timestamp).FirstAsync();
        Assert.Equal("RECEPCION", movement.Tipo);
        Assert.Equal(3.5m, movement.Cantidad);
    }

    [Fact]
    public async Task RecepcionInvalida_NoModificaInventario()
    {
        _fixture.Authenticate(_fixture.TenantA);
        await using var beforeDb = _fixture.OpenDb(_fixture.TenantA);
        var before = await Snapshot(beforeDb);

        var response = await _fixture.Client.PostAsJsonAsync("/recepciones", _fixture.Request(0));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var afterDb = _fixture.OpenDb(_fixture.TenantA);
        Assert.Equal(before, await Snapshot(afterDb));
    }

    [Fact]
    public async Task OtroTenant_NoVeNiModificaLaRecepcion()
    {
        _fixture.Authenticate(_fixture.TenantA);
        Assert.Equal(HttpStatusCode.Created,
            (await _fixture.Client.PostAsJsonAsync("/recepciones", _fixture.Request(2))).StatusCode);
        await using var ownerBeforeDb = _fixture.OpenDb(_fixture.TenantA);
        var ownerBefore = await Snapshot(ownerBeforeDb);

        _fixture.Authenticate(_fixture.TenantB);
        var forbiddenMutation = await _fixture.Client.PostAsJsonAsync("/recepciones", _fixture.Request(5));
        var visibleStock = await _fixture.Client.GetFromJsonAsync<StockResponse[]>(
            $"/stock?sucursal_id={_fixture.SucursalId}");

        Assert.Equal(HttpStatusCode.NotFound, forbiddenMutation.StatusCode);
        Assert.Empty(visibleStock!);
        await using var otherDb = _fixture.OpenDb(_fixture.TenantB);
        Assert.Equal(new InventorySnapshot(0, 0, 0, 0), await Snapshot(otherDb));
        await using var ownerAfterDb = _fixture.OpenDb(_fixture.TenantA);
        Assert.Equal(ownerBefore, await Snapshot(ownerAfterDb));
    }

    private static async Task<InventorySnapshot> Snapshot(WarehouseDbContext db) => new(
        await db.Stocks.CountAsync(),
        await db.Lotes.CountAsync(),
        await db.Movimientos.CountAsync(),
        await db.Stocks.SumAsync(s => s.CantidadActual));

    private sealed record InventorySnapshot(int Stocks, int Lotes, int Movimientos, decimal Cantidad);
    private sealed record StockResponse(Guid ProductoId, Guid SucursalId, decimal CantidadActual);
}

public sealed class RecepcionesPostgresFixture : IAsyncLifetime
{
    private const string JwtKey = "Recepciones-integration-tests-signing-key-2026";
    private readonly string _schema = "recepciones_" + Guid.NewGuid().ToString("N");
    private string _baseConnection = null!;
    private string _connection = null!;
    private RecepcionesFactory _factory = null!;

    public Guid TenantA { get; } = Guid.NewGuid();
    public Guid TenantB { get; } = Guid.NewGuid();
    public Guid ProductoId { get; } = Guid.NewGuid();
    public Guid SucursalId { get; } = Guid.NewGuid();
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _baseConnection = Environment.GetEnvironmentVariable("GLOBALMART_TEST_POSTGRES")
            ?? throw new InvalidOperationException("Defina GLOBALMART_TEST_POSTGRES apuntando a PostgreSQL de pruebas.");
        await using (var connection = new NpgsqlConnection(_baseConnection))
        {
            await connection.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE SCHEMA \"{_schema}\"", connection);
            await create.ExecuteNonQueryAsync();
        }

        _connection = new NpgsqlConnectionStringBuilder(_baseConnection)
        {
            SearchPath = _schema,
            Pooling = false
        }.ConnectionString;
        await using (var db = OpenDb(Guid.Empty))
            await db.Database.MigrateAsync();

        _factory = new RecepcionesFactory(this, _connection);
        Client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
    }

    public void Authenticate(Guid tenantId)
    {
        var token = new JwtSecurityToken(
            issuer: "RecepcionesTests",
            audience: "RecepcionesTests",
            claims: new[] { new Claim("tenant_id", tenantId.ToString()), new Claim(ClaimTypes.Role, "ADMIN") },
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey)), SecurityAlgorithms.HmacSha256));
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", new JwtSecurityTokenHandler().WriteToken(token));
    }

    public object Request(decimal cantidad) => new
    {
        productoId = ProductoId,
        sucursalId = SucursalId,
        cantidad,
        fechaVencimiento = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
        ubicacion = "Bodega integración"
    };

    public WarehouseDbContext OpenDb(Guid tenantId) => new(
        new DbContextOptionsBuilder<WarehouseDbContext>().UseNpgsql(_connection).Options)
    {
        CurrentTenantId = tenantId
    };

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (_factory is not null) await _factory.DisposeAsync();
        if (string.IsNullOrEmpty(_baseConnection)) return;
        NpgsqlConnection.ClearAllPools();
        await using var connection = new NpgsqlConnection(_baseConnection);
        await connection.OpenAsync();
        await using var drop = new NpgsqlCommand($"DROP SCHEMA IF EXISTS \"{_schema}\" CASCADE", connection);
        await drop.ExecuteNonQueryAsync();
    }

    private sealed class RecepcionesFactory(
        RecepcionesPostgresFixture fixture,
        string connection) : WebApplicationFactory<WarehouseInventoryServiceAlias::Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connection,
                ["Kafka:Enabled"] = "false",
                ["Jwt:Key"] = JwtKey,
                ["Jwt:Issuer"] = "RecepcionesTests",
                ["Jwt:Audience"] = "RecepcionesTests"
            }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IHostedService>();
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(new TenantAwareHttpClientFactory(fixture));
            });
        }
    }

    private sealed class TenantAwareHttpClientFactory(RecepcionesPostgresFixture fixture) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new TenantAwareHandler(fixture));
    }

    private sealed class TenantAwareHandler(RecepcionesPostgresFixture fixture) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var token = new JwtSecurityTokenHandler().ReadJwtToken(request.Headers.Authorization!.Parameter);
            var tenantId = Guid.Parse(token.Claims.Single(c => c.Type == "tenant_id").Value);
            HttpResponseMessage response;
            if (request.RequestUri!.AbsolutePath.StartsWith("/products/", StringComparison.Ordinal))
            {
                var visible = tenantId == fixture.TenantA &&
                    request.RequestUri.AbsolutePath.EndsWith(fixture.ProductoId.ToString(), StringComparison.OrdinalIgnoreCase);
                response = new(visible ? HttpStatusCode.OK : HttpStatusCode.NotFound);
            }
            else if (request.RequestUri.AbsolutePath == "/sucursales")
            {
                var branches = tenantId == fixture.TenantA
                    ? new[] { new BranchResponse(fixture.SucursalId, true) }
                    : Array.Empty<BranchResponse>();
                response = new(HttpStatusCode.OK) { Content = JsonContent.Create(branches) };
            }
            else
            {
                response = new(HttpStatusCode.NotFound);
            }
            return Task.FromResult(response);
        }
    }

    private sealed record BranchResponse(Guid Id, bool Activa);
}

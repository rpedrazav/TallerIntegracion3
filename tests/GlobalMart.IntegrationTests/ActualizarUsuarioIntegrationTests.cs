extern alias TenantIdentityServiceAlias;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Xunit;

namespace GlobalMart.IntegrationTests;

public class ActualizarUsuarioIntegrationTests : IClassFixture<Ms1WebApplicationFactory>, IAsyncLifetime
{
    private const string JwtKey = "TI3-454-integration-tests-signing-key-only-2026";
    private readonly Ms1WebApplicationFactory _factory;
    private readonly string _schema = "ti3454_" + Guid.NewGuid().ToString("N");
    private readonly Guid _tenantA = Guid.NewGuid();
    private readonly Guid _tenantB = Guid.NewGuid();
    private readonly Guid _userA = Guid.NewGuid();
    private readonly Guid _userB = Guid.NewGuid();
    private readonly Guid _otherUserA = Guid.NewGuid();
    private readonly Guid _sucursal = Guid.NewGuid();
    private string _connectionString = null!;
    private WebApplicationFactory<TenantIdentityServiceAlias::Program>? _configuredFactory;
    private HttpClient _client = null!;

    public ActualizarUsuarioIntegrationTests(Ms1WebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        var connection = Environment.GetEnvironmentVariable("TI3454_TEST_CONNECTION")
            ?? throw new InvalidOperationException("Configure TI3454_TEST_CONNECTION con una base PostgreSQL de pruebas.");
        var settings = new NpgsqlConnectionStringBuilder(connection) { SearchPath = _schema };
        _connectionString = settings.ConnectionString;
        await using var db = new NpgsqlConnection(_connectionString);
        await db.OpenAsync();
        // Nombre generado internamente; ningún identificador procede de una petición.
        await using var setup = new NpgsqlCommand($"""
            CREATE SCHEMA "{_schema}";
            CREATE TABLE "{_schema}".users (
                id uuid PRIMARY KEY,
                tenant_id uuid NOT NULL,
                sucursal_id uuid NULL,
                email varchar(200) NOT NULL,
                password_hash text NOT NULL,
                role text NOT NULL,
                is_active boolean NOT NULL,
                last_login_at timestamptz NULL,
                created_at timestamptz NOT NULL,
                CONSTRAINT uq_users_tenant_email UNIQUE (tenant_id, email)
            );
            """, db);
        await setup.ExecuteNonQueryAsync();
        await SeedAsync(db, _userA, _tenantA, "original@example.test");
        await SeedAsync(db, _otherUserA, _tenantA, "ocupado@example.test");
        await SeedAsync(db, _userB, _tenantB, "otro-tenant@example.test");

        _configuredFactory = _factory.WithWebHostBuilder(builder =>
        {
            // Evita migraciones y seed Development sobre el esquema externo users.
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = _connectionString,
                    ["Jwt:Key"] = JwtKey,
                    ["Jwt:Issuer"] = "TI3454",
                    ["Jwt:Audience"] = "TI3454"
                }));
            // Program captura la clave antes del override de configuración del host de pruebas.
            builder.ConfigureTestServices(services => services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.TokenValidationParameters.ValidIssuer = "TI3454";
                    options.TokenValidationParameters.ValidAudience = "TI3454";
                    options.TokenValidationParameters.IssuerSigningKey =
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
                }));
        });
        _client = _configuredFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        var token = new JwtSecurityToken(
            issuer: "TI3454", audience: "TI3454",
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
                new Claim("tenant_id", _tenantA.ToString()),
                new Claim(ClaimTypes.Role, "ADMIN")
            },
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey)), SecurityAlgorithms.HmacSha256));
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", new JwtSecurityTokenHandler().WriteToken(token));
    }

    [Fact]
    public async Task Put_UsuarioDelMismoTenant_Returns200YPersisteSoloDatosBasicos()
    {
        var before = await SnapshotAsync(_userA, _tenantA);
        // El email de B se puede usar en A: la unicidad es por tenant.
        var response = await _client.PutAsJsonAsync($"/api/v1/users/{_userA}",
            new { Email = "otro-tenant@example.test", SucursalId = (Guid?)null });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(_userA, body.GetProperty("id").GetGuid());
        Assert.Equal(_tenantA, body.GetProperty("tenantId").GetGuid());
        Assert.Equal("otro-tenant@example.test", body.GetProperty("email").GetString());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("sucursalId").ValueKind);
        Assert.False(body.TryGetProperty("nombre", out _));
        Assert.False(body.TryGetProperty("passwordHash", out _));

        var after = await SnapshotAsync(_userA, _tenantA);
        Assert.Equal(before with { Email = "otro-tenant@example.test", SucursalId = null }, after);

        // Conservar el email propio y asignar sucursal también debe funcionar.
        var repeat = await _client.PutAsJsonAsync($"/api/v1/users/{_userA}",
            new { Email = after.Email, SucursalId = _sucursal });
        Assert.Equal(HttpStatusCode.OK, repeat.StatusCode);
        Assert.Equal(after with { SucursalId = _sucursal }, await SnapshotAsync(_userA, _tenantA));
    }

    [Fact]
    public async Task Put_EmailDuplicadoEnMismoTenant_Returns409SinCambios()
    {
        var before = await SnapshotAsync(_userA, _tenantA);
        var otherBefore = await SnapshotAsync(_otherUserA, _tenantA);
        var response = await _client.PutAsJsonAsync($"/api/v1/users/{_userA}",
            new { Email = "ocupado@example.test", SucursalId = (Guid?)null });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(before, await SnapshotAsync(_userA, _tenantA));
        Assert.Equal(otherBefore, await SnapshotAsync(_otherUserA, _tenantA));
    }

    [Fact]
    public async Task Put_UsuarioDeOtroTenant_Returns404SinCambios()
    {
        var before = await SnapshotAsync(_userB, _tenantB);
        var response = await _client.PutAsJsonAsync($"/api/v1/users/{_userB}",
            new { Email = "intrusion@example.test", SucursalId = _sucursal });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(before, await SnapshotAsync(_userB, _tenantB));
    }

    private async Task SeedAsync(NpgsqlConnection db, Guid id, Guid tenant, string email)
    {
        await using var command = new NpgsqlCommand("""
            INSERT INTO users (id, tenant_id, sucursal_id, email, password_hash, role,
                               is_active, last_login_at, created_at)
            VALUES (@id, @tenant, @sucursal, @email, 'hash-no-modificar', 'CAJERO', true,
                    '2026-10-01T10:00:00Z', '2026-09-01T10:00:00Z')
            """, db);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("tenant", tenant);
        command.Parameters.AddWithValue("sucursal", _sucursal);
        command.Parameters.AddWithValue("email", email);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<UserSnapshot> SnapshotAsync(Guid id, Guid tenant)
    {
        // Conexión independiente: comprueba el estado realmente persistido.
        await using var db = new NpgsqlConnection(_connectionString);
        await db.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT id, tenant_id, sucursal_id, email, password_hash, role,
                   is_active, last_login_at, created_at
            FROM users WHERE id = @id AND tenant_id = @tenant
            """, db);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("tenant", tenant);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new UserSnapshot(reader.GetGuid(0), reader.GetGuid(1),
            reader.IsDBNull(2) ? null : reader.GetGuid(2), reader.GetString(3),
            reader.GetString(4), reader.GetString(5), reader.GetBoolean(6),
            reader.IsDBNull(7) ? null : reader.GetDateTime(7), reader.GetDateTime(8));
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_configuredFactory is not null)
            await _configuredFactory.DisposeAsync();
        if (_connectionString is null)
            return;
        await using var db = new NpgsqlConnection(_connectionString);
        await db.OpenAsync();
        await using var cleanup = new NpgsqlCommand($"DROP SCHEMA IF EXISTS \"{_schema}\" CASCADE", db);
        await cleanup.ExecuteNonQueryAsync();
    }

    private record UserSnapshot(Guid Id, Guid TenantId, Guid? SucursalId, string Email,
        string PasswordHash, string Role, bool IsActive, DateTime? LastLoginAt, DateTime CreatedAt);
}

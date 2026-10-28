extern alias TenantIdentityServiceAlias;

using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using TenantDbContext = TenantIdentityServiceAlias::TenantIdentityService.Data.TenantDbContext;
using Tenant = TenantIdentityServiceAlias::TenantIdentityService.Models.Tenant;
using Usuario = TenantIdentityServiceAlias::TenantIdentityService.Models.Usuario;
using Sucursal = TenantIdentityServiceAlias::TenantIdentityService.Models.Sucursal;
using UsuarioSucursal = TenantIdentityServiceAlias::TenantIdentityService.Models.UsuarioSucursal;

namespace GlobalMart.IntegrationTests;

/// <summary>Esquema PostgreSQL aislado que reproduce las migraciones reales de MS-1.</summary>
internal sealed class AdminUsersTestData : IAsyncDisposable
{
    private const string JwtKey = "Admin-users-integration-tests-signing-key-2026";
    private readonly Ms1WebApplicationFactory _baseFactory;
    private readonly string _schema = "admin_users_" + Guid.NewGuid().ToString("N");
    private string? _connectionString;
    private WebApplicationFactory<TenantIdentityServiceAlias::Program>? _factory;

    public Guid TenantA { get; } = Guid.NewGuid();
    public Guid TenantB { get; } = Guid.NewGuid();
    public Guid AdminId { get; } = Guid.NewGuid();
    public Guid UserA { get; } = Guid.NewGuid();
    public Guid OtherUserA { get; } = Guid.NewGuid();
    public Guid UserB { get; } = Guid.NewGuid();
    public Guid BranchA { get; } = Guid.NewGuid();
    public HttpClient Client { get; private set; } = null!;

    public AdminUsersTestData(Ms1WebApplicationFactory baseFactory) => _baseFactory = baseFactory;

    public async Task InitializeAsync()
    {
        var baseConnection = _baseFactory.Services.GetRequiredService<IConfiguration>()
            .GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Falta conexión de MS-1.");
        await using (var connection = new NpgsqlConnection(baseConnection))
        {
            await connection.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE SCHEMA \"{_schema}\"", connection);
            await create.ExecuteNonQueryAsync();
        }
        _connectionString = new NpgsqlConnectionStringBuilder(baseConnection) { SearchPath = _schema }.ConnectionString;

        await using (var db = OpenDb())
        {
            await db.Database.MigrateAsync();
            db.Tenants.AddRange(NewTenant(TenantA), NewTenant(TenantB));
            db.Sucursales.Add(new Sucursal { Id = BranchA, TenantId = TenantA, Nombre = "Centro", Direccion = "Calle 1" });
            db.Usuarios.AddRange(
                NewUser(AdminId, TenantA, "admin@example.test"),
                NewUser(UserA, TenantA, "original@example.test"),
                NewUser(OtherUserA, TenantA, "ocupado@example.test"),
                NewUser(UserB, TenantB, "otro-tenant@example.test"));
            db.UsuarioSucursales.Add(new UsuarioSucursal { UsuarioId = UserA, SucursalId = BranchA });
            await db.SaveChangesAsync();
        }

        _factory = _baseFactory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                ["Jwt:Key"] = JwtKey,
                ["Jwt:Issuer"] = "AdminUsersTests",
                ["Jwt:Audience"] = "AdminUsersTests"
            }));
            builder.ConfigureTestServices(services => services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.TokenValidationParameters.ValidIssuer = "AdminUsersTests";
                    options.TokenValidationParameters.ValidAudience = "AdminUsersTests";
                    options.TokenValidationParameters.IssuerSigningKey =
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
                }));
        });
        Client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        Authenticate(AdminId, "ADMIN");
    }

    public void Authenticate(Guid userId, string role)
    {
        var jwt = new JwtSecurityToken(
            issuer: "AdminUsersTests", audience: "AdminUsersTests",
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim("tenant_id", TenantA.ToString()),
                new Claim(ClaimTypes.Role, role)
            },
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey)), SecurityAlgorithms.HmacSha256));
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(jwt));
    }

    public async Task<UserSnapshot> SnapshotAsync(Guid id)
    {
        await using var db = OpenDb();
        var user = await db.Usuarios.IgnoreQueryFilters().AsNoTracking()
            .Include(u => u.UsuarioSucursales).SingleAsync(u => u.Id == id);
        return new UserSnapshot(user.Id, user.TenantId, user.Email, user.PasswordHash,
            user.Activo, user.CreadoEn, user.UltimoLogin,
            user.UsuarioSucursales.Select(s => (Guid?)s.SucursalId).FirstOrDefault());
    }

    public async ValueTask DisposeAsync()
    {
        Client?.Dispose();
        if (_factory is not null) await _factory.DisposeAsync();
        if (_connectionString is null) return;
        var connectionString = new NpgsqlConnectionStringBuilder(_connectionString) { SearchPath = "public" }.ConnectionString;
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var cleanup = new NpgsqlCommand($"DROP SCHEMA IF EXISTS \"{_schema}\" CASCADE", connection);
        await cleanup.ExecuteNonQueryAsync();
    }

    private TenantDbContext OpenDb() => new(new DbContextOptionsBuilder<TenantDbContext>()
        .UseNpgsql(_connectionString, options => options.MigrationsHistoryTable("__EFMigrationsHistory", _schema)).Options);

    private static Tenant NewTenant(Guid id) => new()
    {
        Id = id, Nombre = $"Test {id:N}", Pais = "CL", Moneda = "CLP",
        Idioma = "es", ZonaHoraria = "America/Santiago", PorcentajeIva = 19
    };

    private static Usuario NewUser(Guid id, Guid tenantId, string email) => new()
    {
        Id = id, TenantId = tenantId, Nombre = "Usuario de prueba", Email = email,
        PasswordHash = "hash-no-modificar", Activo = true,
        CreadoEn = new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc),
        UltimoLogin = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc)
    };
}

internal sealed record UserSnapshot(Guid Id, Guid TenantId, string Email, string PasswordHash,
    bool Activo, DateTime CreadoEn, DateTime? UltimoLogin, Guid? SucursalId);

extern alias CatalogPricingServiceAlias;

using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using CatalogDbContext = CatalogPricingServiceAlias::CatalogPricingService.Data.CatalogDbContext;

namespace GlobalMart.IntegrationTests.Fixtures;

internal static class Rn01HttpSupport
{
    private const string Key = "S2-023-RN01-http-tests-signing-key-2026-only";
    private const string Issuer = "S2023Tests";

    public static void ConfigureJwt(IServiceCollection services) =>
        services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.TokenValidationParameters.ValidIssuer = Issuer;
            options.TokenValidationParameters.ValidAudience = Issuer;
            options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key));
        });

    public static void Authenticate(HttpClient client, TenantData tenant, string role = "ADMIN")
    {
        var token = new JwtSecurityToken(Issuer, Issuer, new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, tenant.CajeroId.ToString()),
            new Claim("tenant_id", tenant.TenantId.ToString()),
            new Claim("sucursal_id", tenant.SucursalId.ToString()),
            new Claim(ClaimTypes.Role, role)
        }, expires: DateTime.UtcNow.AddMinutes(15), signingCredentials: new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)), SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
    }
}

internal sealed class Rn01CatalogFactory(TwoTenantFixture fixture) : Ms3WebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<CatalogDbContext>>();
            services.AddSingleton(new DbContextOptionsBuilder<CatalogDbContext>()
                .UseNpgsql(fixture.Connection("catalog"))
                .ReplaceService<IMigrator, FixtureModelMigrator>().Options);
            Rn01HttpSupport.ConfigureJwt(services);
        });
    }
}


public sealed class FixtureModelMigrator : IMigrator
{
    public void Migrate(string? targetMigration = null) { }
    public Task MigrateAsync(string? targetMigration = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public string GenerateScript(string? fromMigration = null, string? toMigration = null,
        MigrationsSqlGenerationOptions options = MigrationsSqlGenerationOptions.Default) =>
        throw new NotSupportedException("Fixture schemas use the current EF model, not migration scripts.");
}

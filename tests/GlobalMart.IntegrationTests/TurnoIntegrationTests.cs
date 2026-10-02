extern alias POSCartServiceAlias;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PosCartDbContext = POSCartServiceAlias::POSCartService.Data.PosCartDbContext;
using EstadoTurno = POSCartServiceAlias::POSCartService.Models.EstadoTurno;
using Turno = POSCartServiceAlias::POSCartService.Models.Turno;
using Xunit;

namespace GlobalMart.IntegrationTests;

public class TurnoIntegrationTests : IClassFixture<Ms5WebApplicationFactory>
{
    private readonly Ms5WebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TurnoIntegrationTests(Ms5WebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AbrirTurno_ConJwtCajero_GuardaTurnoAbiertoEnDb()
    {
        var tenantId = Guid.NewGuid();
        var cajeroId = Guid.NewGuid();
        var sucursalId = Guid.NewGuid();
        const decimal montoFondoInicial = 50000m;

        var configuration = _factory.Services.GetRequiredService<IConfiguration>();
        var token = CreateCajeroToken(configuration, tenantId, cajeroId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using (var migrationScope = _factory.Services.CreateScope())
        {
            var migrationDb = migrationScope.ServiceProvider.GetRequiredService<PosCartDbContext>();
            await migrationDb.Database.MigrateAsync();
        }

        var request = new
        {
            sucursal_id = sucursalId,
            monto_fondo_inicial = montoFondoInicial
        };

        var response = await _client.PostAsJsonAsync("/turnos/abrir", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PosCartDbContext>();
        Turno? turno = await db.Turnos
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(t => t.CajeroId == cajeroId
                                    && t.TenantId == tenantId
                                    && t.SucursalId == sucursalId);

        Assert.NotNull(turno);
        Assert.Equal(montoFondoInicial, turno.MontoApertura);
        Assert.Equal(EstadoTurno.ABIERTO, turno.Estado);
    }

    private static string CreateCajeroToken(IConfiguration configuration, Guid tenantId, Guid cajeroId)
    {
        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key no configurada.");
        var issuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("JWT Issuer no configurado.");
        var audience = configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("JWT Audience no configurado.");

        var claims = new[]
        {
            new Claim("tenant_id", tenantId.ToString()),
            new Claim("sub", cajeroId.ToString()),
            new Claim("cajero_id", cajeroId.ToString()),
            new Claim(ClaimTypes.Role, "CAJERO")
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

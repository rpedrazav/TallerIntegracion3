extern alias POSCartServiceAlias;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using EstadoVenta = POSCartServiceAlias::POSCartService.Models.EstadoVenta;
using MetodoPago = POSCartServiceAlias::POSCartService.Models.MetodoPago;
using MetodoPagoVenta = POSCartServiceAlias::POSCartService.Models.MetodoPagoVenta;
using PosCartDbContext = POSCartServiceAlias::POSCartService.Data.PosCartDbContext;
using Venta = POSCartServiceAlias::POSCartService.Models.Venta;
using ItemVenta = POSCartServiceAlias::POSCartService.Models.ItemVenta;
using Pago = POSCartServiceAlias::POSCartService.Models.Pago;
using Xunit;

namespace GlobalMart.IntegrationTests;

public class CierreTurnoIntegrationTests : IClassFixture<Ms5WebApplicationFactory>
{
    private readonly Ms5WebApplicationFactory _factory;

    public CierreTurnoIntegrationTests(Ms5WebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CerrarTurno_ConVentaEnEfectivo_CalculaCuadreYDiferenciaCorrectamente()
    {
        var tenantId = Guid.NewGuid();
        var cajeroId = Guid.NewGuid();
        var sucursalId = Guid.NewGuid();

        var configuredFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
                services.Configure<MvcOptions>(options => options.ModelValidatorProviders.Clear()));
        });

        var configuration = configuredFactory.Services.GetRequiredService<IConfiguration>();
        var token = CreateCajeroToken(configuration, tenantId, cajeroId);

        using var client = configuredFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var abrirTurnoResponse = await client.PostAsJsonAsync("/turnos/abrir", new
        {
            sucursal_id = sucursalId,
            monto_fondo_inicial = 1000m
        });
        Assert.Equal(HttpStatusCode.OK, abrirTurnoResponse.StatusCode);

        var turnoId = await ReadGuidPropertyAsync(abrirTurnoResponse, "id");

        using (var scope = configuredFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PosCartDbContext>();
            await db.Database.MigrateAsync();

            var ventaId = Guid.NewGuid();
            db.Ventas.Add(new Venta
            {
                Id = ventaId,
                TenantId = tenantId,
                TurnoId = turnoId,
                CajeroId = cajeroId,
                SucursalId = sucursalId,
                Subtotal = 125m,
                Impuestos = 0m,
                Total = 125m,
                MetodoPago = MetodoPagoVenta.EFECTIVO,
                Estado = EstadoVenta.COMPLETADA,
                Items = new List<ItemVenta>
                {
                    new()
                    {
                        ProductoId = Guid.NewGuid(),
                        NombreProducto = "Producto de cuadre",
                        Cantidad = 1m,
                        PrecioUnitario = 125m,
                        Subtotal = 125m
                    }
                },
                Pagos = new List<Pago>
                {
                    new()
                    {
                        Metodo = MetodoPago.EFECTIVO,
                        Monto = 125m,
                        Vuelto = 0m
                    }
                }
            });

            await db.SaveChangesAsync();
        }

        var cuadreResponse = await client.PostAsJsonAsync("/turnos/cuadre", new
        {
            monto_declarado = 100m
        });

        Assert.Equal(HttpStatusCode.OK, cuadreResponse.StatusCode);

        using var responseDocument = JsonDocument.Parse(await cuadreResponse.Content.ReadAsStringAsync());
        var responseJson = responseDocument.RootElement;
        Assert.Equal(125m, responseJson.GetProperty("efectivo_esperado").GetDecimal());
        Assert.Equal(100m, responseJson.GetProperty("monto_declarado").GetDecimal());
        Assert.Equal(-25m, responseJson.GetProperty("diferencia").GetDecimal());
    }

    private static async Task<Guid> ReadGuidPropertyAsync(HttpResponseMessage response, string propertyName)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty(propertyName).GetGuid();
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

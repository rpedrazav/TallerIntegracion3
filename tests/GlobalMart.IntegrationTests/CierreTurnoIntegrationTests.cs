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

        var abrirTurnoResponse = await client.PostAsJsonAsync("/api/turnos/abrir", new
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

        var cuadreResponse = await client.PostAsJsonAsync("/api/turnos/cuadre", new
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

    [Fact]
    [Trait("Plan", "S3-042")]
    public async Task Cuadre_ConVentaAnulada_ExcluyeEfectivoDeVentaAnulada()
    {
        var tenantId = Guid.NewGuid();
        var cajeroId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var sucursalId = Guid.NewGuid();

        var configuredFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
                services.Configure<MvcOptions>(options => options.ModelValidatorProviders.Clear()));
        });

        var configuration = configuredFactory.Services.GetRequiredService<IConfiguration>();
        var cajeroToken = CreateCajeroToken(configuration, tenantId, cajeroId);
        var adminToken = CreateAdminToken(configuration, tenantId, adminId);

        using var cajeroClient = configuredFactory.CreateClient();
        cajeroClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cajeroToken);

        using var adminClient = configuredFactory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // 1. Abrir turno para el cajero
        var abrirTurnoResponse = await cajeroClient.PostAsJsonAsync("/api/turnos/abrir", new
        {
            sucursal_id = sucursalId,
            monto_fondo_inicial = 500m
        });
        Assert.Equal(HttpStatusCode.OK, abrirTurnoResponse.StatusCode);
        var turnoId = await ReadGuidPropertyAsync(abrirTurnoResponse, "id");

        var ventaAId = Guid.NewGuid();
        var ventaBId = Guid.NewGuid();

        // 2. Registrar 2 ventas en efectivo completadas con pago:
        // Venta A: Total 119 (Recibido 200, Vuelto 81 -> Neto 119)
        // Venta B: Total 60 (Recibido 100, Vuelto 40 -> Neto 60)
        using (var scope = configuredFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PosCartDbContext>();
            await db.Database.MigrateAsync();

            db.Ventas.Add(new Venta
            {
                Id = ventaAId,
                TenantId = tenantId,
                TurnoId = turnoId,
                CajeroId = cajeroId,
                SucursalId = sucursalId,
                Subtotal = 119m,
                Impuestos = 0m,
                Total = 119m,
                MetodoPago = MetodoPagoVenta.EFECTIVO,
                Estado = EstadoVenta.COMPLETADA,
                Items = new List<ItemVenta>
                {
                    new()
                    {
                        ProductoId = Guid.NewGuid(),
                        NombreProducto = "Producto A",
                        Cantidad = 1m,
                        PrecioUnitario = 119m,
                        Subtotal = 119m
                    }
                },
                Pagos = new List<Pago>
                {
                    new()
                    {
                        Metodo = MetodoPago.EFECTIVO,
                        Monto = 200m,
                        Vuelto = 81m
                    }
                }
            });

            db.Ventas.Add(new Venta
            {
                Id = ventaBId,
                TenantId = tenantId,
                TurnoId = turnoId,
                CajeroId = cajeroId,
                SucursalId = sucursalId,
                Subtotal = 60m,
                Impuestos = 0m,
                Total = 60m,
                MetodoPago = MetodoPagoVenta.EFECTIVO,
                Estado = EstadoVenta.COMPLETADA,
                Items = new List<ItemVenta>
                {
                    new()
                    {
                        ProductoId = Guid.NewGuid(),
                        NombreProducto = "Producto B",
                        Cantidad = 1m,
                        PrecioUnitario = 60m,
                        Subtotal = 60m
                    }
                },
                Pagos = new List<Pago>
                {
                    new()
                    {
                        Metodo = MetodoPago.EFECTIVO,
                        Monto = 100m,
                        Vuelto = 40m
                    }
                }
            });

            await db.SaveChangesAsync();
        }

        // 3. Cuadre inicial: Total efectivo esperado = 119 + 60 = 179
        var cuadre1Response = await cajeroClient.PostAsJsonAsync("/api/turnos/cuadre", new
        {
            monto_declarado = 179m
        });
        Assert.Equal(HttpStatusCode.OK, cuadre1Response.StatusCode);
        using var doc1 = JsonDocument.Parse(await cuadre1Response.Content.ReadAsStringAsync());
        Assert.Equal(179m, doc1.RootElement.GetProperty("efectivo_esperado").GetDecimal());
        Assert.Equal(179m, doc1.RootElement.GetProperty("monto_declarado").GetDecimal());
        Assert.Equal(0m, doc1.RootElement.GetProperty("diferencia").GetDecimal());

        // 4. Anular venta A mediante POST /api/ventas/{id}/anular con token ADMIN y motivo
        var anularResponse = await adminClient.PostAsJsonAsync($"/api/ventas/{ventaAId}/anular", new
        {
            motivo = "Error de digitacion en caja"
        });
        Assert.Equal(HttpStatusCode.OK, anularResponse.StatusCode);

        // 5. Cuadre posterior a anulación: Efectivo esperado debe excluir venta A y ser únicamente 60
        var cuadre2Response = await cajeroClient.PostAsJsonAsync("/api/turnos/cuadre", new
        {
            monto_declarado = 60m
        });
        Assert.Equal(HttpStatusCode.OK, cuadre2Response.StatusCode);
        using var doc2 = JsonDocument.Parse(await cuadre2Response.Content.ReadAsStringAsync());
        Assert.Equal(60m, doc2.RootElement.GetProperty("efectivo_esperado").GetDecimal());
        Assert.Equal(60m, doc2.RootElement.GetProperty("monto_declarado").GetDecimal());
        Assert.Equal(0m, doc2.RootElement.GetProperty("diferencia").GetDecimal());

        // 6. Repetir la invocación de anulación sobre la misma venta (409 Conflict) y validar que cuadre sigue en 60
        var reanularResponse = await adminClient.PostAsJsonAsync($"/api/ventas/{ventaAId}/anular", new
        {
            motivo = "Reintento de anulacion"
        });
        Assert.Equal(HttpStatusCode.Conflict, reanularResponse.StatusCode);

        var cuadre3Response = await cajeroClient.PostAsJsonAsync("/api/turnos/cuadre", new
        {
            monto_declarado = 60m
        });
        Assert.Equal(HttpStatusCode.OK, cuadre3Response.StatusCode);
        using var doc3 = JsonDocument.Parse(await cuadre3Response.Content.ReadAsStringAsync());
        Assert.Equal(60m, doc3.RootElement.GetProperty("efectivo_esperado").GetDecimal());
        Assert.Equal(0m, doc3.RootElement.GetProperty("diferencia").GetDecimal());

        // 7. Verificar que los registros históricos de pagos de la venta A persisten en BD y su estado es ANULADA
        using (var scope = configuredFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PosCartDbContext>();
            db.CurrentTenantId = tenantId;
            var ventaAEnDb = await db.Ventas.Include(v => v.Pagos).SingleAsync(v => v.Id == ventaAId);
            Assert.Equal(EstadoVenta.ANULADA, ventaAEnDb.Estado);
            Assert.Single(ventaAEnDb.Pagos);
            Assert.Equal(200m, ventaAEnDb.Pagos.First().Monto);
            Assert.Equal(81m, ventaAEnDb.Pagos.First().Vuelto);
        }
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

    private static string CreateAdminToken(IConfiguration configuration, Guid tenantId, Guid adminId)
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
            new Claim("sub", adminId.ToString()),
            new Claim("admin_id", adminId.ToString()),
            new Claim(ClaimTypes.Role, "ADMIN")
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

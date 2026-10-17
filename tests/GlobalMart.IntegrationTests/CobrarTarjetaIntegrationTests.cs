extern alias POSCartServiceAlias;

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
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using Pos = POSCartServiceAlias::POSCartService;

namespace GlobalMart.IntegrationTests;

[Trait("Plan", "S2-033")]
public sealed class CobrarTarjetaIntegrationTests : IClassFixture<Ms5WebApplicationFactory>
{
    private readonly Ms5WebApplicationFactory _factory;

    public CobrarTarjetaIntegrationTests(Ms5WebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CobrarTarjeta_VentaCompletada_Retorna409SinLlamarPasarela()
    {
        var tenantId = Guid.NewGuid();
        var cajeroId = Guid.NewGuid();
        var sucursalId = Guid.NewGuid();
        var turnoId = Guid.NewGuid();
        var ventaId = Guid.NewGuid();

        var gatewayMock = new RecordingPaymentGatewayClient();

        var configuredFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<Pos.Services.IPaymentGatewayClient>(gatewayMock);
            });
        });

        // Crear turno ABIERTO y venta COMPLETADA directamente en base
        using (var scope = configuredFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Pos.Data.PosCartDbContext>();
            await db.Database.MigrateAsync();

            db.CurrentTenantId = tenantId;
            db.Turnos.Add(new Pos.Models.Turno
            {
                Id = turnoId,
                TenantId = tenantId,
                CajeroId = cajeroId,
                SucursalId = sucursalId,
                MontoApertura = 10000m,
                Estado = Pos.Models.EstadoTurno.ABIERTO
            });

            db.Ventas.Add(new Pos.Models.Venta
            {
                Id = ventaId,
                TenantId = tenantId,
                TurnoId = turnoId,
                CajeroId = cajeroId,
                SucursalId = sucursalId,
                Subtotal = 10000m,
                Impuestos = 1900m,
                Total = 11900m,
                MetodoPago = Pos.Models.MetodoPagoVenta.TARJETA,
                Estado = Pos.Models.EstadoVenta.COMPLETADA // YA COMPLETADA
            });
            await db.SaveChangesAsync();
        }

        var configuration = configuredFactory.Services.GetRequiredService<IConfiguration>();
        var token = CreateCajeroToken(configuration, tenantId, cajeroId);

        using var client = configuredFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync($"/ventas/{ventaId}/cobrar-tarjeta", new
        {
            scenario_id = "sim-approved-v1"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        // Criterio de Aceptación: Venta completada NO llama a la pasarela
        Assert.Equal(0, gatewayMock.CallCount);
    }

    [Fact]
    public async Task CobrarTarjeta_VentaAjenaDeOtroTenant_Retorna404SinLlamarPasarela()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var cajeroA = Guid.NewGuid();
        var cajeroB = Guid.NewGuid();
        var sucursalId = Guid.NewGuid();
        var turnoIdA = Guid.NewGuid();
        var turnoIdB = Guid.NewGuid();
        var ventaIdA = Guid.NewGuid();

        var gatewayMock = new RecordingPaymentGatewayClient();

        var configuredFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<Pos.Services.IPaymentGatewayClient>(gatewayMock);
            });
        });

        // Crear venta en Tenant A y turno abierto para Cajero B en Tenant B
        using (var scope = configuredFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Pos.Data.PosCartDbContext>();
            await db.Database.MigrateAsync();

            db.CurrentTenantId = null; // Bypass para preparar datos de ambos tenants

            db.Turnos.AddRange(
                new Pos.Models.Turno
                {
                    Id = turnoIdA,
                    TenantId = tenantA,
                    CajeroId = cajeroA,
                    SucursalId = sucursalId,
                    MontoApertura = 10000m,
                    Estado = Pos.Models.EstadoTurno.ABIERTO
                },
                new Pos.Models.Turno
                {
                    Id = turnoIdB,
                    TenantId = tenantB,
                    CajeroId = cajeroB,
                    SucursalId = sucursalId,
                    MontoApertura = 10000m,
                    Estado = Pos.Models.EstadoTurno.ABIERTO
                }
            );

            db.Ventas.Add(new Pos.Models.Venta
            {
                Id = ventaIdA,
                TenantId = tenantA,
                TurnoId = turnoIdA,
                CajeroId = cajeroA,
                SucursalId = sucursalId,
                Subtotal = 5000m,
                Impuestos = 950m,
                Total = 5950m,
                MetodoPago = Pos.Models.MetodoPagoVenta.TARJETA,
                Estado = Pos.Models.EstadoVenta.PENDIENTE
            });

            await db.SaveChangesAsync();
        }

        // Cajero B (Tenant B) intenta cobrar la venta de Tenant A
        var configuration = configuredFactory.Services.GetRequiredService<IConfiguration>();
        var tokenB = CreateCajeroToken(configuration, tenantB, cajeroB);

        using var client = configuredFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);

        var response = await client.PostAsJsonAsync($"/ventas/{ventaIdA}/cobrar-tarjeta", new
        {
            scenario_id = "sim-approved-v1"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        // Criterio de Aceptación: Venta ajena NO llama a la pasarela
        Assert.Equal(0, gatewayMock.CallCount);
    }

    [Fact]
    public async Task CobrarTarjeta_SinTurnoAbierto_Retorna409SinLlamarPasarela()
    {
        var tenantId = Guid.NewGuid();
        var cajeroId = Guid.NewGuid();
        var ventaId = Guid.NewGuid();

        var gatewayMock = new RecordingPaymentGatewayClient();

        var configuredFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<Pos.Services.IPaymentGatewayClient>(gatewayMock);
            });
        });

        using (var scope = configuredFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Pos.Data.PosCartDbContext>();
            await db.Database.MigrateAsync();
        }

        var configuration = configuredFactory.Services.GetRequiredService<IConfiguration>();
        var token = CreateCajeroToken(configuration, tenantId, cajeroId);

        using var client = configuredFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync($"/ventas/{ventaId}/cobrar-tarjeta", new
        {
            scenario_id = "sim-approved-v1"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        // Sin turno abierto NO se contacta a la pasarela
        Assert.Equal(0, gatewayMock.CallCount);
    }

    [Fact]
    public async Task CobrarTarjeta_SinJwt_Retorna401SinLlamarPasarela()
    {
        var gatewayMock = new RecordingPaymentGatewayClient();

        var configuredFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<Pos.Services.IPaymentGatewayClient>(gatewayMock);
            });
        });

        using var client = configuredFactory.CreateClient();
        // Sin header de autorización
        var response = await client.PostAsJsonAsync($"/ventas/{Guid.NewGuid()}/cobrar-tarjeta", new
        {
            scenario_id = "sim-approved-v1"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        // Sin JWT NO se contacta a la pasarela
        Assert.Equal(0, gatewayMock.CallCount);
    }

    [Fact]
    public async Task CobrarTarjeta_VentaEfectivo_Retorna400SinLlamarPasarela()
    {
        var tenantId = Guid.NewGuid();
        var cajeroId = Guid.NewGuid();
        var sucursalId = Guid.NewGuid();
        var turnoId = Guid.NewGuid();
        var ventaId = Guid.NewGuid();

        var gatewayMock = new RecordingPaymentGatewayClient();

        var configuredFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<Pos.Services.IPaymentGatewayClient>(gatewayMock);
            });
        });

        using (var scope = configuredFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Pos.Data.PosCartDbContext>();
            await db.Database.MigrateAsync();

            db.CurrentTenantId = tenantId;
            db.Turnos.Add(new Pos.Models.Turno
            {
                Id = turnoId,
                TenantId = tenantId,
                CajeroId = cajeroId,
                SucursalId = sucursalId,
                MontoApertura = 10000m,
                Estado = Pos.Models.EstadoTurno.ABIERTO
            });

            db.Ventas.Add(new Pos.Models.Venta
            {
                Id = ventaId,
                TenantId = tenantId,
                TurnoId = turnoId,
                CajeroId = cajeroId,
                SucursalId = sucursalId,
                Subtotal = 10000m,
                Impuestos = 1900m,
                Total = 11900m,
                MetodoPago = Pos.Models.MetodoPagoVenta.EFECTIVO, // EFECTIVO
                Estado = Pos.Models.EstadoVenta.PENDIENTE
            });
            await db.SaveChangesAsync();
        }

        var configuration = configuredFactory.Services.GetRequiredService<IConfiguration>();
        var token = CreateCajeroToken(configuration, tenantId, cajeroId);

        using var client = configuredFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync($"/ventas/{ventaId}/cobrar-tarjeta", new
        {
            scenario_id = "sim-approved-v1"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        // Venta con método EFECTIVO NO llama a pasarela
        Assert.Equal(0, gatewayMock.CallCount);
    }

    [Fact]
    public async Task CobrarTarjeta_VentaValidaPendiente_InvocaPasarelaYRetorna200()
    {
        var tenantId = Guid.NewGuid();
        var cajeroId = Guid.NewGuid();
        var sucursalId = Guid.NewGuid();
        var turnoId = Guid.NewGuid();
        var ventaId = Guid.NewGuid();
        var customKey = Guid.NewGuid().ToString();

        var gatewayMock = new RecordingPaymentGatewayClient
        {
            ResponseFunc = req => new Pos.DTOs.PaymentGatewayResponse
            {
                RequestId = req.RequestId,
                TransactionId = "TX-SIM-999",
                Status = "APPROVED",
                AuthorizationCode = "AUTH-OK-123",
                GatewayReference = "RRN-456",
                ProcessedAt = DateTimeOffset.UtcNow
            }
        };

        var configuredFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<Pos.Services.IPaymentGatewayClient>(gatewayMock);
            });
        });

        using (var scope = configuredFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Pos.Data.PosCartDbContext>();
            await db.Database.MigrateAsync();

            db.CurrentTenantId = tenantId;
            db.Turnos.Add(new Pos.Models.Turno
            {
                Id = turnoId,
                TenantId = tenantId,
                CajeroId = cajeroId,
                SucursalId = sucursalId,
                MontoApertura = 10000m,
                Estado = Pos.Models.EstadoTurno.ABIERTO
            });

            db.Ventas.Add(new Pos.Models.Venta
            {
                Id = ventaId,
                TenantId = tenantId,
                TurnoId = turnoId,
                CajeroId = cajeroId,
                SucursalId = sucursalId,
                Subtotal = 10000m,
                Impuestos = 1900m,
                Total = 11900m,
                MetodoPago = Pos.Models.MetodoPagoVenta.TARJETA,
                Estado = Pos.Models.EstadoVenta.PENDIENTE
            });
            await db.SaveChangesAsync();
        }

        var configuration = configuredFactory.Services.GetRequiredService<IConfiguration>();
        var token = CreateCajeroToken(configuration, tenantId, cajeroId);

        using var client = configuredFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync($"/ventas/{ventaId}/cobrar-tarjeta", new
        {
            idempotency_key = customKey,
            scenario_id = "sim-approved-v1"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Pasarela SÍ fue llamada 1 sola vez
        Assert.Equal(1, gatewayMock.CallCount);
        var capturedRequest = Assert.Single(gatewayMock.Requests);
        Assert.Equal(tenantId, capturedRequest.TenantId);
        Assert.Equal(ventaId, capturedRequest.SaleId);
        Assert.Equal(11900m, capturedRequest.Amount);
        Assert.Equal("CLP", capturedRequest.Currency);
        Assert.Equal("sim-approved-v1", capturedRequest.ScenarioId);

        var result = await response.Content.ReadFromJsonAsync<Pos.DTOs.CobrarTarjetaResponse>();
        Assert.NotNull(result);
        Assert.Equal("APPROVED", result!.Status);
        Assert.Equal("TX-SIM-999", result.TransactionId);
        Assert.Equal("AUTH-OK-123", result.AuthorizationCode);
        Assert.Equal("RRN-456", result.GatewayReference);
    }

    private static string CreateCajeroToken(IConfiguration configuration, Guid tenantId, Guid cajeroId)
    {
        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key no configurada");
        var issuer = configuration["Jwt:Issuer"] ?? "GlobalMartOS";
        var audience = configuration["Jwt:Audience"] ?? "GlobalMartOS_Clients";

        var claims = new[]
        {
            new Claim("tenant_id", tenantId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, cajeroId.ToString()),
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

    private sealed class RecordingPaymentGatewayClient : Pos.Services.IPaymentGatewayClient
    {
        public int CallCount { get; private set; }
        public List<Pos.DTOs.PaymentGatewayRequest> Requests { get; } = new();
        public Func<Pos.DTOs.PaymentGatewayRequest, Pos.DTOs.PaymentGatewayResponse>? ResponseFunc { get; set; }

        public Task<Pos.DTOs.PaymentGatewayResponse> ProcessPaymentAsync(
            Pos.DTOs.PaymentGatewayRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            Requests.Add(request);

            if (ResponseFunc != null)
                return Task.FromResult(ResponseFunc(request));

            return Task.FromResult(new Pos.DTOs.PaymentGatewayResponse
            {
                RequestId = request.RequestId,
                TransactionId = "TX-DEFAULT",
                Status = "APPROVED",
                AuthorizationCode = "AUTH-DEFAULT",
                GatewayReference = "REF-DEFAULT",
                ProcessedAt = DateTimeOffset.UtcNow
            });
        }

        public Task<Pos.DTOs.PaymentGatewayResponse?> GetPaymentStatusAsync(
            Guid tenantId,
            Guid requestId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Pos.DTOs.PaymentGatewayResponse?>(null);
        }
    }
}

extern alias POSCartServiceAlias;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
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

    // ── Tests S2-034: Persistencia de aprobación y rechazo ────────────────────

    [Fact]
    [Trait("Plan", "S2-034")]
    [Trait("Plan", "S2-040")]
    public async Task CobrarTarjeta_PasarelaAprueba_CompletaVentaGeneraPagoTarjetaYOutbox()
    {
        var tenantId   = Guid.NewGuid();
        var cajeroId   = Guid.NewGuid();
        var sucursalId = Guid.NewGuid();
        var turnoId    = Guid.NewGuid();
        var ventaId    = Guid.NewGuid();

        var gatewayMock = new RecordingPaymentGatewayClient
        {
            ResponseFunc = req => new Pos.DTOs.PaymentGatewayResponse
            {
                RequestId         = req.RequestId,
                TransactionId     = "TX-APPROVED-001",
                Status            = "APPROVED",
                AuthorizationCode = "AUTH-S034",
                GatewayReference  = "REF-S034",
                ProcessedAt       = DateTimeOffset.UtcNow
            }
        };

        var configuredFactory = _factory.WithWebHostBuilder(b =>
            b.ConfigureTestServices(s =>
                s.AddSingleton<Pos.Services.IPaymentGatewayClient>(gatewayMock)));

        using (var scope = configuredFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Pos.Data.PosCartDbContext>();
            await db.Database.MigrateAsync();
            db.CurrentTenantId = tenantId;
            db.Turnos.Add(new Pos.Models.Turno
            {
                Id = turnoId, TenantId = tenantId, CajeroId = cajeroId,
                SucursalId = sucursalId, MontoApertura = 10000m,
                Estado = Pos.Models.EstadoTurno.ABIERTO
            });
            db.Ventas.Add(new Pos.Models.Venta
            {
                Id = ventaId, TenantId = tenantId, TurnoId = turnoId,
                CajeroId = cajeroId, SucursalId = sucursalId,
                Subtotal = 10000m, Impuestos = 1900m, Total = 11900m,
                MetodoPago = Pos.Models.MetodoPagoVenta.TARJETA,
                Estado = Pos.Models.EstadoVenta.PENDIENTE
            });
            await db.SaveChangesAsync();
        }

        var config = configuredFactory.Services.GetRequiredService<IConfiguration>();
        var token  = CreateCajeroToken(config, tenantId, cajeroId);
        using var client = configuredFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync($"/ventas/{ventaId}/cobrar-tarjeta",
            new { scenario_id = "sim-approved-v1" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var responseDocument = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var referenciaMostrada = responseDocument.RootElement
            .GetProperty("gateway_reference")
            .GetString();
        Assert.Equal("REF-S034", referenciaMostrada);

        // Verificar DB: venta COMPLETADA, 1 Pago TARJETA, 1 OutboxMessage, 1 IntentoPago APROBADO
        using var verifyScope = configuredFactory.Services.CreateScope();
        var dbVerify = verifyScope.ServiceProvider.GetRequiredService<Pos.Data.PosCartDbContext>();
        dbVerify.CurrentTenantId = tenantId;

        var venta = await dbVerify.Ventas.FindAsync(ventaId);
        Assert.NotNull(venta);
        Assert.Equal(Pos.Models.EstadoVenta.COMPLETADA, venta!.Estado);

        var pagos = dbVerify.Pagos.Where(p => p.VentaId == ventaId).ToList();
        var pago  = Assert.Single(pagos);
        Assert.Equal(Pos.Models.MetodoPago.TARJETA, pago.Metodo);
        Assert.Equal(11900m, pago.Monto);
        Assert.Equal(referenciaMostrada, pago.Referencia);

        var outbox = dbVerify.OutboxMessages.Where(o => o.VentaId == ventaId).ToList();
        var outboxMessage = Assert.Single(outbox);
        Assert.Equal(Pos.Models.OutboxStatus.PENDIENTE, outboxMessage.Status);
        var evento = JsonSerializer.Deserialize<Pos.Messaging.SaleCompletedEvent>(outboxMessage.Payload);
        Assert.NotNull(evento);
        Assert.Equal(ventaId, evento!.VentaId);
        Assert.Equal("TARJETA", evento.MetodoPago);

        var intentos = dbVerify.IntentosPago.Where(i => i.VentaId == ventaId).ToList();
        var intento  = Assert.Single(intentos);
        Assert.Equal(Pos.Models.EstadoIntentoPago.APROBADO, intento.Estado);
        Assert.Equal("AUTH-S034", intento.AuthorizationCode);
        Assert.Equal(referenciaMostrada, intento.Referencia);
    }

    [Fact]
    [Trait("Plan", "S2-034")]
    [Trait("Plan", "S2-040")]
    public async Task CobrarTarjeta_PasarelaRechaza_ConservaVentaPendienteSinPagoNiOutbox_Retorna422()
    {
        var tenantId   = Guid.NewGuid();
        var cajeroId   = Guid.NewGuid();
        var sucursalId = Guid.NewGuid();
        var turnoId    = Guid.NewGuid();
        var ventaId    = Guid.NewGuid();

        var gatewayMock = new RecordingPaymentGatewayClient
        {
            ResponseFunc = req => new Pos.DTOs.PaymentGatewayResponse
            {
                RequestId    = req.RequestId,
                TransactionId = "TX-DECLINED-001",
                Status       = "DECLINED",
                DeclineCode  = "INSUFFICIENT_FUNDS",
                Message      = "Fondos insuficientes",
                ProcessedAt  = DateTimeOffset.UtcNow
            }
        };

        var configuredFactory = _factory.WithWebHostBuilder(b =>
            b.ConfigureTestServices(s =>
                s.AddSingleton<Pos.Services.IPaymentGatewayClient>(gatewayMock)));

        using (var scope = configuredFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Pos.Data.PosCartDbContext>();
            await db.Database.MigrateAsync();
            db.CurrentTenantId = tenantId;
            db.Turnos.Add(new Pos.Models.Turno
            {
                Id = turnoId, TenantId = tenantId, CajeroId = cajeroId,
                SucursalId = sucursalId, MontoApertura = 5000m,
                Estado = Pos.Models.EstadoTurno.ABIERTO
            });
            db.Ventas.Add(new Pos.Models.Venta
            {
                Id = ventaId, TenantId = tenantId, TurnoId = turnoId,
                CajeroId = cajeroId, SucursalId = sucursalId,
                Subtotal = 10000m, Impuestos = 1900m, Total = 11900m,
                MetodoPago = Pos.Models.MetodoPagoVenta.TARJETA,
                Estado = Pos.Models.EstadoVenta.PENDIENTE
            });
            await db.SaveChangesAsync();
        }

        var config = configuredFactory.Services.GetRequiredService<IConfiguration>();
        var token  = CreateCajeroToken(config, tenantId, cajeroId);
        using var client = configuredFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync($"/ventas/{ventaId}/cobrar-tarjeta",
            new { scenario_id = "sim-declined-v1" });

        // Criterio: rechazo no genera sale.completed ni Pago aprobado
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        using var verifyScope = configuredFactory.Services.CreateScope();
        var dbVerify = verifyScope.ServiceProvider.GetRequiredService<Pos.Data.PosCartDbContext>();
        dbVerify.CurrentTenantId = tenantId;

        var venta = await dbVerify.Ventas.FindAsync(ventaId);
        Assert.Equal(Pos.Models.EstadoVenta.PENDIENTE, venta!.Estado);

        var pagos = dbVerify.Pagos.Where(p => p.VentaId == ventaId).ToList();
        Assert.Empty(pagos);

        var outbox = dbVerify.OutboxMessages.Where(o => o.VentaId == ventaId).ToList();
        Assert.Empty(outbox);

        var intentos = dbVerify.IntentosPago.Where(i => i.VentaId == ventaId).ToList();
        var intento  = Assert.Single(intentos);
        Assert.Equal(Pos.Models.EstadoIntentoPago.RECHAZADO, intento.Estado);
        Assert.Equal("INSUFFICIENT_FUNDS", intento.DeclineCode);
    }

    [Fact]
    [Trait("Plan", "S2-034")]
    [Trait("Plan", "S2-040")]
    public async Task CobrarTarjeta_PasarelaTimeout_ConservaVentaPendienteSinPagoNiOutbox_Retorna504()
    {
        var tenantId   = Guid.NewGuid();
        var cajeroId   = Guid.NewGuid();
        var sucursalId = Guid.NewGuid();
        var turnoId    = Guid.NewGuid();
        var ventaId    = Guid.NewGuid();

        var gatewayMock = new RecordingPaymentGatewayClient
        {
            ResponseFunc = _ => throw new TimeoutException("Simulated gateway timeout")
        };

        var configuredFactory = _factory.WithWebHostBuilder(b =>
            b.ConfigureTestServices(s =>
                s.AddSingleton<Pos.Services.IPaymentGatewayClient>(gatewayMock)));

        using (var scope = configuredFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Pos.Data.PosCartDbContext>();
            await db.Database.MigrateAsync();
            db.CurrentTenantId = tenantId;
            db.Turnos.Add(new Pos.Models.Turno
            {
                Id = turnoId, TenantId = tenantId, CajeroId = cajeroId,
                SucursalId = sucursalId, MontoApertura = 5000m,
                Estado = Pos.Models.EstadoTurno.ABIERTO
            });
            db.Ventas.Add(new Pos.Models.Venta
            {
                Id = ventaId, TenantId = tenantId, TurnoId = turnoId,
                CajeroId = cajeroId, SucursalId = sucursalId,
                Subtotal = 8000m, Impuestos = 1520m, Total = 9520m,
                MetodoPago = Pos.Models.MetodoPagoVenta.TARJETA,
                Estado = Pos.Models.EstadoVenta.PENDIENTE
            });
            await db.SaveChangesAsync();
        }

        var config = configuredFactory.Services.GetRequiredService<IConfiguration>();
        var token  = CreateCajeroToken(config, tenantId, cajeroId);
        using var client = configuredFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync($"/ventas/{ventaId}/cobrar-tarjeta",
            new { scenario_id = "sim-timeout-v1" });

        Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);

        var consulta = await client.GetAsync($"/ventas/{ventaId}/intento-tarjeta");
        Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);
        using (var estado = JsonDocument.Parse(await consulta.Content.ReadAsStringAsync()))
            Assert.Equal("UNCERTAIN", estado.RootElement.GetProperty("status").GetString());

        var segundo = await client.PostAsJsonAsync($"/ventas/{ventaId}/cobrar-tarjeta",
            new { idempotency_key = Guid.NewGuid().ToString(), scenario_id = "sim-approved-v1" });
        Assert.Equal(HttpStatusCode.Conflict, segundo.StatusCode);
        Assert.Equal(1, gatewayMock.CallCount);

        using var verifyScope = configuredFactory.Services.CreateScope();
        var dbVerify = verifyScope.ServiceProvider.GetRequiredService<Pos.Data.PosCartDbContext>();
        dbVerify.CurrentTenantId = tenantId;

        var venta = await dbVerify.Ventas.FindAsync(ventaId);
        Assert.Equal(Pos.Models.EstadoVenta.PENDIENTE, venta!.Estado);
        Assert.Empty(dbVerify.Pagos.Where(p => p.VentaId == ventaId).ToList());
        Assert.Empty(dbVerify.OutboxMessages.Where(o => o.VentaId == ventaId).ToList());

        var intentos = dbVerify.IntentosPago.Where(i => i.VentaId == ventaId).ToList();
        var intento  = Assert.Single(intentos);
        Assert.Equal(Pos.Models.EstadoIntentoPago.PENDIENTE, intento.Estado);

        gatewayMock.StatusResponseFunc = (_, requestId) => new Pos.DTOs.PaymentGatewayResponse
        {
            RequestId = requestId,
            TransactionId = "TX-RECOVERED",
            Status = "APPROVED",
            AuthorizationCode = "AUTH-RECOVERED",
            GatewayReference = "REF-RECOVERED",
            ProcessedAt = DateTimeOffset.UtcNow
        };
        var reconciliada = await client.GetAsync($"/ventas/{ventaId}/intento-tarjeta");
        Assert.Equal(HttpStatusCode.OK, reconciliada.StatusCode);
        using (var estado = JsonDocument.Parse(await reconciliada.Content.ReadAsStringAsync()))
            Assert.Equal("APPROVED", estado.RootElement.GetProperty("status").GetString());
        Assert.Equal(1, gatewayMock.CallCount);

        using var finalScope = configuredFactory.Services.CreateScope();
        var dbFinal = finalScope.ServiceProvider.GetRequiredService<Pos.Data.PosCartDbContext>();
        dbFinal.CurrentTenantId = tenantId;
        Assert.Equal(Pos.Models.EstadoVenta.COMPLETADA, (await dbFinal.Ventas.FindAsync(ventaId))!.Estado);
        Assert.Single(dbFinal.Pagos.Where(p => p.VentaId == ventaId));
        Assert.Single(dbFinal.OutboxMessages.Where(o => o.VentaId == ventaId));
        Assert.Equal(Pos.Models.EstadoIntentoPago.APROBADO,
            Assert.Single(dbFinal.IntentosPago.Where(i => i.VentaId == ventaId)).Estado);
    }

    [Fact]
    [Trait("Plan", "S2-034")]
    public async Task CobrarTarjeta_PasarelaError_ConservaVentaPendienteSinPagoNiOutbox_Retorna502()
    {
        var tenantId   = Guid.NewGuid();
        var cajeroId   = Guid.NewGuid();
        var sucursalId = Guid.NewGuid();
        var turnoId    = Guid.NewGuid();
        var ventaId    = Guid.NewGuid();

        var gatewayMock = new RecordingPaymentGatewayClient
        {
            ResponseFunc = _ => throw new InvalidOperationException("Simulated gateway error")
        };

        var configuredFactory = _factory.WithWebHostBuilder(b =>
            b.ConfigureTestServices(s =>
                s.AddSingleton<Pos.Services.IPaymentGatewayClient>(gatewayMock)));

        using (var scope = configuredFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Pos.Data.PosCartDbContext>();
            await db.Database.MigrateAsync();
            db.CurrentTenantId = tenantId;
            db.Turnos.Add(new Pos.Models.Turno
            {
                Id = turnoId, TenantId = tenantId, CajeroId = cajeroId,
                SucursalId = sucursalId, MontoApertura = 5000m,
                Estado = Pos.Models.EstadoTurno.ABIERTO
            });
            db.Ventas.Add(new Pos.Models.Venta
            {
                Id = ventaId, TenantId = tenantId, TurnoId = turnoId,
                CajeroId = cajeroId, SucursalId = sucursalId,
                Subtotal = 5000m, Impuestos = 950m, Total = 5950m,
                MetodoPago = Pos.Models.MetodoPagoVenta.TARJETA,
                Estado = Pos.Models.EstadoVenta.PENDIENTE
            });
            await db.SaveChangesAsync();
        }

        var config = configuredFactory.Services.GetRequiredService<IConfiguration>();
        var token  = CreateCajeroToken(config, tenantId, cajeroId);
        using var client = configuredFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync($"/ventas/{ventaId}/cobrar-tarjeta",
            new { scenario_id = "sim-error-v1" });

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);

        using var verifyScope = configuredFactory.Services.CreateScope();
        var dbVerify = verifyScope.ServiceProvider.GetRequiredService<Pos.Data.PosCartDbContext>();
        dbVerify.CurrentTenantId = tenantId;

        var venta = await dbVerify.Ventas.FindAsync(ventaId);
        Assert.Equal(Pos.Models.EstadoVenta.PENDIENTE, venta!.Estado);
        Assert.Empty(dbVerify.Pagos.Where(p => p.VentaId == ventaId).ToList());
        Assert.Empty(dbVerify.OutboxMessages.Where(o => o.VentaId == ventaId).ToList());

        var intentos = dbVerify.IntentosPago.Where(i => i.VentaId == ventaId).ToList();
        var intento  = Assert.Single(intentos);
        Assert.Equal(Pos.Models.EstadoIntentoPago.PENDIENTE, intento.Estado);
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
        public Func<Guid, Guid, Pos.DTOs.PaymentGatewayResponse?>? StatusResponseFunc { get; set; }

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
            return Task.FromResult(StatusResponseFunc?.Invoke(tenantId, requestId));
        }
    }
}

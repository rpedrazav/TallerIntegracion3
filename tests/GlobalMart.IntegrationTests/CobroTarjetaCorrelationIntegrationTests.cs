extern alias POSCartServiceAlias;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using Pos = POSCartServiceAlias::POSCartService;

namespace GlobalMart.IntegrationTests;

/// <summary>
/// Verificaciones de integración para:
/// - S2-059 (TI3-690): Logs estructurados de correlación sin datos sensibles de tarjeta.
/// - S2-060 (TI3-691): Smoke de despliegue de tarjeta con 3 estados del simulador (Aprobado, Rechazado, Timeout).
/// </summary>
[Trait("Plan", "S2-059")]
[Trait("Plan", "S2-060")]
public sealed class CobroTarjetaCorrelationIntegrationTests : IClassFixture<Ms5WebApplicationFactory>
{
    private readonly Ms5WebApplicationFactory _factory;

    public CobroTarjetaCorrelationIntegrationTests(Ms5WebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CobroTarjeta_EmiteLogsDeCorrelacionSinExponerDatosSensibles_S2_059()
    {
        var tenantId = Guid.NewGuid();
        var cajeroId = Guid.NewGuid();
        var sucursalId = Guid.NewGuid();
        var turnoId = Guid.NewGuid();
        var ventaId = Guid.NewGuid();
        var customKey = Guid.NewGuid().ToString();

        var logSink = new InMemoryLogSink();

        var gatewayMock = new RecordingGatewayClient
        {
            ResponseFunc = req => new Pos.DTOs.PaymentGatewayResponse
            {
                RequestId = req.RequestId,
                TransactionId = "TX-CORR-001",
                Status = "APPROVED",
                AuthorizationCode = "AUTH-OK-789",
                GatewayReference = "RRN-789",
                ProcessedAt = DateTimeOffset.UtcNow
            }
        };

        var configuredFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging =>
            {
                logging.AddProvider(logSink);
            });
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<Pos.Services.IPaymentGatewayClient>(gatewayMock);
            });
        });

        // Crear turno ABIERTO y venta PENDIENTE
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

        // 1. Verificar Criterio S2-059: Entrada, llamada y respuesta registradas con IDs de correlación
        var cobroLogs = logSink.Messages.Where(m => m.Contains("[COBRO_CORRELATION]")).ToList();
        Assert.NotEmpty(cobroLogs);

        var entradaLog = cobroLogs.FirstOrDefault(m => m.Contains("Entrada:"));
        Assert.NotNull(entradaLog);
        Assert.Contains($"VentaId={ventaId}", entradaLog);
        Assert.Contains($"CorrelationId={customKey}", entradaLog);

        var llamadaLog = cobroLogs.FirstOrDefault(m => m.Contains("Llamada pasarela:"));
        Assert.NotNull(llamadaLog);
        Assert.Contains($"VentaId={ventaId}", llamadaLog);
        Assert.Contains($"CorrelationId={customKey}", llamadaLog);

        var respuestaLog = cobroLogs.FirstOrDefault(m => m.Contains("Respuesta pasarela:"));
        Assert.NotNull(respuestaLog);
        Assert.Contains($"VentaId={ventaId}", respuestaLog);
        Assert.Contains($"CorrelationId={customKey}", respuestaLog);
        Assert.Contains("Status=APPROVED", respuestaLog);

        // 2. Verificar Criterio S2-059: Nunca figuran números de tarjeta (PAN), CVVs ni secretos
        var panRegex = new Regex(@"\b4[0-9]{12}(?:[0-9]{3})?\b|\b5[1-5][0-9]{14}\b", RegexOptions.Compiled);
        var cvvRegex = new Regex(@"\b(?:cvv|cvc|pin|secret|tarjeta_numero)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        foreach (var log in cobroLogs)
        {
            Assert.False(panRegex.IsMatch(log), $"Log contiene potencial PAN: {log}");
            Assert.False(cvvRegex.IsMatch(log), $"Log contiene palabras clave de secretos: {log}");
        }
    }

    [Fact]
    public async Task Smoke_DespliegueTarjeta_TresEstadosSimulador_S2_060()
    {
        var tenantId = Guid.NewGuid();
        var cajeroId = Guid.NewGuid();
        var sucursalId = Guid.NewGuid();
        var turnoId = Guid.NewGuid();

        var gatewayMock = new RecordingGatewayClient();

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
                MontoApertura = 5000m,
                Estado = Pos.Models.EstadoTurno.ABIERTO
            });
            await db.SaveChangesAsync();
        }

        var configuration = configuredFactory.Services.GetRequiredService<IConfiguration>();
        var token = CreateCajeroToken(configuration, tenantId, cajeroId);
        using var client = configuredFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // ── Estado 1: Aprobado -> Venta pasa a COMPLETADA (S2-060 Criterio 1) ──
        var ventaAprobId = Guid.NewGuid();
        using (var scope = configuredFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Pos.Data.PosCartDbContext>();
            db.CurrentTenantId = tenantId;
            db.Ventas.Add(new Pos.Models.Venta
            {
                Id = ventaAprobId,
                TenantId = tenantId,
                TurnoId = turnoId,
                CajeroId = cajeroId,
                SucursalId = sucursalId,
                Subtotal = 1000m,
                Impuestos = 190m,
                Total = 1190m,
                MetodoPago = Pos.Models.MetodoPagoVenta.TARJETA,
                Estado = Pos.Models.EstadoVenta.PENDIENTE
            });
            await db.SaveChangesAsync();
        }

        gatewayMock.ResponseFunc = req => new Pos.DTOs.PaymentGatewayResponse
        {
            RequestId = req.RequestId,
            TransactionId = "TX-SMOKE-APROB",
            Status = "APPROVED",
            AuthorizationCode = "AUTH-SMOKE-OK",
            GatewayReference = "REF-SMOKE-OK",
            ProcessedAt = DateTimeOffset.UtcNow
        };

        var resAprob = await client.PostAsJsonAsync($"/ventas/{ventaAprobId}/cobrar-tarjeta", new
        {
            scenario_id = "sim-approved-v1"
        });
        Assert.Equal(HttpStatusCode.OK, resAprob.StatusCode);

        using (var scope = configuredFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Pos.Data.PosCartDbContext>();
            db.CurrentTenantId = tenantId;
            var ventaAprob = await db.Ventas.FindAsync(ventaAprobId);
            Assert.Equal(Pos.Models.EstadoVenta.COMPLETADA, ventaAprob!.Estado);
        }

        // ── Estado 2: Rechazado -> Venta permanece PENDIENTE (S2-060 Criterio 2) ──
        var ventaRechId = Guid.NewGuid();
        using (var scope = configuredFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Pos.Data.PosCartDbContext>();
            db.CurrentTenantId = tenantId;
            db.Ventas.Add(new Pos.Models.Venta
            {
                Id = ventaRechId,
                TenantId = tenantId,
                TurnoId = turnoId,
                CajeroId = cajeroId,
                SucursalId = sucursalId,
                Subtotal = 2000m,
                Impuestos = 380m,
                Total = 2380m,
                MetodoPago = Pos.Models.MetodoPagoVenta.TARJETA,
                Estado = Pos.Models.EstadoVenta.PENDIENTE
            });
            await db.SaveChangesAsync();
        }

        gatewayMock.ResponseFunc = req => new Pos.DTOs.PaymentGatewayResponse
        {
            RequestId = req.RequestId,
            TransactionId = "TX-SMOKE-RECH",
            Status = "DECLINED",
            DeclineCode = "CARD_DECLINED",
            Message = "Tarjeta declinada por el banco emisor",
            ProcessedAt = DateTimeOffset.UtcNow
        };

        var resRech = await client.PostAsJsonAsync($"/ventas/{ventaRechId}/cobrar-tarjeta", new
        {
            scenario_id = "sim-declined-v1"
        });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, resRech.StatusCode);

        using (var scope = configuredFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Pos.Data.PosCartDbContext>();
            db.CurrentTenantId = tenantId;
            var ventaRech = await db.Ventas.FindAsync(ventaRechId);
            Assert.Equal(Pos.Models.EstadoVenta.PENDIENTE, ventaRech!.Estado);
        }

        // ── Estado 3: Timeout/No disponible -> Venta permanece PENDIENTE (S2-060 Criterio 3) ──
        var ventaTimeoutId = Guid.NewGuid();
        using (var scope = configuredFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Pos.Data.PosCartDbContext>();
            db.CurrentTenantId = tenantId;
            db.Ventas.Add(new Pos.Models.Venta
            {
                Id = ventaTimeoutId,
                TenantId = tenantId,
                TurnoId = turnoId,
                CajeroId = cajeroId,
                SucursalId = sucursalId,
                Subtotal = 3000m,
                Impuestos = 570m,
                Total = 3570m,
                MetodoPago = Pos.Models.MetodoPagoVenta.TARJETA,
                Estado = Pos.Models.EstadoVenta.PENDIENTE
            });
            await db.SaveChangesAsync();
        }

        gatewayMock.ResponseFunc = _ => throw new TimeoutException("Gateway timeout simulado");

        var resTimeout = await client.PostAsJsonAsync($"/ventas/{ventaTimeoutId}/cobrar-tarjeta", new
        {
            scenario_id = "sim-timeout-v1"
        });
        Assert.Equal(HttpStatusCode.GatewayTimeout, resTimeout.StatusCode);

        using (var scope = configuredFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Pos.Data.PosCartDbContext>();
            db.CurrentTenantId = tenantId;
            var ventaTimeout = await db.Ventas.FindAsync(ventaTimeoutId);
            Assert.Equal(Pos.Models.EstadoVenta.PENDIENTE, ventaTimeout!.Estado);
        }
    }

    private static string CreateCajeroToken(IConfiguration configuration, Guid tenantId, Guid cajeroId)
    {
        var key = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key no configurada");
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

    private sealed class InMemoryLogSink : ILoggerProvider, ILogger
    {
        public List<string> Messages { get; } = new();

        public ILogger CreateLogger(string categoryName) => this;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var msg = formatter(state, exception);
            lock (Messages)
            {
                Messages.Add($"[{logLevel}] {msg}");
            }
        }

        public void Dispose() { }
    }

    private sealed class RecordingGatewayClient : Pos.Services.IPaymentGatewayClient
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

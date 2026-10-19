extern alias POSCartServiceAlias;

using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Xunit;
using Pos = POSCartServiceAlias::POSCartService;

namespace GlobalMart.IntegrationTests;

// S2-055: real Electron/main/preload -> HTTP MS-5 -> PostgreSQL -> Node gateway.
// Identity/catalog/tax are fixture dependencies. Event verification is durable outbox,
// not Kafka delivery. Never run against a developer's Electron profile or POS database.
[Trait("Plan", "S2-055")]
public sealed class ElectronCardE2ETests
{
    [ElectronTheory]
    [InlineData("sim-approved-v1", true)]
    [InlineData("sim-declined-v1", false)]
    public async Task Electron_AbreTurno_CreaVenta_Cobra_VerificaPersistencia(string scenario, bool approved)
    {
        var connection = Environment.GetEnvironmentVariable("GLOBALMART_TEST_POSTGRES")
            ?? throw new InvalidOperationException("Defina GLOBALMART_TEST_POSTGRES con PostgreSQL de pruebas.");
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "GlobalMartOS.sln"))) root = root.Parent;
        Assert.NotNull(root);
        var frontend = Path.Combine(root!.FullName, "globalmart-frontend");
        Assert.True(File.Exists(Path.Combine(frontend, ".webpack/electron-e2e/main.js")), "Ejecute node e2e/electron/build.cjs primero.");
        var output = Path.Combine(frontend, "test-results", "S2-055", Guid.NewGuid().ToString("N"), scenario);
        Directory.CreateDirectory(output);
        var schema = "s2055_" + Guid.NewGuid().ToString("N");
        var tenant = Guid.NewGuid(); var cashier = Guid.NewGuid(); var branch = Guid.NewGuid();
        var product = new Pos.DTOs.ProductoCatalogDto { Id = Guid.NewGuid(), Nombre = "Leche S2-055", PrecioBase = 100 };
        await using var admin = new NpgsqlConnection(connection);
        await admin.OpenAsync();
        await using (var command = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", admin)) await command.ExecuteNonQueryAsync();
        var isolated = new NpgsqlConnectionStringBuilder(connection) { SearchPath = schema, Pooling = false }.ConnectionString;
        Process? simulator = null;
        WebApplicationFactory<POSCartServiceAlias::Program>? host = null;
        HttpListener? listener = null;
        Task? bridge = null;
        using var lifetime = new CancellationTokenSource();
        try
        {
            using var portFinder = new TcpListener(IPAddress.Loopback, 0);
            portFinder.Start(); var port = ((IPEndPoint)portFinder.LocalEndpoint).Port; portFinder.Stop();
            var start = new ProcessStartInfo("node") { UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add(Path.Combine(root.FullName, "Docker/simulator/server.js"));
            start.Environment["PORT"] = port.ToString(); start.Environment["HOST"] = "127.0.0.1";
            simulator = Process.Start(start)!;
            simulator.BeginOutputReadLine(); simulator.BeginErrorReadLine();
            using var gateway = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}/") };
            using var ready = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            while (true)
            {
                try { using var health = await gateway.GetAsync("health", ready.Token); if (health.IsSuccessStatusCode) break; }
                catch (HttpRequestException) { }
                await Task.Delay(100, ready.Token);
            }
            var traffic = new ScenarioTraffic(scenario);
            host = new WebApplicationFactory<POSCartServiceAlias::Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSetting("ConnectionStrings:DefaultConnection", isolated);
                builder.UseSetting("OutboxPublisher:Enabled", "false");
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<DbContextOptions<Pos.Data.PosCartDbContext>>();
                    services.AddSingleton(new DbContextOptionsBuilder<Pos.Data.PosCartDbContext>()
                        .UseNpgsql(isolated, pg => pg.MigrationsHistoryTable("__EFMigrationsHistory", schema)).Options);
                    services.RemoveAll<Pos.Services.ICatalogClient>();
                    services.AddSingleton<Pos.Services.ICatalogClient>(new CatalogFixture(product));
                    services.RemoveAll<Pos.Services.ITaxClient>();
                    services.AddSingleton<Pos.Services.ITaxClient>(new TaxFixture());
                    services.AddHttpClient(Pos.Services.PaymentGatewayClient.HttpClientName, client =>
                    { client.BaseAddress = gateway.BaseAddress; client.Timeout = TimeSpan.FromSeconds(5); })
                        .AddHttpMessageHandler(() => traffic);
                });
            });
            using var client = host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
            var config = host.Services.GetRequiredService<IConfiguration>();
            var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
                config["Jwt:Issuer"], config["Jwt:Audience"], new[] {
                    new Claim("sub", cashier.ToString()), new Claim("cajero_id", cashier.ToString()),
                    new Claim("tenant_id", tenant.ToString()), new Claim("sucursal_id", branch.ToString()),
                    new Claim("active_role", "CAJERO"), new Claim("roles", "CAJERO"), new Claim(ClaimTypes.Role, "CAJERO")
                }, expires: DateTime.UtcNow.AddMinutes(15), signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)), SecurityAlgorithms.HmacSha256)));
            listener = new HttpListener(); listener.Prefixes.Add("http://127.0.0.1:4199/"); listener.Start();
            bridge = Serve(listener, client, tenant, token, product, lifetime.Token);
            var ui = new ProcessStartInfo("node") { WorkingDirectory = frontend, UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true };
            ui.ArgumentList.Add("e2e/electron/card-backend.cjs");
            ui.Environment["S2055_OUTPUT"] = output; ui.Environment["S2055_TENANT"] = tenant.ToString();
            ui.Environment["S2055_SCENARIO"] = scenario;
            using (var process = Process.Start(ui)!)
            {
                var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
                try { await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(90)); }
                finally { if (!process.HasExited) { process.Kill(true); await process.WaitForExitAsync(); } }
                Assert.True(process.ExitCode == 0, $"Electron ({output}):\n{await stdout}\n{await stderr}");
            }
            using var result = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(output, "ui-result.json")));
            var saleId = result.RootElement.GetProperty("sale").GetProperty("id").GetGuid();
            var requestKey = result.RootElement.GetProperty("request").GetProperty("idempotency_key").GetString();
            await using var db = new Pos.Data.PosCartDbContext(new DbContextOptionsBuilder<Pos.Data.PosCartDbContext>().UseNpgsql(isolated).Options)
                { CurrentTenantId = tenant };
            var shift = await db.Turnos.SingleAsync();
            Assert.Equal(Pos.Models.EstadoTurno.ABIERTO, shift.Estado);
            Assert.Equal(cashier, shift.CajeroId); Assert.Equal(branch, shift.SucursalId); Assert.Equal(20000m, shift.MontoApertura);
            var sale = await db.Ventas.SingleAsync(v => v.Id == saleId);
            Assert.Equal(shift.Id, sale.TurnoId); Assert.Equal(tenant, sale.TenantId); Assert.Equal(cashier, sale.CajeroId);
            Assert.Equal(Pos.Models.MetodoPagoVenta.TARJETA, sale.MetodoPago);
            Assert.Equal(100m, sale.Subtotal); Assert.Equal(19m, sale.Impuestos); Assert.Equal(119m, sale.Total);
            Assert.Equal(approved ? Pos.Models.EstadoVenta.COMPLETADA : Pos.Models.EstadoVenta.PENDIENTE, sale.Estado);
            var payments = await db.Pagos.Where(p => p.VentaId == saleId).ToListAsync();
            var events = await db.OutboxMessages.Where(e => e.VentaId == saleId).ToListAsync();
            var attempt = await db.IntentosPago.SingleAsync(i => i.VentaId == saleId);
            Assert.Equal(requestKey, attempt.IdempotencyKey); Assert.Equal(sale.Total, attempt.Monto);
            Assert.Equal(approved ? Pos.Models.EstadoIntentoPago.APROBADO : Pos.Models.EstadoIntentoPago.RECHAZADO, attempt.Estado);
            Assert.Equal(1, traffic.Posts);
            Assert.NotNull(traffic.Request);
            Assert.Equal(tenant, traffic.Request!.TenantId); Assert.Equal(saleId, traffic.Request.SaleId);
            Assert.Equal(sale.Total, traffic.Request.Amount); Assert.Equal(attempt.Moneda, traffic.Request.Currency);
            Assert.Equal(requestKey, traffic.Request.RequestId.ToString()); Assert.Equal(scenario, traffic.Request.ScenarioId);
            Assert.Equal(approved ? 1 : 0, payments.Count); Assert.Equal(approved ? 1 : 0, events.Count);
            Assert.NotNull(traffic.Response);
            var gatewayResult = traffic.Response!;
            Assert.Equal(approved ? "APPROVED" : "DECLINED", gatewayResult.Status);
            Assert.Equal(requestKey, gatewayResult.RequestId.ToString());
            Assert.Equal(gatewayResult.GatewayReference, attempt.Referencia);
            if (approved)
            {
                Assert.Equal(Pos.Models.MetodoPago.TARJETA, payments[0].Metodo);
                Assert.Equal(sale.Total, payments[0].Monto); Assert.Equal(0m, payments[0].Vuelto);
                Assert.Equal(gatewayResult.GatewayReference, payments[0].Referencia);
                Assert.Equal(payments[0].Referencia, result.RootElement.GetProperty("body").GetProperty("gateway_reference").GetString());
                Assert.Equal("sale.completed", events[0].EventType); Assert.Equal("sale.completed", events[0].Topic);
                Assert.Equal(Pos.Models.OutboxStatus.PENDIENTE, events[0].Status);
                var payload = JsonSerializer.Deserialize<Pos.Messaging.SaleCompletedEvent>(events[0].Payload)!;
                Assert.Equal(events[0].EventId, payload.EventId); Assert.Equal(tenant, payload.TenantId);
                Assert.Equal(sale.Id, payload.VentaId); Assert.Equal(cashier, payload.CajeroId); Assert.Equal(branch, payload.SucursalId);
                Assert.Equal(sale.Total, payload.Total); Assert.Equal(sale.Subtotal, payload.Subtotal); Assert.Equal(sale.Impuestos, payload.Iva);
                Assert.Equal("TARJETA", payload.MetodoPago); Assert.Equal(product.Id, Assert.Single(payload.Items).ProductoId);
            }
            else Assert.Equal(gatewayResult.DeclineCode, attempt.DeclineCode);
            await File.WriteAllTextAsync(Path.Combine(output, "verification.json"), JsonSerializer.Serialize(new {
                scenario, verifiedAt = DateTime.UtcNow, tenant, saleId, shiftId = shift.Id, total = sale.Total,
                saleState = sale.Estado.ToString(), attemptState = attempt.Estado.ToString(), gateway = gatewayResult, gatewayRequest = traffic.Request,
                paymentCount = payments.Count, eventCount = events.Count, payments = payments.Select(p => new { p.Monto, p.Referencia }),
                events = events.Select(e => new { e.EventId, e.Topic, e.Payload }), authorizationPosts = traffic.Posts,
                scope = "Electron + MS-5 + PostgreSQL + Node simulator; fixture identity/catalog/tax; durable outbox, no Kafka delivery"
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        finally
        {
            lifetime.Cancel(); listener?.Close();
            if (bridge != null) { try { await bridge; } catch (Exception e) when (e is OperationCanceledException or HttpListenerException or ObjectDisposedException) { } }
            if (host != null) await host.DisposeAsync();
            if (simulator != null) { if (!simulator.HasExited) simulator.Kill(true); await simulator.WaitForExitAsync(); simulator.Dispose(); }
            await using var drop = new NpgsqlCommand($"DROP SCHEMA IF EXISTS \"{schema}\" CASCADE", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static async Task Serve(HttpListener listener, HttpClient pos, Guid tenant, string token,
        Pos.DTOs.ProductoCatalogDto product, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var context = await listener.GetContextAsync().WaitAsync(ct);
            var req = context.Request; var res = context.Response;
            res.Headers["Access-Control-Allow-Origin"] = "*";
            res.Headers["Access-Control-Allow-Headers"] = "Authorization,Content-Type,X-Tenant-Id,Idempotency-Key";
            res.Headers["Access-Control-Allow-Methods"] = "GET,POST,PUT,DELETE,OPTIONS";
            try
            {
                if (req.HttpMethod == "OPTIONS") { res.StatusCode = 204; continue; }
                var route = req.Url!.AbsolutePath;
                object? fixture = null;
                if (route == "/auth/login" && req.HttpMethod == "POST")
                {
                    using var login = await JsonDocument.ParseAsync(req.InputStream, cancellationToken: ct);
                    var data = login.RootElement;
                    if (data.GetProperty("tenantId").GetGuid() != tenant || data.GetProperty("email").GetString() != "cashier@example.test"
                        || data.GetProperty("password").GetString() != "dummy-e2e-only") { res.StatusCode = 401; continue; }
                    fixture = new { token };
                }
                else if (route.StartsWith("/products") && req.HttpMethod == "GET")
                {
                    if (req.Headers["Authorization"] != $"Bearer {token}") { res.StatusCode = 401; continue; }
                    var item = new { id = product.Id, nombre = product.Nombre, precioBase = product.PrecioBase, codigoBarras = "7800055", isActive = true };
                    fixture = route == "/products/lookup" ? item : new { data = new[] { item }, totalCount = 1, page = 1, pageSize = 500 };
                }
                if (fixture != null)
                {
                    res.ContentType = "application/json";
                    await JsonSerializer.SerializeAsync(res.OutputStream, fixture, cancellationToken: ct);
                }
                else
                {
                    using var message = new HttpRequestMessage(new HttpMethod(req.HttpMethod), req.Url.PathAndQuery);
                    if (req.HasEntityBody) { message.Content = new StreamContent(req.InputStream); message.Content.Headers.TryAddWithoutValidation("Content-Type", req.ContentType); }
                    foreach (var header in new[] { "Authorization", "Idempotency-Key", "X-Tenant-Id" })
                        if (req.Headers[header] is { } value) message.Headers.TryAddWithoutValidation(header, value);
                    using var response = await pos.SendAsync(message, ct);
                    res.StatusCode = (int)response.StatusCode; res.ContentType = response.Content.Headers.ContentType?.ToString();
                    await response.Content.CopyToAsync(res.OutputStream, ct);
                }
            }
            finally { res.Close(); }
        }
    }

    private sealed class CatalogFixture(Pos.DTOs.ProductoCatalogDto product) : Pos.Services.ICatalogClient
    {
        public Task<Pos.DTOs.ProductoCatalogDto?> GetProductAsync(Guid id, string? bearerToken = null, CancellationToken cancellationToken = default)
            => Task.FromResult(id == product.Id ? product : null);
    }
    private sealed class TaxFixture : Pos.Services.ITaxClient
    {
        public Task<Pos.DTOs.TaxCalculationResult?> CalculateTaxAsync(IEnumerable<Pos.DTOs.TaxItemDto> items,
            decimal? porcentajeIva = null, string? bearerToken = null, CancellationToken cancellationToken = default)
        {
            var subtotal = items.Sum(i => i.Precio * i.Cantidad);
            return Task.FromResult<Pos.DTOs.TaxCalculationResult?>(new() { Subtotal = subtotal, Iva = subtotal * .19m, Total = subtotal * 1.19m, PorcentajeIva = 19 });
        }
    }
    // The UI doesn't expose scenario selection. Only this test handler chooses the
    // external simulator scenario; authorization and responses are never stubbed.
    private sealed class ScenarioTraffic(string scenario) : DelegatingHandler
    {
        public int Posts { get; private set; }
        public Pos.DTOs.PaymentGatewayRequest? Request { get; private set; }
        public Pos.DTOs.PaymentGatewayResponse? Response { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (request.Method == HttpMethod.Post)
            {
                Posts++;
                var body = JsonNode.Parse(await request.Content!.ReadAsStringAsync(ct))!;
                body["scenario_id"] = scenario;
                Request = JsonSerializer.Deserialize<Pos.DTOs.PaymentGatewayRequest>(body.ToJsonString());
                request.Content.Dispose(); request.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
            }
            var response = await base.SendAsync(request, ct);
            if (request.Method == HttpMethod.Post)
                Response = JsonSerializer.Deserialize<Pos.DTOs.PaymentGatewayResponse>(await response.Content.ReadAsStringAsync(ct));
            return response;
        }
    }
}

// Opt-in: the normal backend CI has no Electron build or desktop session.
// Once enabled, missing dependencies fail the test rather than silently skipping.
public sealed class ElectronTheoryAttribute : TheoryAttribute
{
    public ElectronTheoryAttribute()
    {
        if (Environment.GetEnvironmentVariable("GLOBALMART_ELECTRON_E2E") != "1")
            Skip = "Electron E2E opt-in: run npm run test:e2e:card in globalmart-frontend.";
    }
}

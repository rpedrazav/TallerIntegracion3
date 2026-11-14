extern alias POSCartServiceAlias;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using Pos = POSCartServiceAlias::POSCartService;

namespace GlobalMart.IntegrationTests;

[Trait("Plan", "S2-093")]
public sealed class ReversaTarjetaIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<POSCartServiceAlias::Program> _factory = null!;
    private Process _node = null!;
    private string _simulatorUrl = "";
    private readonly Traffic _traffic = new();
    public async Task InitializeAsync()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Docker/simulator/server.js"))) root = root.Parent;
        var start = new ProcessStartInfo("node", Path.Combine(root!.FullName, "Docker/simulator/server.js")) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.Environment["PORT"] = port.ToString(); start.Environment["HOST"] = "127.0.0.1";
        _node = Process.Start(start)!;
        _node.BeginOutputReadLine(); _node.BeginErrorReadLine();
        _simulatorUrl = $"http://127.0.0.1:{port}/";
        using var ping = new HttpClient();
        for (var i = 0; i < 50; i++) {
            try { using var response = await ping.GetAsync(_simulatorUrl); break; }
            catch (HttpRequestException) { await Task.Delay(50); }
        }
        _factory = new Ms5WebApplicationFactory().WithWebHostBuilder(builder => builder.ConfigureTestServices(services => {
            services.AddHttpClient(Pos.Services.PaymentGatewayClient.HttpClientName, client => {
                client.BaseAddress = new Uri(_simulatorUrl); client.Timeout = TimeSpan.FromSeconds(1);
            }).AddHttpMessageHandler(() => _traffic);
        }));
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<Pos.Data.PosCartDbContext>().Database.MigrateAsync();
    }
    public Task DisposeAsync() { _factory?.Dispose(); if (_node is { HasExited: false }) _node.Kill(true); _node?.Dispose(); return Task.CompletedTask; }
    private void Scenario(string name) => _factory.Services.GetRequiredService<IConfiguration>()["PasarelaPago:ReversalScenarioId"] = $"sim-reversal-{name}-v1";

    private async Task<Pos.Models.Venta> Seed(string escenario = "valida")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Pos.Data.PosCartDbContext>();
        var tenant = Guid.NewGuid();
        db.CurrentTenantId = tenant;
        var turno = new Pos.Models.Turno { TenantId = tenant, CajeroId = Guid.NewGuid(), SucursalId = Guid.NewGuid(),
            Estado = escenario == "cerrado" ? Pos.Models.EstadoTurno.CERRADO : Pos.Models.EstadoTurno.ABIERTO };
        var venta = new Pos.Models.Venta { TenantId = tenant, Turno = turno, TurnoId = turno.Id,
            CajeroId = turno.CajeroId, SucursalId = turno.SucursalId, Total = 119, Subtotal = 100, Impuestos = 19,
            Estado = Pos.Models.EstadoVenta.COMPLETADA, MetodoPago = Pos.Models.MetodoPagoVenta.TARJETA };
        venta.Items.Add(new Pos.Models.ItemVenta { ProductoId = Guid.NewGuid(), NombreProducto = "Producto propio",
            Cantidad = 1, PrecioUnitario = 100, Subtotal = 100 });
        var requestId = Guid.NewGuid();
        using var simulator = new HttpClient { BaseAddress = new Uri(_simulatorUrl) };
        simulator.DefaultRequestHeaders.Add("Idempotency-Key", requestId.ToString());
        var approved = await simulator.PostAsJsonAsync("simulator/payments", new {
            request_id = requestId, tenant_id = tenant, sale_id = venta.Id, amount = 119,
            currency = "CLP", scenario_id = "sim-approved-v1" });
        approved.EnsureSuccessStatusCode();
        var reference = JsonDocument.Parse(await approved.Content.ReadAsStringAsync()).RootElement.GetProperty("gateway_reference").GetString();
        venta.Pagos.Add(new Pos.Models.Pago { Metodo = Pos.Models.MetodoPago.TARJETA, Monto = 119, Referencia = reference });
        venta.IntentosPago.Add(new Pos.Models.IntentoPago { TenantId = tenant, IdempotencyKey = requestId.ToString(),
            Monto = 119, Moneda = "CLP", Referencia = reference, Estado = Pos.Models.EstadoIntentoPago.APROBADO });
        if (escenario == "sin_referencia") venta.Pagos.Single().Referencia = null;
        if (escenario == "incierto") venta.IntentosPago.Single().Estado = Pos.Models.EstadoIntentoPago.FALLIDO;
        db.Ventas.Add(venta);
        db.OutboxMessages.Add(new Pos.Models.OutboxMessage { EventId = Guid.NewGuid(), TenantId = tenant, VentaId = venta.Id,
            EventType = "sale.completed", Topic = "sale.completed", MessageKey = venta.Id.ToString(), Payload = "{}" });
        await db.SaveChangesAsync();
        return venta;
    }

    private HttpClient Client(Guid tenant, Guid admin, string? role = "ADMIN", bool expired = false, bool identity = true, WebApplicationFactory<POSCartServiceAlias::Program>? factory = null)
    {
        var client = (factory ?? _factory).CreateClient();
        if (role is null) return client;
        var config = _factory.Services.GetRequiredService<IConfiguration>();
        var claims = new List<Claim> { new("tenant_id", tenant.ToString()), new(ClaimTypes.Role, role) };
        if (identity) claims.Add(new Claim("sub", admin.ToString()));
        var token = new JwtSecurityToken(config["Jwt:Issuer"], config["Jwt:Audience"], claims,
            expires: expired ? DateTime.UtcNow.AddMinutes(-5) : DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)), SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        return client;
    }
    private static Task<HttpResponseMessage> Send(HttpClient client, Guid id, string body = "{\"motivo\":\"  Error total  \"}", string prefix = "")
        => client.PostAsync($"{prefix}/ventas/{id}/anular", new StringContent(body, Encoding.UTF8, "application/json"));


    private async Task AssertState(Pos.Models.Venta venta, Pos.Models.EstadoReversa state, bool cancelled)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Pos.Data.PosCartDbContext>(); db.CurrentTenantId = venta.TenantId;
        var reversal = Assert.Single(await db.ReversasPago.ToListAsync());
        Assert.Equal(state, reversal.Estado); Assert.Equal(119, reversal.Monto);
        var sale = await db.Ventas.Include(v => v.Pagos).Include(v => v.IntentosPago).Include(v => v.Items).SingleAsync();
        Assert.Equal(cancelled ? Pos.Models.EstadoVenta.ANULADA : Pos.Models.EstadoVenta.COMPLETADA, sale.Estado);
        Assert.Equal(119, sale.Total); Assert.Single(sale.Items); Assert.Equal(119, Assert.Single(sale.Pagos).Monto);
        Assert.Equal(Pos.Models.EstadoIntentoPago.APROBADO, Assert.Single(sale.IntentosPago).Estado);
        Assert.Equal(cancelled ? 1 : 0, await db.Anulaciones.CountAsync()); Assert.Single(await db.OutboxMessages.Where(o => o.EventType == "sale.completed").ToListAsync());
        var events = await db.OutboxMessages.Where(o => o.EventType == "sale.reversed").ToListAsync();
        Assert.Equal(cancelled ? 1 : 0, events.Count);
        if (cancelled) {
            var audit = await db.Anulaciones.SingleAsync();
            var message = Assert.Single(events);
            var payload = JsonDocument.Parse(message.Payload).RootElement;
            Assert.Equal(audit.Id, message.EventId);
            Assert.Equal(message.EventId, payload.GetProperty("event_id").GetGuid());
            Assert.Equal(venta.Id, payload.GetProperty("venta_id").GetGuid());
            Assert.Equal(venta.TenantId, payload.GetProperty("tenant_id").GetGuid());
            Assert.Equal(119, payload.GetProperty("total").GetDecimal());
            Assert.Single(payload.GetProperty("items").EnumerateArray());
        }
        if (cancelled) Assert.StartsWith("SIM-REV-", reversal.ReferenciaReversa);
    }

    [Theory]
    [InlineData("approved", 200, Pos.Models.EstadoReversa.APROBADA)]
    [InlineData("declined", 409, Pos.Models.EstadoReversa.RECHAZADA)]
    public async Task ResultadoPersistido_RepetirNoReenvia(string scenario, int status, Pos.Models.EstadoReversa state)
    {
        Scenario(scenario); var venta = await Seed(); using var client = Client(venta.TenantId, Guid.NewGuid());
        using var response = await Send(client, venta.Id); Assert.Equal(status, (int)response.StatusCode);
        await AssertState(venta, state, status == 200);
        using var repeated = await Send(client, venta.Id); Assert.Equal(status, (int)repeated.StatusCode);
        using var query = await client.GetAsync($"/ventas/{venta.Id}/reversa-tarjeta"); query.EnsureSuccessStatusCode();
        var json = JsonDocument.Parse(await query.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(state.ToString(), json.GetProperty("estado").GetString());
        Assert.Equal(status == 200, json.GetProperty("reembolso_confirmado").GetBoolean());
        Assert.Equal(1, _traffic.Posts);
    }

    [Fact]
    public async Task Timeout_ConservaVenta_GetRecuperaSinOtraReversa()
    {
        Scenario("timeout"); var venta = await Seed(); using var client = Client(venta.TenantId, Guid.NewGuid());
        using var response = await Send(client, venta.Id); Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertState(venta, Pos.Models.EstadoReversa.PENDIENTE, false);
        using var repeated = await Send(client, venta.Id); Assert.Equal(HttpStatusCode.Conflict, repeated.StatusCode);
        // La respuesta externa debe reconciliarse aunque el turno se haya cerrado después de reservar.
        using (var scope = _factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<Pos.Data.PosCartDbContext>(); db.CurrentTenantId = venta.TenantId;
            (await db.Turnos.SingleAsync()).Estado = Pos.Models.EstadoTurno.CERRADO; await db.SaveChangesAsync();
        }
        using var query = await client.GetAsync($"/api/ventas/{venta.Id}/reversa-tarjeta"); query.EnsureSuccessStatusCode();
        await AssertState(venta, Pos.Models.EstadoReversa.APROBADA, true); Assert.Equal(1, _traffic.Posts);
    }

    [Theory]
    [InlineData("tenant_id")]
    [InlineData("amount")]
    [InlineData("gateway_reference")]
    [InlineData("status")]
    public async Task RespuestaInconsistente_NoFingeReembolso(string field)
    {
        _traffic.Corrupt = field; var venta = await Seed(); using var client = Client(venta.TenantId, Guid.NewGuid());
        using var response = await Send(client, venta.Id); Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertState(venta, Pos.Models.EstadoReversa.PENDIENTE, false);
        _traffic.Corrupt = null;
        using var query = await client.GetAsync($"/ventas/{venta.Id}/reversa-tarjeta"); query.EnsureSuccessStatusCode();
        await AssertState(venta, Pos.Models.EstadoReversa.APROBADA, true); Assert.Equal(1, _traffic.Posts);
    }

    [Theory]
    [InlineData("cerrado", "TURNO_NO_ANULABLE")]
    [InlineData("sin_referencia", "PAGO_INCONSISTENTE")]
    [InlineData("incierto", "PAGO_INCIERTO")]
    public async Task Precondiciones_NoLlamanPasarela(string scenario, string code)
    {
        var venta = await Seed(scenario); using var client = Client(venta.TenantId, Guid.NewGuid());
        using var response = await Send(client, venta.Id); Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(code, JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("codigo").GetString());
        Assert.Equal(0, _traffic.Posts);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Pos.Data.PosCartDbContext>(); db.CurrentTenantId = venta.TenantId;
        Assert.Empty(await db.ReversasPago.ToListAsync()); Assert.Empty(await db.Anulaciones.ToListAsync());
    }

    [Theory]
    [InlineData(null, false, 401)]
    [InlineData("CAJERO", false, 403)]
    [InlineData("ADMIN", true, 404)]
    public async Task Permisos_Aislamiento(string? role, bool foreign, int status)
    {
        var venta = await Seed(); using var client = Client(foreign ? Guid.NewGuid() : venta.TenantId, Guid.NewGuid(), role);
        using var post = await Send(client, venta.Id); using var get = await client.GetAsync($"/ventas/{venta.Id}/reversa-tarjeta");
        Assert.Equal(status, (int)post.StatusCode); Assert.Equal(status, (int)get.StatusCode); Assert.Equal(0, _traffic.Posts);
    }

    [Fact]
    public async Task ReversaExistente_NoEsVisibleNiModificablePorOtroTenant()
    {
        Scenario("declined"); var venta = await Seed();
        using var owner = Client(venta.TenantId, Guid.NewGuid());
        using var first = await Send(owner, venta.Id); Assert.Equal(HttpStatusCode.Conflict, first.StatusCode);
        using var foreign = Client(Guid.NewGuid(), Guid.NewGuid());
        foreign.DefaultRequestHeaders.Add("X-Tenant-ID", venta.TenantId.ToString());
        using var query = await foreign.GetAsync($"/ventas/{venta.Id}/reversa-tarjeta");
        using var post = await Send(foreign, venta.Id);
        Assert.Equal(HttpStatusCode.NotFound, query.StatusCode); Assert.Equal(HttpStatusCode.NotFound, post.StatusCode);
        await AssertState(venta, Pos.Models.EstadoReversa.RECHAZADA, false); Assert.Equal(1, _traffic.Posts);
    }

    [Fact]
    public async Task PasarelaNoDisponible_ConservaPendienteSinReenvio()
    {
        var venta = await Seed(); _node.Kill(true); await _node.WaitForExitAsync();
        using var client = Client(venta.TenantId, Guid.NewGuid());
        using var response = await Send(client, venta.Id); Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var query = await client.GetAsync($"/ventas/{venta.Id}/reversa-tarjeta"); query.EnsureSuccessStatusCode();
        using var repeated = await Send(client, venta.Id); Assert.Equal(HttpStatusCode.Conflict, repeated.StatusCode);
        await AssertState(venta, Pos.Models.EstadoReversa.PENDIENTE, false); Assert.Equal(1, _traffic.Posts);
    }

    [Fact]
    public async Task Concurrentes_ReservanUnaReversaYUnaAuditoria()
    {
        var venta = await Seed(); using var client = Client(venta.TenantId, Guid.NewGuid());
        var responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => Send(client, venta.Id)));
        Assert.Contains(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.All(responses, r => Assert.Contains(r.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Conflict }));
        await AssertState(venta, Pos.Models.EstadoReversa.APROBADA, true); Assert.Equal(1, _traffic.Posts);
        foreach (var response in responses) response.Dispose();
    }

    [Fact]
    public async Task FalloLocalTrasAprobacion_ReservaPersisteYConsultaRecupera()
    {
        var venta = await Seed();
        var connection = _factory.Services.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection");
        // Cliente separado para el fallo de persistencia; usa el mismo simulador real.
        using var failedFactory = new Ms5WebApplicationFactory().WithWebHostBuilder(builder => builder.ConfigureTestServices(services => {
            services.RemoveAll<DbContextOptions<Pos.Data.PosCartDbContext>>();
            services.RemoveAll<Pos.Data.PosCartDbContext>();
            services.AddDbContext<Pos.Data.PosCartDbContext>(options => options.UseNpgsql(connection).AddInterceptors(new FailConfirmation()));
            services.AddHttpClient(Pos.Services.PaymentGatewayClient.HttpClientName, client => client.BaseAddress = new Uri(_simulatorUrl));
        }));
        using (var failedClient = Client(venta.TenantId, Guid.NewGuid(), factory: failedFactory)) {
            using var response = await Send(failedClient, venta.Id); Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }
        await AssertState(venta, Pos.Models.EstadoReversa.PENDIENTE, false);
        using var client = Client(venta.TenantId, Guid.NewGuid());
        using var query = await client.GetAsync($"/ventas/{venta.Id}/reversa-tarjeta"); query.EnsureSuccessStatusCode();
        await AssertState(venta, Pos.Models.EstadoReversa.APROBADA, true);
        Assert.Equal(0, _traffic.Posts); // La recuperación solo consulta el intento reservado por el otro host.
    }

    private sealed class FailConfirmation : SaveChangesInterceptor
    {
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<Pos.Models.ReversaPago>().Any(e => e.Entity.Estado == Pos.Models.EstadoReversa.APROBADA))
                throw new InvalidOperationException("Fallo controlado después de escritura y antes de commit");
            return ValueTask.FromResult(result);
        }
    }

    private sealed class Traffic : DelegatingHandler
    {
        public int Posts; public string? Corrupt;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            if (request.Method == HttpMethod.Post && request.RequestUri!.AbsolutePath.EndsWith("reversals")) Interlocked.Increment(ref Posts);
            var response = await base.SendAsync(request, token);
            if (Corrupt is not null && response.IsSuccessStatusCode) {
                var json = System.Text.Json.Nodes.JsonNode.Parse(await response.Content.ReadAsStringAsync(token))!;
                json[Corrupt] = Corrupt == "amount" ? System.Text.Json.Nodes.JsonValue.Create(999) : System.Text.Json.Nodes.JsonValue.Create(Corrupt == "tenant_id" ? Guid.NewGuid().ToString() : "INCORRECTO");
                response.Content = new StringContent(json.ToJsonString(), Encoding.UTF8, "application/json");
            }
            return response;
        }
    }
}

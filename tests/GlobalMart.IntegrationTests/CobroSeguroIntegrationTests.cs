extern alias POSCartServiceAlias;

using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text;
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

// HTTP MS-5 + JWT real + PostgreSQL migrado + adaptador real + proceso Node real.
[Trait("Plan", "S2-035")]
public sealed class CobroSeguroIntegrationTests : IAsyncLifetime
{
    private readonly string _schema = "s2035_" + Guid.NewGuid().ToString("N");
    private NpgsqlConnection? _admin;
    private Process? _simulator;
    private WebApplicationFactory<POSCartServiceAlias::Program>? _host;
    private readonly GatewayTraffic _traffic = new();
    private readonly Guid _tenant = Guid.NewGuid();
    private readonly Guid _cashier = Guid.NewGuid();
    private HttpClient _client = null!;
    private string _connection = null!;
    private Guid _sale;

    public async Task InitializeAsync()
    {
        var connection = Environment.GetEnvironmentVariable("GLOBALMART_TEST_POSTGRES")
            ?? throw new InvalidOperationException("Defina GLOBALMART_TEST_POSTGRES con PostgreSQL de pruebas.");
        _admin = new NpgsqlConnection(connection);
        await _admin.OpenAsync();
        await using (var command = new NpgsqlCommand($"CREATE SCHEMA \"{_schema}\"", _admin))
            await command.ExecuteNonQueryAsync();
        _connection = new NpgsqlConnectionStringBuilder(connection)
        {
            SearchPath = _schema, Pooling = false
        }.ConnectionString;

        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Docker/simulator/server.js")))
            root = root.Parent;
        Assert.NotNull(root);
        var start = new ProcessStartInfo("node")
        {
            UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true
        };
        start.ArgumentList.Add(Path.Combine(root!.FullName, "Docker/simulator/server.js"));
        start.Environment["PORT"] = port.ToString();
        start.Environment["HOST"] = "127.0.0.1";
        _simulator = Process.Start(start)!;
        _simulator.BeginOutputReadLine();
        _simulator.BeginErrorReadLine();
        var url = $"http://127.0.0.1:{port}/";
        using var health = new HttpClient { BaseAddress = new Uri(url) };
        using var ready = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (true)
        {
            try
            {
                using var response = await health.GetAsync("health", ready.Token);
                if (response.IsSuccessStatusCode) break;
            }
            catch (HttpRequestException) { }
            await Task.Delay(50, ready.Token);
        }

        _host = new WebApplicationFactory<POSCartServiceAlias::Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:DefaultConnection", _connection);
            builder.UseSetting("OutboxPublisher:Enabled", "false");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<Pos.Data.PosCartDbContext>>();
                services.AddSingleton(new DbContextOptionsBuilder<Pos.Data.PosCartDbContext>()
                    .UseNpgsql(_connection, postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", _schema)).Options);
                // Solo observa el tráfico: las respuestas provienen del simulador Node.
                services.AddHttpClient(Pos.Services.PaymentGatewayClient.HttpClientName, client =>
                {
                    client.BaseAddress = new Uri(url);
                    client.Timeout = TimeSpan.FromSeconds(2);
                }).AddHttpMessageHandler(() => _traffic);
            });
        });
        _client = _host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        Authorize(_tenant, _cashier);
        _sale = await SeedSale(_tenant, _cashier);
    }

    [Theory]
    [InlineData("sim-approved-v1", 200, true)]
    [InlineData("sim-declined-v1", 422, false)]
    [InlineData("sim-timeout-v1", 504, false)]
    public async Task ResultadoReal_PersisteSoloCuandoHayAprobacion(string scenario, int status, bool approved)
    {
        var key = Guid.NewGuid();
        using var response = await Charge(_sale, key, scenario);
        Assert.Equal(status, (int)response.StatusCode);
        await Verify(_tenant, _sale, approved, 1);
        Assert.Equal(1, _traffic.Posts);
        Assert.Equal(scenario == "sim-timeout-v1" ? 1 : 0, _traffic.Gets);
        await using var db = Db(_tenant);
        var attempt = await db.IntentosPago.SingleAsync();
        Assert.Equal(approved ? Pos.Models.EstadoIntentoPago.APROBADO
            : scenario == "sim-declined-v1" ? Pos.Models.EstadoIntentoPago.RECHAZADO
            : Pos.Models.EstadoIntentoPago.PENDIENTE, attempt.Estado);
        var result = await GatewayStatus(_tenant, key);
        if (approved) Assert.Equal(result!.GatewayReference, attempt.Referencia);
        Assert.Equal(approved ? "APPROVED" : scenario == "sim-declined-v1" ? "DECLINED" : null, result?.Status);
    }

    [Theory]
    [InlineData("sim-approved-v1", 200, true)]
    [InlineData("sim-declined-v1", 422, false)]
    [InlineData("sim-timeout-v1", 504, false)]
    public async Task RepetirClave_NoReautorizaNiDuplica(string scenario, int firstStatus, bool approved)
    {
        var key = Guid.NewGuid();
        using var first = await Charge(_sale, key, scenario);
        Assert.Equal(firstStatus, (int)first.StatusCode);
        var original = await GatewayStatus(_tenant, key);
        using var repeated = await Charge(_sale, key, scenario);
        Assert.Equal(HttpStatusCode.Conflict, repeated.StatusCode);
        Assert.Equal(1, _traffic.Posts);
        await Verify(_tenant, _sale, approved, 1);
        Assert.Equal(original, await GatewayStatus(_tenant, key));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SolicitudesSimultaneas_ReservanAntesDeContactarPasarela(bool differentKey)
    {
        var key = Guid.NewGuid();
        _traffic.HoldFirstResponse = true;
        var first = Charge(_sale, key, "sim-approved-v1");
        await _traffic.ResponseArrived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        try
        {
            // Primera autorización ya ocurrió; MS-5 aún no recibe su respuesta.
            await using var db = Db(_tenant);
            Assert.Equal(Pos.Models.EstadoIntentoPago.PENDIENTE, (await db.IntentosPago.SingleAsync()).Estado);
            using var repeated = await Charge(_sale, differentKey ? Guid.NewGuid() : key, "sim-approved-v1");
            Assert.Equal(HttpStatusCode.Conflict, repeated.StatusCode);
            Assert.Equal(1, _traffic.Posts);
        }
        finally { _traffic.Release.TrySetResult(); }
        using var response = await first;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await Verify(_tenant, _sale, true, 1);
    }

    [Fact]
    public async Task Timeout_NoPermiteOtraClaveMientrasResultadoEsIncierto()
    {
        using var first = await Charge(_sale, Guid.NewGuid(), "sim-timeout-v1");
        Assert.Equal(HttpStatusCode.GatewayTimeout, first.StatusCode);
        using var retry = await Charge(_sale, Guid.NewGuid(), "sim-approved-v1");
        Assert.Equal(HttpStatusCode.Conflict, retry.StatusCode);
        Assert.Equal(1, _traffic.Posts);
        await Verify(_tenant, _sale, false, 1);
    }

    [Fact]
    public async Task ClaveDeOtraVentaDelTenant_NoLlamaPasarela()
    {
        var key = Guid.NewGuid();
        using var first = await Charge(_sale, key, "sim-approved-v1");
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var other = await SeedSale(_tenant, _cashier);
        using var retry = await Charge(other, key, "sim-approved-v1");
        Assert.Equal(HttpStatusCode.Conflict, retry.StatusCode);
        Assert.Equal(1, _traffic.Posts);
        await Verify(_tenant, other, false, 0);
    }

    [Fact]
    public async Task MismaClaveEnDosTenants_NoColisiona()
    {
        var key = Guid.NewGuid();
        using var first = await Charge(_sale, key, "sim-approved-v1");
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var tenantB = Guid.NewGuid();
        var cashierB = Guid.NewGuid();
        var saleB = await SeedSale(tenantB, cashierB);
        Authorize(tenantB, cashierB);
        using var second = await Charge(saleB, key, "sim-approved-v1");
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        await Verify(_tenant, _sale, true, 1);
        await Verify(tenantB, saleB, true, 1);
        Assert.NotEqual((await GatewayStatus(_tenant, key))!.TransactionId,
            (await GatewayStatus(tenantB, key))!.TransactionId);
    }

    [Theory]
    [InlineData("clave-invalida")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task ClaveInvalida_NoGeneraUnaNuevaAutorizacion(string key)
    {
        using var response = await _client.PostAsJsonAsync($"/ventas/{_sale}/cobrar-tarjeta",
            new { idempotency_key = key, scenario_id = "sim-approved-v1" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, _traffic.Posts);
        await Verify(_tenant, _sale, false, 0);
    }

    [Fact]
    public async Task RechazoDefinitivo_PermiteNuevoIntentoConOtraClave()
    {
        using var first = await Charge(_sale, Guid.NewGuid(), "sim-declined-v1");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, first.StatusCode);
        using var second = await Charge(_sale, Guid.NewGuid(), "sim-approved-v1");
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(2, _traffic.Posts);
        await Verify(_tenant, _sale, true, 2);
    }

    [Fact]
    public async Task RespuestaPerdida_ConsultaAprobacionSinReautorizar()
    {
        // El simulador aprueba realmente. Retenemos solo la respuesta del POST hasta
        // que expire el cliente; el GET posterior conserva la respuesta real de Node.
        _traffic.HoldFirstResponse = true;
        using var response = await Charge(_sale, Guid.NewGuid(), "sim-approved-v1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, _traffic.Posts);
        Assert.Equal(1, _traffic.Gets);
        await Verify(_tenant, _sale, true, 1);
    }

    [Fact]
    public async Task ErrorAlPersistirAprobacion_NoPierdeReservaNiReautoriza()
    {
        await using (var db = Db(_tenant))
        {
            // Fallo local determinista, después de la autorización externa.
            await db.Database.ExecuteSqlRawAsync("""
                CREATE FUNCTION fail_outbox() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN RAISE EXCEPTION 'S2-035 fallo de persistencia controlado'; END; $$;
                CREATE TRIGGER fail_outbox BEFORE INSERT ON "OutboxMessages"
                FOR EACH ROW EXECUTE FUNCTION fail_outbox();
                """);
        }
        var key = Guid.NewGuid();
        await Assert.ThrowsAsync<DbUpdateException>(() => Charge(_sale, key, "sim-approved-v1"));
        await Verify(_tenant, _sale, false, 1);
        await using (var db = Db(_tenant))
            Assert.Equal(Pos.Models.EstadoIntentoPago.PENDIENTE, (await db.IntentosPago.SingleAsync()).Estado);
        Assert.Equal("APPROVED", (await GatewayStatus(_tenant, key))!.Status);
        using var retry = await Charge(_sale, key, "sim-approved-v1");
        Assert.Equal(HttpStatusCode.Conflict, retry.StatusCode);
        Assert.Equal(1, _traffic.Posts);
        await using (var db = Db(_tenant))
            await db.Database.ExecuteSqlRawAsync("DROP TRIGGER fail_outbox ON \"OutboxMessages\";");
        using var recovered = await _client.GetAsync($"/ventas/{_sale}/intento-tarjeta");
        Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
        var body = await recovered.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal("APPROVED", body.GetProperty("status").GetString());
        await Verify(_tenant, _sale, true, 1);
        using var repeatedQuery = await _client.GetAsync($"/ventas/{_sale}/intento-tarjeta");
        Assert.Equal(HttpStatusCode.OK, repeatedQuery.StatusCode);
        await Verify(_tenant, _sale, true, 1);
        Assert.Equal(1, _traffic.Posts);
    }

    [Fact]
    public async Task ClaveEnHeader_SeRespetaYRechazaConflictoConBody()
    {
        var key = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("Idempotency-Key", key.ToString());
        using var conflict = await Charge(_sale, Guid.NewGuid(), "sim-approved-v1");
        Assert.Equal(HttpStatusCode.BadRequest, conflict.StatusCode);
        Assert.Equal(0, _traffic.Posts);
        using var response = await _client.PostAsJsonAsync($"/ventas/{_sale}/cobrar-tarjeta",
            new { scenario_id = "sim-approved-v1" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var db = Db(_tenant);
        Assert.Equal(key.ToString(), (await db.IntentosPago.SingleAsync()).IdempotencyKey);
        Assert.Equal("APPROVED", (await GatewayStatus(_tenant, key))!.Status);
    }

    private Task<HttpResponseMessage> Charge(Guid sale, Guid key, string scenario) =>
        _client.PostAsJsonAsync($"/ventas/{sale}/cobrar-tarjeta",
            new { idempotency_key = key, scenario_id = scenario });

    private async Task<Pos.DTOs.PaymentGatewayResponse?> GatewayStatus(Guid tenant, Guid key)
    {
        using var scope = _host!.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<Pos.Services.IPaymentGatewayClient>()
            .GetPaymentStatusAsync(tenant, key);
    }

    private void Authorize(Guid tenant, Guid cashier) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            CreateCajeroToken(_host!.Services.GetRequiredService<IConfiguration>(), tenant, cashier));

    private Pos.Data.PosCartDbContext Db(Guid tenant) => new(
        new DbContextOptionsBuilder<Pos.Data.PosCartDbContext>().UseNpgsql(_connection).Options)
        { CurrentTenantId = tenant };

    private async Task<Guid> SeedSale(Guid tenant, Guid cashier)
    {
        await using var db = Db(tenant);
        var turno = await db.Turnos.FirstOrDefaultAsync(t => t.CajeroId == cashier);
        if (turno == null)
        {
            turno = new Pos.Models.Turno { Id = Guid.NewGuid(), TenantId = tenant, CajeroId = cashier,
                SucursalId = Guid.NewGuid(), Estado = Pos.Models.EstadoTurno.ABIERTO, MontoApertura = 1000m };
            db.Turnos.Add(turno);
        }
        var sale = new Pos.Models.Venta { Id = Guid.NewGuid(), TenantId = tenant, CajeroId = cashier,
            SucursalId = turno.SucursalId, TurnoId = turno.Id, Subtotal = 100m, Impuestos = 19m, Total = 119m,
            MetodoPago = Pos.Models.MetodoPagoVenta.TARJETA, Estado = Pos.Models.EstadoVenta.PENDIENTE };
        db.Ventas.Add(sale);
        await db.SaveChangesAsync();
        return sale.Id;
    }

    private async Task Verify(Guid tenant, Guid sale, bool approved, int attempts)
    {
        await using var db = Db(tenant);
        Assert.Equal(approved ? Pos.Models.EstadoVenta.COMPLETADA : Pos.Models.EstadoVenta.PENDIENTE,
            (await db.Ventas.SingleAsync(v => v.Id == sale)).Estado);
        var payments = await db.Pagos.Where(p => p.VentaId == sale).ToListAsync();
        Assert.Equal(approved ? 1 : 0, payments.Count);
        if (approved) { Assert.Equal(119m, payments[0].Monto); Assert.Equal(Pos.Models.MetodoPago.TARJETA, payments[0].Metodo); }
        var events = await db.OutboxMessages.Where(o => o.VentaId == sale).ToListAsync();
        Assert.Equal(approved ? 1 : 0, events.Count);
        if (approved)
        {
            Assert.Equal("sale.completed", events[0].EventType);
            Assert.Equal(Pos.Models.OutboxStatus.PENDIENTE, events[0].Status);
        }
        Assert.Equal(attempts, await db.IntentosPago.CountAsync(i => i.VentaId == sale));
    }

    public async Task DisposeAsync()
    {
        _traffic.Release.TrySetResult();
        _client?.Dispose();
        if (_host != null) await _host.DisposeAsync();
        if (_simulator != null)
        {
            if (!_simulator.HasExited) _simulator.Kill(entireProcessTree: true);
            await _simulator.WaitForExitAsync();
            _simulator.Dispose();
        }
        if (_admin?.State == System.Data.ConnectionState.Open)
        {
            await using var command = new NpgsqlCommand($"DROP SCHEMA IF EXISTS \"{_schema}\" CASCADE", _admin);
            await command.ExecuteNonQueryAsync();
            await _admin.DisposeAsync();
        }
    }

    private sealed class GatewayTraffic : DelegatingHandler
    {
        private int _posts, _gets;
        public int Posts => _posts;
        public int Gets => _gets;
        public bool HoldFirstResponse { get; set; }
        public TaskCompletionSource ResponseArrived { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var post = request.Method == HttpMethod.Post;
            var count = post ? Interlocked.Increment(ref _posts) : Interlocked.Increment(ref _gets);
            var response = await base.SendAsync(request, ct);
            if (post && count == 1 && HoldFirstResponse)
            {
                ResponseArrived.TrySetResult();
                try { await Release.Task.WaitAsync(ct); }
                catch { response.Dispose(); throw; }
            }
            return response;
        }
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

}

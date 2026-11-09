extern alias POSCartServiceAlias;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
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

[Trait("Plan", "S2-092")]
public sealed class AnulacionVentaIntegrationTests : IClassFixture<Ms5WebApplicationFactory>, IAsyncLifetime
{
    private readonly Ms5WebApplicationFactory _factory;
    public AnulacionVentaIntegrationTests(Ms5WebApplicationFactory factory) => _factory = factory;
    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<Pos.Data.PosCartDbContext>().Database.MigrateAsync();
    }
    public Task DisposeAsync() => Task.CompletedTask;

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
            Estado = Pos.Models.EstadoVenta.COMPLETADA, MetodoPago = Pos.Models.MetodoPagoVenta.EFECTIVO };
        venta.Items.Add(new Pos.Models.ItemVenta { ProductoId = Guid.NewGuid(), NombreProducto = "Producto propio",
            Cantidad = 1, PrecioUnitario = 100, Subtotal = 100 });
        venta.Pagos.Add(new Pos.Models.Pago { Metodo = Pos.Models.MetodoPago.EFECTIVO, Monto = 200, Vuelto = 81 });
        if (Enum.TryParse<Pos.Models.EstadoVenta>(escenario, out var estado)) venta.Estado = estado;
        if (escenario == "ANULADA") venta.Anulacion = new Pos.Models.Anulacion { Motivo = "Anterior", AutorizadoPor = Guid.NewGuid() };
        if (escenario == "tarjeta") venta.MetodoPago = Pos.Models.MetodoPagoVenta.TARJETA;
        if (escenario == "mixto") venta.MetodoPago = Pos.Models.MetodoPagoVenta.MIXTO;
        if (escenario == "pago_tarjeta") venta.Pagos.Single().Metodo = Pos.Models.MetodoPago.TARJETA;
        if (escenario == "sin_pago") venta.Pagos.Clear();
        if (escenario == "monto_incorrecto") venta.Pagos.Single().Monto = 199;
        if (escenario == "vuelto_negativo") venta.Pagos.Single().Vuelto = -1;
        if (escenario is "incierto" or "fallido" or "aprobado" or "rechazado")
            venta.IntentosPago.Add(new Pos.Models.IntentoPago { TenantId = tenant, IdempotencyKey = Guid.NewGuid().ToString(),
                Monto = 119, Moneda = "CLP", Estado = escenario switch {
                    "fallido" => Pos.Models.EstadoIntentoPago.FALLIDO,
                    "aprobado" => Pos.Models.EstadoIntentoPago.APROBADO,
                    "rechazado" => Pos.Models.EstadoIntentoPago.RECHAZADO,
                    _ => Pos.Models.EstadoIntentoPago.PENDIENTE } });
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

    private async Task<string> Snapshot(Guid tenant)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Pos.Data.PosCartDbContext>();
        db.CurrentTenantId = tenant;
        // Proyecciones sin navegaciones cíclicas, contexto nuevo: detectar toda mutación relevante.
        var ventas = await db.Ventas.OrderBy(v => v.Id).Select(v => new { v.Id, v.TenantId, v.Estado, v.Total, v.Subtotal, v.Impuestos, v.TurnoId, v.CajeroId, v.SucursalId, v.MetodoPago }).ToListAsync();
        var items = await db.ItemsVenta.OrderBy(i => i.Id).Select(i => new { i.Id, i.VentaId, i.ProductoId, i.NombreProducto, i.Cantidad, i.PesoKg, i.PrecioUnitario, i.Subtotal }).ToListAsync();
        var pagos = await db.Pagos.OrderBy(p => p.Id).Select(p => new { p.Id, p.VentaId, p.Metodo, p.Monto, p.Vuelto, p.Referencia }).ToListAsync();
        var intentos = await db.IntentosPago.OrderBy(i => i.Id).Select(i => new { i.Id, i.Estado, i.IdempotencyKey, i.Monto, i.Moneda, i.Referencia, i.UpdatedAt }).ToListAsync();
        var anulaciones = await db.Anulaciones.OrderBy(a => a.Id).Select(a => new { a.Id, a.VentaId, a.Motivo, a.AutorizadoPor, a.CreatedAt }).ToListAsync();
        var outbox = await db.OutboxMessages.OrderBy(o => o.EventId).Select(o => new { o.EventId, o.Payload, o.Status }).ToListAsync();
        return JsonSerializer.Serialize(new { ventas, items, pagos, intentos, anulaciones, outbox });
    }

    [Theory]
    [InlineData("", "valida")]
    [InlineData("/api", "rechazado")]
    public async Task Admin_AnulaTotal_ConservaHistoricoYSoloUnaAuditoria(string prefix, string escenario)
    {
        var venta = await Seed(escenario);
        var admin = Guid.NewGuid(); // ADMIN no es el cajero y no tiene turno propio.
        using var client = Client(venta.TenantId, admin);
        using var response = await Send(client, venta.Id, prefix: prefix);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("ANULADA", json.GetProperty("estado").GetString());
        Assert.Equal(admin, json.GetProperty("anulacion").GetProperty("autorizado_por").GetGuid());
        Assert.Equal("Error total", json.GetProperty("anulacion").GetProperty("motivo").GetString());
        Assert.Equal("NO_APLICA", json.GetProperty("reembolso_tarjeta").GetString());
        Assert.Equal("GESTION_MANUAL", json.GetProperty("devolucion_efectivo").GetString());
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Pos.Data.PosCartDbContext>();
        db.CurrentTenantId = venta.TenantId;
        var persisted = await db.Ventas.Include(v => v.Pagos).Include(v => v.Items).SingleAsync(v => v.Id == venta.Id);
        Assert.Equal(Pos.Models.EstadoVenta.ANULADA, persisted.Estado);
        Assert.Equal(119, persisted.Total);
        Assert.Equal(200, Assert.Single(persisted.Pagos).Monto);
        Assert.Equal(81, persisted.Pagos.Single().Vuelto);
        Assert.Equal(100, Assert.Single(persisted.Items).PrecioUnitario);
        Assert.Single(await db.Anulaciones.ToListAsync());
        Assert.Single(await db.OutboxMessages.ToListAsync());
        var snapshot = await Snapshot(venta.TenantId);
        using var repeated = await Send(client, venta.Id);
        Assert.Equal(HttpStatusCode.Conflict, repeated.StatusCode);
        Assert.Equal(snapshot, await Snapshot(venta.TenantId));
    }

    [Theory]
    [InlineData(null, false, true, 401)]
    [InlineData("ADMIN", true, true, 401)]
    [InlineData("ADMIN", false, false, 401)]
    [InlineData("CAJERO", false, true, 403)]
    [InlineData("REPONEDOR", false, true, 403)]
    [InlineData("SUPER_ADMIN", false, true, 403)]
    public async Task Permisos_RechazanSinMutacion(string? role, bool expired, bool identity, int expected)
    {
        var venta = await Seed(); var before = await Snapshot(venta.TenantId);
        using var client = Client(venta.TenantId, Guid.NewGuid(), role, expired, identity);
        using var response = await Send(client, venta.Id);
        Assert.Equal(expected, (int)response.StatusCode);
        Assert.Equal(before, await Snapshot(venta.TenantId));
    }

    [Fact]
    public async Task TenantAjeno_EnAmbosSentidos_404ComoInexistente()
    {
        var a = await Seed(); var b = await Seed();
        var beforeA = await Snapshot(a.TenantId); var beforeB = await Snapshot(b.TenantId);
        foreach (var (owner, target) in new[] { (a, b), (b, a) })
        {
            using var client = Client(owner.TenantId, Guid.NewGuid());
            client.DefaultRequestHeaders.Add("X-Tenant-ID", target.TenantId.ToString());
            using var response = await Send(client, target.Id);
            using var missing = await Send(client, Guid.NewGuid());
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(await missing.Content.ReadAsStringAsync(), await response.Content.ReadAsStringAsync());
        }
        Assert.Equal(beforeA, await Snapshot(a.TenantId)); Assert.Equal(beforeB, await Snapshot(b.TenantId));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{\"motivo\":null}")]
    [InlineData("{\"motivo\":12}")]
    [InlineData("{\"motivo\":\"   \"}")]
    [InlineData("{\"motivo\":\"a\",\"items\":[]}")]
    [InlineData("{\"motivo\":\"a\",\"monto\":10}")]
    [InlineData("{\"motivo\":\"a\",\"tenant_id\":\"falso\"}")]
    [InlineData("{\"motivo\":\"a\",\"autorizado_por\":\"falso\"}")]
    [InlineData("{\"motivo\":\"a\",\"motivo\":\"b\"}")]
    [InlineData("{")]
    [InlineData("largo")]
    public async Task BodyInvalido_400SinMutacion(string body)
    {
        var venta = await Seed(); var before = await Snapshot(venta.TenantId);
        using var client = Client(venta.TenantId, Guid.NewGuid());
        using var response = await Send(client, venta.Id, body == "largo" ? JsonSerializer.Serialize(new { motivo = new string('a', 501) }) : body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(before, await Snapshot(venta.TenantId));
    }

    [Theory]
    [InlineData("PENDIENTE", "ESTADO_NO_ANULABLE")]
    [InlineData("CANCELADA", "ESTADO_NO_ANULABLE")]
    [InlineData("ANULADA", "VENTA_YA_ANULADA")]
    [InlineData("cerrado", "TURNO_NO_ANULABLE")]
    [InlineData("tarjeta", "REEMBOLSO_NO_DISPONIBLE")]
    [InlineData("pago_tarjeta", "REEMBOLSO_NO_DISPONIBLE")]
    [InlineData("aprobado", "REEMBOLSO_NO_DISPONIBLE")]
    [InlineData("incierto", "PAGO_INCIERTO")]
    [InlineData("fallido", "PAGO_INCIERTO")]
    [InlineData("mixto", "METODO_NO_SOPORTADO")]
    [InlineData("sin_pago", "PAGO_INCONSISTENTE")]
    [InlineData("monto_incorrecto", "PAGO_INCONSISTENTE")]
    [InlineData("vuelto_negativo", "PAGO_INCONSISTENTE")]
    public async Task Conflictos_409SinMutacion(string escenario, string codigo)
    {
        var venta = await Seed(escenario); var before = await Snapshot(venta.TenantId);
        using var client = Client(venta.TenantId, Guid.NewGuid());
        using var response = await Send(client, venta.Id);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(codigo, JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("codigo").GetString());
        Assert.Equal(before, await Snapshot(venta.TenantId));
    }

    [Fact]
    public async Task Concurrentes_UnaSolaAnulacion()
    {
        var venta = await Seed();
        using var a = Client(venta.TenantId, Guid.NewGuid()); using var b = Client(venta.TenantId, Guid.NewGuid());
        var responses = await Task.WhenAll(Send(a, venta.Id), Send(b, venta.Id));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Pos.Data.PosCartDbContext>(); db.CurrentTenantId = venta.TenantId;
        Assert.Single(await db.Anulaciones.ToListAsync());
        foreach (var response in responses) response.Dispose();
    }

    [Fact]
    public async Task ServicioSinAdmin_NoPermiteEvitarController()
    {
        var venta = await Seed(); var before = await Snapshot(venta.TenantId);
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<Pos.Data.PosCartDbContext>().CurrentTenantId = venta.TenantId;
        var service = scope.ServiceProvider.GetRequiredService<Pos.Services.IVentaService>();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.AnularAsync(venta.Id, Guid.NewGuid(), "motivo"));
        Assert.Equal(before, await Snapshot(venta.TenantId));
    }
    [Fact]
    public async Task FalloPersistencia_RollbackSinAnulacionParcial()
    {
        var venta = await Seed(); var before = await Snapshot(venta.TenantId);
        var connection = _factory.Services.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection");
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<Pos.Data.PosCartDbContext>>();
            services.RemoveAll<Pos.Data.PosCartDbContext>();
            services.AddDbContext<Pos.Data.PosCartDbContext>(options =>
                options.UseNpgsql(connection).AddInterceptors(new FalloAnulacion()));
        }));
        using var client = Client(venta.TenantId, Guid.NewGuid(), factory: factory);
        using var response = await Send(client, venta.Id);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("fallo controlado", await response.Content.ReadAsStringAsync());
        Assert.Equal(before, await Snapshot(venta.TenantId));
    }

    // Falla después de ejecutar SaveChanges, antes del commit: prueba rollback real.
    private sealed class FalloAnulacion : SaveChangesInterceptor
    {
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<Pos.Models.Anulacion>().Any())
                throw new InvalidOperationException("fallo controlado tras escritura");
            return ValueTask.FromResult(result);
        }
    }

}

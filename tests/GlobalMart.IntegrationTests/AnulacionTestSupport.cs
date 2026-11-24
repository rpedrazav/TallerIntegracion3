extern alias POSCartServiceAlias;

using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Pos = POSCartServiceAlias::POSCartService;

namespace GlobalMart.IntegrationTests;

internal static class AnulacionTestSupport
{
    public static HttpClient Client(WebApplicationFactory<POSCartServiceAlias::Program> factory, Guid tenant, Guid admin, string? role = "ADMIN", bool expired = false, bool identity = true)
    {
        var client = factory.CreateClient();
        if (role is null) return client;
        var config = factory.Services.GetRequiredService<IConfiguration>();
        var claims = new List<Claim> { new("tenant_id", tenant.ToString()), new(ClaimTypes.Role, role) };
        if (identity) claims.Add(new Claim("sub", admin.ToString()));
        var token = new JwtSecurityToken(config["Jwt:Issuer"], config["Jwt:Audience"], claims,
            expires: expired ? DateTime.UtcNow.AddMinutes(-5) : DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)), SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        return client;
    }
    public static Task<HttpResponseMessage> Send(HttpClient client, Guid id, string body = "{\"motivo\":\"  Error total  \"}")
        => client.PostAsync($"/api/ventas/{id}/anular", new StringContent(body, Encoding.UTF8, "application/json"));

    public static async Task<string> Snapshot(IServiceProvider services, Guid tenant)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Pos.Data.PosCartDbContext>();
        db.CurrentTenantId = tenant;
        // Proyecciones sin navegaciones cíclicas, contexto nuevo: detectar toda mutación relevante.
        var ventas = await db.Ventas.OrderBy(v => v.Id).Select(v => new { v.Id, v.TenantId, v.Estado, v.Total, v.Subtotal, v.Impuestos, v.TurnoId, v.CajeroId, v.SucursalId, v.MetodoPago }).ToListAsync();
        var items = await db.ItemsVenta.OrderBy(i => i.Id).Select(i => new { i.Id, i.VentaId, i.ProductoId, i.NombreProducto, i.Cantidad, i.PesoKg, i.PrecioUnitario, i.Subtotal }).ToListAsync();
        var pagos = await db.Pagos.OrderBy(p => p.Id).Select(p => new { p.Id, p.VentaId, p.Metodo, p.Monto, p.Vuelto, p.Referencia }).ToListAsync();
        var intentos = await db.IntentosPago.OrderBy(i => i.Id).Select(i => new { i.Id, i.Estado, i.IdempotencyKey, i.Monto, i.Moneda, i.Referencia, i.UpdatedAt }).ToListAsync();
        var anulaciones = await db.Anulaciones.OrderBy(a => a.Id).Select(a => new { a.Id, a.VentaId, a.Motivo, a.AutorizadoPor, a.CreatedAt }).ToListAsync();
        var outbox = await db.OutboxMessages.OrderBy(o => o.EventId).Select(o => new { o.EventId, o.TenantId, o.VentaId, o.EventType, o.Topic, o.MessageKey, o.Payload, o.Status, o.CreatedAt, o.AttemptCount, o.TotalAttemptCount, o.NextAttemptAt, o.PublishedAt }).ToListAsync();
        var reversas = await db.ReversasPago.OrderBy(r => r.Id).Select(r => new { r.Id, r.TenantId, r.VentaId, r.Estado, r.ReferenciaPago, r.ReferenciaReversa, r.Monto, r.Moneda, r.CodigoRechazo, r.Motivo, r.AutorizadoPor, r.CreatedAt, r.UpdatedAt }).ToListAsync();
        return JsonSerializer.Serialize(new { ventas, items, pagos, intentos, anulaciones, outbox, reversas });
    }

}

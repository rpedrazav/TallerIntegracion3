extern alias WarehouseInventoryServiceAlias;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using WarehouseDbContext = WarehouseInventoryServiceAlias::WarehouseInventoryService.Data.WarehouseDbContext;
using Lote = WarehouseInventoryServiceAlias::WarehouseInventoryService.Models.Lote;
using Stock = WarehouseInventoryServiceAlias::WarehouseInventoryService.Models.Stock;
using Xunit;

namespace GlobalMart.IntegrationTests;

public class StockConsumerIntegrationTests : IClassFixture<Ms4WebApplicationFactory>
{
    private readonly Ms4WebApplicationFactory _factory;

    public StockConsumerIntegrationTests(Ms4WebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProcesarSaleCompleted_DescuentaStock_VerificablePorApi()
    {
        var tenantId = Guid.NewGuid();
        var sucursalId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var loteId = Guid.NewGuid();
        var ventaId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        const decimal stockInicial = 10m;
        const decimal cantidadVendida = 2m;
        const decimal stockEsperado = stockInicial - cantidadVendida;

        var configuration = _factory.Services.GetRequiredService<IConfiguration>();
        var token = CreateReponedorToken(configuration, tenantId);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
            await db.Database.MigrateAsync();

            db.Lotes.Add(new Lote
            {
                Id = loteId,
                ProductoId = productId,
                TenantId = tenantId,
                FechaVencimiento = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
                Cantidad = stockInicial,
                Ubicacion = "Bodega de prueba"
            });
            db.Stocks.Add(new Stock
            {
                ProductoId = productId,
                SucursalId = sucursalId,
                TenantId = tenantId,
                CantidadActual = stockInicial,
                StockMinimo = 1m
            });
            await db.SaveChangesAsync();
        }

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = "localhost:9092",
            ClientId = $"integration-test-{Guid.NewGuid():N}",
            Acks = Acks.All
        };

        using (var producer = new ProducerBuilder<string, string>(producerConfig).Build())
        {
            var payload = new
            {
                event_id = eventId,
                venta_id = ventaId,
                tenant_id = tenantId,
                sucursal_id = sucursalId,
                total = 100m,
                timestamp = DateTime.UtcNow,
                items = new[]
                {
                    new
                    {
                        producto_id = productId,
                        cantidad = cantidadVendida,
                        lote_id = loteId
                    }
                }
            };

            await producer.ProduceAsync(_factory.Topic, new Message<string, string>
            {
                Key = tenantId.ToString(),
                Value = JsonSerializer.Serialize(payload)
            });
            producer.Flush(TimeSpan.FromSeconds(5));
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var deadline = DateTime.UtcNow.AddSeconds(10);
        HttpStatusCode? lastStatus = null;
        decimal? observedQuantity = null;

        while (DateTime.UtcNow < deadline)
        {
            using var response = await client.GetAsync($"/stock/{productId}?sucursal_id={sucursalId}");
            lastStatus = response.StatusCode;

            if (response.IsSuccessStatusCode)
            {
                using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var quantityProperty = document.RootElement.TryGetProperty("cantidadActual", out var camelCase)
                    ? camelCase
                    : document.RootElement.GetProperty("cantidad_actual");
                observedQuantity = quantityProperty.GetDecimal();

                if (observedQuantity == stockEsperado)
                {
                    break;
                }
            }

            await Task.Delay(250);
        }

        Assert.Equal(HttpStatusCode.OK, lastStatus);
        Assert.Equal(stockEsperado, observedQuantity);
    }

    [Fact]
    [Trait("Plan", "S2-100")]
    public async Task EventoDuplicado_DescuentaStockUnaSolaVez()
    {
        var tenantId = Guid.NewGuid();
        var sucursalId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
            db.CurrentTenantId = tenantId;
            db.Stocks.Add(new Stock
            {
                ProductoId = productId,
                SucursalId = sucursalId,
                TenantId = tenantId,
                CantidadActual = 10
            });
            await db.SaveChangesAsync();
        }

        var payload = JsonSerializer.Serialize(new
        {
            event_id = eventId,
            venta_id = Guid.NewGuid(),
            tenant_id = tenantId,
            sucursal_id = sucursalId,
            items = new[] { new { producto_id = productId, cantidad = 2m } }
        });

        await PublishAsync(tenantId, payload);
        await PublishAsync(tenantId, payload);
        await WaitUntilAsync(() => EventWasProcessed(eventId));
        await Task.Delay(1000);

        using var verificationScope = _factory.Services.CreateScope();
        var verification = verificationScope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
        verification.CurrentTenantId = tenantId;
        Assert.Equal(8m, await verification.Stocks.Where(s => s.ProductoId == productId).Select(s => s.CantidadActual).SingleAsync());
        Assert.Equal(1, await verification.Movimientos.CountAsync(m => m.ProductoId == productId));
        Assert.Equal(1, await verification.EventosKafkaProcesados.CountAsync(e => e.EventId == eventId));
    }

    [Fact]
    [Trait("Plan", "S2-100")]
    public async Task FalloEnSegundoItem_NoDejaDescuentoParcialYSeRecupera()
    {
        var tenantId = Guid.NewGuid();
        var sucursalId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
            db.CurrentTenantId = tenantId;
            db.Stocks.AddRange(
                new Stock { ProductoId = productId, SucursalId = sucursalId, TenantId = tenantId, CantidadActual = 10 },
                new Stock { ProductoId = productId2, SucursalId = sucursalId, TenantId = tenantId, CantidadActual = 1 });
            await db.SaveChangesAsync();
        }

        var payload = JsonSerializer.Serialize(new
        {
            event_id = eventId,
            venta_id = Guid.NewGuid(),
            tenant_id = tenantId,
            sucursal_id = sucursalId,
            items = new[]
            {
                new { producto_id = productId, cantidad = 2m },
                new { producto_id = productId2, cantidad = 3m }
            }
        });

        await PublishAsync(tenantId, payload);
        await Task.Delay(2000);

        using (var failedScope = _factory.Services.CreateScope())
        {
            var db = failedScope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
            db.CurrentTenantId = tenantId;
            Assert.Equal(10m, await db.Stocks.Where(s => s.ProductoId == productId).Select(s => s.CantidadActual).SingleAsync());
            Assert.Equal(1m, await db.Stocks.Where(s => s.ProductoId == productId2).Select(s => s.CantidadActual).SingleAsync());
            Assert.Empty(await db.Movimientos.ToListAsync());
            Assert.False(await db.EventosKafkaProcesados.AnyAsync(e => e.EventId == eventId));

            var secondStock = await db.Stocks.SingleAsync(s => s.ProductoId == productId2);
            secondStock.CantidadActual = 5;
            await db.SaveChangesAsync();
        }

        await WaitUntilAsync(() => EventWasProcessed(eventId));

        using var recoveredScope = _factory.Services.CreateScope();
        var recovered = recoveredScope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
        recovered.CurrentTenantId = tenantId;
        Assert.Equal(8m, await recovered.Stocks.Where(s => s.ProductoId == productId).Select(s => s.CantidadActual).SingleAsync());
        Assert.Equal(2m, await recovered.Stocks.Where(s => s.ProductoId == productId2).Select(s => s.CantidadActual).SingleAsync());
        Assert.Equal(2, await recovered.Movimientos.CountAsync());
        Assert.Equal(1, await recovered.EventosKafkaProcesados.CountAsync(e => e.EventId == eventId));
    }

    private async Task PublishAsync(Guid tenantId, string payload)
    {
        using var producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = "localhost:9092",
            Acks = Acks.All
        }).Build();
        await producer.ProduceAsync(_factory.Topic, new Message<string, string>
        {
            Key = tenantId.ToString(),
            Value = payload
        });
    }

    private async Task<bool> EventWasProcessed(Guid eventId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
        return await db.EventosKafkaProcesados.AnyAsync(e => e.EventId == eventId);
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(250);
        }

        Assert.Fail("El evento no fue procesado antes del timeout");
    }

    private static string CreateReponedorToken(IConfiguration configuration, Guid tenantId)
    {
        var secretKey = configuration["Jwt:Key"]
            ?? "TuSuperSecretoDeDesarrollo1234567890!";
        var issuer = configuration["Jwt:Issuer"] ?? "GlobalMart";
        var audience = configuration["Jwt:Audience"] ?? "GlobalMartOS_Clients";

        var claims = new[]
        {
            new Claim("tenant_id", tenantId.ToString()),
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "REPONEDOR")
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
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

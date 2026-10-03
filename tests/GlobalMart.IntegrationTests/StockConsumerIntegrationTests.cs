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
    private const string Topic = "sale.completed";
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

            await producer.ProduceAsync(Topic, new Message<string, string>
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

    private static string CreateReponedorToken(IConfiguration configuration, Guid tenantId)
    {
        var secretKey = configuration["Jwt:SecretKey"]
            ?? "TuSuperSecretoDeDesarrollo1234567890!";
        var issuer = configuration["Jwt:Issuer"] ?? "GlobalMart";
        var audience = configuration["Jwt:Audience"] ?? "GlobalMartUsers";

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

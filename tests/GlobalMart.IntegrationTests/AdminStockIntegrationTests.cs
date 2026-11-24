extern alias WarehouseInventoryServiceAlias;

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
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using WarehouseDbContext = WarehouseInventoryServiceAlias::WarehouseInventoryService.Data.WarehouseDbContext;
using Xunit;

namespace GlobalMart.IntegrationTests;

public class AdminStockIntegrationTests : IClassFixture<Ms4WebApplicationFactory>
{
    private readonly Ms4WebApplicationFactory _baseFactory;

    public AdminStockIntegrationTests(Ms4WebApplicationFactory baseFactory) => _baseFactory = baseFactory;

    [Fact]
    public async Task Ajuste_ValidaRolYReferencias_RegistraStockYAislaTenant()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        using var factory = _baseFactory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHttpClientFactory>();
            services.AddSingleton<IHttpClientFactory>(new StubHttpClientFactory(productId, branchId));
        }));
        using var client = factory.CreateClient();
        var configuration = factory.Services.GetRequiredService<IConfiguration>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(configuration, tenantId, "CAJERO"));
        var request = new { productoId = productId, sucursalId = branchId, cantidadActual = 12.5m, stockMinimo = 2m, motivo = "Conteo inicial" };
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/stock/ajuste", request)).StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(configuration, tenantId, "ADMIN"));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/stock/ajuste", request with { productoId = Guid.NewGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/stock/ajuste", request with { sucursalId = Guid.NewGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/stock/ajuste", request with { cantidadActual = -1m })).StatusCode);

        var adjusted = await client.PostAsJsonAsync("/stock/ajuste", request);
        Assert.Equal(HttpStatusCode.OK, adjusted.StatusCode);
        var list = await client.GetFromJsonAsync<StockResponse[]>($"/stock?sucursal_id={branchId}");
        Assert.Single(list!);
        Assert.Equal(12.5m, list![0].CantidadActual);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
            db.CurrentTenantId = tenantId;
            Assert.Equal(1, await db.Movimientos.CountAsync());
            Assert.Equal(12.5m, await db.Movimientos.Select(m => m.Cantidad).SingleAsync());
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(configuration, otherTenantId, "ADMIN"));
        Assert.Empty((await client.GetFromJsonAsync<StockResponse[]>($"/stock?sucursal_id={branchId}"))!);
    }

    private static string Token(IConfiguration configuration, Guid tenantId, string role)
    {
        var claims = new[] { new Claim("tenant_id", tenantId.ToString()), new Claim(ClaimTypes.Role, role) };
        var jwt = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"], audience: configuration["Jwt:Audience"],
            claims: claims, expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!)),
                SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private sealed record StockResponse(Guid ProductoId, Guid SucursalId, decimal CantidadActual, decimal StockMinimo);

    private sealed class StubHttpClientFactory(Guid productId, Guid branchId) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new StubHandler(productId, branchId));
    }

    private sealed class StubHandler(Guid productId, Guid branchId) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.NotNull(request.Headers.Authorization);
            HttpResponseMessage response;
            if (request.RequestUri!.AbsolutePath.StartsWith("/products/", StringComparison.Ordinal))
                response = new(request.RequestUri.AbsolutePath.EndsWith(productId.ToString(), StringComparison.OrdinalIgnoreCase)
                    ? HttpStatusCode.OK : HttpStatusCode.NotFound);
            else if (request.RequestUri.AbsolutePath == "/sucursales")
                response = new(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new[] { new { id = branchId, activa = true } })
                };
            else
                response = new(HttpStatusCode.NotFound);
            return Task.FromResult(response);
        }
    }
}

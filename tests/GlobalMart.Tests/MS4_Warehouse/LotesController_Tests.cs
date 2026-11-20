using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using WarehouseInventoryService.Controllers;
using WarehouseInventoryService.Data;
using WarehouseInventoryService.Models;
using WarehouseInventoryService.Services;
using Xunit;

namespace GlobalMart.Tests.MS4_Warehouse;

public sealed class LotesController_Tests
{
    [Fact]
    public async Task List_DevuelveSoloDisponiblesDelTenantSucursalYRango()
    {
        var tenantId = Guid.NewGuid();
        var otroTenant = Guid.NewGuid();
        var sucursalId = Guid.NewGuid();
        var otraSucursal = Guid.NewGuid();
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        await using var db = CreateDb(tenantId);
        var temprano = Lote(tenantId, sucursalId, hoy.AddDays(2), 3);
        var tardio = Lote(tenantId, sucursalId, hoy.AddDays(30), 4);
        db.Lotes.AddRange(
            tardio,
            temprano,
            Lote(tenantId, sucursalId, hoy.AddDays(-1), 5),
            Lote(tenantId, sucursalId, hoy.AddDays(31), 5),
            Lote(tenantId, sucursalId, hoy.AddDays(3), 0),
            Lote(tenantId, otraSucursal, hoy.AddDays(3), 5),
            Lote(otroTenant, sucursalId, hoy.AddDays(3), 5));
        await db.SaveChangesAsync();
        var controller = CreateController(db, tenantId, sucursalId);

        var result = await controller.List(30, sucursalId, default);

        var response = Assert.IsType<OkObjectResult>(result);
        var lotes = Assert.IsType<List<LotesController.LoteResponse>>(response.Value);
        Assert.Equal([temprano.Id, tardio.Id], lotes.Select(l => l.Id));
        Assert.All(lotes, lote =>
        {
            Assert.Equal(sucursalId, lote.SucursalId);
            Assert.True(lote.Cantidad > 0);
        });
    }

    [Theory]
    [InlineData(null)]
    [InlineData(-1)]
    [InlineData(366)]
    public async Task List_PorVencerFueraDeRango_Retorna400(int? porVencer)
    {
        var tenantId = Guid.NewGuid();
        var sucursalId = Guid.NewGuid();
        await using var db = CreateDb(tenantId);
        var controller = CreateController(db, tenantId, sucursalId);

        var result = await controller.List(porVencer, sucursalId, default);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    private static WarehouseDbContext CreateDb(Guid tenantId)
    {
        var options = new DbContextOptionsBuilder<WarehouseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new WarehouseDbContext(options) { CurrentTenantId = tenantId };
    }

    private static Lote Lote(Guid tenantId, Guid sucursalId, DateOnly vencimiento, decimal cantidad) => new()
    {
        TenantId = tenantId,
        ProductoId = Guid.NewGuid(),
        SucursalId = sucursalId,
        FechaVencimiento = vencimiento,
        Cantidad = cantidad,
        Ubicacion = "Bodega"
    };

    private static LotesController CreateController(WarehouseDbContext db, Guid tenantId, Guid sucursalId)
    {
        var httpClients = new Mock<IHttpClientFactory>();
        httpClients.Setup(factory => factory.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(new StubHandler(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent($"[{{\"id\":\"{sucursalId}\",\"activa\":true}}]")
                })));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Services:IdentityUrl"] = "http://identity"
        }).Build();
        var controller = new LotesController(
            new LoteService(db),
            new RecepcionValidator(httpClients.Object, configuration));
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("tenant_id", tenantId.ToString()),
                new Claim(ClaimTypes.Role, "ADMIN")
            }, "TestAuth"))
        };
        context.Request.Headers.Authorization = "Bearer test-token";
        controller.ControllerContext = new ControllerContext { HttpContext = context };
        return controller;
    }

    private sealed class StubHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(response);
    }
}

using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Moq;
using WarehouseInventoryService.Controllers;
using WarehouseInventoryService.Data;
using WarehouseInventoryService.Services;
using Xunit;

namespace GlobalMart.Tests.MS4_Warehouse;

public sealed class RecepcionValidator_Tests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ProductoId = Guid.NewGuid();
    private static readonly Guid SucursalId = Guid.NewGuid();

    [Fact]
    public async Task Create_CantidadNoPositivaOVencimientoPasado_Retorna400SinPersistir()
    {
        await using var db = CreateDb();
        var controller = CreateController(db, _ => throw new InvalidOperationException("No debe llamar servicios externos."));

        var cantidadResult = await controller.Create(Request(cantidad: 0), default);
        var vencimientoResult = await controller.Create(Request(fecha: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))), default);

        Assert.Equal(400, Assert.IsType<ObjectResult>(cantidadResult).StatusCode);
        Assert.Equal(400, Assert.IsType<ObjectResult>(vencimientoResult).StatusCode);
        await AssertEmpty(db);
    }

    [Fact]
    public async Task Create_ProductoInexistente_Retorna404SinPersistir()
    {
        await using var db = CreateDb();
        var controller = CreateController(db, _ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var result = await controller.Create(Request(), default);

        Assert.Equal(404, Assert.IsType<ObjectResult>(result).StatusCode);
        await AssertEmpty(db);
    }

    [Fact]
    public async Task Create_SucursalAjena_Retorna404SinPersistir()
    {
        await using var db = CreateDb();
        var controller = CreateController(db, request => request.RequestUri!.AbsolutePath.StartsWith("/products/")
            ? Json(HttpStatusCode.OK, "{}")
            : Json(HttpStatusCode.OK, $"[{{\"id\":\"{Guid.NewGuid()}\",\"activa\":true}}]"));

        var result = await controller.Create(Request(), default);

        Assert.Equal(404, Assert.IsType<ObjectResult>(result).StatusCode);
        await AssertEmpty(db);
    }

    [Fact]
    public async Task Create_ReferenciasValidas_PersisteRecepcion()
    {
        await using var db = CreateDb();
        var controller = CreateController(db, request => request.RequestUri!.AbsolutePath.StartsWith("/products/")
            ? Json(HttpStatusCode.OK, "{}")
            : Json(HttpStatusCode.OK, $"[{{\"id\":\"{SucursalId}\",\"activa\":true}}]"));

        var result = await controller.Create(Request(), default);

        Assert.Equal(201, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Equal(2, (await db.Stocks.SingleAsync()).CantidadActual);
        Assert.Single(await db.Lotes.ToListAsync());
        Assert.Single(await db.Movimientos.ToListAsync());
    }

    private static WarehouseDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<WarehouseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new WarehouseDbContext(options) { CurrentTenantId = TenantId };
    }

    private static RecepcionesController CreateController(
        WarehouseDbContext db,
        Func<HttpRequestMessage, HttpResponseMessage> response)
    {
        var httpClients = new Mock<IHttpClientFactory>();
        httpClients.Setup(factory => factory.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(new StubHandler(response)));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Services:CatalogUrl"] = "http://catalog",
            ["Services:IdentityUrl"] = "http://identity"
        }).Build();
        var controller = new RecepcionesController(
            new RecepcionService(db),
            new RecepcionValidator(httpClients.Object, configuration));
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("tenant_id", TenantId.ToString()),
                new Claim(ClaimTypes.Role, "ADMIN")
            }, "TestAuth"))
        };
        context.Request.Headers.Authorization = "Bearer test-token";
        controller.ControllerContext = new ControllerContext { HttpContext = context };
        return controller;
    }

    private static RecepcionesController.RecepcionRequest Request(decimal cantidad = 2, DateOnly? fecha = null) =>
        new(ProductoId, SucursalId, cantidad, fecha ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), "Bodega");

    private static HttpResponseMessage Json(HttpStatusCode status, string content) =>
        new(status) { Content = new StringContent(content) };

    private static async Task AssertEmpty(WarehouseDbContext db)
    {
        Assert.Empty(await db.Stocks.ToListAsync());
        Assert.Empty(await db.Lotes.ToListAsync());
        Assert.Empty(await db.Movimientos.ToListAsync());
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(response(request));
    }
}

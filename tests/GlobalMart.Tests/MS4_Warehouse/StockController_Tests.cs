using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WarehouseInventoryService.Controllers;
using WarehouseInventoryService.Data;
using WarehouseInventoryService.Models;
using WarehouseInventoryService.Repositories;
using Xunit;

namespace GlobalMart.Tests.MS4_Warehouse;

/// <summary>
/// Tests unitarios para TI3-255: Revisión y finalización de endpoints en MS-4 (StockController).
/// Verifica códigos de respuesta HTTP: 200, 400, 401, 404 y aislamiento multi-tenant.
/// </summary>
public class StockController_Tests : IDisposable
{
    private static readonly Guid TenantId   = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid SucursalId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");
    private static readonly Guid ProductoId = Guid.Parse("cccccccc-0000-0000-0000-000000000001");

    private readonly WarehouseDbContext _context;
    private readonly Mock<IStockRepository> _repoMock;
    private readonly Mock<ILogger<StockController>> _loggerMock;
    private readonly StockController _controller;

    public StockController_Tests()
    {
        var options = new DbContextOptionsBuilder<WarehouseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new WarehouseDbContext(options);
        _repoMock = new Mock<IStockRepository>();
        _loggerMock = new Mock<ILogger<StockController>>();

        _controller = new StockController(_repoMock.Object, _context, _loggerMock.Object);

        // Por defecto, configurar HttpContext con claim de tenant válido
        ConfigurarUserConTenant(TenantId);
    }

    private void ConfigurarUserConTenant(Guid? tenantId)
    {
        var claims = new List<Claim>();
        if (tenantId.HasValue)
        {
            claims.Add(new Claim("tenant_id", tenantId.Value.ToString()));
            claims.Add(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));
        }

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    // ────────────────────────────────────────────────────────────────────────
    // GET /stock/{productId}?sucursal_id=
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByProducto_ProductoExiste_Retorna200ConStock()
    {
        // Arrange
        var stockEsperado = new Stock
        {
            ProductoId = ProductoId,
            SucursalId = SucursalId,
            TenantId = TenantId,
            CantidadActual = 42,
            StockMinimo = 5
        };

        _repoMock
            .Setup(r => r.GetByProducto(ProductoId, SucursalId, TenantId))
            .ReturnsAsync(stockEsperado);

        // Act
        var result = await _controller.GetByProducto(ProductoId, SucursalId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        var stockRetornado = okResult.Value.Should().BeOfType<Stock>().Subject;
        stockRetornado.CantidadActual.Should().Be(42);
        stockRetornado.ProductoId.Should().Be(ProductoId);
    }

    [Fact]
    public async Task GetByProducto_ProductoNoExiste_Retorna404()
    {
        // Arrange
        _repoMock
            .Setup(r => r.GetByProducto(ProductoId, SucursalId, TenantId))
            .ReturnsAsync((Stock?)null);

        // Act
        var result = await _controller.GetByProducto(ProductoId, SucursalId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetByProducto_SucursalVacia_Retorna400()
    {
        // Act — Guid.Empty como sucursal_id
        var result = await _controller.GetByProducto(ProductoId, Guid.Empty);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task GetByProducto_SinTenantClaim_Retorna401()
    {
        // Arrange — Usuario sin claim tenant_id
        ConfigurarUserConTenant(null);

        // Act
        var result = await _controller.GetByProducto(ProductoId, SucursalId);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.StatusCode.Should().Be(401);
    }

    // ────────────────────────────────────────────────────────────────────────
    // GET /stock?sucursal_id=
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_RetornaListaDeStocksDelTenant()
    {
        // Arrange
        _context.Stocks.AddRange(
            new Stock { ProductoId = Guid.NewGuid(), SucursalId = SucursalId, TenantId = TenantId, CantidadActual = 10 },
            new Stock { ProductoId = Guid.NewGuid(), SucursalId = SucursalId, TenantId = TenantId, CantidadActual = 25 }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(null);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        var stocks = okResult.Value.Should().BeAssignableTo<IEnumerable<Stock>>().Subject;
        stocks.Count().Should().Be(2);
    }

    [Fact]
    public async Task GetAll_ConFiltroSucursal_RetornaSoloDeEsaSucursal()
    {
        // Arrange
        var sucursal2 = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
        _context.Stocks.AddRange(
            new Stock { ProductoId = Guid.NewGuid(), SucursalId = SucursalId, TenantId = TenantId, CantidadActual = 10 },
            new Stock { ProductoId = Guid.NewGuid(), SucursalId = sucursal2, TenantId = TenantId, CantidadActual = 50 }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(SucursalId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var stocks = okResult.Value.Should().BeAssignableTo<IEnumerable<Stock>>().Subject;
        stocks.Count().Should().Be(1);
        stocks.First().SucursalId.Should().Be(SucursalId);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

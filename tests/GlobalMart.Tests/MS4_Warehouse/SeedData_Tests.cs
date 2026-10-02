using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GlobalMart.Tests.MS4_Warehouse;

/// <summary>
/// Tests unitarios para TI3-256: Script de seed `SeedData.cs` con 30 productos
/// variados (frutas, carnes, lácteos, snacks), códigos de barras, precios y stock.
/// </summary>
public class SeedData_Tests : IDisposable
{
    private readonly CatalogPricingService.Data.CatalogDbContext _catalogContext;
    private readonly WarehouseInventoryService.Data.WarehouseDbContext _warehouseContext;

    public SeedData_Tests()
    {
        var catalogOptions = new DbContextOptionsBuilder<CatalogPricingService.Data.CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _catalogContext = new CatalogPricingService.Data.CatalogDbContext(catalogOptions);

        var warehouseOptions = new DbContextOptionsBuilder<WarehouseInventoryService.Data.WarehouseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _warehouseContext = new WarehouseInventoryService.Data.WarehouseDbContext(warehouseOptions);
    }

    [Fact]
    public void CatalogSeedData_InsertaExactamente30ProductosConCategoriasYPrecios()
    {
        // Act
        CatalogPricingService.Data.SeedData.Initialize(_catalogContext);

        // Assert — 4 Categorías creadas
        var categorias = _catalogContext.Categorias.IgnoreQueryFilters().ToList();
        categorias.Should().HaveCount(4);
        categorias.Select(c => c.Nombre).Should().Contain(new[]
        {
            "Frutas y Verduras",
            "Carnes y Cecinas",
            "Lácteos y Huevos",
            "Snacks y Dulces"
        });

        // Assert — Exactamente 30 Productos
        var productos = _catalogContext.Productos.IgnoreQueryFilters().ToList();
        productos.Should().HaveCount(30);

        // Assert — Cada producto tiene código de barras único y precio mayor a 0
        productos.Select(p => p.CodigoBarras).Distinct().Should().HaveCount(30);
        productos.Should().AllSatisfy(p =>
        {
            p.CodigoBarras.Should().NotBeNullOrWhiteSpace();
            p.PrecioBase.Should().BeGreaterThan(0);
            p.Nombre.Should().NotBeNullOrWhiteSpace();
            p.TenantId.Should().Be(CatalogPricingService.Data.SeedData.DemoTenantId);
            p.CategoriaId.Should().NotBeNull();
        });

        // Assert — Cada producto tiene su registro de Precio
        var precios = _catalogContext.Precios.IgnoreQueryFilters().ToList();
        precios.Should().HaveCount(30);
        precios.Should().AllSatisfy(pr =>
        {
            pr.PrecioLocal.Should().BeGreaterThan(0);
            pr.MonedaFx.Should().Be("CLP");
            pr.SucursalId.Should().Be(CatalogPricingService.Data.SeedData.DemoSucursalId);
        });
    }

    [Fact]
    public void CatalogSeedData_EjecutarDosVeces_EsIdempotenteNoDuplica()
    {
        // Act — Ejecutar dos veces consecutivas
        CatalogPricingService.Data.SeedData.Initialize(_catalogContext);
        CatalogPricingService.Data.SeedData.Initialize(_catalogContext);

        // Assert — Siguen habiendo exactamente 30 productos y 4 categorías
        _catalogContext.Productos.IgnoreQueryFilters().Count().Should().Be(30);
        _catalogContext.Categorias.IgnoreQueryFilters().Count().Should().Be(4);
        _catalogContext.Precios.IgnoreQueryFilters().Count().Should().Be(30);
    }

    [Fact]
    public void WarehouseSeedData_InsertaStockParaLos30Productos()
    {
        // Act
        WarehouseInventoryService.Data.SeedData.Initialize(_warehouseContext);

        // Assert — 30 registros de stock creados
        var stocks = _warehouseContext.Stocks.IgnoreQueryFilters().ToList();
        stocks.Should().HaveCount(30);
        stocks.Should().AllSatisfy(s =>
        {
            s.CantidadActual.Should().Be(50m);
            s.StockMinimo.Should().Be(10m);
            s.TenantId.Should().Be(WarehouseInventoryService.Data.SeedData.DemoTenantId);
            s.SucursalId.Should().Be(WarehouseInventoryService.Data.SeedData.DemoSucursalId);
        });

        // Los ProductoId coinciden con los del catálogo
        var productoIdsSeed = WarehouseInventoryService.Data.SeedData.ProductoIds;
        stocks.Select(s => s.ProductoId).Should().BeEquivalentTo(productoIdsSeed);
    }

    [Fact]
    public void WarehouseSeedData_EjecutarDosVeces_EsIdempotente()
    {
        // Act
        WarehouseInventoryService.Data.SeedData.Initialize(_warehouseContext);
        WarehouseInventoryService.Data.SeedData.Initialize(_warehouseContext);

        // Assert
        _warehouseContext.Stocks.IgnoreQueryFilters().Count().Should().Be(30);
    }

    public void Dispose()
    {
        _catalogContext.Dispose();
        _warehouseContext.Dispose();
    }
}

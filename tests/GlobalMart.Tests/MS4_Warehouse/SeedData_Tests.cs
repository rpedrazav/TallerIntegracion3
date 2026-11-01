using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GlobalMart.Tests.MS4_Warehouse;

/// <summary>
/// Tests unitarios para TI3-256 / TI3-257 / TI3-258:
/// - TI3-256: Script de seed con 30 productos variados.
/// - TI3-257: Cantidad aleatoria 10-100, stock_minimo = 5.
/// - TI3-258: Endpoint POST /seed (verificación de idempotencia).
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

    /// <summary>
    /// TI3-257: Verifica que el seed crea 30 registros con cantidades entre 10-100
    /// y stock_minimo = 5 para todos los productos.
    /// </summary>
    [Fact]
    public void WarehouseSeedData_InsertaStockParaLos30Productos_ConCantidadAleatoriaYStockMinimo5()
    {
        // Act
        WarehouseInventoryService.Data.SeedData.Initialize(_warehouseContext);

        // Assert — 30 registros de stock creados
        var stocks = _warehouseContext.Stocks.IgnoreQueryFilters().ToList();
        stocks.Should().HaveCount(30);
        stocks.Should().AllSatisfy(s =>
        {
            // TI3-257: Cada stock debe tener cantidad entre 10 y 100
            s.CantidadActual.Should().BeGreaterThanOrEqualTo(10m,
                "la cantidad inicial aleatoria debe ser >= 10");
            s.CantidadActual.Should().BeLessThanOrEqualTo(100m,
                "la cantidad inicial aleatoria debe ser <= 100");

            // TI3-257: stock_minimo = 5 para todos
            s.StockMinimo.Should().Be(5m,
                "el stock mínimo debe ser 5 según TI3-257");

            s.TenantId.Should().Be(WarehouseInventoryService.Data.SeedData.DemoTenantId);
            s.SucursalId.Should().Be(WarehouseInventoryService.Data.SeedData.DemoSucursalId);
        });

        // Los ProductoId coinciden con los del catálogo
        var productoIdsSeed = WarehouseInventoryService.Data.SeedData.ProductoIds;
        stocks.Select(s => s.ProductoId).Should().BeEquivalentTo(productoIdsSeed);
    }

    /// <summary>
    /// TI3-257: Verifica que la semilla fija (42) produce cantidades variadas
    /// (no todas iguales) entre los 30 productos.
    /// </summary>
    [Fact]
    public void WarehouseSeedData_CantidadesSonVariadas_NoTodasIguales()
    {
        // Act
        WarehouseInventoryService.Data.SeedData.Initialize(_warehouseContext);

        // Assert — Al menos 5 cantidades distintas entre los 30 productos
        var stocks = _warehouseContext.Stocks.IgnoreQueryFilters().ToList();
        var cantidadesDistintas = stocks.Select(s => s.CantidadActual).Distinct().Count();
        cantidadesDistintas.Should().BeGreaterThanOrEqualTo(5,
            "con Random(42) y 30 productos, debe haber variedad en las cantidades");
    }

    /// <summary>
    /// TI3-257: Verifica que el seed es reproducible (misma semilla = mismas cantidades).
    /// </summary>
    [Fact]
    public void WarehouseSeedData_EsReproducible_MismaSemillaMismoResultado()
    {
        // Act — Ejecutar en un contexto
        WarehouseInventoryService.Data.SeedData.Initialize(_warehouseContext);
        var stocks1 = _warehouseContext.Stocks.IgnoreQueryFilters()
            .OrderBy(s => s.ProductoId)
            .Select(s => s.CantidadActual)
            .ToList();

        // Act — Ejecutar en otro contexto con la misma DB en blanco
        var options2 = new DbContextOptionsBuilder<WarehouseInventoryService.Data.WarehouseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var ctx2 = new WarehouseInventoryService.Data.WarehouseDbContext(options2);
        WarehouseInventoryService.Data.SeedData.Initialize(ctx2);
        var stocks2 = ctx2.Stocks.IgnoreQueryFilters()
            .OrderBy(s => s.ProductoId)
            .Select(s => s.CantidadActual)
            .ToList();

        // Assert — Deben ser idénticas
        stocks1.Should().BeEquivalentTo(stocks2,
            "la semilla fija 42 debe producir las mismas cantidades en cualquier ejecución");
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

    /// <summary>
    /// TI3-258: Verifica que las constantes del seed están correctamente definidas.
    /// </summary>
    [Fact]
    public void SeedData_ConstantesConfiguradasCorrectamente()
    {
        // Assert
        WarehouseInventoryService.Data.SeedData.RandomSeed.Should().Be(42);
        WarehouseInventoryService.Data.SeedData.StockMinimoDefault.Should().Be(5m);
        WarehouseInventoryService.Data.SeedData.ProductoIds.Should().HaveCount(30);
        WarehouseInventoryService.Data.SeedData.DemoTenantId.Should().NotBeEmpty();
        WarehouseInventoryService.Data.SeedData.DemoSucursalId.Should().NotBeEmpty();
    }

    public void Dispose()
    {
        _catalogContext.Dispose();
        _warehouseContext.Dispose();
    }
}


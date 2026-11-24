using Microsoft.EntityFrameworkCore;
using WarehouseInventoryService.Models;

namespace WarehouseInventoryService.Data;

/// <summary>
/// TI3-256 / TI3-257: Seed inicial de stock en MS-4 para los 30 productos variados
/// del catálogo (frutas, carnes, lácteos, snacks).
/// TI3-257: Cada producto recibe una cantidad aleatoria entre 10 y 100 unidades,
/// stock_minimo = 5. Se usa Random con semilla fija (42) para reproducibilidad.
/// </summary>
public static class SeedData
{
    public static readonly Guid DemoTenantId   = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    public static readonly Guid DemoSucursalId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");

    /// <summary>
    /// Semilla fija para el generador aleatorio, garantiza reproducibilidad
    /// del seed en distintos entornos de desarrollo.
    /// </summary>
    public const int RandomSeed = 42;

    /// <summary>
    /// Stock mínimo estándar para todos los productos del seed (TI3-257).
    /// </summary>
    public const decimal StockMinimoDefault = 5m;

    public static readonly Guid[] ProductoIds = new[]
    {
        // Frutas y Verduras (8)
        Guid.Parse("20000000-0000-0000-0000-000000000001"), // Manzana Fuji
        Guid.Parse("20000000-0000-0000-0000-000000000002"), // Plátano Seda
        Guid.Parse("20000000-0000-0000-0000-000000000003"), // Naranja Valencia
        Guid.Parse("20000000-0000-0000-0000-000000000004"), // Limón Sutil
        Guid.Parse("20000000-0000-0000-0000-000000000005"), // Tomate Larga Vida
        Guid.Parse("20000000-0000-0000-0000-000000000006"), // Palta Hass
        Guid.Parse("20000000-0000-0000-0000-000000000007"), // Lechuga Costina
        Guid.Parse("20000000-0000-0000-0000-000000000008"), // Zanahoria

        // Carnes y Cecinas (7)
        Guid.Parse("20000000-0000-0000-0000-000000000009"), // Pechuga de Pollo
        Guid.Parse("20000000-0000-0000-0000-000000000010"), // Carne Molida Vacuno
        Guid.Parse("20000000-0000-0000-0000-000000000011"), // Lomo Vetado
        Guid.Parse("20000000-0000-0000-0000-000000000012"), // Chuleta Centro Cerdo
        Guid.Parse("20000000-0000-0000-0000-000000000013"), // Vienesas Tradicionales
        Guid.Parse("20000000-0000-0000-0000-000000000014"), // Jamón Pierna Acaramelado
        Guid.Parse("20000000-0000-0000-0000-000000000015"), // Salchichón Cervecero

        // Lácteos y Huevos (8)
        Guid.Parse("20000000-0000-0000-0000-000000000016"), // Leche Entera
        Guid.Parse("20000000-0000-0000-0000-000000000017"), // Leche Descremada
        Guid.Parse("20000000-0000-0000-0000-000000000018"), // Yogur Batido
        Guid.Parse("20000000-0000-0000-0000-000000000019"), // Queso Gauda
        Guid.Parse("20000000-0000-0000-0000-000000000020"), // Queso Chanco
        Guid.Parse("20000000-0000-0000-0000-000000000021"), // Mantequilla con Sal
        Guid.Parse("20000000-0000-0000-0000-000000000022"), // Crema de Leche
        Guid.Parse("20000000-0000-0000-0000-000000000023"), // Huevos Blancos

        // Snacks y Dulces (7)
        Guid.Parse("20000000-0000-0000-0000-000000000024"), // Papas Fritas
        Guid.Parse("20000000-0000-0000-0000-000000000025"), // Ramitas de Queso
        Guid.Parse("20000000-0000-0000-0000-000000000026"), // Maní Tostado
        Guid.Parse("20000000-0000-0000-0000-000000000027"), // Galletas Chocochips
        Guid.Parse("20000000-0000-0000-0000-000000000028"), // Barra Chocolate
        Guid.Parse("20000000-0000-0000-0000-000000000029"), // Barra de Cereal
        Guid.Parse("20000000-0000-0000-0000-000000000030")  // Gomitas Frutales
    };

    /// <summary>
    /// Inicializa el seed de stock para los 30 productos del catálogo.
    /// Es idempotente: si ya existen registros de stock del tenant demo, no hace nada.
    /// TI3-257: Cantidad aleatoria entre 10 y 100 unidades, stock_minimo = 5.
    /// </summary>
    public static void Initialize(WarehouseDbContext context)
    {
        if (context.Stocks.IgnoreQueryFilters().Any(s => s.TenantId == DemoTenantId))
        {
            return;
        }

        // Semilla fija para que el seed sea reproducible en cualquier entorno
        var random = new Random(RandomSeed);

        foreach (var productoId in ProductoIds)
        {
            // TI3-257: cantidad aleatoria entre 10 y 100 (incluyendo ambos extremos)
            var cantidadInicial = random.Next(10, 101);

            context.Stocks.Add(new Stock
            {
                ProductoId = productoId,
                SucursalId = DemoSucursalId,
                TenantId = DemoTenantId,
                CantidadActual = cantidadInicial,
                StockMinimo = StockMinimoDefault
            });
        }

        context.SaveChanges();
    }
}

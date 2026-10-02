using Microsoft.EntityFrameworkCore;
using WarehouseInventoryService.Models;

namespace WarehouseInventoryService.Data;

/// <summary>
/// TI3-256: Seed inicial de stock en MS-4 para los 30 productos variados
/// del catálogo (frutas, carnes, lácteos, snacks).
/// </summary>
public static class SeedData
{
    public static readonly Guid DemoTenantId   = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    public static readonly Guid DemoSucursalId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");

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

    public static void Initialize(WarehouseDbContext context)
    {
        if (context.Stocks.IgnoreQueryFilters().Any(s => s.TenantId == DemoTenantId))
        {
            return;
        }

        foreach (var productoId in ProductoIds)
        {
            context.Stocks.Add(new Stock
            {
                ProductoId = productoId,
                SucursalId = DemoSucursalId,
                TenantId = DemoTenantId,
                CantidadActual = 50m,
                StockMinimo = 10m
            });
        }

        context.SaveChanges();
    }
}

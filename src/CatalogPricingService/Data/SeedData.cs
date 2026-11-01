using Microsoft.EntityFrameworkCore;
using CatalogPricingService.Models;

namespace CatalogPricingService.Data;

/// <summary>
/// TI3-256: Script de seed de desarrollo con 30 productos variados
/// (frutas, carnes, lácteos, snacks) con códigos de barras, precios y categorías.
/// </summary>
public static class SeedData
{
    public static readonly Guid DemoTenantId   = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    public static readonly Guid DemoSucursalId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");

    // Unidades de medida (UOM)
    public static readonly Guid UomUnidad = Guid.Parse("cccccccc-0000-0000-0000-000000000001");
    public static readonly Guid UomKilo   = Guid.Parse("cccccccc-0000-0000-0000-000000000002");

    // Categorías fijas
    public static readonly Guid CatFrutasVerduras = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid CatCarnes         = Guid.Parse("10000000-0000-0000-0000-000000000002");
    public static readonly Guid CatLacteos        = Guid.Parse("10000000-0000-0000-0000-000000000003");
    public static readonly Guid CatSnacks         = Guid.Parse("10000000-0000-0000-0000-000000000004");

    public static void Initialize(CatalogDbContext context)
    {
        // Evita duplicados verificando si ya existen productos para el tenant demo
        if (context.Productos.IgnoreQueryFilters().Any(p => p.TenantId == DemoTenantId))
        {
            return;
        }

        // 1. Categorías
        var categorias = new List<Categoria>
        {
            new() { Id = CatFrutasVerduras, TenantId = DemoTenantId, Nombre = "Frutas y Verduras", Level = 0, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = CatCarnes,         TenantId = DemoTenantId, Nombre = "Carnes y Cecinas",  Level = 0, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = CatLacteos,        TenantId = DemoTenantId, Nombre = "Lácteos y Huevos",  Level = 0, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = CatSnacks,         TenantId = DemoTenantId, Nombre = "Snacks y Dulces",   Level = 0, IsActive = true, CreatedAt = DateTime.UtcNow },
        };

        foreach (var cat in categorias)
        {
            if (!context.Categorias.IgnoreQueryFilters().Any(c => c.Id == cat.Id))
            {
                context.Categorias.Add(cat);
            }
        }
        context.SaveChanges();

        // 2. 30 Productos variados
        var productos = new List<Producto>
        {
            // ── Frutas y Verduras (8) ──────────────────────────────────────
            new() {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                TenantId = DemoTenantId,
                Nombre = "Manzana Fuji Granel",
                Descripcion = "Manzana Fuji crujiente y dulce",
                CodigoBarras = "780100000001",
                CategoriaId = CatFrutasVerduras,
                UomBaseId = UomKilo,
                PrecioBase = 1690m,
                EsPesoVariable = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000002"),
                TenantId = DemoTenantId,
                Nombre = "Plátano Seda Granel",
                Descripcion = "Plátano importado primera selección",
                CodigoBarras = "780100000002",
                CategoriaId = CatFrutasVerduras,
                UomBaseId = UomKilo,
                PrecioBase = 1290m,
                EsPesoVariable = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000003"),
                TenantId = DemoTenantId,
                Nombre = "Naranja Valencia Malla 1kg",
                Descripcion = "Naranja jugosa ideal para jugo",
                CodigoBarras = "780100000003",
                CategoriaId = CatFrutasVerduras,
                UomBaseId = UomUnidad,
                PrecioBase = 1890m,
                EsPesoVariable = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000004"),
                TenantId = DemoTenantId,
                Nombre = "Limón Sutil Malla 500g",
                Descripcion = "Limón fresco de exportación",
                CodigoBarras = "780100000004",
                CategoriaId = CatFrutasVerduras,
                UomBaseId = UomUnidad,
                PrecioBase = 1490m,
                EsPesoVariable = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000005"),
                TenantId = DemoTenantId,
                Nombre = "Tomate Larga Vida Granel",
                Descripcion = "Tomate rojo firme y sabroso",
                CodigoBarras = "780100000005",
                CategoriaId = CatFrutasVerduras,
                UomBaseId = UomKilo,
                PrecioBase = 1990m,
                EsPesoVariable = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000006"),
                TenantId = DemoTenantId,
                Nombre = "Palta Hass Granel",
                Descripcion = "Palta Hass chilena maduración media",
                CodigoBarras = "780100000006",
                CategoriaId = CatFrutasVerduras,
                UomBaseId = UomKilo,
                PrecioBase = 4990m,
                EsPesoVariable = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000007"),
                TenantId = DemoTenantId,
                Nombre = "Lechuga Costina Hidropónica",
                Descripcion = "Lechuga costina fresca en bolsa",
                CodigoBarras = "780100000007",
                CategoriaId = CatFrutasVerduras,
                UomBaseId = UomUnidad,
                PrecioBase = 1190m,
                EsPesoVariable = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000008"),
                TenantId = DemoTenantId,
                Nombre = "Zanahoria Bolsa 1kg",
                Descripcion = "Zanahoria seleccionada limpia",
                CodigoBarras = "780100000008",
                CategoriaId = CatFrutasVerduras,
                UomBaseId = UomUnidad,
                PrecioBase = 1090m,
                EsPesoVariable = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },

            // ── Carnes y Cecinas (7) ───────────────────────────────────────
            new() {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000009"),
                TenantId = DemoTenantId,
                Nombre = "Pechuga de Pollo Deshuesada 700g",
                Descripcion = "Pechuga de pollo fresca en bandeja",
                CodigoBarras = "780100000009",
                CategoriaId = CatCarnes,
                UomBaseId = UomUnidad,
                PrecioBase = 4490m,
                EsPesoVariable = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000010"),
                TenantId = DemoTenantId,
                Nombre = "Carne Molida Vacuno 4% Grasa 500g",
                Descripcion = "Carne molida magra sellada al vacío",
                CodigoBarras = "780100000010",
                CategoriaId = CatCarnes,
                UomBaseId = UomUnidad,
                PrecioBase = 4990m,
                EsPesoVariable = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000011"),
                TenantId = DemoTenantId,
                Nombre = "Lomo Vetado Vacuno Granel",
                Descripcion = "Corte parrillero premium nacional",
                CodigoBarras = "780100000011",
                CategoriaId = CatCarnes,
                UomBaseId = UomKilo,
                PrecioBase = 12990m,
                EsPesoVariable = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000012"),
                TenantId = DemoTenantId,
                Nombre = "Chuleta Centro Cerdo 800g",
                Descripcion = "Chuleta de cerdo fresca marinada suave",
                CodigoBarras = "780100000012",
                CategoriaId = CatCarnes,
                UomBaseId = UomUnidad,
                PrecioBase = 3890m,
                EsPesoVariable = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000013"),
                TenantId = DemoTenantId,
                Nombre = "Vienesas Tradicionales 500g 10un",
                Descripcion = "Vienesas de cerdo y pollo selección",
                CodigoBarras = "780100000013",
                CategoriaId = CatCarnes,
                UomBaseId = UomUnidad,
                PrecioBase = 2290m,
                EsPesoVariable = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000014"),
                TenantId = DemoTenantId,
                Nombre = "Jamón Pierna Acaramelado Laminado 200g",
                Descripcion = "Jamón pierna acaramelado fiambrería",
                CodigoBarras = "780100000014",
                CategoriaId = CatCarnes,
                UomBaseId = UomUnidad,
                PrecioBase = 2490m,
                EsPesoVariable = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000015"),
                TenantId = DemoTenantId,
                Nombre = "Salchichón Cervecero Laminado 150g",
                Descripcion = "Salchichón ahumado con especias",
                CodigoBarras = "780100000015",
                CategoriaId = CatCarnes,
                UomBaseId = UomUnidad,
                PrecioBase = 1890m,
                EsPesoVariable = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },

            // ── Lácteos y Huevos (8) ───────────────────────────────────────
            new() {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000016"),
                TenantId = DemoTenantId,
                Nombre = "Leche Entera Tetra 1L",
                Descripcion = "Leche natural entera ultra pasteurizada",
                CodigoBarras = "780100000016",
                CategoriaId = CatLacteos,
                UomBaseId = UomUnidad,
                PrecioBase = 1090m,
                EsPesoVariable = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000017"),
                TenantId = DemoTenantId,
                Nombre = "Leche Descremada Tetra 1L",
                Descripcion = "Leche natural descremada 0% grasa",
                CodigoBarras = "780100000017",
                CategoriaId = CatLacteos,
                UomBaseId = UomUnidad,
                PrecioBase = 1150m,
                EsPesoVariable = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000018"),
                TenantId = DemoTenantId,
                Nombre = "Yogur Batido Frutilla 120g",
                Descripcion = "Yogur sabor frutilla con probióticos",
                CodigoBarras = "780100000018",
                CategoriaId = CatLacteos,
                UomBaseId = UomUnidad,
                PrecioBase = 450m,
                EsPesoVariable = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000019"),
                TenantId = DemoTenantId,
                Nombre = "Queso Gauda Laminado 250g",
                Descripcion = "Queso gauda laminado tradicional",
                CodigoBarras = "780100000019",
                CategoriaId = CatLacteos,
                UomBaseId = UomUnidad,
                PrecioBase = 2690m,
                EsPesoVariable = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000020"),
                TenantId = DemoTenantId,
                Nombre = "Queso Chanco Trozo 500g",
                Descripcion = "Queso chanco maduro artesanal",
                CodigoBarras = "780100000020",
                CategoriaId = CatLacteos,
                UomBaseId = UomUnidad,
                PrecioBase = 4890m,
                EsPesoVariable = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000021"),
                TenantId = DemoTenantId,
                Nombre = "Mantequilla con Sal Pan 250g",
                Descripcion = "Mantequilla natural con sal de mar",
                CodigoBarras = "780100000021",
                CategoriaId = CatLacteos,
                UomBaseId = UomUnidad,
                PrecioBase = 2890m,
                EsPesoVariable = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000022"),
                TenantId = DemoTenantId,
                Nombre = "Crema de Leche Tetra 200ml",
                Descripcion = "Crema natural espesa para cocinar o batir",
                CodigoBarras = "780100000022",
                CategoriaId = CatLacteos,
                UomBaseId = UomUnidad,
                PrecioBase = 990m,
                EsPesoVariable = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000023"),
                TenantId = DemoTenantId,
                Nombre = "Huevos Blancos Extra Bandeja 12un",
                Descripcion = "Huevos frescos seleccionados extra grandes",
                CodigoBarras = "780100000023",
                CategoriaId = CatLacteos,
                UomBaseId = UomUnidad,
                PrecioBase = 3290m,
                EsPesoVariable = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },

            // ── Snacks y Dulces (7) ────────────────────────────────────────
            new() {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000024"),
                TenantId = DemoTenantId,
                Nombre = "Papas Fritas Corte Americano 220g",
                Descripcion = "Papas fritas crujientes con sal",
                CodigoBarras = "780100000024",
                CategoriaId = CatSnacks,
                UomBaseId = UomUnidad,
                PrecioBase = 1890m,
                EsPesoVariable = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000025"),
                TenantId = DemoTenantId,
                Nombre = "Ramitas de Queso Horneadas 180g",
                Descripcion = "Snack de harina de maíz sabor queso",
                CodigoBarras = "780100000025",
                CategoriaId = CatSnacks,
                UomBaseId = UomUnidad,
                PrecioBase = 1490m,
                EsPesoVariable = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000026"),
                TenantId = DemoTenantId,
                Nombre = "Maní Tostado Salado Bolsa 150g",
                Descripcion = "Maní tostado crujiente con toque de sal",
                CodigoBarras = "780100000026",
                CategoriaId = CatSnacks,
                UomBaseId = UomUnidad,
                PrecioBase = 890m,
                EsPesoVariable = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000027"),
                TenantId = DemoTenantId,
                Nombre = "Galletas Chocochips Paquete 130g",
                Descripcion = "Galletas horneadas con chips de chocolate",
                CodigoBarras = "780100000027",
                CategoriaId = CatSnacks,
                UomBaseId = UomUnidad,
                PrecioBase = 990m,
                EsPesoVariable = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000028"),
                TenantId = DemoTenantId,
                Nombre = "Barra Chocolate con Leche 100g",
                Descripcion = "Chocolate con leche suizo suave",
                CodigoBarras = "780100000028",
                CategoriaId = CatSnacks,
                UomBaseId = UomUnidad,
                PrecioBase = 1590m,
                EsPesoVariable = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000029"),
                TenantId = DemoTenantId,
                Nombre = "Barra de Cereal Avena y Miel 23g",
                Descripcion = "Barra energética con avena integral y miel",
                CodigoBarras = "780100000029",
                CategoriaId = CatSnacks,
                UomBaseId = UomUnidad,
                PrecioBase = 490m,
                EsPesoVariable = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000030"),
                TenantId = DemoTenantId,
                Nombre = "Gomitas Frutales Ositos 100g",
                Descripcion = "Caramelos de goma sabores frutales surtidos",
                CodigoBarras = "780100000030",
                CategoriaId = CatSnacks,
                UomBaseId = UomUnidad,
                PrecioBase = 790m,
                EsPesoVariable = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Productos.AddRange(productos);

        // 3. Precios por sucursal para cada uno de los 30 productos
        foreach (var p in productos)
        {
            context.Precios.Add(new Precio
            {
                ProductoId = p.Id,
                SucursalId = DemoSucursalId,
                PrecioLocal = p.PrecioBase,
                MonedaFx = "CLP",
                PrecioFx = p.PrecioBase,
                TenantId = DemoTenantId
            });
        }

        context.SaveChanges();
    }
}

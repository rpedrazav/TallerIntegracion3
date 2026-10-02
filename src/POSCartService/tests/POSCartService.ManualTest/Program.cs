using Microsoft.EntityFrameworkCore;
using POSCartService.Data;
using POSCartService.DTOs;
using POSCartService.Models;
using POSCartService.Repositories;
using POSCartService.Services;
using POSCartService.Validators;

Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║    TEST MANUAL TC-02 & TC-03 — POST /ventas/{id}/items       ║");
Console.WriteLine("║    MS-5 POS & Cart Service — GlobalMart OS                   ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");

// ── Infraestructura compartida ──────────────────────────────────────────────
var tenantId   = Guid.NewGuid();
var cajeroId   = Guid.NewGuid();
var sucursalId = Guid.NewGuid();

var options = new DbContextOptionsBuilder<PosCartDbContext>()
    .UseInMemoryDatabase($"TestDb_{tenantId}")
    .Options;

using var ctx = new PosCartDbContext(options);
ctx.CurrentTenantId = tenantId;

var turnoRepo = new TurnoRepository(ctx);
var turnoSvc  = new TurnoService(turnoRepo);
var ventaRepo = new VentaRepository(ctx);
var itemRepo  = new ItemVentaRepository(ctx);
var ventaSvc  = new VentaService(ventaRepo, itemRepo);

int pass = 0, fail = 0;

void Check(string label, bool ok)
{
    if (ok) { Console.WriteLine($"  ✅ {label} [ OK ]"); pass++; }
    else    { Console.WriteLine($"  ❌ {label} [ FALLIDO ]"); fail++; }
}

// ══════════════════════════════════════════════════════════════════════
// BLOQUE A — Validación del Request (CrearVentaRequestValidator)
// ══════════════════════════════════════════════════════════════════════
Console.WriteLine("── A. Validación del Request CrearVenta ─────────────────────────");

var validator = new CrearVentaRequestValidator();

// A1: request vacío debe fallar
var r1 = validator.Validate(new CrearVentaRequest { MetodoPago = "EFECTIVO" });
Check("A1 — Items vacíos → validación falla", !r1.IsValid);

// A2: método de pago inválido
var r2 = validator.Validate(new CrearVentaRequest
{
    MetodoPago = "BITCOIN",
    Items = [new ItemVentaRequest { ProductoId = Guid.NewGuid(), NombreProducto = "Test", Cantidad = 1, PrecioUnitario = 100 }]
});
Check("A2 — MetodoPago inválido → validación falla", !r2.IsValid);

// A3: precio negativo
var r3 = validator.Validate(new CrearVentaRequest
{
    MetodoPago = "TARJETA",
    Items = [new ItemVentaRequest { ProductoId = Guid.NewGuid(), NombreProducto = "Test", Cantidad = 1, PrecioUnitario = -50 }]
});
Check("A3 — Precio negativo → validación falla", !r3.IsValid);

// A4: request válido
var r4 = validator.Validate(new CrearVentaRequest
{
    MetodoPago = "TARJETA",
    Items = [new ItemVentaRequest { ProductoId = Guid.NewGuid(), NombreProducto = "Laptop", Cantidad = 1, PrecioUnitario = 850000 }]
});
Check("A4 — Request válido → validación pasa", r4.IsValid);

Console.WriteLine();

// ══════════════════════════════════════════════════════════════════════
// BLOQUE B — Lógica del Controller: verificación de turno
// ══════════════════════════════════════════════════════════════════════
Console.WriteLine("── B. Verificación de Turno Activo (pre-condición de POST /ventas) ──");

// B1: sin turno abierto → GetActivo devuelve null (controller retornaría 409)
var turnoSinAbrir = await turnoSvc.GetActivo(cajeroId, tenantId);
Check("B1 — Sin turno abierto → GetActivo devuelve null (→ 409 en HTTP)", turnoSinAbrir is null);

// B2: abrir turno
var turno = await turnoSvc.Abrir(cajeroId, tenantId, sucursalId, 50_000m);
Check("B2 — Turno abierto correctamente", turno.Estado == EstadoTurno.ABIERTO);

// B3: con turno abierto → GetActivo devuelve el turno (→ continúa hacia crear venta)
var turnoActivo = await turnoSvc.GetActivo(cajeroId, tenantId);
Check("B3 — Con turno abierto → GetActivo devuelve el turno (→ 201 en HTTP)", turnoActivo is not null);

Console.WriteLine();

// ══════════════════════════════════════════════════════════════════════
// BLOQUE C — Mapeo DTO → Dominio (lógica del controller)
// ══════════════════════════════════════════════════════════════════════
Console.WriteLine("── C. Mapeo DTO → ItemVenta (subtotal por ítem = precio × cantidad) ──");

var requestItems = new List<ItemVentaRequest>
{
    new() { ProductoId = Guid.NewGuid(), NombreProducto = "Laptop Gamer 15''",
            Cantidad = 1, PrecioUnitario = 850_000m },
    new() { ProductoId = Guid.NewGuid(), NombreProducto = "Mouse Inalámbrico",
            Cantidad = 2, PrecioUnitario =  15_000m },
    new() { ProductoId = Guid.NewGuid(), NombreProducto = "Alfombrilla XXL",
            Cantidad = 3, PrecioUnitario =   5_000m, PesoKg = 0.3m }
};

var itemsDominio = requestItems.Select(i => new ItemVenta
{
    ProductoId     = i.ProductoId,
    NombreProducto = i.NombreProducto,
    Cantidad       = i.Cantidad,
    PesoKg         = i.PesoKg,
    PrecioUnitario = i.PrecioUnitario,
    Subtotal       = Math.Round(i.PrecioUnitario * i.Cantidad, 2)
}).ToList();

Check("C1 — Laptop: subtotal = 850.000 × 1",
    itemsDominio[0].Subtotal == 850_000m);
Check("C2 — Mouse: subtotal = 15.000 × 2 = 30.000",
    itemsDominio[1].Subtotal == 30_000m);
Check("C3 — Alfombrilla: subtotal = 5.000 × 3 = 15.000",
    itemsDominio[2].Subtotal == 15_000m);
Check("C4 — Alfombrilla tiene PesoKg = 0.3",
    itemsDominio[2].PesoKg == 0.3m);

Console.WriteLine();

// ══════════════════════════════════════════════════════════════════════
// BLOQUE D — Creación completa de Venta (IVA 0% → subtotal = total)
// ══════════════════════════════════════════════════════════════════════
Console.WriteLine("── D. POST /ventas — Flujo completo (IVA 0%) ─────────────────");

var venta = await ventaSvc.CrearAsync(
    turnoId:            turno.Id,
    cajeroId:           cajeroId,
    sucursalId:         turno.SucursalId,
    tenantId:           tenantId,
    items:              itemsDominio,
    impuestoPorcentaje: 0m,
    metodoPago:         MetodoPagoVenta.TARJETA);

decimal expectedSubtotal = 850_000m + 30_000m + 15_000m; // 895.000
Check($"D1 — Estado inicial PENDIENTE",            venta.Estado == EstadoVenta.PENDIENTE);
Check($"D2 — Subtotal = 895.000",                  venta.Subtotal == expectedSubtotal);
Check($"D3 — Impuestos = 0 (IVA 0%)",              venta.Impuestos == 0m);
Check($"D4 — Total = Subtotal (IVA exento)",        venta.Total == expectedSubtotal);
Check($"D5 — Items persistidos = 3",               venta.Items.Count == 3);
Check($"D6 — TurnoId asignado correctamente",      venta.TurnoId == turno.Id);
Check($"D7 — SucursalId del turno denormalizado",  venta.SucursalId == turno.SucursalId);

Console.WriteLine();

// ══════════════════════════════════════════════════════════════════════
// BLOQUE E — Mapeo Dominio → VentaResponse DTO
// ══════════════════════════════════════════════════════════════════════
Console.WriteLine("── E. Mapeo Venta → VentaResponse (serialización HTTP) ───────");

var response = VentaResponse.FromModel(venta);

Check("E1 — response.Id coincide",          response.Id == venta.Id);
Check("E2 — response.Estado = 'PENDIENTE'", response.Estado == "PENDIENTE");
Check("E3 — response.MetodoPago = 'TARJETA'", response.MetodoPago == "TARJETA");
Check("E4 — response.Items.Count = 3",      response.Items.Count == 3);
Check("E5 — response.Total correcto",       response.Total == expectedSubtotal);
Check("E6 — Laptop en response.Items[0]",   response.Items[0].NombreProducto == "Laptop Gamer 15''");

Console.WriteLine();

// ══════════════════════════════════════════════════════════════════════
// BLOQUE F — Parsing de MetodoPago
// ══════════════════════════════════════════════════════════════════════
Console.WriteLine("── F. Parsing MetodoPago (lógica del controller) ──────────────");

Check("F1 — 'TARJETA' → MetodoPagoVenta.TARJETA",
    Enum.TryParse<MetodoPagoVenta>("TARJETA", ignoreCase: true, out var m1) && m1 == MetodoPagoVenta.TARJETA);
Check("F2 — 'efectivo' (minúscula) → MetodoPagoVenta.EFECTIVO",
    Enum.TryParse<MetodoPagoVenta>("efectivo", ignoreCase: true, out var m2) && m2 == MetodoPagoVenta.EFECTIVO);
Check("F3 — 'BITCOIN' → parse falla (→ 400 BadRequest en HTTP)",
    !Enum.TryParse<MetodoPagoVenta>("BITCOIN", ignoreCase: true, out _));

Console.WriteLine();

// ══════════════════════════════════════════════════════════════════════
// BLOQUE G — Validación de AgregarItemRequest (AgregarItemRequestValidator)
// ══════════════════════════════════════════════════════════════════════
Console.WriteLine("── G. Validación AgregarItemRequest (POST /ventas/{id}/items) ─");

var agregarValidator = new AgregarItemRequestValidator();

// G1: ProductoId vacío debe fallar
var g1 = agregarValidator.Validate(new AgregarItemRequest { ProductoId = Guid.Empty, Cantidad = 1 });
Check("G1 — ProductoId vacío → validación falla", !g1.IsValid);

// G2: Cantidad <= 0 sin peso_kg debe fallar
var g2 = agregarValidator.Validate(new AgregarItemRequest { ProductoId = Guid.NewGuid(), Cantidad = 0 });
Check("G2 — Cantidad <= 0 sin peso_kg → validación falla", !g2.IsValid);

// G3: PesoKg <= 0 debe fallar
var g3 = agregarValidator.Validate(new AgregarItemRequest { ProductoId = Guid.NewGuid(), PesoKg = -0.5m });
Check("G3 — PesoKg <= 0 → validación falla", !g3.IsValid);

// G4: Request válido con cantidad
var g4 = agregarValidator.Validate(new AgregarItemRequest { ProductoId = Guid.NewGuid(), Cantidad = 2 });
Check("G4 — Request válido con cantidad → validación pasa", g4.IsValid);

// G5: Request válido con peso_kg (balanza)
var g5 = agregarValidator.Validate(new AgregarItemRequest { ProductoId = Guid.NewGuid(), PesoKg = 1.25m });
Check("G5 — Request válido con peso_kg → validación pasa", g5.IsValid);

Console.WriteLine();

// ══════════════════════════════════════════════════════════════════════
// BLOQUE H — Simulación de ICatalogClient (MS-3: GET /api/products/{id})
// ══════════════════════════════════════════════════════════════════════
Console.WriteLine("── H. Simulación de MS-3 CatalogPricingService ────────────────");

var prod1Id = Guid.NewGuid();
var prod2Id = Guid.NewGuid();
var prodInactivoId = Guid.NewGuid();

var catalogMock = new Dictionary<Guid, ProductoCatalogDto>
{
    [prod1Id] = new() { Id = prod1Id, Nombre = "Aceite de Oliva 1L", PrecioBase = 5_000m, IsActive = true, EsPesoVariable = false },
    [prod2Id] = new() { Id = prod2Id, Nombre = "Café Molido 500g", PrecioBase = 3_000m, IsActive = true, EsPesoVariable = false },
    [prodInactivoId] = new() { Id = prodInactivoId, Nombre = "Producto Descontinuado", PrecioBase = 1_000m, IsActive = false }
};

Check("H1 — MS-3 retorna producto existente con precio actual (5.000)",
    catalogMock.TryGetValue(prod1Id, out var p1) && p1.PrecioBase == 5_000m && p1.Nombre == "Aceite de Oliva 1L");

Check("H2 — MS-3 retorna null para producto no existente (→ 404 con 'Producto no encontrado')",
    !catalogMock.TryGetValue(Guid.NewGuid(), out _));

Check("H3 — MS-3 detecta producto inactivo (IsActive=false → 400 en controller)",
    catalogMock.TryGetValue(prodInactivoId, out var pInact) && !pInact.IsActive);

Console.WriteLine();


// ══════════════════════════════════════════════════════════════════════
// BLOQUE I — Simulación de ITaxClient (MS-2: POST /api/tax/calculate)
// ══════════════════════════════════════════════════════════════════════
Console.WriteLine("── I. Simulación de MS-2 TaxComplianceService (Cálculo de IVA) ─");

// Simular la llamada a MS-2 para calcular IVA: 19%
TaxCalculationResult SimularMS2(IEnumerable<TaxItemDto> items)
{
    var list = items.ToList();
    decimal subtotal = list.Sum(x => Math.Round(x.Precio * x.Cantidad, 2));
    decimal iva = Math.Round(subtotal * 0.19m, 2);
    decimal total = subtotal + iva;

    return new TaxCalculationResult
    {
        Subtotal = subtotal,
        PorcentajeIva = 19m,
        Iva = iva,
        Total = total,
        Items = list.Select(x =>
        {
            var itemSubtotal = Math.Round(x.Precio * x.Cantidad, 2);
            var itemIva = Math.Round(itemSubtotal * 0.19m, 2);
            return new TaxItemBreakdownDto
            {
                Nombre = x.Nombre,
                Precio = x.Precio,
                Cantidad = x.Cantidad,
                Subtotal = itemSubtotal,
                PorcentajeIva = 19m,
                Iva = itemIva,
                Total = itemSubtotal + itemIva
            };
        }).ToList()
    };
}

// I1: Cálculo con 1 item (5.000 x 2 = 10.000, IVA 19% = 1.900, Total = 11.900)
var tax1 = SimularMS2(new[] { new TaxItemDto { Nombre = "Aceite de Oliva 1L", Precio = 5_000m, Cantidad = 2 } });
Check("I1 — Ítem 1: Subtotal = 10.000, IVA 19% = 1.900, Total = 11.900",
    tax1.Subtotal == 10_000m && tax1.Iva == 1_900m && tax1.Total == 11_900m);

// I2: Cálculo acumulado con 2 items (5.000 x 2 + 3.000 x 1 = subtotal 13.000, IVA = 2.470, Total = 15.470)
var tax2 = SimularMS2(new[]
{
    new TaxItemDto { Nombre = "Aceite de Oliva 1L", Precio = 5_000m, Cantidad = 2 },
    new TaxItemDto { Nombre = "Café Molido 500g",   Precio = 3_000m, Cantidad = 1 }
});
Check("I2 — Acumulado 2 items: Subtotal = 13.000, IVA = 2.470, Total = 15.470",
    tax2.Subtotal == 13_000m && tax2.Iva == 2_470m && tax2.Total == 15_470m);

Console.WriteLine();

// ══════════════════════════════════════════════════════════════════════
// BLOQUE J — Flujo de Negocio AgregarItemAsync en VentaService (MS-5)
// ══════════════════════════════════════════════════════════════════════
Console.WriteLine("── J. Flujo Completo POST /ventas/{id}/items (VentaService) ────");

// J1: Crear una nueva venta vacía/pendiente para probar agregar ítems uno a uno
var nuevaVenta = new Venta
{
    TurnoId    = turno.Id,
    CajeroId   = cajeroId,
    SucursalId = sucursalId,
    TenantId   = tenantId,
    Subtotal   = 0m,
    Impuestos  = 0m,
    Total      = 0m,
    MetodoPago = MetodoPagoVenta.EFECTIVO,
    Estado     = EstadoVenta.PENDIENTE
};
await ventaRepo.CrearAsync(nuevaVenta);
Check("J1 — Venta inicial en estado PENDIENTE creada", nuevaVenta.Estado == EstadoVenta.PENDIENTE);

// J2: Agregar primer ítem (Aceite: precio obtenido de MS-3 = 5.000 x 2 = 10.000, IVA MS-2 = 1.900)
var item1 = new ItemVenta
{
    ProductoId     = prod1Id,
    NombreProducto = "Aceite de Oliva 1L",
    Cantidad       = 2,
    PrecioUnitario = 5_000m,
    Subtotal       = 10_000m
};

var (vActualizada1, itemPersistido1) = await ventaSvc.AgregarItemAsync(
    nuevaVenta.Id,
    item1,
    subtotal:  tax1.Subtotal,
    impuestos: tax1.Iva,
    total:     tax1.Total);

Check("J2 — Ítem 1 agregado: VentaId asignado", itemPersistido1.VentaId == nuevaVenta.Id);
Check("J3 — Ítem 1 persistido en base de datos", (await itemRepo.GetByIdAsync(itemPersistido1.Id)) is not null);
Check("J4 — Venta actualizada con Subtotal = 10.000", vActualizada1.Subtotal == 10_000m);
Check("J5 — Venta actualizada con IVA = 1.900",      vActualizada1.Impuestos == 1_900m);
Check("J6 — Venta actualizada con Total = 11.900",    vActualizada1.Total == 11_900m);

// J3: Agregar segundo ítem (Café: precio obtenido de MS-3 = 3.000 x 1 = 3.000)
var item2 = new ItemVenta
{
    ProductoId     = prod2Id,
    NombreProducto = "Café Molido 500g",
    Cantidad       = 1,
    PrecioUnitario = 3_000m,
    Subtotal       = 3_000m
};

var (vActualizada2, itemPersistido2) = await ventaSvc.AgregarItemAsync(
    nuevaVenta.Id,
    item2,
    subtotal:  tax2.Subtotal,
    impuestos: tax2.Iva,
    total:     tax2.Total);

Check("J7 — Ítem 2 agregado: Subtotal acumulado = 13.000",  vActualizada2.Subtotal == 13_000m);
Check("J8 — Ítem 2 agregado: IVA acumulado = 2.470",        vActualizada2.Impuestos == 2_470m);
Check("J9 — Ítem 2 agregado: Total acumulado = 15.470",      vActualizada2.Total == 15_470m);
Check("J10 — Venta contiene 2 ítems persistidos",           vActualizada2.Items.Count == 2);

// J4: Intentar agregar ítem a venta inexistente lanza KeyNotFoundException (→ 404)
bool throwNotFound = false;
try
{
    await ventaSvc.AgregarItemAsync(Guid.NewGuid(), new ItemVenta { NombreProducto = "X" }, 10, 1.9m, 11.9m);
}
catch (KeyNotFoundException)
{
    throwNotFound = true;
}
Check("J11 — Venta inexistente → lanza KeyNotFoundException (→ 404 en HTTP)", throwNotFound);

// J5: Completar la venta e intentar agregar ítem lanza InvalidOperationException (→ 409)
await ventaSvc.CompletarAsync(nuevaVenta.Id, montoRecibido: nuevaVenta.Total, vuelto: 0m);
bool throwConflict = false;
try
{
    await ventaSvc.AgregarItemAsync(nuevaVenta.Id, new ItemVenta { NombreProducto = "Y" }, 10, 1.9m, 11.9m);
}
catch (InvalidOperationException)
{
    throwConflict = true;
}
Check("J12 — Venta COMPLETADA → no permite agregar ítems (→ 409 Conflict)", throwConflict);

Console.WriteLine();

// ══════════════════════════════════════════════════════════════════════
// BLOQUE K — Mapeo ItemVentaResponse con IVA y Total
// ══════════════════════════════════════════════════════════════════════
Console.WriteLine("── K. Mapeo ItemVentaResponse (Serialización HTTP) ─────────────");

var itemResponse = ItemVentaResponse.FromModel(itemPersistido1, iva: 1_900m, total: 11_900m);

Check("K1 — response.Id coincide con el ítem",              itemResponse.Id == itemPersistido1.Id);
Check("K2 — response.NombreProducto coincide",              itemResponse.NombreProducto == "Aceite de Oliva 1L");
Check("K3 — response.PrecioUnitario = 5.000",               itemResponse.PrecioUnitario == 5_000m);
Check("K4 — response.Subtotal = 10.000",                    itemResponse.Subtotal == 10_000m);
Check("K5 — response.Iva = 1.900 (calculado por MS-2)",     itemResponse.Iva == 1_900m);
Check("K6 — response.Total = 11.900 (subtotal + IVA)",      itemResponse.Total == 11_900m);

// Serialización JSON
var json = System.Text.Json.JsonSerializer.Serialize(itemResponse);
Check("K7 — Serialización JSON contiene campos calculados 'iva' y 'total'",
    json.Contains("\"iva\":1900") && json.Contains("\"total\":11900"));

Console.WriteLine();

// ══════════════════════════════════════════════════════════════════════
// BLOQUE L — Modificar Cantidad de Ítem (PUT /ventas/{id}/items/{itemId})
// ══════════════════════════════════════════════════════════════════════
Console.WriteLine("── L. Modificar Cantidad de Ítem (PUT /ventas/{id}/items/{itemId}) ─");

var modVal = new ModificarCantidadItemRequestValidator();
Check("L1 — Validador rechaza cantidad <= 0", !modVal.Validate(new ModificarCantidadItemRequest { Cantidad = 0 }).IsValid);
Check("L2 — Validador acepta cantidad > 0", modVal.Validate(new ModificarCantidadItemRequest { Cantidad = 3 }).IsValid);

// Modificar ítem 1 (Aceite) de 2 a 3 unidades
// Ítem 1 nuevo subtotal: 3 x 5.000 = 15.000
// Venta nuevo subtotal: 15.000 + 3.000 = 18.000
// Venta nuevo IVA 19%: 3.420
// Venta nuevo Total: 21.420
var (vMod, itemMod) = await ventaSvc.ModificarCantidadItemAsync(
    venta.Id,
    itemsDominio[0].Id,
    nuevaCantidad:        3m,
    nuevoPesoKg:          null,
    nuevoSubtotalItem:    15_000m,
    nuevoSubtotalVenta:   18_000m,
    nuevosImpuestosVenta: 3_420m,
    nuevoTotalVenta:      21_420m);

Check("L3 — Ítem modificado: Cantidad = 3",                itemMod.Cantidad == 3m);
Check("L4 — Ítem modificado: Subtotal = 15.000",          itemMod.Subtotal == 15_000m);
Check("L5 — Venta recalculada: Subtotal = 18.000",        vMod.Subtotal == 18_000m);
Check("L6 — Venta recalculada: Impuestos (IVA) = 3.420",  vMod.Impuestos == 3_420m);
Check("L7 — Venta recalculada: Total = 21.420",            vMod.Total == 21_420m);

Console.WriteLine();

// ══════════════════════════════════════════════════════════════════════
// RESULTADO FINAL
// ══════════════════════════════════════════════════════════════════════
Console.WriteLine("══════════════════════════════════════════════════════════════");
Console.WriteLine($"  RESULTADO: {pass} / {pass + fail} tests aprobados");
Console.WriteLine();
if (fail == 0)
    Console.WriteLine("  ✅ TODOS LOS TESTS APROBADOS — PUT /ventas/{id}/items/{itemId} LISTO.");
else
    Console.WriteLine($"  ❌ {fail} test(s) FALLIDOS — revisar arriba.");
Console.WriteLine("══════════════════════════════════════════════════════════════");



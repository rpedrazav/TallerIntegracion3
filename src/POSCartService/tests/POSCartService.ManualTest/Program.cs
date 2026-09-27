using Microsoft.EntityFrameworkCore;
using POSCartService.Data;
using POSCartService.DTOs;
using POSCartService.Models;
using POSCartService.Repositories;
using POSCartService.Services;
using POSCartService.Validators;

Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║    TEST MANUAL TC-02 — POST /ventas + Ciclo de Vida          ║");
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
var ventaSvc  = new VentaService(ventaRepo);

int pass = 0, fail = 0;

void Check(string label, bool ok)
{
    if (ok) { Console.WriteLine($"  ✅ {label} [ OK ]"); pass++; }
    else    { Console.WriteLine($"  ❌ {label} [ FALLIDO ]"); fail++; }
}

// ══════════════════════════════════════════════════════════════════════
// BLOQUE A — Validación del Request (CrearVentaRequestValidator)
// ══════════════════════════════════════════════════════════════════════
Console.WriteLine("── A. Validación del Request ──────────────────────────────────");

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

// Replicar el mapeo exacto que hace el controller
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
    impuestoPorcentaje: 0m,       // IVA 0% — sin integrar MS-2 aún
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
// RESULTADO FINAL
// ══════════════════════════════════════════════════════════════════════
Console.WriteLine("══════════════════════════════════════════════════════════════");
Console.WriteLine($"  RESULTADO: {pass} / {pass + fail} tests aprobados");
Console.WriteLine();
if (fail == 0)
    Console.WriteLine("  ✅ TEST MANUAL APROBADO — POST /ventas listo.");
else
    Console.WriteLine($"  ❌ {fail} test(s) FALLIDOS — revisar arriba.");
Console.WriteLine("══════════════════════════════════════════════════════════════");

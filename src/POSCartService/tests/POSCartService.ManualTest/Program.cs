using Microsoft.EntityFrameworkCore;
using POSCartService.Data;
using POSCartService.Models;
using POSCartService.Repositories;
using POSCartService.Services;

Console.WriteLine("==============================================================");
Console.WriteLine("       TEST MANUAL TC-02 — CREACIÓN Y FLUJO DE VENTA          ");
Console.WriteLine("       MS-5 POS & Cart Service — GlobalMart OS                ");
Console.WriteLine("==============================================================\n");

// ── Configurar DB en memoria ────────────────────────────────────────────────
// NOTA: EF Core InMemory + QueryFilters con navegaciones (i.Venta.TenantId)
// tienen limitaciones conocidas al usar múltiples instancias de contexto.
// Usamos un único contexto compartido, igual que lo haría un request HTTP real
// (el DI container entrega el mismo DbContext durante todo un request).
var tenantId  = Guid.NewGuid();
var cajeroId  = Guid.NewGuid();
var sucursalId = Guid.NewGuid();

var options = new DbContextOptionsBuilder<PosCartDbContext>()
    .UseInMemoryDatabase($"TestDb_{tenantId}")   // DB única por ejecución
    .Options;

using var ctx = new PosCartDbContext(options);
ctx.CurrentTenantId = tenantId;

var turnoRepo  = new TurnoRepository(ctx);
var turnoSvc   = new TurnoService(turnoRepo);
var ventaRepo  = new VentaRepository(ctx);
var ventaSvc   = new VentaService(ventaRepo);

// ── [1] Abrir Turno ─────────────────────────────────────────────────────────
Console.WriteLine("[1] Abriendo Turno de Caja...");
var turno = await turnoSvc.Abrir(cajeroId, tenantId, sucursalId, 50000m);
Console.WriteLine($"✅ Turno abierto con ID: {turno.Id} (Fondo: $50,000)\n");

// ── [2] Crear Venta PENDIENTE ───────────────────────────────────────────────
Console.WriteLine("[2] Registrando Venta PENDIENTE...");
var items = new List<ItemVenta>
{
    new() { ProductoId = Guid.NewGuid(), NombreProducto = "Laptop Gamer 15''",
            Cantidad = 1, PrecioUnitario = 850_000m, Subtotal = 850_000m },
    new() { ProductoId = Guid.NewGuid(), NombreProducto = "Mouse Inalámbrico",
            Cantidad = 2, PrecioUnitario =  15_000m, Subtotal =  30_000m }
};

var venta = await ventaSvc.CrearAsync(
    turnoId: turno.Id, cajeroId: cajeroId, sucursalId: sucursalId,
    tenantId: tenantId, items: items,
    impuestoPorcentaje: 19m,
    metodoPago: MetodoPagoVenta.TARJETA);

Console.WriteLine("   Detalle de Venta Registrada:");
Console.WriteLine($"   - ID Venta  : {venta.Id}");
Console.WriteLine($"   - Estado    : {venta.Estado}");
Console.WriteLine($"   - Subtotal  : ${venta.Subtotal:N0}");
Console.WriteLine($"   - Impuestos : ${venta.Impuestos:N0} (19%)");
Console.WriteLine($"   - Total     : ${venta.Total:N0}");
Console.WriteLine($"   - Items     : {venta.Items.Count}\n");

bool subtotalOk  = venta.Subtotal  == 880_000m;
bool impuestosOk = venta.Impuestos == 167_200m;   // 880.000 × 19%
bool totalOk     = venta.Total     == 1_047_200m;

Console.WriteLine("  VERIFICACIÓN DE CÁLCULOS:");
Console.WriteLine($"  ✅ Subtotal  : esperado = 880.000    | obtenido = {venta.Subtotal:N0} [ {(subtotalOk  ? "OK" : "ERROR")} ]");
Console.WriteLine($"  ✅ Impuestos : esperado = 167.200    | obtenido = {venta.Impuestos:N0} [ {(impuestosOk ? "OK" : "ERROR")} ]");
Console.WriteLine($"  ✅ Total     : esperado = 1.047.200  | obtenido = {venta.Total:N0} [ {(totalOk     ? "OK" : "ERROR")} ]\n");

// ── [3] Completar Venta ─────────────────────────────────────────────────────
Console.WriteLine("[3] Completando la Venta...");
var ventaCompletada = await ventaSvc.CompletarAsync(venta.Id);
bool completadaOk = ventaCompletada.Estado == EstadoVenta.COMPLETADA;
Console.WriteLine($"✅ Venta completada. Nuevo estado: {ventaCompletada.Estado} [ {(completadaOk ? "OK" : "ERROR")} ]\n");

// ── [4] Anular Venta ────────────────────────────────────────────────────────
Console.WriteLine("[4] Anulando la Venta...");
var ventaAnulada = await ventaSvc.AnularAsync(
    ventaCompletada.Id, cajeroId,
    "Cliente se arrepintió después del cobro");

bool anuladaOk = ventaAnulada.Estado == EstadoVenta.ANULADA;
bool motivoOk  = ventaAnulada.Anulacion?.Motivo is not null;

Console.WriteLine($"✅ Venta anulada. Nuevo estado: {ventaAnulada.Estado} [ {(anuladaOk ? "OK" : "ERROR")} ]");
Console.WriteLine($"✅ Motivo       : '{ventaAnulada.Anulacion?.Motivo}' [ {(motivoOk ? "OK" : "ERROR")} ]");
Console.WriteLine($"✅ AutorizadoPor: {ventaAnulada.Anulacion?.AutorizadoPor}\n");

// ── [5] Validar que Items se guardaron correctamente ────────────────────────
Console.WriteLine("[5] Verificando Items en repositorio...");
var itemsGuardados = await new ItemVentaRepository(ctx).GetByVentaAsync(venta.Id);
bool itemsOk = itemsGuardados.Count == 2;
Console.WriteLine($"✅ Items guardados : {itemsGuardados.Count} (esperado: 2) [ {(itemsOk ? "OK" : "ERROR")} ]");
foreach (var item in itemsGuardados)
    Console.WriteLine($"   • {item.NombreProducto}: {item.Cantidad}x ${item.PrecioUnitario:N0} = ${item.Subtotal:N0}");

// ── RESULTADO FINAL ─────────────────────────────────────────────────────────
bool passed = subtotalOk && impuestosOk && totalOk && completadaOk && anuladaOk && motivoOk && itemsOk;
Console.WriteLine();
if (passed)
    Console.WriteLine("✅ TEST MANUAL APROBADO — VentaRepository, ItemVentaRepository y VentaService funcionan correctamente.");
else
    Console.WriteLine("❌ TEST MANUAL FALLIDO — Revisa los items marcados como ERROR.");

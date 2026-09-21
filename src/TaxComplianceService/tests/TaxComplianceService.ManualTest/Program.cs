// ============================================================
//  Test Manual TC-01 — MS-2 Tax & Compliance Service
//  Caso de uso: Calcular Impuesto de Venta (caso de uso TC-01)
//  Descripción: Verifica el cálculo correcto de subtotal,
//               IVA 19% (Chile) y total para 2 items.
//
//  HOW TO RUN:
//    cd tests/TaxComplianceService.ManualTest
//    dotnet run
// ============================================================

using TaxComplianceService.Services;

Console.WriteLine();
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║         TEST MANUAL TC-01 — POST /tax/calculate             ║");
Console.WriteLine("║         MS-2 Tax & Compliance Service — GlobalMart OS        ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
Console.ResetColor();
Console.WriteLine();

// ─── Datos de entrada ────────────────────────────────────────────────────────
var items = new List<TaxItem>
{
    new(precio: 5000m, cantidad: 2m, nombre: "Bebida Cola 2L"),
    new(precio: 3000m, cantidad: 1m, nombre: "Pan Molde 600g"),
};
decimal porcentajeIva = 19m; // IVA Chile

Console.WriteLine("  Tenant: Chile (IVA 19%)");
Console.WriteLine($"  Items ingresados: {items.Count}");
Console.WriteLine();

// ─── Ejecutar el servicio real de MS-2 ───────────────────────────────────────
var breakdown = TaxCalculatorService.Calculate(items, porcentajeIva);

// ─── Mostrar tabla de resultados ──────────────────────────────────────────────
Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine($"  {"Producto",-20} {"Precio",10} {"Cantidad",10} {"Subtotal",12} {"IVA",10} {"Total",12}");
Console.WriteLine("  " + new string('─', 76));
Console.ResetColor();

foreach (var item in breakdown.Items)
{
    Console.WriteLine($"  {item.Nombre,-20} {item.Precio,10:N0} {item.Cantidad,10:N0} {item.Subtotal,12:N0} {item.Iva,10:N0} {item.Total,12:N0}");
}

Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("  " + new string('─', 76));
Console.ResetColor();
Console.ForegroundColor = ConsoleColor.White;
Console.WriteLine($"  {"TOTALES",-20} {"",10} {"",10} {breakdown.Subtotal,12:N0} {breakdown.Iva,10:N0} {breakdown.Total,12:N0}");
Console.ResetColor();
Console.WriteLine();

// ─── Valores esperados según la especificación de la tarea ───────────────────
decimal subtotalEsperado = 13_000m;
decimal ivaEsperado      = 2_470m;
decimal totalEsperado    = 15_470m;

bool subtotalOk = breakdown.Subtotal == subtotalEsperado;
bool ivaOk      = breakdown.Iva      == ivaEsperado;
bool totalOk    = breakdown.Total    == totalEsperado;
bool todosOk    = subtotalOk && ivaOk && totalOk;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("  VERIFICACIÓN:");
Console.ResetColor();
PrintCheck("Subtotal",  subtotalEsperado, breakdown.Subtotal, subtotalOk);
PrintCheck("IVA (19%)", ivaEsperado,      breakdown.Iva,      ivaOk);
PrintCheck("Total",     totalEsperado,    breakdown.Total,    totalOk);
Console.WriteLine();

if (todosOk)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("  ✅  TEST MANUAL APROBADO — cálculo correcto según especificación.");
}
else
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("  ❌  TEST MANUAL FALLIDO — revisar lógica de TaxCalculatorService.");
}

Console.ResetColor();
Console.WriteLine();
return todosOk ? 0 : 1;

// ─── Helpers ─────────────────────────────────────────────────────────────────
static void PrintCheck(string campo, decimal esperado, decimal obtenido, bool ok)
{
    Console.Write($"  {(ok ? "✅" : "❌")} {campo,-12}: ");
    Console.Write($"esperado = {esperado,8:N0}  |  obtenido = {obtenido,8:N0}");
    Console.ForegroundColor = ok ? ConsoleColor.Green : ConsoleColor.Red;
    Console.WriteLine($"  [{(ok ? " OK " : "FAIL")}]");
    Console.ResetColor();
}

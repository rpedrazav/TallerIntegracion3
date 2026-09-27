using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSCartService.Controllers;
using POSCartService.Data;
using POSCartService.DTOs;
using POSCartService.Exceptions;
using POSCartService.Models;
using POSCartService.Repositories;
using POSCartService.Services;
using POSCartService.Validators;

namespace POSCartService.AgregarItemTest;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("╔══════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   SUITE DE PRUEBAS DE VERIFICACIÓN: POST /ventas/{id}/items (MS-5)       ║");
        Console.WriteLine("║   Comprueba requisitos funcionales, llamadas a MS-3, MS-2 y errores      ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════╝\n");

        int pass = 0, fail = 0;

        void Check(string id, string descripcion, bool condicion, string? detalleFalla = null)
        {
            if (condicion)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"  [PASS] ");
                Console.ResetColor();
                Console.WriteLine($"{id}: {descripcion}");
                pass++;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($"  [FAIL] ");
                Console.ResetColor();
                Console.WriteLine($"{id}: {descripcion}");
                if (!string.IsNullOrWhiteSpace(detalleFalla))
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"         -> Detalle: {detalleFalla}");
                    Console.ResetColor();
                }
                fail++;
            }
        }

        // ── 1. Base de Datos En Memoria ──────────────────────────────────────────────
        var tenantId   = Guid.NewGuid();
        var cajeroId   = Guid.NewGuid();
        var sucursalId = Guid.NewGuid();

        var dbOptions = new DbContextOptionsBuilder<PosCartDbContext>()
            .UseInMemoryDatabase($"TestAgregarItemDb_{Guid.NewGuid()}")
            .Options;

        using var dbContext = new PosCartDbContext(dbOptions);
        dbContext.CurrentTenantId = tenantId;

        var turnoRepo = new TurnoRepository(dbContext);
        var turnoSvc  = new TurnoService(turnoRepo);
        var ventaRepo = new VentaRepository(dbContext);
        var itemRepo  = new ItemVentaRepository(dbContext);
        var ventaSvc  = new VentaService(ventaRepo, itemRepo);

        var catalogMock = new TestCatalogClient();
        var taxMock     = new TestTaxClient();

        // Registrar productos en el mock de MS-3
        var productoAceiteId = Guid.NewGuid();
        catalogMock.Productos[productoAceiteId] = new ProductoCatalogDto
        {
            Id = productoAceiteId,
            Nombre = "Aceite de Oliva Extra Virgen 1L",
            PrecioBase = 5000m,
            IsActive = true,
            EsPesoVariable = false
        };

        var productoCafeId = Guid.NewGuid();
        catalogMock.Productos[productoCafeId] = new ProductoCatalogDto
        {
            Id = productoCafeId,
            Nombre = "Café en Grano Tostado 500g",
            PrecioBase = 3000m,
            IsActive = true,
            EsPesoVariable = false
        };

        var productoInactivoId = Guid.NewGuid();
        catalogMock.Productos[productoInactivoId] = new ProductoCatalogDto
        {
            Id = productoInactivoId,
            Nombre = "Producto Descatalogado",
            PrecioBase = 2500m,
            IsActive = false,
            EsPesoVariable = false
        };

        var productoBalanzaId = Guid.NewGuid();
        catalogMock.Productos[productoBalanzaId] = new ProductoCatalogDto
        {
            Id = productoBalanzaId,
            Nombre = "Manzanas Fuji a Granel",
            PrecioBase = 1200m,
            IsActive = true,
            EsPesoVariable = true
        };

        // Crear Turno Abierto y Venta Inicial en estado PENDIENTE
        var turno = await turnoSvc.Abrir(cajeroId, tenantId, sucursalId, 20000m);

        var ventaPendiente = new Venta
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
        await ventaRepo.CrearAsync(ventaPendiente);

        // Factory para instanciar el controlador con contexto HTTP autenticado
        VentasController CrearController(bool autenticado = true)
        {
            var ctrl = new VentasController(ventaSvc, turnoSvc, catalogMock, taxMock);
            var httpCtx = new DefaultHttpContext();

            if (autenticado)
            {
                var claims = new[]
                {
                    new Claim("cajero_id", cajeroId.ToString()),
                    new Claim("tenant_id", tenantId.ToString())
                };
                httpCtx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
                httpCtx.Request.Headers["Authorization"] = "Bearer simulated_jwt_token_123";
            }

            ctrl.ControllerContext = new ControllerContext { HttpContext = httpCtx };
            return ctrl;
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // SECCIÓN 1: Validación del Modelo Request (AgregarItemRequestValidator)
        // ═══════════════════════════════════════════════════════════════════════════════
        Console.WriteLine("── 1. Validación de Entrada (AgregarItemRequestValidator) ──────────────────");

        var validator = new AgregarItemRequestValidator();

        var v1 = validator.Validate(new AgregarItemRequest { ProductoId = Guid.Empty, Cantidad = 1 });
        Check("V-01", "Rechaza ProductoId vacío (Guid.Empty)", !v1.IsValid);

        var v2 = validator.Validate(new AgregarItemRequest { ProductoId = Guid.NewGuid(), Cantidad = 0 });
        Check("V-02", "Rechaza Cantidad <= 0 cuando no hay peso", !v2.IsValid);

        var v3 = validator.Validate(new AgregarItemRequest { ProductoId = Guid.NewGuid(), Cantidad = -2 });
        Check("V-03", "Rechaza Cantidad negativa", !v3.IsValid);

        var v4 = validator.Validate(new AgregarItemRequest { ProductoId = Guid.NewGuid(), PesoKg = -1.5m });
        Check("V-04", "Rechaza PesoKg negativo o cero", !v4.IsValid);

        var v5 = validator.Validate(new AgregarItemRequest { ProductoId = Guid.NewGuid(), Cantidad = 2 });
        Check("V-05", "Acepta request válido con Cantidad > 0", v5.IsValid);

        var v6 = validator.Validate(new AgregarItemRequest { ProductoId = Guid.NewGuid(), PesoKg = 1.75m });
        Check("V-06", "Acepta request válido con PesoKg > 0 (balanza)", v6.IsValid);

        Console.WriteLine();

        // ═══════════════════════════════════════════════════════════════════════════════
        // SECCIÓN 2: Autenticación JWT y Claims
        // ═══════════════════════════════════════════════════════════════════════════════
        Console.WriteLine("── 2. Autenticación y Seguridad (Claims JWT) ──────────────────────────────");

        var controllerSinAuth = CrearController(autenticado: false);
        var resSinAuth = await controllerSinAuth.AgregarItem(
            ventaPendiente.Id,
            new AgregarItemRequest { ProductoId = productoAceiteId, Cantidad = 1 },
            CancellationToken.None);

        Check("SEC-01", "Petición sin JWT / claims retorna 401 Unauthorized",
            resSinAuth is UnauthorizedObjectResult);

        Console.WriteLine();

        // ═══════════════════════════════════════════════════════════════════════════════
        // SECCIÓN 3: Precondiciones de la Venta (Existencia y Estado PENDIENTE)
        // ═══════════════════════════════════════════════════════════════════════════════
        Console.WriteLine("── 3. Precondiciones de la Venta / Carrito ────────────────────────────────");

        var controller = CrearController(autenticado: true);

        // 3.1 Venta no existente
        var resVentaNoExiste = await controller.AgregarItem(
            Guid.NewGuid(),
            new AgregarItemRequest { ProductoId = productoAceiteId, Cantidad = 1 },
            CancellationToken.None);

        Check("EST-01", "Venta inexistente retorna 404 Not Found",
            resVentaNoExiste is NotFoundObjectResult);

        // 3.2 Venta en estado COMPLETADA
        var ventaCompletada = new Venta
        {
            TurnoId    = turno.Id,
            CajeroId   = cajeroId,
            SucursalId = sucursalId,
            TenantId   = tenantId,
            Estado     = EstadoVenta.COMPLETADA
        };
        await ventaRepo.CrearAsync(ventaCompletada);

        var resVentaCompletada = await controller.AgregarItem(
            ventaCompletada.Id,
            new AgregarItemRequest { ProductoId = productoAceiteId, Cantidad = 1 },
            CancellationToken.None);

        Check("EST-02", "Venta COMPLETADA no permite agregar ítems (retorna 409 Conflict)",
            resVentaCompletada is ConflictObjectResult);

        // 3.3 Venta en estado ANULADA
        var ventaAnulada = new Venta
        {
            TurnoId    = turno.Id,
            CajeroId   = cajeroId,
            SucursalId = sucursalId,
            TenantId   = tenantId,
            Estado     = EstadoVenta.ANULADA
        };
        await ventaRepo.CrearAsync(ventaAnulada);

        var resVentaAnulada = await controller.AgregarItem(
            ventaAnulada.Id,
            new AgregarItemRequest { ProductoId = productoAceiteId, Cantidad = 1 },
            CancellationToken.None);

        Check("EST-03", "Venta ANULADA no permite agregar ítems (retorna 409 Conflict)",
            resVentaAnulada is ConflictObjectResult);

        Console.WriteLine();

        // ═══════════════════════════════════════════════════════════════════════════════
        // SECCIÓN 4: Integración con MS-3 (Catálogo y Precios)
        // ═══════════════════════════════════════════════════════════════════════════════
        Console.WriteLine("── 4. Consulta a MS-3 (GET /api/products/{id}) ────────────────────────────");

        // 4.1 Producto que no existe en MS-3
        var productoFantasmaId = Guid.NewGuid();
        var resProdNoExiste = await controller.AgregarItem(
            ventaPendiente.Id,
            new AgregarItemRequest { ProductoId = productoFantasmaId, Cantidad = 1 },
            CancellationToken.None);

        bool prodNoExisteOk = resProdNoExiste is NotFoundObjectResult nf &&
            JsonSerializer.Serialize(nf.Value).Contains("Producto no encontrado");

        Check("MS3-01", "Producto no encontrado en MS-3 retorna 404 con mensaje 'Producto no encontrado'",
            prodNoExisteOk);

        // 4.2 Producto inactivo en MS-3
        var resProdInactivo = await controller.AgregarItem(
            ventaPendiente.Id,
            new AgregarItemRequest { ProductoId = productoInactivoId, Cantidad = 1 },
            CancellationToken.None);

        Check("MS3-02", "Producto inactivo (IsActive=false) retorna 400 Bad Request",
            resProdInactivo is BadRequestObjectResult);

        // 4.3 Falla de comunicación con MS-3
        catalogMock.SimularFallaDeRed = true;
        var resFallaMS3 = await controller.AgregarItem(
            ventaPendiente.Id,
            new AgregarItemRequest { ProductoId = productoAceiteId, Cantidad = 1 },
            CancellationToken.None);
        catalogMock.SimularFallaDeRed = false;

        Check("MS3-03", "Falla de red con MS-3 retorna 502 Bad Gateway",
            resFallaMS3 is ObjectResult obj3 && obj3.StatusCode == StatusCodes.Status502BadGateway);

        Console.WriteLine();

        // ═══════════════════════════════════════════════════════════════════════════════
        // SECCIÓN 5: Integración con MS-2 (Tax & Compliance)
        // ═══════════════════════════════════════════════════════════════════════════════
        Console.WriteLine("── 5. Consulta a MS-2 (POST /api/tax/calculate) ───────────────────────────");

        // 5.1 Falla de comunicación con MS-2 (no responde)
        taxMock.SimularFallaDeRed = true;
        var resFallaMS2 = await controller.AgregarItem(
            ventaPendiente.Id,
            new AgregarItemRequest { ProductoId = productoAceiteId, Cantidad = 1 },
            CancellationToken.None);
        taxMock.SimularFallaDeRed = false;

        Check("MS2-01", "Si MS-2 no responde retorna 503 Service Unavailable",
            resFallaMS2 is ObjectResult obj2 && obj2.StatusCode == StatusCodes.Status503ServiceUnavailable);

        Console.WriteLine();


        // ═══════════════════════════════════════════════════════════════════════════════
        // SECCIÓN 6: Flujo Completo Exitoso y Acumulación de Datos Calculados
        // ═══════════════════════════════════════════════════════════════════════════════
        Console.WriteLine("── 6. Flujo Exitoso: Agregar Ítems con Datos Calculados ───────────────────");

        // 6.1 AGREGAR ÍTEM 1: Aceite de Oliva ($5.000 x 2 unidades)
        // Cálculo esperado:
        // - Subtotal ítem = $10.000
        // - IVA 19% MS-2 = $1.900
        // - Total ítem = $11.900
        var actionResult1 = await controller.AgregarItem(
            ventaPendiente.Id,
            new AgregarItemRequest { ProductoId = productoAceiteId, Cantidad = 2 },
            CancellationToken.None);

        Check("FLOW-01", "Endpoint retorna 201 Created al agregar primer ítem",
            actionResult1 is CreatedAtActionResult created1 && created1.StatusCode == StatusCodes.Status201Created);

        var createdResult1 = (CreatedAtActionResult)actionResult1;
        var itemResp1 = (ItemVentaResponse)createdResult1.Value!;

        Check("FLOW-02", "MS-3 fue consultado con el ProductoId solicitado",
            catalogMock.UltimoProductoConsultado == productoAceiteId);

        Check("FLOW-03", "Ítem toma el precio actual de MS-3 ($5.000) y calcula subtotal ($10.000)",
            itemResp1.PrecioUnitario == 5000m && itemResp1.Subtotal == 10000m);

        Check("FLOW-04", "Ítem contiene IVA calculado por MS-2 ($1.900)",
            itemResp1.Iva == 1900m);

        Check("FLOW-05", "Ítem contiene Total calculado ($11.900)",
            itemResp1.Total == 11900m);

        // Verificar persistencia en base de datos para ítem 1
        var itemDb1 = await itemRepo.GetByIdAsync(itemResp1.Id);
        Check("FLOW-06", "Ítem 1 está persistido en la tabla ItemsVenta con VentaId correcto",
            itemDb1 is not null && itemDb1.VentaId == ventaPendiente.Id);

        var ventaDb1 = await ventaRepo.GetByIdAsync(ventaPendiente.Id);
        Check("FLOW-07", "Venta en DB actualizada: Subtotal=10.000, Impuestos=1.900, Total=11.900",
            ventaDb1!.Subtotal == 10000m && ventaDb1.Impuestos == 1900m && ventaDb1.Total == 11900m);

        // 6.2 AGREGAR ÍTEM 2: Café Molido ($3.000 x 1 unidad)
        // Cálculo acumulado esperado:
        // - Subtotal acumulado = $10.000 + $3.000 = $13.000
        // - IVA acumulado (19%) = $2.470
        // - Total acumulado = $15.470
        var actionResult2 = await controller.AgregarItem(
            ventaPendiente.Id,
            new AgregarItemRequest { ProductoId = productoCafeId, Cantidad = 1 },
            CancellationToken.None);

        Check("FLOW-08", "Endpoint retorna 201 Created al agregar segundo ítem",
            actionResult2 is CreatedAtActionResult created2 && created2.StatusCode == StatusCodes.Status201Created);

        var itemResp2 = (ItemVentaResponse)((CreatedAtActionResult)actionResult2).Value!;

        Check("FLOW-09", "Ítem 2 toma precio de MS-3 ($3.000) y calcula subtotal ($3.000)",
            itemResp2.PrecioUnitario == 3000m && itemResp2.Subtotal == 3000m);

        var ventaDb2 = await ventaRepo.GetByIdAsync(ventaPendiente.Id);
        Check("FLOW-10", "Venta en DB acumula Subtotal consolidado = $13.000",
            ventaDb2!.Subtotal == 13000m);

        Check("FLOW-11", "Venta en DB acumula IVA consolidado = $2.470",
            ventaDb2.Impuestos == 2470m);

        Check("FLOW-12", "Venta en DB acumula Total consolidado = $15.470",
            ventaDb2.Total == 15470m);

        Check("FLOW-13", "Venta en DB contiene exactamente 2 ítems en su colección",
            ventaDb2.Items.Count == 2);

        // 6.3 AGREGAR ÍTEM DE BALANZA (Peso variable: 1.5 kg x $1.200 = $1.800)
        var actionResult3 = await controller.AgregarItem(
            ventaPendiente.Id,
            new AgregarItemRequest { ProductoId = productoBalanzaId, PesoKg = 1.5m },
            CancellationToken.None);

        var itemResp3 = (ItemVentaResponse)((CreatedAtActionResult)actionResult3).Value!;

        Check("FLOW-14", "Producto de balanza calcula subtotal = peso_kg * precio (1.5 * 1.200 = 1.800)",
            itemResp3.PesoKg == 1.5m && itemResp3.Cantidad == 1.5m && itemResp3.Subtotal == 1800m);

        Console.WriteLine();

        // ═══════════════════════════════════════════════════════════════════════════════
        // SECCIÓN 7: Serialización JSON del Response (ItemVentaResponse)
        // ═══════════════════════════════════════════════════════════════════════════════
        Console.WriteLine("── 7. Serialización JSON del Response ─────────────────────────────────────");

        var jsonResponse = JsonSerializer.Serialize(itemResp1);

        Check("JSON-01", "JSON contiene 'id'", jsonResponse.Contains("\"id\":"));
        Check("JSON-02", "JSON contiene 'producto_id'", jsonResponse.Contains("\"producto_id\":"));
        Check("JSON-03", "JSON contiene 'nombre_producto'", jsonResponse.Contains("\"nombre_producto\":"));
        Check("JSON-04", "JSON contiene 'precio_unitario'", jsonResponse.Contains("\"precio_unitario\":"));
        Check("JSON-05", "JSON contiene 'subtotal'", jsonResponse.Contains("\"subtotal\":"));
        Check("JSON-06", "JSON contiene 'iva'", jsonResponse.Contains("\"iva\":1900"));
        Check("JSON-07", "JSON contiene 'total'", jsonResponse.Contains("\"total\":11900"));

        Console.WriteLine();

        // ═══════════════════════════════════════════════════════════════════════════════
        // SECCIÓN 8: Modificar Cantidad de Ítem (PUT /ventas/{id}/items/{itemId})
        // ═══════════════════════════════════════════════════════════════════════════════
        Console.WriteLine("── 8. Modificar Cantidad de Ítem (PUT /ventas/{id}/items/{itemId}) ─────────");

        var modValidator = new ModificarCantidadItemRequestValidator();
        var mv1 = modValidator.Validate(new ModificarCantidadItemRequest { Cantidad = 0 });
        Check("MOD-01", "Validador rechaza Cantidad <= 0", !mv1.IsValid);

        var mv2 = modValidator.Validate(new ModificarCantidadItemRequest { Cantidad = 3 });
        Check("MOD-02", "Validador acepta Cantidad > 0", mv2.IsValid);

        // Error: Venta inexistente
        var resModVentaNoExiste = await controller.ModificarCantidadItem(
            Guid.NewGuid(), itemResp1.Id,
            new ModificarCantidadItemRequest { Cantidad = 3 },
            CancellationToken.None);
        Check("MOD-03", "Venta inexistente retorna 404 Not Found",
            resModVentaNoExiste is NotFoundObjectResult);

        // Error: Ítem inexistente en la venta
        var resModItemNoExiste = await controller.ModificarCantidadItem(
            ventaPendiente.Id, Guid.NewGuid(),
            new ModificarCantidadItemRequest { Cantidad = 3 },
            CancellationToken.None);
        Check("MOD-04", "Ítem no perteneciente a la venta retorna 404 Not Found",
            resModItemNoExiste is NotFoundObjectResult);

        // Error: Venta completada
        var resModVentaComp = await controller.ModificarCantidadItem(
            ventaCompletada.Id, Guid.NewGuid(),
            new ModificarCantidadItemRequest { Cantidad = 3 },
            CancellationToken.None);
        Check("MOD-05", "Venta COMPLETADA retorna 409 Conflict al intentar modificar ítem",
            resModVentaComp is ConflictObjectResult);

        // Error: MS-2 no responde
        taxMock.SimularFallaDeRed = true;
        var resModFallaMS2 = await controller.ModificarCantidadItem(
            ventaPendiente.Id, itemResp1.Id,
            new ModificarCantidadItemRequest { Cantidad = 3 },
            CancellationToken.None);
        taxMock.SimularFallaDeRed = false;
        Check("MOD-06", "Si MS-2 no responde retorna 503 Service Unavailable",
            resModFallaMS2 is ObjectResult objMod && objMod.StatusCode == StatusCodes.Status503ServiceUnavailable);

        // Éxito: Modificar cantidad de Ítem 1 (Aceite de Oliva) de 2 a 3 unidades
        // Carrito con 3 ítems:
        // Ítem 1 ahora: 3 x $5.000 = $15.000
        // Ítem 2: 1 x $3.000 = $3.000
        // Ítem 3 (Balanza): 1.5 kg x $1.200 = $1.800
        // Nuevo subtotal venta: $15.000 + $3.000 + $1.800 = $19.800
        // Nuevo IVA venta (19%): $3.762
        // Nuevo total venta: $23.562
        var resModOk = await controller.ModificarCantidadItem(
            ventaPendiente.Id, itemResp1.Id,
            new ModificarCantidadItemRequest { Cantidad = 3 },
            CancellationToken.None);

        Check("MOD-07", "Endpoint retorna 200 OK al modificar cantidad",
            resModOk is OkObjectResult okMod && okMod.StatusCode == StatusCodes.Status200OK);

        var itemModResp = (ItemVentaResponse)((OkObjectResult)resModOk).Value!;

        Check("MOD-08", "Ítem modificado recalcula cantidad = 3",
            itemModResp.Cantidad == 3m);

        Check("MOD-09", "Ítem modificado recalcula subtotal = $15.000 ($5.000 x 3)",
            itemModResp.Subtotal == 15000m);

        var ventaEnDbMod = await ventaRepo.GetByIdAsync(ventaPendiente.Id);

        Check("MOD-10", "Venta en DB recalcula Subtotal consolidado = $19.800",
            ventaEnDbMod!.Subtotal == 19800m);

        Check("MOD-11", "Venta en DB recalcula IVA consolidado (19%) = $3.762 vía MS-2",
            ventaEnDbMod.Impuestos == 3762m);

        Check("MOD-12", "Venta en DB recalcula Total consolidado = $23.562",
            ventaEnDbMod.Total == 23562m);


        var itemEnDbMod = await itemRepo.GetByIdAsync(itemResp1.Id);
        Check("MOD-13", "Ítem en DB persistido con nueva cantidad=3 y subtotal=15.000",
            itemEnDbMod!.Cantidad == 3m && itemEnDbMod.Subtotal == 15000m);

        Console.WriteLine();

        // ═══════════════════════════════════════════════════════════════════════════════
        // SECCIÓN 9: Eliminar Ítem (DELETE /ventas/{id}/items/{itemId})
        // ═══════════════════════════════════════════════════════════════════════════════
        Console.WriteLine("── 9. Eliminar Ítem (DELETE /ventas/{id}/items/{itemId}) ───────────────────");

        // Estado actual del carrito (tras MOD-07):
        // Ítem 1 (Aceite): 3 x $5.000 = $15.000
        // Ítem 2 (Café):   1 x $3.000 =  $3.000
        // Ítem 3 (Balanza):1.5 x $1.200 = $1.800
        // Subtotal = $19.800 | IVA = $3.762 | Total = $23.562

        // DEL-01: Sin JWT → 401
        var ctrlSinAuth = CrearController(autenticado: false);
        var resDelSinAuth = await ctrlSinAuth.EliminarItem(ventaPendiente.Id, itemResp1.Id, CancellationToken.None);
        Check("DEL-01", "Sin JWT retorna 401 Unauthorized", resDelSinAuth is UnauthorizedObjectResult);

        // DEL-02: Venta inexistente → 404
        var resDelVentaNoExiste = await controller.EliminarItem(Guid.NewGuid(), itemResp1.Id, CancellationToken.None);
        Check("DEL-02", "Venta inexistente retorna 404 Not Found", resDelVentaNoExiste is NotFoundObjectResult);

        // DEL-03: Ítem inexistente en la venta → 404
        var resDelItemNoExiste = await controller.EliminarItem(ventaPendiente.Id, Guid.NewGuid(), CancellationToken.None);
        Check("DEL-03", "Ítem inexistente retorna 404 Not Found", resDelItemNoExiste is NotFoundObjectResult);

        // DEL-04: Venta en estado COMPLETADA → 409
        var resDelVentaComp = await controller.EliminarItem(ventaCompletada.Id, Guid.NewGuid(), CancellationToken.None);
        Check("DEL-04", "Venta COMPLETADA retorna 409 Conflict al intentar eliminar ítem",
            resDelVentaComp is ConflictObjectResult);

        // DEL-05: MS-2 no responde al eliminar → 503
        // (solo se llama a MS-2 si quedan ítems; el carrito tiene 3 ítems, así que sí llama)
        taxMock.SimularFallaDeRed = true;
        var resDelFallaMS2 = await controller.EliminarItem(ventaPendiente.Id, itemResp1.Id, CancellationToken.None);
        taxMock.SimularFallaDeRed = false;
        Check("DEL-05", "Si MS-2 no responde retorna 503 Service Unavailable",
            resDelFallaMS2 is ObjectResult objDel && objDel.StatusCode == StatusCodes.Status503ServiceUnavailable);

        // DEL-06: Flujo exitoso — eliminar Ítem 3 (Balanza: $1.800)
        // Carrito resultante:
        // Ítem 1 (Aceite): 3 x $5.000 = $15.000
        // Ítem 2 (Café):   1 x $3.000 =  $3.000
        // Nuevo subtotal = $18.000 | IVA 19% = $3.420 | Total = $21.420
        var resDelItem3 = await controller.EliminarItem(ventaPendiente.Id, itemResp3.Id, CancellationToken.None);
        Check("DEL-06", "Eliminar ítem de balanza retorna 200 OK",
            resDelItem3 is OkObjectResult okDel && okDel.StatusCode == StatusCodes.Status200OK);

        var ventaDbDel1 = await ventaRepo.GetByIdAsync(ventaPendiente.Id);
        Check("DEL-07", "Venta en DB recalcula Subtotal = $18.000 tras eliminar ítem balanza",
            ventaDbDel1!.Subtotal == 18000m);

        Check("DEL-08", "Venta en DB recalcula IVA (19%) = $3.420 vía MS-2",
            ventaDbDel1.Impuestos == 3420m);

        Check("DEL-09", "Venta en DB recalcula Total = $21.420",
            ventaDbDel1.Total == 21420m);

        Check("DEL-10", "Venta en DB tiene exactamente 2 ítems restantes",
            ventaDbDel1.Items.Count == 2);

        // Verificar que el ítem eliminado ya no existe en la base de datos
        var itemEliminadoEnDb = await itemRepo.GetByIdAsync(itemResp3.Id);
        Check("DEL-11", "Ítem eliminado ya no existe en la tabla ItemsVenta",
            itemEliminadoEnDb is null);

        // DEL-12: Eliminar el último ítem restante → totales deben quedar en 0
        // Eliminamos Ítem 2 (Café: $3.000)
        var resDelItem2 = await controller.EliminarItem(ventaPendiente.Id, itemResp2.Id, CancellationToken.None);
        Check("DEL-12", "Eliminar Ítem 2 retorna 200 OK",
            resDelItem2 is OkObjectResult);

        // DEL-13: Eliminar Ítem 1 (el último) → carrito vacío → totales = 0 (MS-2 no se llama)
        var resDelItem1 = await controller.EliminarItem(ventaPendiente.Id, itemResp1.Id, CancellationToken.None);
        Check("DEL-13", "Eliminar último ítem retorna 200 OK",
            resDelItem1 is OkObjectResult);

        var ventaDbVacia = await ventaRepo.GetByIdAsync(ventaPendiente.Id);
        Check("DEL-14", "Venta con carrito vacío tiene Subtotal = 0, Impuestos = 0, Total = 0",
            ventaDbVacia!.Subtotal == 0m && ventaDbVacia.Impuestos == 0m && ventaDbVacia.Total == 0m);

        Check("DEL-15", "Venta vacía tiene 0 ítems en su colección",
            ventaDbVacia.Items.Count == 0);

        Console.WriteLine();

        // ═══════════════════════════════════════════════════════════════════════════════
        // SECCIÓN 10: GET /ventas/{id} — Estado completo del carrito
        // ═══════════════════════════════════════════════════════════════════════════════
        // Para esta sección creamos una venta fresca con ítems conocidos
        // (la venta anterior quedó vacía tras los tests de DELETE)
        Console.WriteLine("── 10. GET /ventas/{id}: Estado completo del carrito ──────────────────────");

        // Crear una venta nueva con 2 ítems para inspeccionar la respuesta
        var ventaParaGet = new Venta
        {
            TurnoId    = turno.Id,
            CajeroId   = cajeroId,
            SucursalId = sucursalId,
            TenantId   = tenantId,
            Subtotal   = 0m,
            Impuestos  = 0m,
            Total      = 0m,
            MetodoPago = MetodoPagoVenta.TARJETA,
            Estado     = EstadoVenta.PENDIENTE
        };
        await ventaRepo.CrearAsync(ventaParaGet);

        // Agregar 2 ítems al carrito: Aceite (2u × $5.000) + Café (1u × $3.000)
        await controller.AgregarItem(ventaParaGet.Id,
            new AgregarItemRequest { ProductoId = productoAceiteId, Cantidad = 2 },
            CancellationToken.None);
        await controller.AgregarItem(ventaParaGet.Id,
            new AgregarItemRequest { ProductoId = productoCafeId, Cantidad = 1 },
            CancellationToken.None);
        // Estado esperado: Subtotal=$13.000 | IVA=$2.470 | Total=$15.470

        // GET-01: Sin JWT → 401
        var ctrlSinAuthGet = CrearController(autenticado: false);
        var resGetSinAuth = await ctrlSinAuthGet.GetById(ventaParaGet.Id);
        Check("GET-01", "Sin JWT retorna 401 Unauthorized", resGetSinAuth is UnauthorizedObjectResult);

        // GET-02: Venta inexistente → 404
        var resGetNoExiste = await controller.GetById(Guid.NewGuid());
        Check("GET-02", "Venta inexistente retorna 404 Not Found", resGetNoExiste is NotFoundObjectResult);

        // GET-03: Venta existente → 200 OK con VentaResponse completo
        var resGetOk = await controller.GetById(ventaParaGet.Id);
        Check("GET-03", "Venta existente retorna 200 OK", resGetOk is OkObjectResult okGet && okGet.StatusCode == StatusCodes.Status200OK);

        var ventaResp = (VentaResponse)((OkObjectResult)resGetOk).Value!;

        // GET-04: Id de la venta correcto
        Check("GET-04", "Response contiene el Id correcto de la venta",
            ventaResp.Id == ventaParaGet.Id);

        // GET-05: Estado PENDIENTE incluido en la respuesta
        Check("GET-05", "Response incluye estado = PENDIENTE",
            ventaResp.Estado == "PENDIENTE");

        // GET-06: Método de pago correcto
        Check("GET-06", "Response incluye metodo_pago = TARJETA",
            ventaResp.MetodoPago == "TARJETA");

        // GET-07: Colección de ítems incluye exactamente 2 ítems
        Check("GET-07", "Response incluye colección de 2 ítems",
            ventaResp.Items.Count == 2);

        // GET-08: Totales actualizados correctos
        Check("GET-08", "Response contiene Subtotal = $13.000 actualizado",
            ventaResp.Subtotal == 13000m);

        Check("GET-09", "Response contiene Impuestos (IVA) = $2.470 actualizado",
            ventaResp.Impuestos == 2470m);

        Check("GET-10", "Response contiene Total = $15.470 actualizado",
            ventaResp.Total == 15470m);

        // GET-11: Datos de cada ítem — verificar primer ítem (Aceite)
        var itemAceiteResp = ventaResp.Items.FirstOrDefault(i => i.ProductoId == productoAceiteId);
        Check("GET-11", "Ítem Aceite incluido en la respuesta con nombre correcto",
            itemAceiteResp is not null && itemAceiteResp.NombreProducto.Contains("Aceite"));

        Check("GET-12", "Ítem Aceite tiene PrecioUnitario = $5.000 y Subtotal = $10.000",
            itemAceiteResp!.PrecioUnitario == 5000m && itemAceiteResp.Subtotal == 10000m);

        Check("GET-13", "Ítem Aceite tiene Cantidad = 2",
            itemAceiteResp.Cantidad == 2m);

        // GET-14: Verificar segundo ítem (Café)
        var itemCafeResp = ventaResp.Items.FirstOrDefault(i => i.ProductoId == productoCafeId);
        Check("GET-14", "Ítem Café incluido en la respuesta con precio y subtotal correctos",
            itemCafeResp is not null && itemCafeResp.PrecioUnitario == 3000m && itemCafeResp.Subtotal == 3000m);

        // GET-15: Serialización JSON del VentaResponse completo
        var jsonVenta = JsonSerializer.Serialize(ventaResp);
        Check("GET-15", "JSON contiene 'estado'", jsonVenta.Contains("\"estado\":"));
        Check("GET-16", "JSON contiene 'subtotal'", jsonVenta.Contains("\"subtotal\":"));
        Check("GET-17", "JSON contiene 'impuestos'", jsonVenta.Contains("\"impuestos\":"));
        Check("GET-18", "JSON contiene 'total'", jsonVenta.Contains("\"total\":"));
        Check("GET-19", "JSON contiene 'items' con array no vacío", jsonVenta.Contains("\"items\":[{"));
        Check("GET-20", "JSON contiene 'created_at'", jsonVenta.Contains("\"created_at\":"));

        Console.WriteLine();

        // ═══════════════════════════════════════════════════════════════════════════════
        // RESUMEN FINAL
        // ═══════════════════════════════════════════════════════════════════════════════
        Console.WriteLine("══════════════════════════════════════════════════════════════════════════");
        if (fail == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✅ RESULTADO: TODOS LOS TESTS PASARON EXITOSAMENTE ({pass} de {pass + fail})");
            Console.ResetColor();
            Console.WriteLine("  Todas las tareas cumplen al 100% los requisitos solicitados:");
            Console.WriteLine("   • POST /ventas/{id}/items: agrega ítem consultando MS-3 y MS-2");
            Console.WriteLine("   • Manejo de errores: MS-3 no encontrado → 404; MS-2 no responde → 503");
            Console.WriteLine("   • PUT /ventas/{id}/items/{itemId}: modifica cantidad y recalcula subtotales/totales");
            Console.WriteLine("   • DELETE /ventas/{id}/items/{itemId}: elimina ítem y actualiza totales (MS-2)");
            Console.WriteLine("   • GET /ventas/{id}: retorna estado completo del carrito con ítems y totales");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ❌ RESULTADO: {fail} PRUEBAS FALLARON (Aprobadas: {pass}, Fallidas: {fail})");
            Console.ResetColor();
        }
        Console.WriteLine("══════════════════════════════════════════════════════════════════════════");

    }
}

// ── Clases Auxiliares Mocks de MS-3 y MS-2 ───────────────────────────────────────

class TestCatalogClient : ICatalogClient
{
    public Dictionary<Guid, ProductoCatalogDto> Productos { get; } = new();
    public bool SimularFallaDeRed { get; set; } = false;
    public Guid? UltimoProductoConsultado { get; private set; }

    public Task<ProductoCatalogDto?> GetProductAsync(
        Guid productId,
        string? bearerToken = null,
        CancellationToken cancellationToken = default)
    {
        UltimoProductoConsultado = productId;

        if (SimularFallaDeRed)
            throw new ExternalServiceException("MS-3", "Error de conexión con el catálogo de productos (503 Service Unavailable)");

        Productos.TryGetValue(productId, out var prod);
        return Task.FromResult(prod);
    }
}

class TestTaxClient : ITaxClient
{
    public bool SimularFallaDeRed { get; set; } = false;
    public List<TaxItemDto>? UltimosItemsRecibidos { get; private set; }

    public Task<TaxCalculationResult?> CalculateTaxAsync(
        IEnumerable<TaxItemDto> items,
        decimal? porcentajeIva = null,
        string? bearerToken = null,
        CancellationToken cancellationToken = default)
    {
        UltimosItemsRecibidos = items.ToList();

        if (SimularFallaDeRed)
            throw new ExternalServiceException("MS-2", "Error de conexión con el servicio tributario (500 Internal Error)");

        var subtotal = UltimosItemsRecibidos.Sum(i => Math.Round(i.Precio * i.Cantidad, 2));
        var tasa = porcentajeIva ?? 19m;
        var iva = Math.Round(subtotal * (tasa / 100m), 2);
        var total = subtotal + iva;

        var result = new TaxCalculationResult
        {
            Subtotal = subtotal,
            PorcentajeIva = tasa,
            Iva = iva,
            Total = total,
            Items = UltimosItemsRecibidos.Select(i =>
            {
                var s = Math.Round(i.Precio * i.Cantidad, 2);
                var v = Math.Round(s * (tasa / 100m), 2);
                return new TaxItemBreakdownDto
                {
                    Nombre = i.Nombre,
                    Precio = i.Precio,
                    Cantidad = i.Cantidad,
                    Subtotal = s,
                    PorcentajeIva = tasa,
                    Iva = v,
                    Total = s + v
                };
            }).ToList()
        };

        return Task.FromResult<TaxCalculationResult?>(result);
    }
}

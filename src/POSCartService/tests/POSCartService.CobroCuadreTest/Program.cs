using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSCartService.Controllers;
using POSCartService.Data;
using POSCartService.DTOs;
using POSCartService.Messaging;
using POSCartService.Models;
using POSCartService.Repositories;
using POSCartService.Services;

var tenantId = Guid.NewGuid();
var cajeroId = Guid.NewGuid();
var sucursalId = Guid.NewGuid();
var options = new DbContextOptionsBuilder<PosCartDbContext>()
    .UseInMemoryDatabase($"CobroCuadre_{Guid.NewGuid()}")
    .Options;

await using var db = new PosCartDbContext(options) { CurrentTenantId = tenantId };
var turnoService = new TurnoService(new TurnoRepository(db));
var producer = new FakeKafkaProducer();
var ventaRepository = new VentaRepository(db);
var ventaService = new VentaService(ventaRepository, kafkaProducer: producer);
var turno = await turnoService.Abrir(cajeroId, tenantId, sucursalId, 100m);
var failures = 0;

var controller = new VentasController(ventaService, turnoService, new FakeCatalogClient(), new FakeTaxClient())
{
    ControllerContext = new ControllerContext { HttpContext = CreateHttpContext() }
};

var inexistente = await controller.Cobrar(
    Guid.NewGuid(),
    new CobrarVentaRequest { MontoRecibido = 1m });
Check("Venta inexistente retorna 404", inexistente is NotFoundObjectResult);

var venta = await CrearVenta(100m);
var antesDeCobrar = await controller.Cobrar(
    venta.Id,
    new CobrarVentaRequest { MontoRecibido = 99m });
Check("Monto insuficiente retorna 400", antesDeCobrar is BadRequestObjectResult);
Check("Monto insuficiente conserva la venta PENDIENTE", venta.Estado == EstadoVenta.PENDIENTE);

var cobro = await controller.Cobrar(
    venta.Id,
    new CobrarVentaRequest { MontoRecibido = 125m });
var cobroResponse = cobro as OkObjectResult;
var respuesta = cobroResponse?.Value as CobrarVentaResponse;
Check("Cobro válido retorna 200 y vuelto 25", cobroResponse is not null && respuesta?.Vuelto == 25m);

var ventaPersistida = await ventaRepository.GetByIdAsync(venta.Id);
var pago = ventaPersistida?.Pagos.SingleOrDefault();
Check("Cobro válido cambia la venta a COMPLETADA", ventaPersistida?.Estado == EstadoVenta.COMPLETADA);
Check("Pago conserva monto recibido, vuelto y EFECTIVO",
    pago is not null
    && pago.Monto == 125m
    && pago.Vuelto == 25m
    && pago.Metodo == MetodoPago.EFECTIVO);

var segundaVenta = await CrearVenta(50m);
await ventaService.CompletarAsync(segundaVenta.Id, 50m, 0m);

var ventaTarjeta = new Venta
{
    TurnoId = turno.Id,
    TenantId = tenantId,
    CajeroId = cajeroId,
    SucursalId = sucursalId,
    Subtotal = 999m,
    Total = 999m,
    MetodoPago = MetodoPagoVenta.TARJETA,
    Estado = EstadoVenta.COMPLETADA,
    Pagos = new List<Pago> { new() { Metodo = MetodoPago.TARJETA, Monto = 999m } }
};
await ventaRepository.CrearAsync(ventaTarjeta);

var turnosController = new TurnosController(turnoService)
{
    ControllerContext = new ControllerContext { HttpContext = CreateHttpContext() }
};
var cuadre = await turnosController.Cuadre(new CuadreRequest { MontoDeclarado = 200m });
var cuadreResponse = (cuadre as OkObjectResult)?.Value as CuadreResponse;
Check("Cuadre suma solo pagos EFECTIVO completados", cuadreResponse?.EfectivoEsperado == 175m);
Check("Cuadre retorna monto declarado y diferencia", cuadreResponse?.MontoDeclarado == 200m && cuadreResponse.Diferencia == 25m);

Check("Se publica sale.completed después de completar", producer.Events.Count == 2);
var evento = producer.Events.FirstOrDefault(e => e.VentaId == venta.Id);
Check("sale.completed contiene payload e identificador único",
    evento is not null
    && evento.EventId != Guid.Empty
    && evento.TenantId == tenantId
    && evento.VentaId == venta.Id
    && evento.CajeroId == cajeroId
    && evento.SucursalId == sucursalId
    && evento.Items.Count == 1
    && evento.Subtotal == venta.Subtotal
    && evento.Iva == venta.Impuestos
    && evento.Total == venta.Total
    && evento.MetodoPago == "EFECTIVO"
    && evento.Timestamp != default);

return failures == 0 ? 0 : 1;

async Task<Venta> CrearVenta(decimal total)
{
    var nuevaVenta = new Venta
    {
        TurnoId = turno.Id,
        TenantId = tenantId,
        CajeroId = cajeroId,
        SucursalId = sucursalId,
        Subtotal = total,
        Total = total,
        MetodoPago = MetodoPagoVenta.EFECTIVO,
        Estado = EstadoVenta.PENDIENTE,
        Items = new List<ItemVenta>
        {
            new()
            {
                ProductoId = Guid.NewGuid(),
                NombreProducto = "Producto de prueba",
                Cantidad = 1m,
                PrecioUnitario = total,
                Subtotal = total
            }
        }
    };

    return await ventaRepository.CrearAsync(nuevaVenta);
}

DefaultHttpContext CreateHttpContext()
{
    var context = new DefaultHttpContext();
    context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
    {
        new Claim("cajero_id", cajeroId.ToString()),
        new Claim("tenant_id", tenantId.ToString())
    }, "TestAuth"));
    return context;
}

void Check(string description, bool condition)
{
    Console.WriteLine($"{(condition ? "[PASS]" : "[FAIL]")} {description}");
    if (!condition)
        failures++;
}

sealed class FakeKafkaProducer : IKafkaProducerService
{
    public List<SaleCompletedEvent> Events { get; } = new();

    public Task PublicarAsync(string topico, string mensaje, string? clave = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task PublicarSaleCompletedAsync(SaleCompletedEvent evento, CancellationToken cancellationToken = default)
    {
        Events.Add(evento);
        return Task.CompletedTask;
    }
}

sealed class FakeCatalogClient : ICatalogClient
{
    public Task<ProductoCatalogDto?> GetProductAsync(
        Guid productId,
        string? bearerToken = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult<ProductoCatalogDto?>(null);
}

sealed class FakeTaxClient : ITaxClient
{
    public Task<TaxCalculationResult?> CalculateTaxAsync(
        IEnumerable<TaxItemDto> items,
        decimal? porcentajeIva = null,
        string? bearerToken = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult<TaxCalculationResult?>(null);
}
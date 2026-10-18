extern alias POSCartServiceAlias;

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Pos = POSCartServiceAlias::POSCartService;

namespace GlobalMart.IntegrationTests;

[Trait("Plan", "S2-040")]
public sealed class PaymentEventSequenceContractTests
{
    [Fact]
    public void Contrato_ExponeYPersisteLaMismaReferenciaOpaca()
    {
        var responseProperty = typeof(Pos.DTOs.CobrarTarjetaResponse)
            .GetProperty(nameof(Pos.DTOs.CobrarTarjetaResponse.GatewayReference));
        var paymentProperty = typeof(Pos.Models.Pago)
            .GetProperty(nameof(Pos.Models.Pago.Referencia));

        Assert.NotNull(responseProperty);
        Assert.Equal(
            "gateway_reference",
            responseProperty!.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name);
        Assert.NotNull(paymentProperty);
        Assert.Equal(typeof(string), Nullable.GetUnderlyingType(paymentProperty!.PropertyType)
            ?? paymentProperty.PropertyType);

        var options = new DbContextOptionsBuilder<Pos.Data.PosCartDbContext>()
            .UseNpgsql("Host=localhost;Database=dummy_test;Username=dummy")
            .Options;
        using var db = new Pos.Data.PosCartDbContext(options);
        var pago = db.Model.FindEntityType(typeof(Pos.Models.Pago));

        Assert.NotNull(pago);
        Assert.Equal(
            100,
            pago!.FindProperty(nameof(Pos.Models.Pago.Referencia))?.GetMaxLength());

        var prohibited = new[] { "Pan", "Cvv", "Pin", "CardNumber", "NumeroTarjeta" };
        Assert.DoesNotContain(
            typeof(Pos.Models.Pago).GetProperties(),
            property => prohibited.Contains(property.Name, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Servicio_AprobacionPropagaReferenciaAPagoIntentoYEvento()
    {
        var tenantId = Guid.NewGuid();
        var venta = new Pos.Models.Venta
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TurnoId = Guid.NewGuid(),
            CajeroId = Guid.NewGuid(),
            SucursalId = Guid.NewGuid(),
            Subtotal = 10000m,
            Impuestos = 1900m,
            Total = 11900m,
            MetodoPago = Pos.Models.MetodoPagoVenta.TARJETA,
            Estado = Pos.Models.EstadoVenta.PENDIENTE
        };
        var repository = new RecordingVentaRepository(venta);
        var service = new Pos.Services.VentaService(repository);
        var intento = new Pos.Models.IntentoPago
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            VentaId = venta.Id,
            IdempotencyKey = Guid.NewGuid().ToString(),
            Estado = Pos.Models.EstadoIntentoPago.APROBADO,
            Monto = venta.Total,
            Moneda = "CLP",
            Referencia = "REF-S2-040",
            AuthorizationCode = "AUTH-S2-040"
        };

        await service.CompletarTarjetaAsync(venta.Id, intento);

        Assert.NotNull(repository.Pago);
        Assert.Equal(Pos.Models.MetodoPago.TARJETA, repository.Pago!.Metodo);
        Assert.Equal(intento.Referencia, repository.Pago.Referencia);
        Assert.Same(intento, repository.Intento);
        Assert.Equal(Pos.Models.OutboxStatus.PENDIENTE, repository.Outbox!.Status);
        var evento = JsonSerializer.Deserialize<Pos.Messaging.SaleCompletedEvent>(repository.Outbox.Payload);
        Assert.NotNull(evento);
        Assert.Equal(venta.Id, evento!.VentaId);
        Assert.Equal("TARJETA", evento.MetodoPago);
    }

    private sealed class RecordingVentaRepository(Pos.Models.Venta venta)
        : Pos.Repositories.IVentaRepository
    {
        public Pos.Models.Pago? Pago { get; private set; }
        public Pos.Models.IntentoPago? Intento { get; private set; }
        public Pos.Models.OutboxMessage? Outbox { get; private set; }

        public Task<Pos.Models.Venta?> GetByIdAsync(Guid ventaId) =>
            Task.FromResult<Pos.Models.Venta?>(venta.Id == ventaId ? venta : null);

        public Task<Pos.Models.Venta> CompletarTarjetaConOutboxAsync(
            Guid ventaId,
            Pos.Models.Pago pago,
            Pos.Models.IntentoPago intentoPago,
            Pos.Models.OutboxMessage outboxMessage)
        {
            Pago = pago;
            Intento = intentoPago;
            Outbox = outboxMessage;
            venta.Estado = Pos.Models.EstadoVenta.COMPLETADA;
            return Task.FromResult(venta);
        }

        public Task<IReadOnlyList<Pos.Models.Venta>> GetByTurnoAsync(Guid turnoId) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<Pos.Models.Venta>> GetByCajeroAsync(Guid cajeroId, DateTime desde, DateTime hasta) =>
            throw new NotSupportedException();
        public Task<Pos.Models.Venta> CrearAsync(Pos.Models.Venta nuevaVenta) =>
            throw new NotSupportedException();
        public Task<Pos.Models.Venta> ActualizarAsync(Pos.Models.Venta actualizada) =>
            throw new NotSupportedException();
        public Task<Pos.Models.Venta> CambiarMetodoPagoPendienteAsync(Guid ventaId, Pos.Models.MetodoPagoVenta metodoPago) =>
            throw new NotSupportedException();
        public Task<Pos.Models.Venta> CompletarConOutboxAsync(
            Guid ventaId,
            Pos.Models.Pago pago,
            Pos.Models.OutboxMessage outboxMessage) => throw new NotSupportedException();
        public Task RegistrarIntentoPagoAsync(Pos.Models.IntentoPago intentoPago) =>
            throw new NotSupportedException();
        public Task<Pos.Models.IntentoPago> IniciarIntentoPagoAsync(Guid ventaId, Guid requestId) =>
            throw new NotSupportedException();
        public Task<Pos.Models.IntentoPago?> GetUltimoIntentoPagoAsync(Guid ventaId) =>
            throw new NotSupportedException();
        public Task ActualizarIntentoPagoAsync(Pos.Models.IntentoPago intentoPago) =>
            throw new NotSupportedException();
    }
}

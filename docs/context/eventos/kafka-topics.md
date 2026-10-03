---
id: kafka-topics
tipo: evento
titulo: Kafka Topics — Diseño vs Implementación Real
estado: parcial
fuentes: [src/POSCartService/Services/KafkaProducerService.cs, src/POSCartService/Messaging/SaleCompletedEvent.cs, src/WarehouseInventoryService/Messaging/KafkaConsumerService.cs, Docker/docker-compose.yml, Docker/scripts/init-kafka-topics.sh]
verificado_contra_codigo: true
ultima_revision: 2026-10-02
depende_de: [ms5-pos, ms4-inventory]
publica: [sale.completed]
consume: [sale.completed]
diseno_publica: [sale.completed, sale.reversed, stock.alert, expiry.alert, stock.updated, purchase.received, fx.rate.updated, points.updated]
diseno_consume: [sale.completed, sale.reversed, stock.alert, expiry.alert, stock.updated, purchase.received, fx.rate.updated, points.updated]
reglas: [RN-04, RF-09]
---
# Kafka Topics — Diseño vs Implementación Real

> Contraste entre los topics diseñados en el ContextMaster y lo que realmente existe en el código. **Estado:** 1 consumer (MS-4) y 1 producer (MS-5) implementados para `sale.completed`.

## Topics aprovisionados (docker-compose init)

El `kafka-init-topics` del docker-compose crea estos topics al iniciar:

```
sale.completed          stock.alert           expiry.alert
sale.reversed           stock.updated         purchase.received
fx.rate.updated         points.updated        tenant.created
tenant.updated          user.registered       product.created
product.price_updated   shift.closed          stock.low
product.expiring_soon   purchase_order.received
```

**Nota:** `stock.low` y `product.expiring_soon` son aliases de `stock.alert` y `expiry.alert` (topics duplicados en init).

## Estado real de cada topic

| Topic | Productor real | Consumidor real | Estado |
|-------|---------------|-----------------|--------|
| `sale.completed` | ✅ MS-5 KafkaProducerService | ✅ MS-4 KafkaConsumerService | [IMPLEMENTADO] |
| `stock.alert` | ❌ NINGUNO | ❌ NINGUNO | [PLANIFICADO] |
| `expiry.alert` | ❌ NINGUNO | ❌ NINGUNO | [PLANIFICADO] |
| `sale.reversed` | ❌ NINGUNO | ❌ NINGUNO | [PLANIFICADO] |
| `stock.updated` | ❌ NINGUNO | ❌ NINGUNO | [PLANIFICADO] |
| `purchase.received` | ❌ NINGUNO | ❌ NINGUNO | [PLANIFICADO] |
| `fx.rate.updated` | ❌ NINGUNO | ❌ NINGUNO | [PLANIFICADO] |
| `points.updated` | ❌ NINGUNO | ❌ NINGUNO | [PLANIFICADO] |
| `tenant.created` | ❌ NINGUNO | ❌ NINGUNO | [PLANIFICADO] |
| `product.created` | ❌ NINGUNO | ❌ NINGUNO | [PLANIFICADO] |

## Payload real de sale.completed (SaleCompletedEvent)

```csharp
// src/POSCartService/Messaging/SaleCompletedEvent.cs  [IMPLEMENTADO]
public sealed class SaleCompletedEvent
{
    public Guid     EventId    { get; init; }  // GUID nuevo por cada publicación
    public Guid     TenantId   { get; init; }
    public Guid     VentaId    { get; init; }  // también clave de idempotencia en MS-4
    public Guid     CajeroId   { get; init; }
    public Guid     SucursalId { get; init; }
    public List<SaleCompletedItemEvent> Items { get; init; }
    public decimal  Subtotal   { get; init; }
    public decimal  Iva        { get; init; }  // = Venta.Impuestos
    public decimal  Total      { get; init; }
    public string   MetodoPago { get; init; }  // "EFECTIVO" | "TARJETA" | "MIXTO"
    public DateTime Timestamp  { get; init; }
}

public sealed class SaleCompletedItemEvent
{
    public Guid    ProductoId     { get; init; }
    public decimal Cantidad       { get; init; }
    public decimal PrecioUnitario { get; init; }
}
```

**Clave Kafka:** `tenant_id` (garantiza orden de eventos por tenant en la misma partición).  
**Idempotencia MS-4:** el consumer usa `EventId` (GUID único generado por MS-5) con fallback a `VentaId` en `EventosKafkaProcesados`.

## Diseño del ContextMaster (9 topics)

El ContextMaster documenta 9 topics. El código y docker-compose tienen 17 topics definidos (algunos adicionales al diseño original).

## Flujo diseñado (cuándo esté implementado)

```
MS-5 POS → sale.completed → MS-4 Warehouse (descuenta stock ✅)
                          → MS-2 Tax (registra hecho imponible ❌)
                          → MS-8 Loyalty (acumula puntos ❌)
                          → MS-7 Analytics (actualiza KPIs ❌)

MS-4 Warehouse → stock.alert → MS-7 Analytics → SMS/Email/Push ❌
               → expiry.alert → MS-7 Analytics ❌
               → stock.updated → MS-7 Analytics ❌

MS-6 Supply → purchase.received → MS-4 Warehouse (recepción FEFO) ❌

MS-1 Identity → fx.rate.updated → MS-3 Catalog, MS-6 Supply, MS-7 Analytics ❌

MS-8 Loyalty → points.updated → MS-7 Analytics ❌
```

## Publicación implementada en MS-5

MS-5 publica `sale.completed` mediante `IKafkaProducerService` (Singleton) al completar la venta en `VentaService.CompletarAsync`:

```csharp
// MS-5 (VentaService.CompletarAsync):
var evento = new SaleCompletedEvent
{
    EventId    = Guid.NewGuid(), // GUID único para idempotencia
    TenantId   = ventaCompletada.TenantId,
    VentaId    = ventaCompletada.Id,
    CajeroId   = ventaCompletada.CajeroId,
    SucursalId = ventaCompletada.SucursalId,
    Subtotal   = ventaCompletada.Subtotal,
    Iva        = ventaCompletada.Impuestos,
    Total      = ventaCompletada.Total,
    MetodoPago = ventaCompletada.MetodoPago.ToString(),
    Timestamp  = DateTime.UtcNow,
    Items      = itemsParaEvento.Select(i => new SaleCompletedItemEvent { ... }).ToList()
};
await _kafkaProducer.PublicarSaleCompletedAsync(evento);
```

## Cobertura de integración

La publicación de `sale.completed` está cubierta mediante un test de integración que usa el `KafkaProducerService` real de MS-5 y un `ConsumerBuilder<string, string>` conectado al broker local. El payload consumido se deserializa y valida contra la venta cobrada, incluyendo `VentaId`, `TenantId` y `Total`.

## Conexiones
- Produce → [[ms5-pos]] [IMPLEMENTADO]
- Consume → [[ms4-inventory]] [IMPLEMENTADO]
- Arquitectura: [[arquitectura]]
- Decisión de Kafka: [[001-kafka-vs-rabbitmq]]

## Fuentes
- `src/WarehouseInventoryService/Messaging/KafkaConsumerService.cs`
- `src/WarehouseInventoryService/Messaging/SaleCompletedEvent.cs`
- `Docker/docker-compose.yml` (kafka-init-topics)
- `Docker/scripts/init-kafka-topics.sh`
- `tests/GlobalMart.IntegrationTests/KafkaPublishIntegrationTests.cs`

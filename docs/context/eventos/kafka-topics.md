---
id: kafka-topics
tipo: evento
titulo: Kafka Topics — Diseño vs Implementación Real
estado: parcial
fuentes: [src/POSCartService/Services/KafkaProducerService.cs, src/POSCartService/Messaging/SaleCompletedEvent.cs, src/WarehouseInventoryService/Messaging/KafkaConsumerService.cs, Docker/docker-compose.yml, Docker/scripts/init-kafka-topics.sh]
verificado_contra_codigo: true
ultima_revision: 2026-10-15
depende_de: [ms5-pos, ms4-inventory]
publica: [sale.completed]
consume: [sale.completed]
diseno_publica: [sale.completed, sale.reversed, stock.alert, expiry.alert, stock.updated, purchase.received, fx.rate.updated, points.updated]
diseno_consume: [sale.completed, sale.reversed, stock.alert, expiry.alert, stock.updated, purchase.received, fx.rate.updated, points.updated]
reglas: [RN-04, RF-09]
---
# Kafka Topics — Diseño vs Implementación Real

> Contraste entre los topics diseñados en el ContextMaster y lo que realmente existe en el código. **Estado:** MS-4 consume `sale.completed`; MS-5 conserva el adaptador productor y persiste eventos pendientes, pero el worker que los publica aún no está implementado.

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
| `sale.completed` | ⚠️ MS-5 guarda outbox; worker pendiente | ✅ MS-4 KafkaConsumerService | [PARCIAL] |
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
    public Guid     EventId    { get; init; }  // identidad estable del evento lógico
    public int      SchemaVersion { get; init; } = 1;
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
**Idempotencia MS-4:** el consumer usa `EventId` (GUID estable generado y persistido por MS-5) con fallback a `VentaId` en `EventosKafkaProcesados`.

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

## Persistencia previa a publicación en MS-5

MS-5 prepara `sale.completed` al completar la venta y conserva el payload para su publicación posterior:

```csharp
// MS-5 (VentaService.CompletarAsync):
var evento = new SaleCompletedEvent
{
    EventId = eventId,
    SchemaVersion = 1,
    TenantId = venta.TenantId,
    VentaId = venta.Id,
    // snapshot de venta e items
};
await _ventaRepository.CompletarConOutboxAsync(venta.Id, pago, outboxMessage);
```

## Cobertura de integración

La persistencia de `sale.completed` está cubierta con PostgreSQL real: el cobro deja venta, pago y outbox pendiente en un solo commit, y un fallo al insertar el outbox revierte los tres cambios. La entrega automática al broker queda [PLANIFICADA] hasta incorporar el worker.

El consumo de `sale.completed` por MS-4 también está cubierto mediante un test de integración que publica un evento real, espera el procesamiento asíncrono y verifica el descuento de stock a través de la API de inventario.

## Conexiones
- Produce → [[ms5-pos]] [PARCIAL — outbox implementado, worker pendiente]
- Consume → [[ms4-inventory]] [IMPLEMENTADO]
- Arquitectura: [[arquitectura]]
- Decisión de Kafka: [[001-kafka-vs-rabbitmq]]

## Fuentes
- `src/WarehouseInventoryService/Messaging/KafkaConsumerService.cs`
- `src/WarehouseInventoryService/Messaging/SaleCompletedEvent.cs`
- `Docker/docker-compose.yml` (kafka-init-topics)
- `Docker/scripts/init-kafka-topics.sh`
- `tests/GlobalMart.IntegrationTests/KafkaPublishIntegrationTests.cs`

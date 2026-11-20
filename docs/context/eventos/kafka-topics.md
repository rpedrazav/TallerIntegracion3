---
id: kafka-topics
tipo: evento
titulo: Kafka Topics — Diseño vs Implementación Real
estado: parcial
fuentes: [src/POSCartService/Services/KafkaProducerService.cs, src/POSCartService/Services/OutboxPublisherWorker.cs, src/POSCartService/Messaging/SaleCompletedEvent.cs, src/WarehouseInventoryService/Messaging/KafkaConsumerService.cs, src/WarehouseInventoryService/Repositories/StockRepository.cs, tests/GlobalMart.IntegrationTests/OutboxBrokerRecoveryIntegrationTests.cs, tests/GlobalMart.Tests/MS4_Warehouse/SaleEventProcessor_MovimientoStockTests.cs, Docker/docker-compose.yml, Docker/scripts/init-kafka-topics.sh]
verificado_contra_codigo: true
ultima_revision: 2026-11-20
depende_de: [ms5-pos, ms4-inventory]
publica: [sale.completed, sale.completed.error, sale.reversed]
consume: [sale.completed]
diseno_publica: [sale.completed, sale.reversed, stock.alert, expiry.alert, stock.updated, purchase.received, fx.rate.updated, points.updated]
diseno_consume: [sale.completed, sale.reversed, stock.alert, expiry.alert, stock.updated, purchase.received, fx.rate.updated, points.updated]
reglas: [RN-04, RN-05, RF-09]
---
# Kafka Topics — Diseño vs Implementación Real

> Contraste entre los topics diseñados en el ContextMaster y lo que realmente existe en el código. **Estado:** MS-5 publica `sale.completed` y `sale.reversed` desde outbox; MS-4 consume `sale.completed` y envía errores permanentes a `sale.completed.error`.

## Topics aprovisionados (docker-compose init)

El `kafka-init-topics` del docker-compose crea estos topics al iniciar:

```
sale.completed          sale.completed.error  stock.alert
expiry.alert            sale.reversed         stock.updated
purchase.received       fx.rate.updated       points.updated
tenant.created          tenant.updated        user.registered
product.created         product.price_updated shift.closed
stock.low               product.expiring_soon purchase_order.received
```

**Nota:** `stock.low` y `product.expiring_soon` son aliases de `stock.alert` y `expiry.alert` (topics duplicados en init).

## Estado real de cada topic

| Topic | Productor real | Consumidor real | Estado |
|-------|---------------|-----------------|--------|
| `sale.completed` | ✅ MS-5 OutboxPublisherWorker | ✅ MS-4 KafkaConsumerService | [IMPLEMENTADO] |
| `sale.completed.error` | ✅ MS-4 KafkaConsumerService | ❌ NINGUNO | [IMPLEMENTADO S2-096; destino de error] |
| `stock.alert` | ❌ NINGUNO | ❌ NINGUNO | [PLANIFICADO] |
| `expiry.alert` | ❌ NINGUNO | ❌ NINGUNO | [PLANIFICADO] |
| `sale.reversed` | ✅ MS-5 OutboxPublisherWorker | ❌ NINGUNO | [PRODUCTOR IMPLEMENTADO S2-094; consumidor pendiente] |
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

El ContextMaster documenta 9 topics. El código y docker-compose tienen 18 topics definidos (algunos adicionales al diseño original).

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

La persistencia y publicación de `sale.completed` están cubiertas con PostgreSQL real y un productor controlado: el cobro deja venta, pago y outbox pendiente en un solo commit; el worker conserva identidad y payload entre intentos, reprograma un fallo transitorio y marca `PUBLICADO` tras el éxito. S2-010 agrega el escenario completo de indisponibilidad y recuperación del productor, con una única entrega lógica del JSON persistido. La caída de un proceso Kafka real permanece [NO VERIFICADO].

El consumo de `sale.completed` por MS-4 también está cubierto mediante un test de integración que publica un evento real, espera el procesamiento asíncrono y verifica el descuento de stock a través de la API de inventario.

S2-096 verificó localmente `sale.completed.error` con Kafka real y la retención de offset ante caída de PostgreSQL: offset 1/log-end 2 durante el fallo, offset 2/log-end 2 tras recuperación. En el clúster permanece [NO VERIFICADO] por ausencia de broker.

S2-097 agrupa en un único `SaveChanges` los descuentos, movimientos y la idempotencia de cada `sale.completed`. La prueba dirigida revierte lógicamente el primer ítem cuando falla el segundo y deja el evento disponible para el reintento de S2-096; PostgreSQL y Kafka reales para este escenario permanecen [NO VERIFICADOS].

S2-098 clasifica stock inexistente o insuficiente como `TransientFailure`: no confirma el offset ni envía el evento a la cola de error, porque una reposición permite recuperarlo. No se persisten cantidades negativas, movimientos ni idempotencia mientras falte stock.

S2-099 restringe el commit a procesamiento válido, duplicado o poison message entregado al destino de error. Fallos transitorios, excepciones y estados desconocidos hacen `seek` al mismo offset y se registran para reproceso; las confirmaciones también quedan registradas con el resultado.

S2-100 verificó con Kafka y PostgreSQL locales reales que dos mensajes con el mismo `EventId` producen un solo descuento y que un fallo en el segundo ítem no deja cambios parciales. Al recuperar stock, el evento pendiente se reprocesa completo. Resultado: 2/2 integraciones aprobadas; clúster remoto [NO VERIFICADO].

El consumidor aplica FEFO dentro de esa misma unidad de trabajo: descuenta `Lote.Cantidad` por `FechaVencimiento ASC, Id ASC` antes de actualizar el stock agregado. Una prueba dirigida con dos lotes verifica el orden y la persistencia del evento; Kafka y PostgreSQL reales para FEFO [NO VERIFICADOS].

## Conexiones
- Produce → [[ms5-pos]] [IMPLEMENTADO]
- Consume → [[ms4-inventory]] [IMPLEMENTADO]
- Arquitectura: [[arquitectura]]
- Decisión de Kafka: [[001-kafka-vs-rabbitmq]]

## Fuentes
- `src/WarehouseInventoryService/Messaging/KafkaConsumerService.cs`
- `src/WarehouseInventoryService/Messaging/SaleCompletedEvent.cs`
- `src/WarehouseInventoryService/Repositories/StockRepository.cs`
- `Docker/docker-compose.yml` (kafka-init-topics)
- `Docker/scripts/init-kafka-topics.sh`
- `tests/GlobalMart.Tests/MS4_Warehouse/SaleEventProcessor_MovimientoStockTests.cs`
- `tests/GlobalMart.IntegrationTests/KafkaPublishIntegrationTests.cs`
- `tests/GlobalMart.IntegrationTests/OutboxBrokerRecoveryIntegrationTests.cs`

## S2-094 — Anulación total

[Payload y contrato de sale.reversed](sale-reversed-outbox.md): evento y auditoría guardados con venta ANULADA, EventId = Anulacion.Id, vínculo por venta_id. Los reintentos conservan JSON e identidad. En tarjeta solo se registra ante reversa aprobada. No garantiza entrega exactamente una vez ni orden entre topics.

---
id: kafka-topics
tipo: evento
titulo: Kafka Topics — Diseño vs Implementación Real
estado: parcial
fuentes: [src/WarehouseInventoryService/Messaging/KafkaConsumerService.cs, Docker/docker-compose.yml, Docker/scripts/init-kafka-topics.sh]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: [ms5-pos, ms4-inventory]
publica: []
consume: []
reglas: [RN-04, RF-09]
---
# Kafka Topics — Diseño vs Implementación Real

> Contraste entre los topics diseñados en el ContextMaster y lo que realmente existe en el código. **Estado crítico:** Solo 1 consumer implementado, 0 producers implementados.

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
| `sale.completed` | ❌ NINGUNO | ✅ MS-4 KafkaConsumerService | [PARCIAL — consumer sin producer] |
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
// src/WarehouseInventoryService/Messaging/SaleCompletedEvent.cs
public class SaleCompletedEvent
{
    public Guid VentaId    { get; set; }  // también usado como EventId de idempotencia
    public Guid TenantId   { get; set; }
    public Guid SucursalId { get; set; }
    public List<SaleItemEvent> Items { get; set; }
}

public class SaleItemEvent
{
    public Guid    ProductoId { get; set; }
    public decimal Cantidad   { get; set; }
}
```

**Nota:** No hay `EventId` explícito — se usa `VentaId` como identificador de idempotencia.

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

## Acción requerida (crítica)

Para que la cadena funcione, MS-5 debe publicar `sale.completed` al completar una venta:
```csharp
// En VentaService.CompletarAsync, agregar después de ActualizarAsync:
await _kafkaProducer.ProduceAsync("sale.completed", new Message<string,string> {
    Key = venta.TenantId.ToString(),
    Value = JsonSerializer.Serialize(new {
        VentaId    = venta.Id,
        TenantId   = venta.TenantId,
        SucursalId = venta.SucursalId,
        Items      = venta.Items.Select(i => new { i.ProductoId, i.Cantidad })
    })
});
```

## Conexiones
- Produce → [[ms5-pos]] (cuando se implemente)
- Consume → [[ms4-inventory]]
- Arquitectura: [[arquitectura]]
- Decisión de Kafka: [[001-kafka-vs-rabbitmq]]

## Fuentes
- `src/WarehouseInventoryService/Messaging/KafkaConsumerService.cs`
- `src/WarehouseInventoryService/Messaging/SaleCompletedEvent.cs`
- `Docker/docker-compose.yml` (kafka-init-topics)
- `Docker/scripts/init-kafka-topics.sh`

---
id: ms4-inventory
tipo: microservicio
titulo: MS-4 · Warehouse & Inventory Service
estado: parcial
fuentes: [src/WarehouseInventoryService/]
verificado_contra_codigo: true
ultima_revision: 2026-10-02
depende_de: [ms5-pos]
publica: []
consume: [sale.completed]
reglas: [RN-04, RN-05, RF-09, RF-10, WI-04, WI-05, WI-13, WI-14]
---
# MS-4 · Warehouse & Inventory Service

> Gestiona el stock de productos por sucursal. Cuenta con consumidor Kafka de `sale.completed` con garantía de idempotencia (tabla EventosKafkaProcesados), log estructurado de movimientos (TI3-253), manejo de caso borde con `CrearYDescontar` (TI3-254), endpoints HTTP finalizados (TI3-255) y seed de 30 productos (TI3-256).

## Estructura del proyecto

```
src/WarehouseInventoryService/
├── Controllers/  StockController.cs
├── Data/
│   ├── WarehouseDbContext.cs
│   ├── SeedData.cs               ← Seed de stock sincronizado con catálogo
│   └── Migrations/
│       ├── 20260913033948_InitialCreate_Warehouse.cs
│       └── 20260927195722_AddEventosKafkaProcesados.cs
├── Messaging/
│   ├── KafkaConsumerService.cs   ← IHostedService, suscrito a "sale.completed"
│   ├── SaleEventProcessor.cs     ← Procesador de eventos con idempotencia y auditoría
│   └── SaleCompletedEvent.cs     ← DTO del evento
├── Middleware/  TenantMiddleware.cs
├── Models/
│   ├── Stock.cs
│   ├── Lote.cs
│   ├── MovimientoStock.cs
│   └── EventoKafkaProcesado.cs   ← tabla para idempotencia
└── Repositories/  IStockRepository · StockRepository
```

## Endpoints reales

| Método | Ruta | Auth | Estado |
|--------|------|------|--------|
| GET | `/stock/{productId}?sucursal_id=` | JWT | [IMPLEMENTADO] |
| GET | `/stock?sucursal_id=` | JWT | [IMPLEMENTADO] |
| GET | `/health` | Público | [IMPLEMENTADO] |

### No implementados
- `POST /stock/ajuste` — ajuste manual [PLANIFICADO]
- `POST /recepciones` — recepción de mercancía [PLANIFICADO]
- `GET /lotes` — gestión de lotes y fechas de caducidad [PLANIFICADO]
- `POST /mermas` — registrar merma [PLANIFICADO]
- `POST /transferencias` — transferir stock entre sucursales [PLANIFICADO]
- `POST /conteos` — conteo físico [PLANIFICADO]

## Kafka Consumer (lo más importante del servicio)

```csharp
// KafkaConsumerService — IHostedService
Topic: "sale.completed"
GroupId: config["Kafka:GroupId"] ?? "warehouse-inventory-service"
AutoOffsetReset: Earliest
EnableAutoCommit: false  ← commit manual después de procesar

Flujo por mensaje:
1. Deserializa SaleCompletedEvent { VentaId, TenantId, SucursalId, Items[] }
2. Verifica idempotencia: ¿eventId (=VentaId) ya está en EventosKafkaProcesados?
   → Si sí: descarta (log Info) y hace commit
   → Si no: continúa
3. Setea context.CurrentTenantId = evento.TenantId (multi-tenant manual, no HTTP)
4. Por cada item: llama stockRepository.Descontar(productoId, cantidad, tenantId, sucursalId)
5. Si stock no encontrado: log Warning, continúa
6. Registra EventoKafkaProcesado { EventId=VentaId, ProcesadoAt=UtcNow }
7. context.SaveChanges()
8. consumer.Commit(result)

IMPORTANTE: Si ProcesarEvento lanza excepción → log Error + commit igualmente (no retry infinito)
```

**Problema crítico:** MS-5 **NO produce** `sale.completed`. El consumer está listo pero sin eventos entrantes.

## Modelo de datos

```
Stock
  id          GUID PK
  tenant_id   GUID (discriminador)
  producto_id GUID
  sucursal_id GUID
  cantidad    decimal
  stock_minimo decimal?

Lote
  id               GUID PK
  tenant_id        GUID
  producto_id      GUID
  sucursal_id      GUID
  cantidad         decimal
  fecha_vencimiento DateTime?
  fecha_recepcion  DateTime

MovimientoStock
  id          GUID PK
  tenant_id   GUID
  stock_id    GUID FK
  tipo        string ("venta"|"recepcion"|"ajuste"|"merma")
  cantidad    decimal
  creado_en   DateTime

EventoKafkaProcesado
  event_id     GUID PK   ← = VentaId del evento sale.completed
  procesado_at DateTime
```

## Multi-tenant

- En requests HTTP: TenantMiddleware extrae `tenant_id` del JWT
- En Kafka consumer: `context.CurrentTenantId = evento.TenantId` (no hay JWT en Kafka)

## Casos de uso cubiertos

| ID | Caso de Uso | Estado |
|----|-------------|--------|
| WI-01 | Consultar Stock Actual | [IMPLEMENTADO] |
| WI-13 | Procesar Evento sale.completed | [IMPLEMENTADO] (consumer listo) |
| WI-14 | Descontar Stock por Venta | [IMPLEMENTADO] (cuando llegan eventos) |
| WI-02 | Ajustar Stock Manualmente | [PLANIFICADO] |
| WI-03 | Registrar Merma | [PLANIFICADO] |
| WI-04 | Registrar Recepción de Mercancía | [PLANIFICADO] |
| WI-05 | Aplicar Regla FEFO | [PLANIFICADO] |
| WI-06 | Ingresar Fecha Caducidad | [PLANIFICADO] |
| WI-07 | Asignar Lote a Nevera | [PLANIFICADO] |
| WI-08 | Controlar Volumen Nevera | [PLANIFICADO] |
| WI-09 | Generar Alerta Stock Mínimo | [PLANIFICADO] |
| WI-10 | Generar Alerta Caducidad | [PLANIFICADO] |
| WI-11 | Conteo Físico | [PLANIFICADO] |
| WI-12 | Conciliar Físico vs Sistema | [PLANIFICADO] |
| WI-15 | Transferir Stock entre Sucursales | [PLANIFICADO] |

## Brechas críticas

| Brecha | Impacto |
|--------|---------|
| MS-5 no produce sale.completed | WI-13/14 sin entrada real |
| Sin FEFO en selección de lotes | RN-05 no cumplido |
| Sin recepciones implementadas | RF-10 no implementado |
| Sin alertas Kafka | stock.alert/expiry.alert no se generan |
| Sin comentario en código sobre FEFO | "No implementa selección de lote por FEFO (ver ticket separado)" |

## Conexiones
- Consume: `sale.completed` ← [[ms5-pos]] (cuando se implemente)
- Produciría: `stock.alert`, `expiry.alert`, `stock.updated` → [[ms7-analytics]] (PLANIFICADO)
- Reglas: [[fefo]] (RN-05), [[reglas-negocio]] (RN-04)
- Eventos: [[kafka-topics]]

## Fuentes
- `src/WarehouseInventoryService/Messaging/KafkaConsumerService.cs`
- `src/WarehouseInventoryService/Controllers/StockController.cs`
- `src/WarehouseInventoryService/Data/Migrations/20260927195722_AddEventosKafkaProcesados.cs`

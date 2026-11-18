---
id: ms4-inventory
tipo: microservicio
titulo: MS-4 · Warehouse & Inventory Service
estado: parcial
fuentes: [src/WarehouseInventoryService/, tests/GlobalMart.Tests/MS4_Warehouse/, Docker/docker-compose.yml]
verificado_contra_codigo: true
ultima_revision: 2026-11-18
depende_de: [ms5-pos]
publica: [sale.completed.error]
consume: [sale.completed]
reglas: [RN-04, RN-05, RF-09, RF-10, WI-04, WI-05, WI-13, WI-14]
---
# MS-4 · Warehouse & Inventory Service

## Administración de stock (2026-10-27)

`GET /stock?sucursal_id=` lista existencias del tenant. `POST /stock/ajuste` exige ADMIN o REPONEDOR, valida cantidad no negativa y cuatro decimales, comprueba producto en MS-3 y sucursal activa en MS-1 con el JWT original, y registra el delta en `MovimientoStock`. `GET /health` comprueba disponibilidad HTTP. JWT usa issuer/audience/firma compatibles con MS-1. Pruebas de integración PostgreSQL de ajuste y aislamiento de tenant aprobadas. En `student-rpedraza`, MS-4 está desplegado y probó ajuste/listado remoto, 401 sin JWT, 403 para CAJERO y 400 para cantidad negativa. `warehouse_db` es una base independiente en el PostgreSQL existente del POS porque el clúster agotó su cuota global de PVC y la cuota de servicios del namespace. Allí no hay broker Kafka: `Kafka__Enabled=false` desactiva el consumidor y la sincronización de stock por eventos queda [NO VERIFICADO].

> Gestiona el stock de productos por sucursal. La recepción atómica y sus validaciones están implementadas; descuento FEFO, mermas y alertas siguen pendientes.

## Estructura del proyecto

```
src/WarehouseInventoryService/
├── Controllers/
│   ├── RecepcionesController.cs
│   ├── StockController.cs
│   └── SeedController.cs            ← POST /seed (solo Development)
├── Data/
│   ├── WarehouseDbContext.cs
│   ├── SeedData.cs               ← Seed de stock sincronizado con catálogo
│   └── Migrations/
│       ├── 20260913033948_InitialCreate_Warehouse.cs
│       ├── 20260927195722_AddEventosKafkaProcesados.cs
│       └── 20261118181528_AddSucursalToLote.cs
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
├── Repositories/  IStockRepository · StockRepository
└── Services/  RecepcionService · RecepcionValidator
```

## Endpoints reales

| Método | Ruta | Auth | Estado |
|--------|------|------|--------|
| GET | `/stock/{productId}?sucursal_id=` | JWT | [IMPLEMENTADO] |
| GET | `/stock?sucursal_id=` | JWT | [IMPLEMENTADO] |
| POST | `/stock/ajuste` | JWT + ADMIN/REPONEDOR | [IMPLEMENTADO] |
| POST | `/recepciones` | JWT + ADMIN/REPONEDOR | [IMPLEMENTADO; PostgreSQL 3/3 EN RAMA `local`] |
| POST | `/seed` | JWT + ADMIN del tenant demo (solo Development) | [IMPLEMENTADO] TI3-258 |
| GET | `/health` | Público | [IMPLEMENTADO] |

### No implementados
- `GET /lotes?porVencer=&sucursal_id=` — [CONTRATO S3-002](../dominio/fefo.md); implementación pendiente
- `POST /mermas` — registrar merma [PLANIFICADO]
- `POST /transferencias` — transferir stock entre sucursales [PLANIFICADO]
- `POST /conteos` — conteo físico [PLANIFICADO]

## Contrato de `POST /recepciones`

Registra mercadería recibida. No acepta `tenantId`: `TenantMiddleware` lo obtiene del claim `tenant_id`. Solo roles `ADMIN` y `REPONEDOR` pueden ejecutar la operación, y el JWT original se reenvía a MS-3 y MS-1 para comprobar que el producto activo y la sucursal activa son visibles para el mismo tenant.

Request:

```json
{
  "productoId": "11111111-1111-4111-8111-111111111111",
  "sucursalId": "22222222-2222-4222-8222-222222222222",
  "cantidad": 24.5,
  "fechaVencimiento": "2027-02-15",
  "ubicacion": "Bodega A-03"
}
```

| Regla | Resultado si no se cumple |
|---|---|
| IDs no vacíos; cantidad mayor que cero, máximo `99999999` y hasta 4 decimales | 400 sin mutación |
| Vencimiento igual o posterior a la fecha UTC actual; ubicación opcional de hasta 100 caracteres | 400 sin mutación |
| Producto activo visible en MS-3 con el JWT recibido | 404 sin mutación |
| Sucursal activa incluida por MS-1 para el JWT recibido | 404 sin mutación |
| MS-1/MS-3 disponibles y con respuesta válida | 503 sin mutación |

HTTP 201 devuelve `id`, `productoId`, `sucursalId`, `cantidad`, `fechaVencimiento` y `ubicacion`; no expone `tenantId`. `RecepcionService` abre una transacción serializable, crea `Lote`, suma `Stock.CantidadActual` y agrega un `MovimientoStock` positivo de tipo `RECEPCION`. Un único `SaveChanges` precede al commit; cualquier fallo descarta las tres mutaciones.

Flujo backend: `RecepcionesController → RecepcionValidator → RecepcionService → WarehouseDbContext`. No existe todavía una pantalla frontend de recepciones.

**Límites:** no hay idempotencia de recepciones, por lo que repetir una solicitud válida vuelve a sumar stock. Este endpoint no descuenta cantidades de `Lote`, no elige lotes por vencimiento, no implementa `GET /lotes`, mermas, transferencias ni alertas Kafka. **FEFO permanece PLANIFICADO** hasta que la venta descuente lotes por `FechaVencimiento ASC`. Despliegue remoto [NO VERIFICADO]. Contrato ampliado: [[fefo]].

## Kafka Consumer (lo más importante del servicio)

```csharp
// KafkaConsumerService — IHostedService
Topic: "sale.completed"
GroupId: config["Kafka:GroupId"] ?? "warehouse-inventory-service"
AutoOffsetReset: Earliest
EnableAutoCommit: false  ← commit manual después de procesar

Política S2-096:
- Processed / Duplicate → commit.
- InvalidPayload / Unrecoverable → publica en "sale.completed.error"; commit solo tras entrega confirmada.
- TransientFailure, excepción o estado desconocido → seek al mismo offset, espera 1 segundo y reintenta sin commit.
```

El mensaje de error conserva topic, partición, offset, tipo, causa, payload original y timestamp. S2-099 hace explícita la lista de resultados confirmables y registra cada confirmación; ningún estado nuevo se confirma por defecto. Desde S2-097, el repositorio solo prepara los cambios y el procesador persiste todos los descuentos, movimientos y `EventId` mediante un único `SaveChanges`; si un ítem falla no queda ningún cambio parcial. S2-098 valida disponibilidad antes de descontar: stock inexistente o insuficiente devuelve `TransientFailure`, conserva el offset y nunca crea stock negativo.

## Test de integración

El flujo de integración publica un evento `sale.completed` real en Kafka, prepara stock y lote en `WarehouseDbContext`, y consulta `GET /stock/{productId}?sucursal_id=` con polling hasta verificar que `cantidad_actual` disminuye según la cantidad vendida.

S2-096 verificó además con Kafka y PostgreSQL locales reales que un payload inválido llega a `sale.completed.error`. Durante una caída de PostgreSQL, el grupo conservó offset 1 con log-end 2 y lag 1; después de recuperar la base avanzó a offset 2 y lag 0. S2-097 prueba que un fallo en el segundo ítem no persiste el primero, movimientos ni idempotencia. S2-098 prueba que una solicitud superior a la disponibilidad no modifica el stock ni registra el evento. S2-099 agrega el estado desconocido como caso seguro de reintento. S2-100 aprobó 2/2 integraciones con broker y PostgreSQL reales para duplicado y recuperación de fallo en el segundo ítem. S3-014 aplicó la migración de `SucursalId` sobre PostgreSQL limpio y preexistente sin alterar stock ni lotes. S3-015 verificó con PostgreSQL que una recepción crea lote, stock y movimiento juntos, y que un fallo forzado revierte todo. S3-016 verificó que cuatro tipos de entrada inválida se rechazan antes de persistir. En la rama `local`, recepciones aprobó 3/3 integraciones HTTP con PostgreSQL 16 y aislamiento entre tenants; MS-1/MS-3 fueron stubs tenant-aware. Pruebas unitarias dirigidas: 38/38 aprobadas. [Evidencia S2-100](../../qa/S2-100/reporte.md).

## Modelo de datos

```
Stock
  (producto_id, sucursal_id) PK
  tenant_id       GUID
  cantidad_actual decimal(12,4)
  stock_minimo    decimal(12,4)

Lote
  id                GUID PK
  tenant_id         GUID
  producto_id       GUID
  fecha_vencimiento DateOnly
  cantidad          decimal(12,4)
  ubicacion         string(100)
  sucursal_id       GUID

MovimientoStock
  id          GUID PK
  tenant_id   GUID
  producto_id GUID
  tipo        string(20)
  cantidad    decimal(12,4)
  motivo      string(255)
  timestamp   DateTime

EventoKafkaProcesado
  event_id     GUID PK   ← EventId; VentaId solo como fallback
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
| WI-02 | Ajustar Stock Manualmente | [IMPLEMENTADO EN CÓDIGO; integración remota NO VERIFICADA] |
| WI-03 | Registrar Merma | [PLANIFICADO] |
| WI-04 | Registrar Recepción de Mercancía | [IMPLEMENTADO; PostgreSQL 3/3 EN RAMA `local`] |
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
- `src/WarehouseInventoryService/Controllers/RecepcionesController.cs`
- `src/WarehouseInventoryService/Services/RecepcionService.cs`
- `src/WarehouseInventoryService/Services/RecepcionValidator.cs`
- `src/WarehouseInventoryService/Controllers/SeedController.cs`
- `src/WarehouseInventoryService/Data/SeedData.cs`
- `src/WarehouseInventoryService/Models/Lote.cs`
- `src/WarehouseInventoryService/Models/Stock.cs`
- `src/WarehouseInventoryService/Models/MovimientoStock.cs`
- `src/WarehouseInventoryService/Data/Migrations/20260927195722_AddEventosKafkaProcesados.cs`
- `src/WarehouseInventoryService/Data/Migrations/20261118181528_AddSucursalToLote.cs`
- `tests/GlobalMart.Tests/MS4_Warehouse/RecepcionService_Tests.cs`
- `tests/GlobalMart.Tests/MS4_Warehouse/RecepcionValidator_Tests.cs`

## Integración de ramas — 2026-11-01

El consumidor delega en `ISaleEventProcessor` para idempotencia y movimientos. Un stock inexistente o insuficiente queda pendiente de recuperación sin confirmar el offset. `POST /seed` requiere JWT, rol ADMIN, tenant demo y entorno Development; fuera de Development devuelve 404 con autenticación válida. Se elimina el mapeo alternativo de `/seed` y se unifica la configuración JWT. `GET /stock/{productId}` rechaza IDs vacíos con 400.

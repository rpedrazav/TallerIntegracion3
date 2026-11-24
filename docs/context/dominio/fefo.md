---
id: fefo
tipo: dominio
titulo: Contrato mínimo de recepciones y lotes FEFO
estado: implementado
fuentes: [src/WarehouseInventoryService/Controllers/RecepcionesController.cs, src/WarehouseInventoryService/Controllers/LotesController.cs, src/WarehouseInventoryService/Services/RecepcionService.cs, src/WarehouseInventoryService/Services/RecepcionValidator.cs, src/WarehouseInventoryService/Services/LoteService.cs, src/WarehouseInventoryService/Repositories/StockRepository.cs, src/WarehouseInventoryService/Models/Lote.cs, src/WarehouseInventoryService/Data/Migrations/20261118181528_AddSucursalToLote.cs, tests/GlobalMart.Tests/MS4_Warehouse/SaleEventProcessor_MovimientoStockTests.cs, tests/GlobalMart.Tests/MS4_Warehouse/LotesController_Tests.cs, tareas/plan_sprint3.xlsx]
verificado_contra_codigo: true
ultima_revision: 2026-11-24
depende_de: [ms4-inventory]
publica: []
consume: []
reglas: [RN-01, RN-05, RF-10, WI-04, WI-05]
---
# Contrato mínimo de recepciones y lotes FEFO — S3-002

## Estado y alcance

**IMPLEMENTADO EN CÓDIGO.** `POST /recepciones` registra la recepción atómica con validaciones; `GET /lotes?porVencer=&sucursal_id=` consulta lotes disponibles del tenant; y el consumidor de `sale.completed` descuenta por FEFO. Las integraciones de recepción están verificadas en `local`; el descuento FEFO de punta a punta (recepción → venta → Kafka → MS-4) se verificó con PostgreSQL y Kafka reales en `docker compose` ([S3-056](../../qa/S3-056/reporte.md), 2026-11-24).

El alcance reutiliza `Lote.Id`, `ProductoId`, `SucursalId`, `FechaVencimiento`, `Cantidad`, `Ubicacion` y `TenantId`. S3-014 agregó `SucursalId` obligatorio y su migración porque el stock se administra por sucursal. No se agrega `FechaRecepcion`: el contrato no la requiere y el modelo actual no la tiene.

Ambos endpoints exigen JWT válido y rol `ADMIN` o `REPONEDOR`. `tenantId` se obtiene exclusivamente del claim `tenant_id`; nunca se acepta en body o query.

## `POST /recepciones`

Registra un lote y aumenta el stock agregado del mismo producto y sucursal. Ruta canónica: `POST /recepciones`; no se define alias adicional.

### Request

```json
{
  "productoId": "11111111-1111-4111-8111-111111111111",
  "sucursalId": "22222222-2222-4222-8222-222222222222",
  "cantidad": 24.5,
  "fechaVencimiento": "2027-02-15",
  "ubicacion": "Bodega A-03"
}
```

| Campo | Regla |
|---|---|
| `productoId` | UUID no vacío; producto activo del tenant autenticado. |
| `sucursalId` | UUID no vacío; sucursal activa del mismo tenant. |
| `cantidad` | Mayor que 0, máximo `99999999`, hasta 4 decimales (`decimal(12,4)`). |
| `fechaVencimiento` | Fecha ISO `YYYY-MM-DD`, igual o posterior a la fecha actual; no incluye hora. |
| `ubicacion` | Opcional; se aplica `Trim`, máximo 100 caracteres; ausencia equivale a cadena vacía. |

El request no acepta `id`, `tenantId`, stock resultante, tipo de movimiento ni fechas generadas por el servidor. Propiedades desconocidas deben rechazarse con 400 para no ignorar accidentalmente datos fuera de alcance.

### Respuesta creada

HTTP 201:

```json
{
  "id": "33333333-3333-4333-8333-333333333333",
  "productoId": "11111111-1111-4111-8111-111111111111",
  "sucursalId": "22222222-2222-4222-8222-222222222222",
  "cantidad": 24.5,
  "fechaVencimiento": "2027-02-15",
  "ubicacion": "Bodega A-03"
}
```

`id` lo genera el servidor. `tenantId` no se expone. La implementación guarda en una sola transacción el lote, el incremento de `Stock.CantidadActual` y un `MovimientoStock` positivo de tipo `RECEPCION`; ante cualquier fallo no queda mutación parcial.

## Descuento FEFO al vender

Al consumir `sale.completed`, `StockRepository` selecciona lotes con cantidad disponible del mismo tenant, producto y sucursal. El orden estable es `FechaVencimiento ASC`, luego `Id ASC`; una venta agota el lote más próximo antes de descontar el siguiente. `SaleEventProcessor` persiste los cambios de lotes y stock agregado junto con movimientos y `EventId` mediante un único `SaveChanges`, por lo que un fallo intermedio no deja el primer lote descontado.

La prueba local con dos lotes de 5 unidades y una venta de 8 deja el lote temprano en 0, el tardío en 2 y el stock agregado en 2. Con Kafka y PostgreSQL reales (`scripts/e2e-fefo.mjs`): dos lotes de 5, venta de 3 → lote temprano 2, tardío 5, stock 7.

### Lotes insuficientes

Si el stock agregado no alcanza o la suma de `Lote.Cantidad` disponible es menor que la cantidad vendida, el evento se clasifica como `TransientFailure`. El consumidor no confirma el offset para permitir recuperación después de una recepción y registra un warning con producto, sucursal, cantidad solicitada y `EventId`. No se persisten descuentos parciales, movimientos ni el registro de idempotencia.

La prueba dirigida con stock agregado 10, lotes por 5 y solicitud de 6 conserva el stock y ambos lotes sin cambios. El evento no se envía a `sale.completed.error`, porque la falta de lotes es recuperable.

### Errores

| Estado | Condición | Mutación |
|---|---|---|
| 400 | JSON/campos inválidos, cantidad, fecha o ubicación fuera de regla | Ninguna |
| 404 | Producto o sucursal inexistente, inactiva o ajena al tenant | Ninguna |
| 401 | JWT ausente, inválido o sin `tenant_id` | Ninguna |
| 403 | Rol distinto de `ADMIN` o `REPONEDOR` | Ninguna |
| 503 | No fue posible validar producto o sucursal | Ninguna |
| 500 | Fallo inesperado de persistencia | Rollback completo; mensaje genérico |

Los errores de validación devuelven `{ "message": "..." }`, siguiendo MS-4. No revelan si una referencia pertenece a otro tenant.

## `GET /lotes?porVencer=`

Lista lotes disponibles de una sucursal que vencen dentro de un rango inclusivo. Ruta:

```http
GET /lotes?porVencer=30&sucursal_id=22222222-2222-4222-8222-222222222222
Authorization: Bearer <JWT>
```

| Parámetro | Regla |
|---|---|
| `porVencer` | Obligatorio, entero entre 0 y 365. `0` consulta los que vencen hoy. |
| `sucursal_id` | Obligatorio, UUID no vacío de una sucursal activa del tenant. |

Filtros obligatorios: `TenantId` del JWT, `SucursalId` solicitado, `Cantidad > 0` y `FechaVencimiento` entre la fecha actual y `fecha actual + porVencer`, ambas inclusive. Orden estable: `FechaVencimiento ASC`, luego `Id ASC`.

HTTP 200; una consulta sin resultados devuelve `[]`:

```json
[
  {
    "id": "33333333-3333-4333-8333-333333333333",
    "productoId": "11111111-1111-4111-8111-111111111111",
    "sucursalId": "22222222-2222-4222-8222-222222222222",
    "cantidad": 24.5,
    "fechaVencimiento": "2027-02-15",
    "ubicacion": "Bodega A-03"
  }
]
```

Parámetros inválidos devuelven 400; sucursal inexistente, inactiva o ajena devuelve 404; autenticación y roles usan 401/403. La respuesta nunca incluye lotes agotados, vencidos, de otro tenant o de otra sucursal.

## Flujos directos

```text
POST /recepciones → RecepcionesController → RecepcionValidator → RecepcionService → WarehouseDbContext
GET /lotes → LotesController → RecepcionValidator/LoteService → WarehouseDbContext
```

No se agregan capas ni repositorios alternativos. Controller valida identidad/forma; servicio aplica reglas y transacción; `WarehouseDbContext` persiste `Lote`, `Stock` y `MovimientoStock`.

## Fuera de alcance de S3-002

- Pantalla frontend de recepciones y lotes.
- Mermas, transferencias, conteos y capacidad de neveras.
- Publicar `stock.alert`, `expiry.alert`, `stock.updated` u otro evento Kafka.
- Idempotencia de recepciones o integración con proveedores/órdenes de compra.

## Conexiones

- Servicio: [[ms4-inventory]]
- Aislamiento: [[multi-tenant]]
- Reglas: [[reglas-negocio]] (RN-01, RN-05)

## Fuentes

- `src/WarehouseInventoryService/Controllers/RecepcionesController.cs`
- `src/WarehouseInventoryService/Controllers/LotesController.cs`
- `src/WarehouseInventoryService/Services/RecepcionService.cs`
- `src/WarehouseInventoryService/Services/RecepcionValidator.cs`
- `src/WarehouseInventoryService/Services/LoteService.cs`
- `src/WarehouseInventoryService/Models/Lote.cs`
- `src/WarehouseInventoryService/Models/Stock.cs`
- `src/WarehouseInventoryService/Models/MovimientoStock.cs`
- `src/WarehouseInventoryService/Repositories/StockRepository.cs`
- `src/WarehouseInventoryService/Data/Migrations/20260913033948_InitialCreate_Warehouse.cs`
- `src/WarehouseInventoryService/Data/Migrations/20261118181528_AddSucursalToLote.cs`
- `tests/GlobalMart.Tests/MS4_Warehouse/SaleEventProcessor_MovimientoStockTests.cs`
- `tests/GlobalMart.Tests/MS4_Warehouse/LotesController_Tests.cs`
- `tareas/plan_sprint3.xlsx` (S3-002)

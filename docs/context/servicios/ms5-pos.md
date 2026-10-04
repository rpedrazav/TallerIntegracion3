---
id: ms5-pos
tipo: microservicio
titulo: MS-5 · POS & Cart Service
estado: parcial
fuentes: [src/POSCartService/]
verificado_contra_codigo: true
ultima_revision: 2026-10-02
depende_de: [ms1-identity, ms2-tax, ms3-catalog]
publica: [sale.completed]
consume: []
reglas: [RN-02, RN-03, RN-06, RF-02, RF-03, RF-04, RF-05, RF-06, RF-07]
---
# MS-5 · POS & Cart Service

> Servicio central del punto de venta. Gestiona turnos de caja y el ciclo completo de una venta (carrito, ítems, cobro, anulación). Integrado con MS-2 (IVA) y MS-3 (productos), y publica `sale.completed` a Kafka. **FALTA:** integración con pasarela de pago y hardware.

## Estructura del proyecto

```
src/POSCartService/
├── Controllers/
│   ├── TurnosController.cs   POST abrir|activo|cerrar
│   └── VentasController.cs   CRUD ventas + gestión de ítems (cobro y anulación aún no expuestos)
├── Data/  PosCartDbContext.cs + Migrations/
├── Models/  Venta · ItemVenta · Turno · Pago · Anulacion
├── Repositories/  IVentaRepository · VentaRepository · ITurnoRepository · TurnoRepository · IItemVentaRepository · ItemVentaRepository
├── Services/
│   ├── VentaService.cs       ← lógica de negocio central + publicación sale.completed
│   ├── TurnoService.cs
│   ├── KafkaProducerService.cs ← publica eventos Kafka
│   ├── CatalogClient.cs      ← llama MS-3
│   └── TaxClient.cs          ← llama MS-2
├── Validators/  (4 validators FluentValidation)
├── Exceptions/  TurnoYaAbiertoException · ExternalServiceException
└── tests/
    ├── POSCartService.AgregarItemTest/
  ├── POSCartService.CobroCuadreTest/
    └── POSCartService.ManualTest/
```

## Endpoints reales

| Método | Ruta | Auth | Estado |
|--------|------|------|--------|
| POST | `/turnos/abrir` o `/api/turnos/abrir` | JWT | [IMPLEMENTADO] |
| GET | `/turnos/activo` o `/api/turnos/activo` | JWT | [IMPLEMENTADO] |
| POST | `/turnos/cerrar` o `/api/turnos/cerrar` | JWT | [IMPLEMENTADO] |
| POST | `/turnos/cuadre` | JWT | [IMPLEMENTADO — suma pagos EFECTIVO/COMPLETADAS; retorna efectivo_esperado, monto_declarado, diferencia] |
| POST | `/ventas` | JWT | [IMPLEMENTADO] |
| POST | `/ventas/{id}/items` | JWT | [IMPLEMENTADO] |
| PUT | `/ventas/{id}/items/{itemId}` | JWT | [IMPLEMENTADO] |
| DELETE | `/ventas/{id}/items/{itemId}` | JWT | [IMPLEMENTADO] |
| GET | `/ventas/{id}` | JWT | [IMPLEMENTADO] |
| GET | `/ventas/turno/{turnoId}` | JWT | [IMPLEMENTADO] |
| POST | `/ventas/{id}/cobrar` | JWT | [IMPLEMENTADO — valida monto_recibido ≥ total, calcula vuelto, persiste pago EFECTIVO y publica sale.completed] |
| POST | `/ventas/{id}/anular` | JWT | [PLANIFICADO en controller — implementado en VentaService.AnularAsync] |
| GET | `/health` | Público | [IMPLEMENTADO] |

## Flujo de venta (implementado)

```
1. POST /turnos/abrir  { sucursalId, montoFondoInicial }
   → Verifica que el cajero no tenga turno activo (TurnoYaAbiertoException → 409)
   → Crea Turno { cajeroId, tenantId, sucursalId, fondoInicial, estado=ABIERTO }

2. POST /ventas  { turnoId, items[], metodoPago }
   → Verifica turno abierto (RN-06)
   → Calcula subtotal, IVA via TaxClient→MS-2, total
   → Estado: PENDIENTE

3. POST /ventas/{id}/items  { productoId, cantidad, pesoKg? }
   → Llama CatalogClient→MS-3 para obtener nombre y precio del producto
   → Llama TaxClient→MS-2 para recalcular IVA con todos los ítems
   → Actualiza Venta.Subtotal / .Impuestos / .Total

4. POST /ventas/{id}/cobrar  { monto_recibido }
   → Verifica JWT y venta PENDIENTE
   → Valida monto_recibido ≥ venta.Total (→ 400 "Monto insuficiente" si no alcanza)
   → Calcula vuelto = monto_recibido − total
   → VentaService.CompletarAsync: estado → COMPLETADA + crea Pago { Metodo=EFECTIVO, Monto=monto_recibido, Vuelto=vuelto }
   → VentaRepository.ActualizarAsync persiste estado y Pago en una sola transacción
   → Retorna venta completada + monto_recibido + vuelto
   → ✅ Publica sale.completed a Kafka con event_id único (GUID)
   → ❌ NO integra pasarela de pago externa

5. POST /ventas/{id}/anular  { motivo }
   → VentaService.AnularAsync: estado → ANULADA
   → Crea Anulacion { ventaId, autorizadoPor, motivo }
   → ❌ NO solicita reembolso a pasarela
```

## Modelo de datos

```
Turno
  id               GUID PK
  tenant_id        GUID
  cajero_id        GUID
  sucursal_id      GUID
  fondo_inicial    decimal
  estado           enum (ABIERTO|CERRADO)
  abierto_en       DateTime
  cerrado_en       DateTime?

Venta
  id          GUID PK
  tenant_id   GUID
  turno_id    GUID FK
  cajero_id   GUID
  sucursal_id GUID
  subtotal    decimal
  impuestos   decimal
  total       decimal
  metodo_pago enum (EFECTIVO|TARJETA|MIXTO)
  estado      enum (PENDIENTE|COMPLETADA|ANULADA|CANCELADA)
  creada_en   DateTime

ItemVenta
  id              GUID PK
  venta_id        GUID FK
  producto_id     GUID
  nombre_producto string (snapshot del nombre al momento de la venta)
  precio_unitario decimal
  cantidad        decimal
  peso_kg         decimal?  (para productos a granel)
  subtotal        decimal

Anulacion
  id             GUID PK
  venta_id       GUID FK
  autorizado_por GUID (cajeroId)
  motivo         string
  creada_en      DateTime

Pago
  id             GUID PK
  venta_id       GUID FK
  metodo         string
  monto          decimal
  referencia     string? (código de pasarela)
```

## Multi-tenant

JWT claims requeridos:
- `cajero_id` (o `sub` o `NameIdentifier`) — identifica el cajero
- `tenant_id` — aislamiento de datos

TenantMiddleware inyecta `CurrentTenantId` en `PosCartDbContext`.

## Publicación Kafka: sale.completed [IMPLEMENTADO]

Al completar una venta en `VentaService.CompletarAsync`, se publica el evento `sale.completed` mediante `IKafkaProducerService` (Singleton).
El evento incluye `EventId = Guid.NewGuid()` como identificador único para garantizar idempotencia en el consumer (MS-4). La suite `POSCartService.CobroCuadreTest` verifica cobro, cuadre, persistencia y payload sin requerir un broker activo.

```csharp
// VentaService.CompletarAsync:
if (_kafkaProducer is not null)
{
    var itemsParaEvento = (ventaCompletada.Items != null && ventaCompletada.Items.Count > 0)
        ? ventaCompletada.Items
        : venta.Items;

    var evento = new SaleCompletedEvent
    {
        EventId    = Guid.NewGuid(),
        TenantId   = ventaCompletada.TenantId,
        VentaId    = ventaCompletada.Id,
        CajeroId   = ventaCompletada.CajeroId,
        SucursalId = ventaCompletada.SucursalId,
        Subtotal   = ventaCompletada.Subtotal,
        Iva        = ventaCompletada.Impuestos,
        Total      = ventaCompletada.Total,
        MetodoPago = ventaCompletada.MetodoPago.ToString(),
        Timestamp  = DateTime.UtcNow,
        Items      = itemsParaEvento.Select(i => new SaleCompletedItemEvent
        {
            ProductoId     = i.ProductoId,
            Cantidad       = i.Cantidad,
            PrecioUnitario = i.PrecioUnitario
        }).ToList()
    };

    try
    {
        await _kafkaProducer.PublicarSaleCompletedAsync(evento);
    }
    catch
    {
        // Fire-and-forget resiliente
    }
}
```

## Tests de integración

- `TurnoIntegrationTests`: abre un turno con JWT de cajero y verifica que se persiste en estado `ABIERTO`.
- `VentaIntegrationTests`: crea una venta con el primer producto, agrega el segundo y verifica subtotal, IVA y total.
- `CobroIntegrationTests`: cobra una venta en efectivo con un monto superior al total y verifica el vuelto, el pago y el estado `COMPLETADA`.
- `KafkaPublishIntegrationTests`: consume desde `sale.completed` y verifica el payload publicado por el productor real de MS-5.
- `CierreTurnoIntegrationTests`: prepara una venta completada en efectivo y verifica el cuadre, el efectivo esperado, el monto declarado y la diferencia.
- Los flujos de venta y cobro usan handlers HTTP en memoria para devolver respuestas deterministas de `CatalogClient` (MS-3) y `TaxClient` (MS-2), sin levantar esos microservicios.

## Casos de uso cubiertos

| ID | Caso de Uso | Estado |
|----|-------------|--------|
| PC-01 | Abrir Turno de Caja | [IMPLEMENTADO] |
| PC-02 | Registrar Fondo de Apertura | [IMPLEMENTADO] |
| PC-03 | Cerrar Turno de Caja | [IMPLEMENTADO] |
| PC-04 | Cuadre de Caja | [PARCIAL — sin declaración de billetes] |
| PC-05 | Declarar Billetes y Monedas | [PLANIFICADO] |
| PC-06 | Iniciar Nueva Venta (Carrito) | [IMPLEMENTADO] |
| PC-07 | Agregar Producto (escaneo) | [IMPLEMENTADO] |
| PC-08 | Agregar Producto (búsqueda) | [IMPLEMENTADO] |
| PC-09 | Agregar Producto a Granel | [PARCIAL — modelo soporta PesoKg] |
| PC-10 | Integrar Peso de Balanza | [PLANIFICADO] |
| PC-11 | Modificar Cantidad | [IMPLEMENTADO] |
| PC-12 | Eliminar Ítem del Carrito | [IMPLEMENTADO] |
| PC-13 | Descuento Manual | [PLANIFICADO] |
| PC-14 | Calcular Total con Impuestos | [IMPLEMENTADO] |
| PC-15 | Cobrar en Efectivo | [IMPLEMENTADO — endpoint expuesto, valida monto ≥ total, calcula vuelto y publica sale.completed a Kafka] |
| PC-16 | Calcular Vuelto | [PLANIFICADO] |
| PC-17 | Cobrar con Tarjeta | [PLANIFICADO] |
| PC-18 | Pago Mixto (efectivo + tarjeta) | [PLANIFICADO] |
| PC-19 | Emitir Recibo / Comprobante | [PLANIFICADO] |
| PC-20 | Anular Venta (Devolución) | [PARCIAL — implementado en VentaService.AnularAsync] |
| PC-21 | Devolución Parcial | [PLANIFICADO] |
| PC-22 | Poner Venta en Espera (Hold) | [PLANIFICADO] |
| PC-23 | Recuperar Venta de Espera | [PLANIFICADO] |
| PC-24 | Publicar sale.completed | [IMPLEMENTADO] |
| PC-25 | Publicar sale.reversed | [PLANIFICADO] |
| PC-26 | Leer Tarjeta en Terminal (NFC/chip/mag) | [PLANIFICADO] |
| PC-27 | Enviar Solicitud a Pasarela | [PLANIFICADO] |
| PC-28 | Recibir Respuesta de Pasarela | [PLANIFICADO] |
| PC-29 | Manejar Rechazo de Pago | [PLANIFICADO] |
| PC-30 | Solicitar Reembolso a Pasarela | [PLANIFICADO] |
| PC-31 | Confirmar Reembolso | [PLANIFICADO] |
| PC-32 | Abrir Cajón de Dinero | [PLANIFICADO] |
| PC-33 | Imprimir Recibo en Terminal | [PLANIFICADO] |
| PC-34 | Mostrar Total en Display Cliente | [PLANIFICADO] |
| PC-35 | Identificar Cliente Afiliado en POS | [PLANIFICADO] |
| PC-36 | Aplicar Beneficios de Membresía | [PLANIFICADO] |
| PC-37 | Acumular Puntos tras Venta | [PLANIFICADO] |

## Conexiones
- Depende de: [[ms2-tax]] (TaxClient), [[ms3-catalog]] (CatalogClient), [[ms1-identity]] (JWT)
- Publica: `sale.completed` → [[ms4-inventory]], [[ms7-analytics]], [[ms8-loyalty]] [IMPLEMENTADO]
- Reglas: [[reglas-negocio]] (RN-02, RN-06), [[multi-tenant]]
- Kafka: [[kafka-topics]]

## Fuentes
- `src/POSCartService/Controllers/VentasController.cs`
- `src/POSCartService/Controllers/TurnosController.cs`
- `src/POSCartService/Services/VentaService.cs`
- `src/POSCartService/Services/TurnoService.cs`
- `tests/GlobalMart.IntegrationTests/TurnoIntegrationTests.cs`
- `tests/GlobalMart.IntegrationTests/VentaIntegrationTests.cs`
- `tests/GlobalMart.IntegrationTests/KafkaPublishIntegrationTests.cs`

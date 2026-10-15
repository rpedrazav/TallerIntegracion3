---
id: ms5-pos
tipo: microservicio
titulo: MS-5 · POS & Cart Service
estado: parcial
fuentes: [src/POSCartService/]
verificado_contra_codigo: true
ultima_revision: 2026-10-15
depende_de: [ms1-identity, ms2-tax, ms3-catalog]
publica: [sale.completed]
consume: []
reglas: [RN-01, RN-02, RN-03, RN-06, RF-02, RF-03, RF-04, RF-05, RF-06, RF-07]
---
# MS-5 · POS & Cart Service

> Servicio central del punto de venta. Gestiona turnos de caja y el ciclo completo de una venta (carrito, ítems, cobro, anulación). Integrado con MS-2 (IVA) y MS-3 (productos). Persiste y publica `sale.completed` mediante outbox; falta la pasarela de pago y el hardware.

## Contrato Oficial de Creación de Venta (POST /ventas) — S2-001 / S2-002 / S2-003

> **Regla de Aceptación (S2-001 / S2-003):** Ningún precio o impuesto aportado por el renderer (cliente) se considera autoridad. Los precios se resuelven en MS-3 y los impuestos se calculan autoritativamente con MS-2 desde la creación inicial.

### Delimitación de Autoridad y Responsabilidades
| Campo | Origen / Aceptado | Fuente de Verdad / Cálculo |
|---|---|---|
| `producto_id` | **Cliente (Request)** | Obligatorio. UUID del producto. Validado contra MS-3 (activo y perteneciente al catálogo). |
| `cantidad` | **Cliente (Request)** | Obligatorio para venta unitaria ($> 0$, default 1). Si se provee `peso_kg`, MS-5 asigna `cantidad = peso_kg`. |
| `peso_kg` | **Cliente (Request)** | Opcional. Capturado desde balanza para productos a granel (`EsPesoVariable`). |
| `precio_unitario` | **MS-5 (Calculado)** | Resuelto autoritativamente desde MS-3 (`precioBase`). Si el cliente lo envía, MS-5 **lo ignora/sobreescribe**. |
| `nombre_producto` | **MS-5 (Calculado)** | Obtenido autoritativamente desde MS-3 al momento de la venta para snapshot histórico inmutable. |
| `subtotal` | **MS-5 (Calculado)** | $\text{precio\_unitario} \times \text{cantidad}$ calculado por MS-5. |
| `impuestos` (IVA) | **MS-5 (Calculado)** | Calculado autoritativamente vía MS-2 (`TaxComplianceService`) según la tasa del tenant (no cero). |
| `total` | **MS-5 (Calculado)** | $\text{subtotal} + \text{impuestos}$. |
| `sucursal_id` | **MS-5 (Determinado)** | Resuelto desde el `Turno` ABIERTO del cajero autenticado. El cliente no puede forzar otra sucursal. |
| `cajero_id` | **MS-5 (Determinado)** | Extraído directamente del JWT (`cajero_id` o `sub`). |
| `tenant_id` | **MS-5 (Determinado)** | Extraído directamente del claim `tenant_id` del JWT validado. |
| `metodo_pago` | **Cliente (Request)** | Obligatorio: `EFECTIVO`, `TARJETA` o `MIXTO`. |
| `estado` | **MS-5 (Determinado)** | Fijado en `PENDIENTE`. |

---

## Endpoints reales

| Método | Ruta | Auth | Estado |
|--------|------|------|--------|
| POST | `/turnos/abrir` o `/api/turnos/abrir` | JWT | [IMPLEMENTADO] |
| GET | `/turnos/activo` o `/api/turnos/activo` | JWT | [IMPLEMENTADO] |
| POST | `/turnos/cerrar` o `/api/turnos/cerrar` | JWT | [IMPLEMENTADO] |
| POST | `/turnos/cuadre` | JWT | [IMPLEMENTADO — suma pagos EFECTIVO/COMPLETADAS; retorna efectivo_esperado, monto_declarado, diferencia] |
| POST | `/ventas` | JWT | [IMPLEMENTADO — Contrato fijado S2-001] |
| POST | `/ventas/{id}/items` | JWT | [IMPLEMENTADO] |
| PUT | `/ventas/{id}/items/{itemId}` | JWT | [IMPLEMENTADO] |
| DELETE | `/ventas/{id}/items/{itemId}` | JWT | [IMPLEMENTADO] |
| GET | `/ventas/{id}` | JWT | [IMPLEMENTADO] |
| GET | `/ventas/turno/{turnoId}` | JWT | [IMPLEMENTADO] |
| POST | `/ventas/{id}/cobrar` | JWT | [IMPLEMENTADO — valida monto_recibido ≥ total, calcula vuelto y persiste venta, pago y sale.completed pendiente en una transacción] |
| POST | `/ventas/{id}/anular` | JWT | [PLANIFICADO en controller — implementado en VentaService.AnularAsync] |
| GET | `/health` | Público | [IMPLEMENTADO] |

## Flujo de venta (implementado)

```
1. POST /turnos/abrir  { sucursalId, montoFondoInicial }
   → Verifica que el cajero no tenga turno activo (TurnoYaAbiertoException → 409)
   → Crea Turno { cajeroId, tenantId, sucursalId, fondoInicial, estado=ABIERTO }

2. POST /ventas  { items: [{ producto_id, cantidad?, peso_kg? }], metodo_pago }
   → Verifica turno abierto del cajero (RN-06)
   → Resuelve sucursalId desde el turno activo
   → Valida que ningún precio del request actúe como autoridad
   → Consulta CatalogClient→MS-3 para cada producto (rechaza 404 si no existe, 400 si está inactivo)
   → Llama TaxClient→MS-2 para calcular IVA y totales consolidados
   → Crea Venta con subtotal, impuestos (IVA) y total de MS-2
   → Estado: PENDIENTE

3. POST /ventas/{id}/items  { productoId, cantidad, pesoKg? }
   → Llama CatalogClient→MS-3 para obtener nombre y precio del producto
   → Llama TaxClient→MS-2 para recalcular IVA con todos los ítems
   → Actualiza Venta.Subtotal / .Impuestos / .Total

4. POST /ventas/{id}/cobrar  { monto_recibido }
   → Verifica JWT y venta PENDIENTE
   → Valida que MetodoPago == EFECTIVO (S2-004: rechaza 400 Bad Request si es TARJETA o MIXTO)
   → Valida monto_recibido ≥ venta.Total (→ 400 "Monto insuficiente" si no alcanza)
   → Calcula vuelto = monto_recibido − total
   → VentaService.CompletarAsync: estado → COMPLETADA + crea Pago { Metodo=EFECTIVO, Monto=monto_recibido, Vuelto=vuelto }
   → VentaRepository.CompletarConOutboxAsync persiste estado, Pago y OutboxMessage PENDIENTE en una transacción
   → Retorna venta completada + monto_recibido + vuelto
   → OutboxPublisherWorker publica el payload persistido y conserva event_id entre reintentos
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

`VentaRepository.ActualizarAsync` e `ItemVentaRepository.ActualizarAsync`/`EliminarAsync` conservan los filtros globales y exigen además `CurrentTenantId` presente y coincidencia explícita con el tenant almacenado (para ítems, mediante `Venta.TenantId`). La pertenencia no se obtiene del objeto recibido ni de la identidad ya rastreada. Las actualizaciones de venta/ítem ajeno o sin tenant lanzan `KeyNotFoundException`; eliminar un ítem no visible es una operación sin efecto, sin guardar cambios.

## Outbox de sale.completed [IMPLEMENTADO]

Al completar una venta, `VentaService.CompletarAsync` genera una sola identidad estable, un payload `schema_version = 1` y un registro `PENDIENTE`. `VentaRepository.CompletarConOutboxAsync` vuelve a consultar la venta con el tenant vigente y guarda el cambio a `COMPLETADA`, el pago y el outbox mediante un único `SaveChangesAsync` dentro de una transacción PostgreSQL explícita. Si falla cualquier escritura, se revierte todo.

```csharp
var eventId = Guid.NewGuid();
var evento = new SaleCompletedEvent
{
    EventId = eventId,
    SchemaVersion = 1,
    TenantId = venta.TenantId,
    VentaId = venta.Id,
    // snapshot inmutable de la venta
};
await _ventaRepository.CompletarConOutboxAsync(venta.Id, pago, outboxMessage);
```

`KafkaProducerService` permanece como adaptador registrado, pero el cobro no lo invoca directamente. `OutboxPublisherWorker` reserva filas elegibles con lease, publica el JSON persistido y marca `PUBLICADO`; ante un fallo conserva la fila y programa el siguiente intento según [[sale-completed-outbox]].

## Tests de integración

- `TurnoIntegrationTests`: abre un turno con JWT de cajero y verifica que se persiste en estado `ABIERTO`.
- `VentaIntegrationTests`: cubre creación de venta con resolución autoritativa en MS-3 e IVA real en MS-2, agregado de ítems, rechazo de productos inexistentes (404) e inactivos (400). Incluye los escenarios S2-005: (1) `CrearVenta_SinTurnoAbierto_Retorna409SinPersistir` — sin turno ABIERTO la venta no se crea; (2) `CrearVenta_PrecioFalsificadoEsIgnorado_UsaPrecioDeMS3` — el `precio_unitario` del request es ignorado y el precio persistido viene de MS-3.
- `CobroIntegrationTests`: cobra una venta en efectivo con un monto superior al total y verifica vuelto, pago, estado `COMPLETADA` y outbox pendiente.
- `KafkaPublishIntegrationTests`: verifica mediante HTTP que el cobro conserva en outbox el payload `sale.completed` versión 1.
- `VentaOutboxTransactionTests` (S2-008): verifica el commit conjunto y el rollback cuando falla la inserción del outbox sobre PostgreSQL real.
- `OutboxPublisherWorkerTests` (S2-009): verifica reintento con identidad y payload estables, marcado de publicado y cancelación limpia.
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
| PC-15 | Cobrar en Efectivo | [IMPLEMENTADO — endpoint expuesto, valida monto ≥ total, calcula vuelto y persiste sale.completed pendiente] |
| PC-16 | Calcular Vuelto | [PLANIFICADO] |
| PC-17 | Cobrar con Tarjeta | [PLANIFICADO] |
| PC-18 | Pago Mixto (efectivo + tarjeta) | [PLANIFICADO] |
| PC-19 | Emitir Recibo / Comprobante | [PLANIFICADO] |
| PC-20 | Anular Venta (Devolución) | [PARCIAL — implementado en VentaService.AnularAsync] |
| PC-21 | Devolución Parcial | [PLANIFICADO] |
| PC-22 | Poner Venta en Espera (Hold) | [PLANIFICADO] |
| PC-23 | Recuperar Venta de Espera | [PLANIFICADO] |
| PC-24 | Publicar sale.completed | [IMPLEMENTADO — outbox, worker y reintentos automáticos] |
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

## S2-022 — Fixtures de dos tenants y turnos

[IMPLEMENTADO] Utilidad reutilizable en [Fixtures/README.md](../../../tests/GlobalMart.IntegrationTests/Fixtures/README.md) con esquemas PostgreSQL exclusivos por ejecución, usuarios CAJERO, sucursales, productos, turnos abiertos/cerrados, ventas pendientes/completadas e ítems/pagos efectivos para dos tenants. Limpieza idempotente por tenant y disposición de los esquemas propios. Compilación Release aprobada; 2/2 pruebas aprobadas el 2026-10-12, incluida la de integración contra PostgreSQL 16 desechable en Docker (filtrado por tenant, limpieza idempotente que conserva al otro tenant y eliminación de esquemas verificada). Corregido el seed del rol CAJERO, que ya existe tras crear las tablas. No cambia endpoints ni acredita E2E de frontend.

## S2-025 — Regresión de carrito

[PARCIAL] Suite Playwright sobre el renderer real de POS, con catálogo y puente de sesión controlados. Ejecución 2026-10-12: 1 aprobado (agregar, cantidad, eliminar y recargar), 2 fallidos (sin respuesta de ventas MS-5 y persistencia del carrito al cambiar sesión de tenant). Pos usa `pos_cart` sin partición por tenant y calcula IVA local al 19%. Igualdad con totales de MS-5 y aislamiento HTTP de backend [NO VERIFICADO]. No acredita E2E completo ni venta funcional. Reporte y capturas: [S2-025](../../qa/S2-025/reporte.md).

## S2-023 — Aislamiento HTTP de venta y producto (RN-01)

Suite [VentaTenantIsolationTests.cs](../../../tests/GlobalMart.IntegrationTests/VentaTenantIsolationTests.cs), filtrable con `Category=RN01` o `Plan=S2-023`. Usa los dos tenants de S2-022 en esquemas PostgreSQL exclusivos, JWT firmados validados por el middleware real y solicitudes HTTP al host MS-5. Verifica lectura por ID y turno ajenos, agregado/modificación/eliminación de ítems y cobro de venta ajena, incluyendo combinación de venta A con ítem B. Compara antes/después las ventas, ítems y pagos persistidos de ambos tenants y exige que no se cree un evento ante ataques.

Los casos de crear venta y agregar producto B con JWT A recorren `CatalogClient` y el host HTTP real de MS-3 con su base de datos y filtros; no simulan la pertenencia del producto. Un control positivo recorre creación, agregado, cambio de cantidad, eliminación y cobro de A, comprueba pago/outbox pendiente y conservación de B. Solo MS-2 se sustituye por una respuesta determinista; no acredita E2E de Electron ni entrega real al broker. Las migraciones de arranque se omiten exclusivamente en los hosts de prueba porque la fixture ya crea el modelo EF vigente. Ejecución previa Release aprobada el 2026-10-14: 10/10 casos MS-5 dentro de la suite conjunta 18/18, sin fallos ni omisiones. Ver [reporte S2-023](../../qa/S2-023/reporte.md) y evidencia TRX.

Defensa adicional en [PosRepositoryTenantIsolationTests.cs](../../../tests/GlobalMart.IntegrationTests/PosRepositoryTenantIsolationTests.cs): 12 ataques directos a repositorios, sin validación previa del servicio, cubren actualización de venta, actualización/eliminación de ítem ajeno y ausencia de tenant, con objetos separados y entidades previamente rastreadas. Comparan persistencia de ventas, ítems y pagos; un control positivo verifica mutaciones propias, no confía en el `TenantId` falsificado del objeto de entrada y conserva los datos de B. Validación Release el 2026-10-14: 13/13 casos directos aprobados (12 ataques y un control propio), junto con los 10/10 HTTP de ventas, dentro de la suite conjunta 32/32. Evidencia: [S2-023-refuerzo.trx](../../qa/S2-023/S2-023-refuerzo.trx).

## S2-007 — Persistencia de outbox

[IMPLEMENTADO] Entidad `OutboxMessage`, `DbSet` y filtro por tenant en `PosCartDbContext`; migración `20261015020010_AddOutboxMessages` generada con EF Core 8.0.11. Crea tabla local a MS-5 con payload textual, estado, versión, contadores y reserva; índice `(TenantId, Status, NextAttemptAt)` y unicidad `(TenantId, VentaId, EventType)`. No modifica tablas de negocio existentes. Build de MS-5 aprobado (advertencia preexistente MSB3539); prueba enfocada **1/1 aprobada** el 2026-10-14 sobre PostgreSQL 16.4 temporal: migraciones reales, consulta por estado, aislamiento, unicidad, reversión y reaplicación. [Diseño y evidencia](../eventos/sale-completed-outbox.md).

## S2-008 — Persistencia atómica de venta y evento

[IMPLEMENTADO] `VentaRepository.CompletarConOutboxAsync` confirma estado, pago y outbox en una transacción explícita, con validación del tenant actual y coherencia del evento. `VentaOutboxTransactionTests` aprobó **2/2** casos sobre PostgreSQL 16.4 temporal: persistencia conjunta y rollback completo ante conflicto del outbox. La publicación posterior se acredita en S2-009.

## S2-009 — Worker de publicación de outbox

[IMPLEMENTADO] `OutboxPublisherWorker` procesa lotes con cancelación del host; `OutboxRepository` usa reservas PostgreSQL `FOR UPDATE SKIP LOCKED`, lease y actualización condicional por tenant/evento/token. Un fallo transitorio vuelve a `PENDIENTE` con backoff exponencial y jitter; el éxito marca `PUBLICADO` sin reconstruir el evento. Pruebas enfocadas **2/2 aprobadas** y regresión de cobro/outbox **6/6 aprobada** el 2026-10-15. [Diseño y evidencia](../eventos/sale-completed-outbox.md).

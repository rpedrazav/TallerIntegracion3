---
id: ms5-pos
tipo: microservicio
titulo: MS-5 · POS & Cart Service
estado: parcial
fuentes: [src/POSCartService/, Docker/simulator/server.js, tests/GlobalMart.IntegrationTests/PaymentSimulatorIntegrationTests.cs, tests/GlobalMart.IntegrationTests/PaymentRetrySafetyTests.cs, tests/GlobalMart.IntegrationTests/CobroSeguroIntegrationTests.cs, docs/qa/S2-035/reporte.md, tests/GlobalMart.IntegrationTests/CobrarTarjetaIntegrationTests.cs, tests/GlobalMart.IntegrationTests/PaymentEventSequenceContractTests.cs]
verificado_contra_codigo: true
ultima_revision: 2026-10-24
depende_de: [ms1-identity, ms2-tax, ms3-catalog]
publica: [sale.completed]
consume: []
reglas: [RN-01, RN-02, RN-03, RN-06, RF-02, RF-03, RF-04, RF-05, RF-06, RF-07]
---
# MS-5 · POS & Cart Service

> Servicio central del punto de venta. Gestiona turnos de caja y el ciclo completo de una venta (carrito, ítems, cobro, anulación). Integrado con MS-2 (IVA) y MS-3 (productos). Persiste y publica `sale.completed` mediante outbox; el frontend usa los cobros en efectivo y tarjeta simulada. El hardware físico sigue pendiente.

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
| POST | `/turnos/abrir` (alias `/api/turnos/abrir`) | JWT | [IMPLEMENTADO] |
| GET | `/turnos/activo` (alias `/api/turnos/activo`) | JWT | [IMPLEMENTADO] |
| POST | `/turnos/cerrar` (alias `/api/turnos/cerrar`) | JWT | [IMPLEMENTADO] |
| POST | `/turnos/cuadre` (alias `/api/turnos/cuadre`) | JWT | [IMPLEMENTADO — suma pagos EFECTIVO/COMPLETADAS; retorna efectivo_esperado, monto_declarado, diferencia] |
| POST | `/ventas` (alias `/api/ventas`) | JWT | [IMPLEMENTADO — Contrato fijado S2-001] |
| POST | `/ventas/{id}/items` (alias `/api/ventas/{id}/items`) | JWT | [IMPLEMENTADO] |
| PUT | `/ventas/{id}/items/{itemId}` (alias `/api/ventas/{id}/items/{itemId}`) | JWT | [IMPLEMENTADO] |
| DELETE | `/ventas/{id}/items/{itemId}` (alias `/api/ventas/{id}/items/{itemId}`) | JWT | [IMPLEMENTADO] |
| GET | `/ventas/{id}` (alias `/api/ventas/{id}`) | JWT | [IMPLEMENTADO] |
| GET | `/ventas/turno/{turnoId}` (alias `/api/ventas/turno/{turnoId}`) | JWT | [IMPLEMENTADO] |
| POST | `/ventas/{id}/cobrar` (alias `/api/ventas/{id}/cobrar`) | JWT | [IMPLEMENTADO — valida monto_recibido ≥ total, calcula vuelto y persiste venta, pago y sale.completed pendiente en una transacción] |
| POST | `/ventas/{id}/cobrar-tarjeta` (alias `/api/ventas/{id}/cobrar-tarjeta`) | JWT | [IMPLEMENTADO — reserva previa, aprobación/rechazo/timeout y bloqueo de repetición; S2-035] |
| POST | `/ventas/{id}/anular` (alias `/api/ventas/{id}/anular`) | JWT | [PLANIFICADO en controller — implementado en VentaService.AnularAsync] |
| GET | `/health` | Público | [IMPLEMENTADO] |

> Ver [[matriz-rutas]] para la comparativa exhaustiva de rutas Kong, controladores y puertos de host.

### S2-049 — Timeout sin doble cobro

Antes de autorizar tarjeta, MS-5 persiste un `IntentoPago PENDIENTE` con clave de idempotencia bajo una transacción serializable. Una venta con intento sin resultado definitivo rechaza un nuevo POST con 409, incluso si llega con otra clave. Timeout, error de red o estado desconocido conservan el intento pendiente. `GET /ventas/{id}/intento-tarjeta` consulta la pasarela con la misma clave sin reenviar la autorización: una aprobación completa la venta y una denegación marca el intento como rechazado; `UNKNOWN` mantiene bloqueado otro cobro. La consulta exige JWT, tenant y cajero de la venta. La verificación de integración HTTP con PostgreSQL está [NO VERIFICADA] en este entorno; el build y las pruebas del adaptador sin base de datos pasaron el 2026-10-18.

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

3. POST /ventas/{id}/items  { producto_id, cantidad?, peso_kg? }
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
   → PaymentGatewayClient está disponible por DI, pero este endpoint aún no lo invoca

5. POST /ventas/{id}/cobrar-tarjeta  { scenario_id?, idempotency_key?, monto? }
   → S2-033 / S2-034: Valida JWT, turno ABIERTO, venta PENDIENTE y MetodoPago != EFECTIVO
   → Reserva IntentoPago PENDIENTE en PostgreSQL antes de autorizar; clave repetida o cobro incierto → 409 sin POST externo
   → Llama a IPaymentGatewayClient.ProcessPaymentAsync
   → Si APPROVED: CompletarTarjetaAsync completa venta, crea Pago TARJETA, actualiza IntentoPago a APROBADO y OutboxMessage sale.completed (200 OK)
   → Si DECLINED: IntentoPago RECHAZADO, venta sigue PENDIENTE, 0 pagos y 0 outbox (422 Unprocessable Entity)
   → Si Timeout/Error: IntentoPago sigue PENDIENTE, venta sigue PENDIENTE, 0 pagos y 0 outbox (504 / 502)
   → GET /ventas/{id}/intento-tarjeta reconcilia el mismo intento sin repetir el POST

6. POST /ventas/{id}/anular  { motivo }
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

- S2-052: dos casos `RechazoOTimeoutHttp_NoCompletaNiGeneraPagoOEvento` en `CobroSeguroIntegrationTests`, **2/2 aprobados** el 2026-10-19 con PostgreSQL temporal y simulador Node real. Verifican 422/504, venta PENDIENTE, intento RECHAZADO/PENDIENTE, cero pagos/outbox, lectura tras 36 s para timeout y bloqueo de reintentos sin otro POST externo. [Evidencia y límites](../../qa/S2-052/reporte.md). El despliegue remoto no alcanzó cobro: creación de venta retornó 502 por conexión de MS-5 a catálogo en `localhost:5203`; aceptación remota [NO VERIFICADO].

- `TurnoIntegrationTests`: abre un turno con JWT de cajero y verifica que se persiste en estado `ABIERTO`.
- `VentaIntegrationTests`: cubre creación de venta con resolución autoritativa en MS-3 e IVA real en MS-2, agregado de ítems, rechazo de productos inexistentes (404) e inactivos (400). Incluye los escenarios S2-005: (1) `CrearVenta_SinTurnoAbierto_Retorna409SinPersistir` — sin turno ABIERTO la venta no se crea; (2) `CrearVenta_PrecioFalsificadoEsIgnorado_UsaPrecioDeMS3` — el `precio_unitario` del request es ignorado y el precio persistido viene de MS-3.
- `CobroIntegrationTests`: cobra una venta en efectivo con un monto superior al total y verifica vuelto, pago, estado `COMPLETADA` y outbox pendiente.
- `KafkaPublishIntegrationTests`: verifica mediante HTTP que el cobro conserva en outbox el payload `sale.completed` versión 1.
- `VentaOutboxTransactionTests` (S2-008): verifica el commit conjunto y el rollback cuando falla la inserción del outbox sobre PostgreSQL real.
- `OutboxPublisherWorkerTests` (S2-009): verifica reintento con identidad y payload estables, marcado de publicado y cancelación limpia.
- `OutboxBrokerRecoveryIntegrationTests` (S2-010): cobra con el productor simulado como indisponible, verifica venta/pago/outbox conservados y, al recuperarlo, una única entrega lógica del payload persistido.
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
| PC-17 | Cobrar con Tarjeta | [IMPLEMENTADO — cobro backend con simulador; S2-035] |
| PC-18 | Pago Mixto (efectivo + tarjeta) | [PLANIFICADO] |
| PC-19 | Emitir Recibo / Comprobante | [PLANIFICADO] |
| PC-20 | Anular Venta (Devolución) | [PARCIAL — implementado en VentaService.AnularAsync] |
| PC-21 | Devolución Parcial | [PLANIFICADO] |
| PC-22 | Poner Venta en Espera (Hold) | [PLANIFICADO] |
| PC-23 | Recuperar Venta de Espera | [PLANIFICADO] |
| PC-24 | Publicar sale.completed | [IMPLEMENTADO — outbox, worker y reintentos automáticos] |
| PC-25 | Publicar sale.reversed | [PLANIFICADO] |
| PC-26 | Leer Tarjeta en Terminal (NFC/chip/mag) | [PLANIFICADO] |
| PC-27 | Enviar Solicitud a Pasarela | [IMPLEMENTADO — invocado desde cobrar-tarjeta] |
| PC-28 | Recibir Respuesta de Pasarela | [IMPLEMENTADO — aprobación, rechazo y timeout; S2-035] |
| PC-29 | Manejar Rechazo de Pago | [IMPLEMENTADO — rechazo definitivo sin pago/outbox; S2-035] |
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
- `src/POSCartService/Services/IPaymentGatewayClient.cs`
- `src/POSCartService/Services/PaymentGatewayClient.cs`
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

## S2-010 — Caída y recuperación del broker

[IMPLEMENTADO] [OutboxBrokerRecoveryIntegrationTests](../../../tests/GlobalMart.IntegrationTests/OutboxBrokerRecoveryIntegrationTests.cs) recorre cobro, pago, outbox, fallo de publicación y recuperación con PostgreSQL real y un productor controlado en el límite Kafka. Comprueba una venta, un pago y un evento lógico; los dos intentos conservan topic, clave, JSON y `EventId`, mientras solo el intento posterior a la recuperación se entrega. Prueba enfocada **1/1 aprobada** y regresión S2-008/S2-009/S2-010 **5/5 aprobada** el 2026-10-15. [Evidencia TRX](../../qa/S2-010/S2-010.trx). La detención de un proceso Kafka real no forma parte de esta prueba determinista.

## S2-031 — Definir contrato de pasarela simulada

[IMPLEMENTADO] Se definieron `IPaymentGatewayClient` y los DTOs `PaymentGatewayRequest` y `PaymentGatewayResponse`. Transportan únicamente referencias opacas de pago (`transaction_id`, `authorization_code`, `gateway_reference`), moneda, monto, tenant y venta; sin datos de tarjeta (PAN/CVV), en cumplimiento estricto de RN-02 y PCI DSS. Pruebas de contrato **5/5 aprobadas** en `PaymentGatewayClientTests`. [Detalles](../integraciones/pasarela-pago.md).

## S2-032 — Agregar persistencia de intento de tarjeta

[IMPLEMENTADO] Entidad `IntentoPago` (`IntentosPago`) y migración EF Core `20261017163020_AddIntentoPago`. Persiste `IdempotencyKey`, `Estado` (`PENDIENTE`, `APROBADO`, `RECHAZADO`, `FALLIDO`), monto, moneda y referencias opacas vinculadas a la venta y tenant. Idempotencia forzada en base de datos mediante índice único `(TenantId, IdempotencyKey)`. Pruebas **4/4 aprobadas** en `IntentoPagoPersistenceTests`. [Detalles](../integraciones/pasarela-pago.md).

## S2-033 — Endpoint cobrar-tarjeta

[IMPLEMENTADO] Endpoint `POST /ventas/{id}/cobrar-tarjeta` en `VentasController` con DTOs `CobrarTarjetaRequest` y `CobrarTarjetaResponse`. Implementa validación de guardias (JWT, turno abierto, venta PENDIENTE en el tenant, total positivo). Criterio de aceptación validado: ventas ajenas o completadas nunca llaman a la pasarela externa. Pruebas **6/6 aprobadas** en `CobrarTarjetaIntegrationTests`. [Detalles](../integraciones/pasarela-pago.md).

## S2-037 — Adaptador HTTP de pasarela

[IMPLEMENTADO] `PaymentGatewayClient` implementa `IPaymentGatewayClient`, usa la URL y timeout configurados para `PasarelaPago`, y propaga `request_id` como `Idempotency-Key`. Su DTO transmite solo tenant, venta, monto, moneda y escenario; no admite datos de tarjeta. Registra trazas estructuradas sin cuerpos ni credenciales y distingue timeout, cancelación del llamador y errores HTTP. Pruebas enfocadas **4/4 aprobadas** el 2026-10-16. [Contrato y evidencia](../integraciones/pasarela-pago.md).

## S2-038 — Servicio simulador de autorizaciones

[IMPLEMENTADO] El simulador Node existente implementa aprobación, rechazo y timeout sin conexión bancaria. Exige la misma identidad en `Idempotency-Key` y `request_id`, conserva la respuesta por tenant y genera referencias opacas. La integración real con `PaymentGatewayClient` aprobó **7/7 pruebas** el 2026-10-17. El endpoint `cobrar-tarjeta` invoca el adaptador; ver S2-035 para pruebas del flujo integrado. [Contrato y evidencia](../integraciones/pasarela-pago.md).

## S2-039 — Evitar reintento inseguro de cobro

[IMPLEMENTADO] Ante timeout de autorización, `PaymentGatewayClient` consulta una vez el estado con el mismo `tenant_id`, `request_id` e `Idempotency-Key`, sin repetir el `POST`. Si recupera un resultado lo devuelve; si el estado no existe o no puede consultarse, lanza `PaymentAuthorizationUncertainException`. Un rechazo `DECLINED` sigue siendo un resultado definitivo y no activa la consulta. Pruebas enfocadas y simulador real: **4/4 aprobadas** el 2026-10-17. La orquestación está conectada en `cobrar-tarjeta`; ver S2-035. [Política y evidencia](../integraciones/pasarela-pago.md).

## S2-056 — Configuración segura del simulador

[IMPLEMENTADO] La configuración de la pasarela separa endpoints de sandbox/producción, timeouts y claves en `src/POSCartService/appsettings.json`, `Docker/env.example` y `.env.example`, sin exponer secretos reales ni permitir datos de tarjeta (RN-02 / PCI DSS). [Detalles](../integraciones/pasarela-pago.md).

## S2-057 y S2-058 — Simulador local y ruta en Gateway

[IMPLEMENTADO] El simulador de pasarela se ejecuta localmente en Docker (`payment-simulator:5050`) y en host, y queda expuesto en Kong (`/simulator/payments`, `/simulator/health`). MS-5 (`PaymentGatewayClient`) puede alcanzarlo por Gateway (:8000) o directo (:5050). Se validó con 7 pruebas de integración en `PaymentSimulatorIntegrationTests`. [Detalles](../integraciones/pasarela-pago.md).

## S2-035 — Pruebas de cobro seguro e idempotente

[IMPLEMENTADO] `CobroSeguroIntegrationTests` conecta el endpoint HTTP de MS-5 y JWT real con PostgreSQL migrado y el proceso Node del simulador. La reserva de `IntentoPago` se confirma antes de contactar la pasarela, con lock breve por venta y unicidad por tenant/clave. Repeticiones y cobros pendientes/inciertos retornan 409 sin otra autorización; claves inválidas o contradictorias retornan 400. Solo un rechazo definitivo permite otro intento con clave nueva. Aprobación actualiza la reserva y confirma venta, pago y outbox en una transacción.

La consulta inmediata tras timeout puede recuperar una aprobación; si no confirma el resultado, no se completa la venta. Si falla el commit local tras aprobación externa, la reserva permanece pendiente y bloquea otro cargo. Tras integrar S2-049, `GET /ventas/{id}/intento-tarjeta` reconcilia el mismo intento sin repetir la autorización (ver validación del merge al final). No acredita Electron, un banco real ni entrega Kafka.

Validación Release el 2026-10-18: **17/17 pruebas aprobadas**, más **35/35 de regresión** (tarjeta, adaptador/simulador, intentos, efectivo y transacción outbox), sin fallos ni omisiones. [Diagnóstico, alcance y reproducción](../../qa/S2-035/reporte.md).

## S2-040 — Probar secuencia de pago y evento

[PARCIAL] `Pago` persiste la misma referencia opaca expuesta por `CobrarTarjetaResponse` y guardada en `IntentoPago`; la aprobación conserva venta, pago, intento y outbox en una transacción. Las pruebas de contrato y servicio aprobaron **2/2** y la migración EF no deja cambios pendientes. Las pruebas de aprobación, rechazo y timeout están implementadas y compiladas, pero [NO VERIFICADAS] porque PostgreSQL `localhost:5436` no estaba disponible y Docker no fue accesible en el entorno. [Reporte](../../qa/S2-040/reporte.md).

Validación posterior del merge, 2026-10-18: **5/5 casos S2-040 aprobados** dentro de las 54 pruebas backend. Las tres integraciones PostgreSQL anteriormente pendientes están verificadas; se conserva la migración y la referencia opaca en pago/intento/respuesta.

## Integración de S2-035 con S2-040 y S2-049 — 2026-10-18

[IMPLEMENTADO] Se unificó la reserva previa de S2-035 con `IniciarIntentoPagoAsync`, preservando el bloqueo por venta y tenant, la validación de clave y la referencia en `Pago`. Timeout/error/estado desconocido conserva `IntentoPago.PENDIENTE` (incierto), compatible con la consulta de S2-049. La actualización condicional solo admite reservas pendientes/fallidas del mismo tenant, venta, clave, monto y moneda; una aprobación no inserta un intento nuevo. La reserva se separa del seguimiento EF para no sobrescribir una reconciliación concurrente.

Validación: **54/54 pruebas backend**, incluidas **17 S2-035** y **5 S2-040**, más typecheck, **2/2 archivos de pruebas de usuarios** y **7/7 pruebas de interfaz de cobro/búsqueda**. El caso de fallo de commit local verifica ahora recuperación mediante GET, segunda consulta estable y un solo POST externo. Las pruebas de navegador usan APIs controladas; no acreditan E2E contra un despliegue. [Resolución y evidencia](../../qa/S2-035/merge.md).

## S2-053 / TI3-684 — Autorización e idempotencia

[IMPLEMENTADO y VERIFICADO localmente] Tres casos con filtro `Plan=S2-053` aprobados en Release el 2026-10-19 con simulador Node real; los dos casos HTTP de MS-5 usan JWT y PostgreSQL 16 temporal. Repetir la misma clave tras aprobación o con respuesta pendiente devuelve 409 sin otro POST externo: una autorización lógica, un intento APROBADO, un Pago TARJETA de 119 CLP con referencia coincidente y un outbox pendiente. Dos POST idénticos directos al simulador conservan transacción, referencia y fecha de procesamiento. No acredita Electron, entrega Kafka ni despliegue remoto; el bloqueo remoto descrito en S2-052 no fue reevaluado. [Evidencia y reproducción](../../qa/S2-053/reporte.md).

## S2-061 — Contrato de peso variable

[CONTRATO DEFINIDO] El [contrato de ítem pesable](../dominio/peso-variable.md) fija kg, precisión de 0.001 kg, mínimo de 0.010 kg y `Cantidad = PesoKg`, con conversión explícita desde gramos y ejemplos de aceptación/rechazo. Especifica precio por kg autoritativo y snapshot con cuatro decimales. **La tabla S2-001 anterior describe el comportamiento actual; no acredita estas nuevas validaciones.** S2-062 implementa tipo, mínimo, precisión y coherencia cantidad/peso al agregar. Siguen pendientes las reglas completas en crear/modificar y la unidad de catálogo; S2-063 resuelve la precisión persistida del precio.

## S2-062 — Validar peso al agregar ítem

[IMPLEMENTADO] `POST /ventas/{id}/items` y su alias `/api` usan `EsPesoVariable` consultado en MS-3. Un pesable exige `peso_kg >= 0.010`, múltiplo de `0.001`, y cantidad coincidente si se envía. Peso ausente/null, cero, negativo, insuficiente, con fracción de gramo o cantidad contradictoria devuelve 400 antes de MS-2 y de la persistencia. No pesables rechazan peso no nulo. `AgregarItemRequest.Cantidad` es nullable; omisión/null deriva del peso para pesables o conserva default 1 para no pesables. No se modifican crear venta, modificar ítem, migraciones ni el simulador.

Validación local del 2026-10-24: **32/32 pruebas Release aprobadas**, 20 de S2-062 y 12 de regresión, con JWT, HTTP y PostgreSQL 16 temporal. MS-3/MS-2 simulados mediante handlers HTTP. Rechazos sin cambios en ítems, estado o totales y sin llamada fiscal adicional. [Evidencia y reproducción](../../qa/S2-062/reporte.md).

## S2-063 — Cálculo pesable autoritativo

[IMPLEMENTADO] Agregar usa el peso validado de S2-062 como cantidad y `PrecioBase` de MS-3 como precio por kg según S2-061; el request no controla precio, subtotal ni IVA. El snapshot `PrecioUnitario` pasa a `numeric(20,4)` mediante `20261024142752_PreserveItemPricePrecision`, conservando los 16 dígitos enteros anteriores. MS-5 redondea subtotal a dos decimales ToEven y MS-2 aplica el mismo redondeo por línea a subtotal e IVA, sumando las líneas para la venta. Crear/agregar rechazan precio de catálogo negativo. La unidad UUID de catálogo sigue sin correspondencia verificable; se aplica la convención `EsPesoVariable => precio por kg`, sin acreditar validación de `UomBaseId`.

Validación S2-063 del 2026-10-24: **42/42 pruebas Release aprobadas** (10 nuevas, 32 de regresión), con PostgreSQL temporal, HTTP MS-5 y calculador productivo MS-2 a través de handler HTTP. Regresión manual TC-01 aprobada. [Evidencia y límites](../../qa/S2-063/reporte.md).

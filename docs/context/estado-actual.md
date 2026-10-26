---
id: estado-actual
tipo: indice
titulo: Estado Real de Implementación — GlobalMart OS
estado: vigente
fuentes: [docs/context/_reports/inventario.md, src/, globalmart-frontend/src/api/client.ts, globalmart-frontend/src/hardware/simulated-scale-adapter.ts, globalmart-frontend/src/hardware/serial-scale-adapter.ts, globalmart-frontend/src/hardware/scale-adapter-factory.ts, globalmart-frontend/src/hardware/scale-ipc.ts, globalmart-frontend/src/payments/terminal-ipc.ts, globalmart-frontend/src/preload.ts, globalmart-frontend/tests/simulated-scale-adapter.test.cjs, globalmart-frontend/tests/serial-scale-adapter.test.cjs, globalmart-frontend/tests/scale-ipc.test.cjs, globalmart-frontend/tests/terminal-ipc.test.cjs, git log, docs/context/_reports/preguntas-abiertas.md, tests/GlobalMart.IntegrationTests/CobroSeguroIntegrationTests.cs, docs/qa/S2-035/reporte.md, new-ingresses.yaml]
verificado_contra_codigo: true
ultima_revision: 2026-10-26
depende_de: []
publica: []
consume: []
reglas: []
---
# Estado Real de Implementación — GlobalMart OS

> Nodo más importante para cualquier IA nueva. Refleja el estado REAL del código al 29-09-2026, validado contra el repositorio y las aclaraciones del equipo de desarrollo.

**Contexto del proyecto:** Proyecto académico de Ingeniería Civil Informática, Universidad Católica de Temuco (UCT). Las integraciones externas que no puedan obtenerse legalmente (certificados digitales reales del SII, terminales de pago bancario físicos) serán simuladas.  
**Sprint en curso:** **Sprint 1** (penúltimo día de la Semana 3 de un ciclo de 4 semanas, de miércoles a miércoles).

**Revisión del DoD:** completada. Ver [dod-sprint1-revision.md](dod-sprint1-revision.md) para el veredicto de los siete criterios y sus hallazgos técnicos.

---

## Matriz de Estado por Microservicio

S2-052 — 2026-10-19: **2/2 pruebas de integración HTTP locales aprobadas** para rechazo y timeout de tarjeta con PostgreSQL temporal y simulador real. La venta conserva PENDIENTE, sin Pago ni evento outbox, incluso tras la respuesta tardía y reintentos bloqueados. Publicación Kafka no ejercitada. Intento contra despliegue remoto bloqueado antes del cobro: `POST /ventas` devuelve 502 por conexión a MS-3 en `localhost:5203`; aceptación remota [NO VERIFICADO]. No se cambiaron destinos del clúster. [Reporte](../qa/S2-052/reporte.md).

| Microservicio | Controllers | Kafka | Tests | Estado General |
|---------------|-------------|-------|-------|----------------|
| MS-1 Identity | ✅ 3 controllers | ❌ No Kafka | ❌ Solo seed | **PARCIAL** |
| MS-2 Tax | ✅ 1 controller | ❌ No Kafka | ✅ ManualTest | **PARCIAL** |
| MS-3 Catalog | ✅ 2 controllers | ❌ No Kafka | ✅ Manual .http + baja/PUT TI3-642 + 9 casos RN-01 S2-023 | **PARCIAL** |
| MS-4 Warehouse | ✅ 1 controller | ✅ Consumer sale.completed | ❌ No tests | **PARCIAL** |
| MS-5 POS | ✅ 2 controllers | ✅ OutboxPublisherWorker sale.completed | ✅ AgregarItemTest + CobroCuadreTest + S2-008/S2-009/S2-010/S2-037/S2-038/S2-039 + contrato S2-040 + 10 HTTP y 13 directos RN-01 S2-023 | **PARCIAL** |
| MS-6 SupplyChain | ❌ Sin controllers | ❌ Sin Kafka | ❌ Sin tests | **PLANIFICADO** |
| MS-7 Analytics | ❌ Sin controllers | ❌ Sin consumers | ❌ Sin tests | **PLANIFICADO** |
| MS-8 Loyalty | ❌ Sin controllers | ⚠️ Registrado en DI sin consumer loop | ❌ Sin tests | **PLANIFICADO** |

---

## Detalle por Microservicio

### MS-1 · Tenant & Identity Service [PARCIAL]
**Implementado:**
- `POST /auth/login` — genera JWT con tenant_id, roles, active_role (expiración: 8 horas por defecto en appsettings.json) [IMPLEMENTADO]
- `GET/POST/PUT/DELETE /users` (alias `/api/v1/users`, `/api/users`) — CRUD usuarios con RBAC [IMPLEMENTADO]
- `DELETE /api/v1/users/{id}` (mismos alias) actualiza solo `users.is_active=false`, filtrando ID y tenant del JWT; retorna 204 también si ya estaba inactivo, 400 ante autodesactivación y 404 para ID inexistente/ajeno. Siete casos DELETE y tres de regresión PUT aprobados con PostgreSQL aislado en Release el 2026-10-06 [IMPLEMENTADO]. PUT/DELETE usan `users`; creación, listado y login aún usan `Usuarios`. Ver [MS-1](servicios/ms1-identity.md).
- `POST /users/{id}/roles` (alias `/api/v1/users/{id}/roles`) — asignar roles (sin SUPER_ADMIN). Actualización atómica de tabla intermedia mediante EF Core Change Tracker y 3 tests de integración validados (TI3-456) [IMPLEMENTADO]
- `GET/PUT /tenants/{id}/config` — config de tenant (país, moneda, IVA), validación con FluentValidation (TI3-459), tracking EF Core y rol ADMIN del mismo tenant (TI3-458) [IMPLEMENTADO]
- `GET /sucursales` — lista sucursales del tenant del JWT, con zona horaria efectiva [IMPLEMENTADO]
- `POST /sucursales` — crea sucursal (solo ADMIN), zona horaria IANA opcional y validada, nombre único por tenant sin distinguir mayúsculas (409) [IMPLEMENTADO]
- TenantMiddleware — extrae tenant_id del JWT e inyecta en DbContext [IMPLEMENTADO]
- Seed de desarrollo: cajero@demo.cl (ver contraseña en [[como-ejecutar]]) [IMPLEMENTADO]

**No implementado (PLANIFICADO):**
- `POST /auth/refresh` — renovar token
- `PUT`/`DELETE /sucursales/{id}` — editar y desactivar sucursales (solo existe crear y listar)
- Asignación de usuarios a sucursales (`UsuarioSucursales` sin endpoint)
- `GET /tenants/{id}/sucursales` — superseded por `GET /sucursales`
- `GET /fx/rates` — tipos de cambio
- `TI-03` MFA (Multi-Factor Authentication)

---

### MS-2 · Tax & Compliance Service [PARCIAL]
**Implementado:**
- `POST /tax/calculate` — calcula IVA para lista de items usando config del tenant [IMPLEMENTADO]
- `POST /comprobantes` — emite comprobante de venta con correlativo atómico por tenant [IMPLEMENTADO]
- `GET /comprobantes/{id}` — obtiene comprobante para reimpresión, aislado por tenant [IMPLEMENTADO]
- TenantMiddleware — inyecta tenant_id del JWT en TaxDbContext para los HasQueryFilter (RN-01), bypass de /health y /swagger, logging y tests (TI3-460) [IMPLEMENTADO]
- TaxCalculatorService — IVA simple (configurable por tenant) [IMPLEMENTADO]
- ComprobanteService — calcula importes en servidor y persiste comprobante (jsonb) [IMPLEMENTADO]
- `obtener_correlativo_comprobante(uuid)` — secuencia PostgreSQL por tenant, correlativo único bajo concurrencia [IMPLEMENTADO]
- Corrección de carrera en la creación de la secuencia por tenant (`CREATE SEQUENCE IF NOT EXISTS` era TOCTOU y devolvía 500 en el primer comprobante de un tenant nuevo) [IMPLEMENTADO]
- Test de concurrencia: 5 requests simultáneos → correlativos consecutivos y sin duplicados; validado también con 20 simultáneos y con 2 tenants en paralelo [IMPLEMENTADO]
- TenantConfigClient — llama a MS-1 para obtener PorcentajeIva [IMPLEMENTADO]

**No implementado (PLANIFICADO / SIMULADO):**
- `sucursal_id` en el comprobante — MS-1 ya expone `GET`/`POST /sucursales`, pero falta la columna en el modelo de MS-2
- `POST /dte/solicitar-folio` — integración simulada con entidad fiscal (SII)
- `POST /dte/emitir` — emisión de boleta/factura electrónica
- `GET /dte/{id}/estado` — estado de DTE
- `GET /reportes/declaracion-fiscal` — reportes fiscales
- IVA compuesto en cascada

---

### MS-3 · Catalog & Pricing Service [PARCIAL]
**Implementado:**
- TI3-643 / S2-012: `GET /products/{id}`, consumido por el POS, solo expone productos propios activos mediante `GetActiveByIdAsync`; productos desactivados o de otro tenant retornan 404. PUT conserva `GetByIdAsync` sin filtro de estado para permitir reactivación ADMIN. Test específico aprobado 1/1 y build de MS-3 aprobado el 2026-10-14 [IMPLEMENTADO].
- TI3-642 / S2-011: `DELETE /products/{id}` exige ADMIN y tenant del JWT; actualiza únicamente `IsActive=false` con filtro explícito por ID y tenant, preservando físicamente el producto. Retorna 204 sin body, también si ya estaba inactivo (sin nueva escritura), y el mismo 404 para inexistente/otro tenant; sin JWT 401 y sin ADMIN 403. PUT exige ADMIN solo si cambia `IsActive`, manteniendo permitidas las demás ediciones autenticadas. `DesactivarProductoIntegrationTests`: 11/11 casos aprobados con PostgreSQL 16 aislado el 2026-10-14; build de MS-3 aprobado [IMPLEMENTADO].
- `ProductosPage` confirma DESACTIVACIÓN, mantiene `axios.delete` y recarga con `fetchDatos()` tras 204; el producto permanece como Inactivo salvo que el filtro solo activos lo oculte. Typecheck aprobado el 2026-10-14 [IMPLEMENTADO].
- `GET/POST/PUT /products` — CRUD productos [IMPLEMENTADO]
- `GET /products/search?q=` — búsqueda full-text con índice trigram `pg_trgm` [IMPLEMENTADO]
- `GET /products/lookup?barcode=` — lookup por código de barras [IMPLEMENTADO]
- `GET/POST /categories` — CRUD categorías con jerarquía (parentId) [IMPLEMENTADO]
- Soporte para `EsPesoVariable`, `CodigoQrUrl`, `UomBaseId` en modelo [IMPLEMENTADO]

**No implementado (PLANIFICADO):**
- `GET/POST /prices` — precios dinámicos por sucursal
- `GET/POST /promotions` — promociones con fechas
- `POST /uom/convert` — conversión de unidades
- Publicación de eventos Kafka (`catalog.updated`, `product.price_updated`)

---

### MS-4 · Warehouse & Inventory Service [PARCIAL]
**Implementado:**
- `GET /stock/{productId}?sucursal_id=` — consulta stock [IMPLEMENTADO]
- `KafkaConsumerService` — consume `sale.completed`, descuenta stock con idempotencia [IMPLEMENTADO]
- Tabla `EventosKafkaProcesados` — garantiza exactly-once processing [IMPLEMENTADO]
- TenantMiddleware [IMPLEMENTADO]
- Test de integración — publica `sale.completed`, espera el procesamiento asíncrono y verifica el descuento mediante `GET /stock/{productId}` [IMPLEMENTADO]

**No implementado (PLANIFICADO):**
- `POST /stock/ajuste` — ajuste manual
- `POST /recepciones` — recepción de mercancía (FEFO)
- `GET /lotes` — gestión de lotes
- `POST /mermas`, `POST /transferencias`, `POST /conteos`
- Generación de alertas Kafka (`stock.alert`, `expiry.alert`)
- FEFO en selección de lotes para venta

---

### MS-5 · POS & Cart Service [PARCIAL]
**Implementado:**
- `POST /turnos/abrir` — abre turno [IMPLEMENTADO]
- `GET /turnos/activo` — turno activo del cajero [IMPLEMENTADO]
- `POST /turnos/cerrar` — cierra turno [IMPLEMENTADO]
- `POST /ventas` — crear venta en estado PENDIENTE. Contratos fijados (S2-001, S2-002 y S2-003): el cliente aporta `producto_id`, `cantidad` y `peso_kg`; MS-5 valida y resuelve cada producto en MS-3 (rechaza 404 inexistentes y 400 inactivos sin persistir datos), usa precio y nombre autoritativos de catálogo, y calcula subtotal, impuestos (IVA real del tenant vía MS-2 en la creación inicial sin requerir edición posterior) y total [IMPLEMENTADO]
- `POST /ventas/{id}/items` — agregar ítem (con tax via MS-2) [IMPLEMENTADO]
- `PUT /ventas/{id}/items/{itemId}` — modificar cantidad [IMPLEMENTADO]
- `DELETE /ventas/{id}/items/{itemId}` — eliminar ítem [IMPLEMENTADO]
- `GET /ventas/{id}` — obtener venta [IMPLEMENTADO]
- `GET /ventas/turno/{turnoId}` — ventas por turno [IMPLEMENTADO]
- `POST /ventas/{id}/cobrar` — valida que la venta tenga método de pago EFECTIVO (rechaza TARJETA o MIXTO con 400 Bad Request según S2-004), valida `monto_recibido` ≥ total, y confirma venta, pago y `sale.completed` pendiente en una transacción [IMPLEMENTADO]
- VentaService con métodos `CompletarAsync` y `AnularAsync` [IMPLEMENTADO en service]
- OutboxPublisherWorker y KafkaProducerService — publicación automática con lease, reintento y cancelación limpia [IMPLEMENTADO]
- PaymentGatewayClient — adaptador HTTP con URL/timeout configurables, idempotencia, payload seguro y consulta de estado tras timeout sin repetir la autorización; conectado a `POST /ventas/{id}/cobrar-tarjeta` [IMPLEMENTADO]
- `POST /ventas/{id}/cobrar-tarjeta` — reserva durable antes de autorizar, aprobación atómica con pago/outbox, rechazo y timeout sin completar; 409 sin reautorizar ante repetición o cobro incierto. [IMPLEMENTADO; S2-035]
- PaymentGatewaySimulator — proceso Node local con aprobación, rechazo, timeout, idempotencia y referencias opacas [IMPLEMENTADO]
- test de integración abre turno con JWT de cajero y verifica persistencia en estado `ABIERTO` [IMPLEMENTADO]
- tests de integración en `VentaIntegrationTests` validan creación de venta con resolución autoritativa en MS-3 e IVA en MS-2, agregado de ítems, y rechazo de productos inexistentes (404) e inactivos (400) sin persistir venta ni ítems [IMPLEMENTADO]
- 7 tests unitarios en `CrearVentaRequestValidatorTests` validan el contrato S2-001 (venta unitaria sin precio, venta a peso con balanza, venta mixta y rechazo de precios/pesos negativos) [IMPLEMENTADO]
- tests de integración en `CobroIntegrationTests` cobran venta en efectivo, calculan vuelto, verifican persistencia y rechazan cobro en efectivo ante ventas con TARJETA y MIXTO sin registrar pagos (S2-004) [IMPLEMENTADO]
- test de integración de cobro verifica el payload versión 1 persistido en outbox, incluido `EventId`, `VentaId`, `TenantId` y `Total` [IMPLEMENTADO]
- test de integración abre turno, registra venta y pago efectivo, y verifica el cuadre con diferencia calculada [IMPLEMENTADO]
- tests de integración S2-005 en `VentaIntegrationTests` cubren los 4 escenarios de creación segura: precio falsificado ignorado (precio persistido viene de MS-3, no del request), IVA por tenant desde MS-2, producto inactivo (400 sin persistir) y ausencia de turno abierto (409 sin persistir venta) [IMPLEMENTADO]

**No implementado / CRÍTICO:**
- Endpoint HTTP `POST /ventas/{id}/anular` aún no está expuesto en `VentasController.cs`
- Integración con hardware (balanza, impresora, cajón de dinero)

---

### MS-6 · Supply Chain & Import Service [PLANIFICADO]
- Modelos: OrdenCompra, OrdenCompraItem, Proveedor, Envio, CostoLandedHistorico [IMPLEMENTADO]
- DbContext y migración inicial [IMPLEMENTADO]
- Sin controllers, sin endpoints, sin lógica de negocio activa.

---

### MS-7 · Analytics & Notification Service [PLANIFICADO]
- Modelos: Alerta, HistorialEnvio, KPIVenta [IMPLEMENTADO]
- DbContext y migración inicial [IMPLEMENTADO]
- Sin controllers ni Kafka consumers.

---

### MS-8 · Loyalty & Customer Service [PLANIFICADO]
- Modelos: ClienteAfiliado, MovimientoPuntos, SaldoPuntos, TierMembresia [IMPLEMENTADO]
- DbContext y migración inicial [IMPLEMENTADO]
- Kafka Consumer y Producer registrados como singletons en `Program.cs`, pero sin `IHostedService` ni consumidor activo.

---

## Funcionalidades Críticas NO Implementadas

| # | Funcionalidad | RN/RF afectado | Gravedad |
|---|---------------|----------------|----------|
| 1 | Exponer endpoint `POST /ventas/{id}/anular` en `VentasController.cs` | RF-22 | 🔴 Crítica |
| 2 | Emisión simulada de DTE (boleta/factura) en MS-2 | RN-03, RF-08, TC-08/09 | 🔴 Crítica |
| 4 | FEFO en recepción e inventario en MS-4 | RN-05, RF-10, WI-04/05 | 🟠 Alta |
| 5 | Integrar frontend `Pos.tsx` con backend real | RF-04, RF-05 | 🟠 Alta |
| 6 | Endpoint POST /auth/refresh | RF-01 | 🟠 Alta |
| 7 | Gestión de sucursales en backend | TI-11, RF-19 | 🟠 Alta |

---

## Estado del Frontend

S2-049 [IMPLEMENTADO en código]: MS-5 guarda el intento de tarjeta antes de autorizar y bloquea otra autorización de la misma venta mientras el resultado siga pendiente. `GET /ventas/{id}/intento-tarjeta` consulta el estado del mismo intento y reconcilia aprobación o rechazo sin repetir el POST. El POS muestra resultado incierto tras timeout, recupera el intento al recargar y solo ofrece reintento manual tras un rechazo definitivo. La prueba dirigida de interfaz y las pruebas del adaptador sin base pasaron el 2026-10-18; la integración HTTP con PostgreSQL temporal y simulador real fue verificada al resolver el merge (54/54 casos backend, incluida recuperación posterior sin otro POST).

S2-071 / TI3-702 [IMPLEMENTADO]: el frontend define el puerto tipado `TerminalPort`, una máquina pura con estados `available`, `processing`, `approved`, `declined`, `uncertain` y `cancelled`, y `Ms5TerminalAdapter`. `Pos.tsx` inicia y consulta intentos mediante ese adaptador conservando la referencia de idempotencia del ciclo. El contrato contiene venta, monto, referencia del intento, estado y referencia opaca, sin PAN/CVV ni persistencia de tarjeta. MS-5 no tiene endpoint de cancelación: `cancelarIntento` responde de forma tipada `CANCELACION_NO_SOPORTADA` y no realiza HTTP. Pruebas enfocadas **4/4** y typecheck aprobados el 2026-10-25.

S2-072 / TI3-703 [IMPLEMENTADO]: el frontend incorpora `SimulatedTerminalAdapter`, seleccionable mediante `createTerminalPort()` y `DEVICE_TERMINAL_MODE` (`mock` simulado, `real` MS-5). Los escenarios configurables `waiting`, `approved`, `declined`, `cancelled` y `disconnected` producen resultados deterministas; lectura/inicio significa que `iniciarCobro` recibe los datos del intento y pasa a `processing`. La aprobación usa una referencia opaca estable y la desconexión conserva el intento como `uncertain`. La configuración reutiliza `MOCK_TERMINAL_DELAY_MS`; `MOCK_TERMINAL_SCENARIO` tiene prioridad y `AUTO_APPROVE_MOCK` queda como fallback compatible. TI3-703 no añadió datos de tarjeta, IPC ni sincronización con backend; el IPC se incorporó posteriormente en TI3-704. Pruebas del terminal **11/11** y typecheck aprobados el 2026-10-26.

S2-073 / TI3-704 [IMPLEMENTADO]: Electron expone al renderer una API `window.api.terminal` tipada y limitada a iniciar y consultar intentos mediante los canales fijos `devices:terminal:iniciarCobro` y `devices:terminal:consultarEstado`; no ofrece IPC genérico, cancelación ni comandos arbitrarios de hardware. Main mantiene un singleton `TerminalPort` en modo simulado, valida objetos planos mediante listas exactas de propiedades, UUID, monto positivo finito/acotado y referencias seguras/acotadas, y filtra las respuestas a cinco campos no sensibles. `Pos.tsx` ya consume esta API en vez de crear adaptadores. El modo `real` falla explícitamente sin fallback porque mover el transporte MS-5 al proceso main queda [PLANIFICADO] para TI3-705. Se conservan `contextIsolation: true`, `nodeIntegration: false` y `sandbox: true`; PAN, CVV y vencimiento no cruzan el bridge. Los IPC heredados de `ITerminalAdapter` permanecen separados. Pruebas IPC puras **6/6** y typecheck aprobados el 2026-10-26.

El frontend usa `src/api/client.ts` para Login (MS-1), catálogo del POS (MS-3) y turnos y ventas (MS-5): URL por servicio, métodos tipados, JWT y timeout de 15 s centralizados, con cierre de sesión ante 401 en llamadas protegidas [IMPLEMENTADO]. POS consulta el turno activo al entrar, muestra carga y solo habilita el carrito tras confirmarlo; ante 404 redirige a apertura y ante otros errores ofrece reintentar [IMPLEMENTADO]. El borrador previo a la venta sigue local; al iniciar, POS envía solo producto y cantidad a `POST /ventas` y usa la respuesta de MS-5 para ID, ítems y totales. Al recargar, identifica la venta PENDIENTE con `GET /ventas/turno/{turnoId}` y recupera ítems y totales con `GET /ventas/{id}`, sin tomar el borrador local como autoridad [IMPLEMENTADO]. En esa venta, escaneo, búsqueda, cambio de cantidad y eliminación usan los endpoints de ítems de MS-5; después de cada operación, `GET /ventas/{id}` renueva ítems, subtotal, impuestos y total [IMPLEMENTADO]. El botón Cobrar abre `PaymentModal`; para efectivo llama `POST /ventas/{id}/cobrar`, muestra el vuelto devuelto por MS-5 o la diferencia del error 400 sin completar la venta, y bloquea reintentos automáticos ante resultado incierto [IMPLEMENTADO]. El POS permite elegir EFECTIVO o TARJETA al iniciar una venta; el modal llama al cobro simulado de MS-5 con clave UUID de idempotencia, muestra aprobación con referencia opaca o rechazo y permite cambiar el método de una venta pendiente mediante PATCH /ventas/{id}/metodo-pago. El resultado incierto exige verificar la venta antes de otro cobro [IMPLEMENTADO]. La búsqueda del POS consulta MS-3 por nombre/código activo y muestra errores de catálogo, mientras /admin/productos sigue reservado a ADMIN/SUPER_ADMIN [IMPLEMENTADO]. Pruebas Playwright dirigidas 4/4 aprobadas en Edge el 2026-10-18; E2E con pasarela desplegada [NO VERIFICADA].

El 2026-10-18, los tres destinos remotos del frontend (MS-1, MS-3 y MS-5) respondieron `GET /health` con HTTP 200 y certificado HTTPS válido de Let's Encrypt. Los Ingress usan `globalmart-dev-tls` [IMPLEMENTADO]. La renovación no está automatizada en el clúster actual; el certificado vence el 2027-01-16. Ver [[matriz-rutas]].

### Guía visual aprobada

El archivo `docs/GlobalMart OS — Guía de estilo.html` fue aprobado como referencia visual del frontend el 2026-10-06. Define tokens CSS, tipografías `Inter`/`IBM Plex Mono`, paleta de ceniza/malva (primario sombra malva), estados verde bosque/ámbar/ladrillo/azul acero, modo oscuro, reglas de contraste y composición de referencia para el POS. La guía es normativa para nuevas implementaciones, pero no cambia el estado funcional de las pantallas: POS sigue [PARCIAL] y Admin sigue [PARCIAL].

Ver el nodo [[guia-estilo]] para el resumen y las reglas aplicables.

| Pantalla | Estado | Integración API |
|----------|--------|-----------------|
| Login | [IMPLEMENTADO] | Llama directo a auth service con manejo de errores específicos por HTTP status/red, timeout de 15s y spinner de carga en botón (TI3-471) |
| Abrir Turno (`AbrirTurnoPage`) | [IMPLEMENTADO] | Llama a MS-5 `/api/turnos/activo` y `POST /api/turnos/abrir` (spinner de carga en botón, redirección con confirmación a POS y manejo 409) (TI3-471) |
| Cerrar Turno (`CerrarTurnoPage`) | [IMPLEMENTADO] | Tabla de denominaciones (billetes/monedas) con conteo y subtotales automáticos. Llama a MS-5 `POST /api/turnos/cuadre` para obtener efectivo esperado. Muestra resultado: efectivo esperado vs declarado, diferencia en **verde** si es $0, en **rojo** si hay discrepancia (sobrante o faltante). Botón `POST /api/turnos/cerrar` con confirmación. |
| Productos Admin (`ProductosPage`) | [IMPLEMENTADO] | `GET /products?page=1&pageSize=500` + `GET /categories` en MS-3 (puerto 5203). Tabla con filtros, estado y acciones de catálogo. TI3-675 protege `/admin/productos` con la misma regla ADMIN/SUPER_ADMIN aplicada al resto de Administración; CAJERO recibe **Acceso denegado**. |
| Admin | [PARCIAL] | TI3-675: `/admin/*` y `/admin/productos` comparten `AdminRoute`; ADMIN/SUPER_ADMIN acceden y CAJERO ve **Acceso denegado** una vez sincronizado el token [IMPLEMENTADO]. `TenantConfig` conserva edición exclusiva de administradores, estado de carga, controles bloqueados durante guardado y feedback de éxito/error; IVA numérico ya cubierto por TI3-645. Lista, creación, edición de email y baja lógica de usuarios disponibles. Persisten las dependencias de `sucursalId`, listado de inactivos y auditoría; sin reactivación UI. |

**Nota de integración Electron:** `preload.ts` expone únicamente funciones de autenticación y sesión (`ping`, `getToken`, `setToken`, `logout`). No expone hardware serial ni actualización automática por ahora.

---

## Conexiones
- Servicios: [[ms1-identity]], [[ms2-tax]], [[ms3-catalog]], [[ms4-inventory]], [[ms5-pos]]
- Infraestructura: [[docker-compose]], [[kong]], [[matriz-rutas]]
- Eventos: [[kafka-topics]]
- Planificación: [[sprint-actual]]
- Reporte de discrepancias: `docs/context/_reports/discrepancias.md`

## Fuentes
- `docs/context/_reports/inventario.md`
- `docs/context/_reports/preguntas-abiertas.md`
- `docs/context/infraestructura/matriz-rutas.md`
- `src/POSCartService/Controllers/VentasController.cs`
- `src/POSCartService/Services/VentaService.cs`
- `src/WarehouseInventoryService/Messaging/KafkaConsumerService.cs`
- `src/TenantIdentityService/appsettings.json`

## S2-022 — Fixtures de dos tenants y turnos

[IMPLEMENTADO] Utilidad reutilizable en [Fixtures/README.md](../../tests/GlobalMart.IntegrationTests/Fixtures/README.md) con esquemas PostgreSQL exclusivos por ejecución, usuarios CAJERO, sucursales, productos, turnos abiertos/cerrados, ventas pendientes/completadas e ítems/pagos efectivos para dos tenants. Limpieza idempotente por tenant y disposición de los esquemas propios. Compilación Release aprobada; 2/2 pruebas aprobadas el 2026-10-12, incluida la de integración contra PostgreSQL 16 desechable en Docker (filtrado por tenant, limpieza idempotente que conserva al otro tenant y eliminación de esquemas verificada). Corregido el seed del rol CAJERO, que ya existe tras crear las tablas. No cambia endpoints ni acredita E2E de frontend.

## S2-025 — Regresión de carrito

[PARCIAL] Suite Playwright sobre el renderer real de POS, con catálogo y puente de sesión controlados. Ejecución 2026-10-12: 1 aprobado (agregar, cantidad, eliminar y recargar), 2 fallidos (sin respuesta de ventas MS-5 y persistencia del carrito al cambiar sesión de tenant). Pos usa `pos_cart` sin partición por tenant y calcula IVA local al 19%. Igualdad con totales de MS-5 y aislamiento HTTP de backend [NO VERIFICADO]. No acredita E2E completo ni venta funcional. Reporte y capturas: [S2-025](../qa/S2-025/reporte.md).

## S2-023 — Aislamiento de venta y producto (RN-01)

[IMPLEMENTADO] Suite RN-01 con JWT firmados, middleware real y PostgreSQL 16 desechable: **32/32 pruebas Release aprobadas el 2026-10-14**, sin fallos ni omisiones (9 HTTP de productos MS-3, 10 HTTP de ventas MS-5 y 13 directas de repositorios). Verifica lecturas y mutaciones ajenas, tenant falsificado, cobro, producto B con JWT A pasando por MS-3 real y defensa directa en repositorios. Snapshots confirman datos ajenos intactos; controles propios aprobados. MS-2 y envío al broker sustituidos; no acredita E2E de Electron ni migraciones. [Reporte y reproducción](../qa/S2-023/reporte.md), [evidencia principal TRX](../qa/S2-023/S2-023.trx).

Refuerzo posterior [IMPLEMENTADO]: `VentaRepository.ActualizarAsync` y `ItemVentaRepository.ActualizarAsync/EliminarAsync` conservan filtros globales y exigen explícitamente el tenant almacenado y un `CurrentTenantId` presente, sin `IgnoreQueryFilters`. PUT de producto ajeno o inexistente devuelve el mismo 404 y cuerpo; se conserva 403 al cambiar estado propio sin ADMIN. Suite ampliada Release del 2026-10-14: **32/32 aprobadas**, sin fallos ni omisiones (9 HTTP MS-3, 10 HTTP MS-5, 13 directas de repositorios), con controles propios y ataques con entidades rastreadas/sin tenant. [Evidencia del refuerzo](../qa/S2-023/S2-023-refuerzo.trx).

## S2-007 — Persistencia de outbox

[IMPLEMENTADO] Entidad `OutboxMessage`, `DbSet` y filtro por tenant en `PosCartDbContext`; migración `20261015020010_AddOutboxMessages` generada con EF Core 8.0.11. Crea tabla local a MS-5 con payload textual, estado, versión, contadores y reserva; índice `(TenantId, Status, NextAttemptAt)` y unicidad `(TenantId, VentaId, EventType)`. No modifica tablas de negocio existentes. Build de MS-5 aprobado (advertencia preexistente MSB3539); prueba enfocada **1/1 aprobada** el 2026-10-14 sobre PostgreSQL 16.4 temporal: migraciones reales, consulta por estado, aislamiento, unicidad, reversión y reaplicación. [Diseño, alcance y evidencia S2-007](eventos/sale-completed-outbox.md).

## S2-008 — Persistencia atómica de venta y evento

[IMPLEMENTADO] El cobro efectivo genera un `EventId` estable y payload `schema_version = 1`; `VentaRepository.CompletarConOutboxAsync` guarda venta `COMPLETADA`, `Pago` y outbox `PENDIENTE` en una sola transacción PostgreSQL. Prueba enfocada **2/2 aprobada** el 2026-10-15 contra PostgreSQL 16.4 temporal, incluido rollback de estado y pago al fallar el outbox. La publicación posterior se acredita en S2-009. [Diseño y evidencia](eventos/sale-completed-outbox.md).

## S2-009 — Publicación de pendientes

[IMPLEMENTADO] Worker hospedado con reserva atómica `FOR UPDATE SKIP LOCKED`, lease, logging estructurado, backoff exponencial con jitter, límite de 10 intentos y cancelación limpia. Publica el payload almacenado sin generar otro GUID; un fallo temporal vuelve a `PENDIENTE`. Prueba enfocada **2/2 aprobada** y regresión S2-008/cobro **6/6 aprobada** el 2026-10-15. [Diseño y evidencia](eventos/sale-completed-outbox.md).

## S2-010 — Caída y recuperación del broker

[IMPLEMENTADO] Prueba de integración sobre PostgreSQL 18.6 temporal y productor Kafka controlado: completa la venta y pago, simula indisponibilidad durante el primer intento, conserva una sola fila outbox `PENDIENTE` y, tras recuperar el productor, entrega una sola vez el payload persistido con el mismo `EventId`, clave, topic y versión. Prueba enfocada **1/1 aprobada** y regresión S2-008/S2-009/S2-010 **5/5 aprobada** el 2026-10-15. No acredita una caída de un proceso Kafka real. [Diseño y evidencia](eventos/sale-completed-outbox.md).

## S2-031 / TI3-662 — Definir contrato de pasarela simulada

[IMPLEMENTADO] Se definieron la interfaz `IPaymentGatewayClient` y los DTOs `PaymentGatewayRequest` y `PaymentGatewayResponse` en `POSCartService`. El contrato respeta estrictamente la regla de dominio RN-02 y las normas PCI DSS: no recibe, transporta ni serializa PAN, CVV, fecha de expiración ni PIN. Transporta únicamente referencias opacas de transacción (`transaction_id`, `authorization_code`, `gateway_reference`), moneda (`currency`), monto (`amount`), tenant (`tenant_id`), venta (`sale_id`) e identificador de escenario de simulación (`scenario_id`). Pruebas de contrato y seguridad **5/5 aprobadas** en `PaymentGatewayClientTests` bajo el trait `S2-031`. [Contrato y diseño](integraciones/pasarela-pago.md).

## S2-032 / TI3-663 — Agregar persistencia de intento de tarjeta

[IMPLEMENTADO] Se implementó la entidad `IntentoPago` con el enum `EstadoIntentoPago` (`PENDIENTE`, `APROBADO`, `RECHAZADO`, `FALLIDO`), mapeada a la tabla `IntentosPago` mediante la migración EF Core `20261017163020_AddIntentoPago`. Persiste `IdempotencyKey`, `Estado`, `Monto` (precisión 18,2), `Moneda`, `Referencia` opaca, `AuthorizationCode`, `DeclineCode` y `Mensaje`, asociado a `VentaId` y `TenantId`. Cumple estrictamente **RN-01** (filtro global `CurrentTenantId`) y **RN-02** (cero datos de tarjeta). La garantía de idempotencia (reintentar con la misma clave no crea dos cargos) se impone a nivel de base de datos mediante el índice único compuesto `IX_IntentosPago_TenantId_IdempotencyKey`. Pruebas de persistencia y contrato **4/4 aprobadas** en `IntentoPagoPersistenceTests` bajo trait `S2-032`. [Diseño](integraciones/pasarela-pago.md).

## S2-033 / TI3-664 — Crear endpoint cobrar-tarjeta

[IMPLEMENTADO] Se expuso el endpoint `POST /ventas/{id}/cobrar-tarjeta` en `VentasController` (MS-5) con DTOs seguros `CobrarTarjetaRequest` y `CobrarTarjetaResponse`. Ejecuta validaciones de guardia estrictas antes de invocar `IPaymentGatewayClient`: valida JWT (401), turno `ABIERTO` del cajero en el tenant (RN-06 -> 409), venta existente y perteneciente al tenant actual (RN-01 -> 404), estado `PENDIENTE` (409), método de pago compatible con tarjeta (400) y total de la venta mayor a 0 (400). Pruebas de integración **6/6 aprobadas** en `CobrarTarjetaIntegrationTests` (trait `S2-033`), verificando en particular que ventas ajenas, completadas o sin turno no realizan ninguna llamada a la pasarela externa. [Diseño](integraciones/pasarela-pago.md).

## S2-034 / TI3-665 — Persistir aprobación y rechazo

[IMPLEMENTADO] El endpoint `POST /ventas/{id}/cobrar-tarjeta` ahora persiste el resultado de cada llamada a la pasarela en un `IntentoPago` y ramifica el flujo según el status devuelto:
- **APPROVED** → llama a `IVentaService.CompletarTarjetaAsync` (atómico con `CompletarTarjetaConOutboxAsync`): marca la venta `COMPLETADA`, crea un `Pago` con `MetodoPago.TARJETA` y publica `sale.completed` en el outbox. Devuelve **HTTP 200**.
- **DECLINED** → persiste `IntentoPago` con `EstadoIntentoPago.RECHAZADO` y `DeclineCode`, la venta se mantiene `PENDIENTE`, no hay `Pago` ni `OutboxMessage`. Devuelve **HTTP 422 Unprocessable Entity**.
- **TimeoutException** → conserva el `IntentoPago PENDIENTE` creado antes de la autorización, venta `PENDIENTE`, sin `Pago` ni `OutboxMessage`. Devuelve **HTTP 504 Gateway Timeout**.
- **Exception genérica** → conserva el intento `PENDIENTE` y devuelve **HTTP 502 Bad Gateway**; la consulta posterior determina si hubo autorización.

Cumple criterio de aceptación: el rechazo no genera `sale.completed` ni un `Pago` aprobado. Nuevas pruebas de integración **4/4 aprobadas** en `CobrarTarjetaIntegrationTests` (trait `S2-034`), verificando DB post-llamada: venta COMPLETADA/PENDIENTE, presencia/ausencia de Pagos, OutboxMessages e IntentosPago con estado correcto. [Diseño](integraciones/pasarela-pago.md).

## S2-036 — Escenarios del simulador de pagos

[IMPLEMENTADO] Se especificaron y materializaron los escenarios no sensibles `sim-approved-v1`, `sim-declined-v1` y `sim-timeout-v1`, con solicitudes y respuestas reproducibles, idempotencia por `request_id`, timeout controlado y campos mínimos de auditoría. Ningún escenario usa PAN, CVV ni otra credencial bancaria. El cobro con tarjeta está implementado en `cobrar-tarjeta`; ver S2-035. [Contrato S2-036](integraciones/pasarela-pago.md).

## S2-037 — Adaptador HTTP de pasarela

[IMPLEMENTADO] `PaymentGatewayClient` implementa `IPaymentGatewayClient` y queda registrado en DI con `PasarelaPago:BaseUrl` y `PasarelaPago:TimeoutSeconds`. Envía únicamente referencias de negocio y el escenario simulado, propaga `request_id` en `Idempotency-Key` y produce trazas estructuradas sin cuerpos, JWT ni datos de tarjeta. Pruebas enfocadas **4/4 aprobadas** el 2026-10-16; su uso desde `cobrar-tarjeta` está implementado. [Contrato y evidencia](integraciones/pasarela-pago.md).

## S2-038 — Servicio simulador de autorizaciones

[IMPLEMENTADO] El proceso local `Docker/simulator/server.js` devuelve aprobación y rechazo deterministas, simula timeout, exige que `Idempotency-Key` coincida con `request_id` y conserva la respuesta por `(tenant_id, request_id)`. Genera referencias opacas estables, rechaza campos de tarjeta y emite trazas JSON con metadatos seguros. Integración real con `PaymentGatewayClient`: **7/7 pruebas aprobadas** el 2026-10-17. [Contrato y evidencia](integraciones/pasarela-pago.md).

## S2-039 / TI3-670 — Evitar reintento inseguro de cobro

[IMPLEMENTADO] Un timeout del `POST` de autorización ahora provoca una única consulta de estado con el mismo `tenant_id`, `request_id` e `Idempotency-Key`; el adaptador no repite automáticamente el cargo. Un resultado almacenado se recupera, mientras que un 404 o una consulta fallida produce `PaymentAuthorizationUncertainException`. Los rechazos son resultados definitivos y no activan reconciliación. Pruebas enfocadas y simulador Node real: **4/4 aprobadas** el 2026-10-17. El endpoint `cobrar-tarjeta` invoca el adaptador; ver S2-035 para pruebas del flujo integrado. [Política y evidencia](integraciones/pasarela-pago.md).

## S2-051 / TI3-682 — Matriz RN-02 de pagos

[CONTRATO DEFINIDO] Se documentaron [18 casos de aceptación](../../diagramas/matriz/S2-051-matriz-rn02-pagos.md) por método, resultado, idempotencia y logs permitidos, con prohibición explícita de captura local de PAN/CVV. La matriz distingue cobertura parcial del adaptador/simulador de la aceptación E2E pendiente. La definición de la matriz no ejecutó estos casos ni implementó código; aceptación E2E de los 18 casos [NO VERIFICADO]. S2-035 verifica ahora el flujo backend de tarjeta con simulador, sin acreditar esa matriz completa.

## S2-026 / TI3-657 — Inventario y matriz de rutas y puertos reales

[IMPLEMENTADO] Se documentó la matriz integral de endpoints, puertos locales (`launchSettings.json`), puertos internos de Docker, rutas Kong y consumidores del frontend en `docs/context/infraestructura/matriz-rutas.md`. Incluye mapeo de los 8 microservicios y reconciliación de prefijos `/api`.

## S2-027 / TI3-658 — Corrección de rutas Kong del flujo crítico y rutas duales

[IMPLEMENTADO] Se resolvieron los errores 404 producidos por Kong (`strip_path: false`) mediante la incorporación de atributos duales `[Route("api/...")]` y `[Route("...")]` en los controladores de MS-1 (`AuthController`, `TenantConfigController`, `SucursalesController`), MS-3 (`ProductoController`, `CategoriaController`), MS-5 (`VentasController`), MS-2 (`TaxController`, `ComprobantesController`) y MS-4 (`StockController`). En MS-1 se actualizó `TenantMiddleware` para admitir `/api/auth/login` y `/api/tenants`. En Kong se agregó `categories-route` y `sucursales-route`, y en Docker Compose se configuró `extra_hosts` para resolución de host gateway. Suite de integración `RoutePrefixIntegrationTests` **10/10 aprobada** el 2026-10-16.

## S2-028 / TI3-659 — Parametrización de destinos de APIs en el frontend

[IMPLEMENTADO] Se centralizó la resolución de URLs en `globalmart-frontend/src/config/api.ts` y plantilla `.env.example`, soportando los modos `remote`, `gateway` y `local` con normalización de prefijos `/api` en interceptores. Se eliminaron URLs hardcodeadas de `client.ts`, `Admin.tsx`, `ProductosPage.tsx` y `TenantConfig.tsx`. Typecheck sin errores en código fuente.

## S2-029 / TI3-660 — Añadir TypeScript al gate CI

[IMPLEMENTADO] Se integró un gate de validación estricta de tipos en el workflow `.github/workflows/ci.yml` mediante el job `frontend-typecheck` que ejecuta `npm ci` y `npm run typecheck` (`tsc --noEmit`). Se agregó el script `typecheck` en `globalmart-frontend/package.json`. Bloquea el pipeline de CI ante discrepancias de tipos como las detectadas previamente en `TenantConfig`. Documentado en `docs/context/infraestructura/ci-cd.md`.

## S2-030 / TI3-661 — Documentar arranque reproducible del núcleo y smoke test HTTP

[IMPLEMENTADO] Se actualizó la guía de arranque reproducible en `docs/context/proyecto/como-ejecutar.md` clarificando explícitamente que Docker Compose aprovisiona la infraestructura compartida (PostgreSQL x8, Kafka, Kong Gateway) pero no contiene los 8 microservicios .NET, los cuales se ejecutan en el host mediante los scripts actualizados `run-apis.sh` y `run-apis.bat` (MS-1 a MS-5). Se implementó el script ejecutable `scripts/smoke-test.sh` que valida de forma automatizada los health checks de MS-1 a MS-5, autenticación pública con credenciales seed (`cajero@demo.cl`), consulta autorizada de catálogo con JWT y estado de turno activo del POS.

## S2-056 / TI3-687 — Definir configuración segura del simulador

[IMPLEMENTADO] Se definió y documentó la configuración segura del simulador de pagos y pasarela en `Docker/env.example`, la plantilla raíz `.env.example` y `src/POSCartService/appsettings.json`. Separa estrictamente URL base (`PasarelaPago:BaseUrl`), timeouts (`PasarelaPago:TimeoutSeconds` = 30s), claves dummy (`PasarelaPago:ApiKey` = `sim_key_dummy_test_only`) y ambientes (`sandbox` vs `production`). Garantiza el cumplimiento de RN-02 y PCI DSS prohibiendo datos de tarjeta en el código y repositorios. Documentado en `docs/context/integraciones/pasarela-pago.md`.

## S2-057 / TI3-688 — Incorporar simulador al entorno local

[IMPLEMENTADO] Se creó el simulador de pasarela externa en `Docker/simulator/server.js` y se añadió el contenedor `payment-simulator` (puerto 5050) en `Docker/docker-compose.yml` con healthcheck automático (`/health`). Implementa los escenarios deterministas `sim-approved-v1`, `sim-declined-v1` y `sim-timeout-v1` con almacenamiento de idempotencia en memoria. Rechaza de forma estricta cualquier payload que contenga números de tarjeta (PAN), CVV o PIN (HTTP 400 `CARD_DATA_FORBIDDEN`), garantizando cumplimiento de RN-02 y PCI DSS. Se añadió al script de arranque local `run-apis.sh` y `run-apis.bat`. Suite `PaymentSimulatorIntegrationTests` **7/7 aprobada** el 2026-10-17. Documentado en `docs/context/integraciones/pasarela-pago.md`.

## S2-058 / TI3-689 — Exponer ruta de pagos en gateway

[IMPLEMENTADO] Se incorporó el servicio `payment-simulator-service` (`http://payment-simulator:5050`) en Kong API Gateway (`Docker/config/kong.yaml`) con las rutas `/simulator/payments` y `/simulator/health` (admitiendo variantes `/api/*` y preservando paths con `strip_path: false`). Permite a MS-5 alcanzar el simulador vía Gateway (`:8000`) o de forma directa (`:5050`). Se validó el contrato de red impidiendo que el frontend Electron transmita datos bancarios. Validado en `scripts/smoke-test.sh` y documentado en `docs/context/infraestructura/matriz-rutas.md` y `docs/context/integraciones/pasarela-pago.md`.

## S2-035 — Pruebas de cobro seguro e idempotente

[IMPLEMENTADO] `CobroSeguroIntegrationTests` conecta el endpoint HTTP de MS-5 y JWT real con PostgreSQL migrado y el proceso Node del simulador. La reserva de `IntentoPago` se confirma antes de contactar la pasarela, con lock breve por venta y unicidad por tenant/clave. Repeticiones y cobros pendientes/inciertos retornan 409 sin otra autorización; claves inválidas o contradictorias retornan 400. Solo un rechazo definitivo permite otro intento con clave nueva. Aprobación actualiza la reserva y confirma venta, pago y outbox en una transacción.

La consulta inmediata tras timeout puede recuperar una aprobación; si no confirma el resultado, no se completa la venta. Si falla el commit local tras aprobación externa, la reserva permanece pendiente y bloquea otro cargo. Tras integrar S2-049, `GET /ventas/{id}/intento-tarjeta` reconcilia el mismo intento sin repetir la autorización (ver validación del merge al final). No acredita Electron, un banco real ni entrega Kafka.

Validación Release el 2026-10-18: **17/17 pruebas aprobadas**, más **35/35 de regresión** (tarjeta, adaptador/simulador, intentos, efectivo y transacción outbox), sin fallos ni omisiones. [Diagnóstico, alcance y reproducción](../qa/S2-035/reporte.md).

## S2-040 / TI3-671 — Probar secuencia de pago y evento

[PARCIAL] La aprobación copia la referencia opaca de pasarela a `IntentoPago`, `Pago` y respuesta HTTP, y conserva venta completada, `Pago TARJETA` y `sale.completed` pendiente dentro de la transacción existente. La migración `20261018040621_AddPagoGatewayReference` agrega la columna nullable sin modificar datos existentes. Pruebas de contrato y servicio **2/2 aprobadas** el 2026-10-18; tres integraciones PostgreSQL implementadas y compiladas, pero [NO VERIFICADAS] porque `localhost:5436` no estaba disponible y Docker no fue accesible. [Reporte](../qa/S2-040/reporte.md).

Validación posterior del merge, 2026-10-18: **5/5 casos S2-040 aprobados** dentro de las 54 pruebas backend. Las tres integraciones PostgreSQL anteriormente pendientes están verificadas; se conserva la migración y la referencia opaca en pago/intento/respuesta.

## Integración de S2-035 con S2-040 y S2-049 — 2026-10-18

[IMPLEMENTADO] Se unificó la reserva previa de S2-035 con `IniciarIntentoPagoAsync`, preservando el bloqueo por venta y tenant, la validación de clave y la referencia en `Pago`. Timeout/error/estado desconocido conserva `IntentoPago.PENDIENTE` (incierto), compatible con la consulta de S2-049. La actualización condicional solo admite reservas pendientes/fallidas del mismo tenant, venta, clave, monto y moneda; una aprobación no inserta un intento nuevo. La reserva se separa del seguimiento EF para no sobrescribir una reconciliación concurrente.

Validación: **54/54 pruebas backend**, incluidas **17 S2-035** y **5 S2-040**, más typecheck, **2/2 archivos de pruebas de usuarios** y **7/7 pruebas de interfaz de cobro/búsqueda**. El caso de fallo de commit local verifica ahora recuperación mediante GET, segunda consulta estable y un solo POST externo. Las pruebas de navegador usan APIs controladas; no acreditan E2E contra un despliegue. [Resolución y evidencia](../qa/S2-035/merge.md).

## S2-053 / TI3-684 — Autorización e idempotencia

[IMPLEMENTADO y VERIFICADO localmente] Tres casos con filtro `Plan=S2-053` aprobados en Release el 2026-10-19 con simulador Node real; los dos casos HTTP de MS-5 usan JWT y PostgreSQL 16 temporal. Repetir la misma clave tras aprobación o con respuesta pendiente devuelve 409 sin otro POST externo: una autorización lógica, un intento APROBADO, un Pago TARJETA de 119 CLP con referencia coincidente y un outbox pendiente. Dos POST idénticos directos al simulador conservan transacción, referencia y fecha de procesamiento. No acredita Electron, entrega Kafka ni despliegue remoto; el bloqueo remoto descrito en S2-052 no fue reevaluado. [Evidencia y reproducción](../qa/S2-053/reporte.md).

## S2-061 — Definir contrato de peso variable

[CONTRATO DEFINIDO] Se documentó el [contrato de ítem pesable](dominio/peso-variable.md): kg, precisión de 1 g, mínimo de 10 g, igualdad `Cantidad = PesoKg`, precio por kg y conversión explícita de gramos. Incluye ejemplos válidos/rechazados y brechas verificadas en DTOs, validación y precisión del precio persistido. Es una entrega documental; no modifica el backend ni implementa balanza/simulador. Cumplimiento integral en ejecución: PENDIENTE, aceptación E2E [NO VERIFICADO].

## S2-062 — Validar peso en agregar ítem

[IMPLEMENTADO] MS-5 consulta `EsPesoVariable` en catálogo al agregar, exige peso válido para pesables (mínimo 10 g, incrementos de 1 g), rechaza contradicciones con cantidad y peso en no pesables. Validación previa al cálculo fiscal y persistencia. Cantidad omitida/null se deriva del peso, o usa 1 en no pesables. El contrato S2-061 permanece parcial: crear/modificar, unidad de catálogo, precisión persistida del precio y conversión del simulador siguen pendientes.

Validación local del 2026-10-24: **32/32 pruebas Release aprobadas**, 20 de S2-062 y 12 de regresión, con JWT, HTTP y PostgreSQL 16 temporal. MS-3/MS-2 simulados mediante handlers HTTP. Rechazos sin cambios en ítems, estado o totales y sin llamada fiscal adicional. [Evidencia y reproducción](../qa/S2-062/reporte.md).

## S2-063 — Calcular total pesable en servidor

[IMPLEMENTADO] El peso validado al agregar y el precio de MS-3 determinan el subtotal. Se conserva el precio con cuatro decimales en `ItemsVenta.PrecioUnitario` (migración a `numeric(20,4)`). MS-2 redondea subtotal e IVA por línea con ToEven y suma para obtener importes reproducibles; no usa importes del renderer. La validación de unidad UUID del catálogo sigue [NO VERIFICADO]; el uso de precio por kg se basa en la convención del contrato para `EsPesoVariable`.

Validación S2-063 del 2026-10-24: **42/42 pruebas Release aprobadas** (10 nuevas, 32 de regresión), con PostgreSQL temporal, HTTP MS-5 y calculador productivo MS-2 a través de handler HTTP. Regresión manual TC-01 aprobada. [Evidencia y límites](../qa/S2-063/reporte.md).

## S2-064 — Actualizar cantidad pesable con recálculo

[IMPLEMENTADO] El PUT de ítems valida por tipo consultado en MS-3: peso obligatorio y válido para pesables, cantidad positiva sin peso para no pesables. Conserva precio snapshot, recalcula con MS-2 e identidad del ítem. Los rechazos y fallos de dependencias previos a persistir no alteran el carrito. Sin migraciones nuevas; crear venta aún no aplica el contrato completo de peso. [Reporte y reproducción](../qa/S2-064/reporte.md).

Validación S2-064 del 2026-10-24: **75/75 pruebas Release aprobadas**, incluidas 23 nuevas de edición y 10 de aislamiento de ventas entre tenants, con HTTP/JWT y PostgreSQL temporal. [Evidencia](../qa/S2-064/reporte.md).

Verificación final S2-064: recompilación Release y **33/33 casos S2-064/S2-063 aprobados**, sin fallos ni omisiones; evidencia adicional en el reporte S2-064.

## S2-065 — Probar fronteras de peso y tenant

[IMPLEMENTADO — suite backend] Pruebas HTTP de creación, agregado y edición con MS-3 real, PostgreSQL aislado y calculador productivo MS-2 mediante handler. Se corrigió creación de pesable sin peso, que devolvía 201: las tres operaciones comparten reglas de peso/tipo/cantidad y crear devuelve 400 sin mutar ante entrada inválida. Las pruebas incluyen ataques entre tenants en ambos sentidos y controles propios. Al cerrar S2-065, la conversión del simulador estaba pendiente; se incorpora ahora con S2-067/S2-069/S2-070. La validación de `UomBaseId` y el E2E desplegado siguen [NO VERIFICADO]. [Reporte](../qa/S2-065/reporte.md).

Validación del 2026-10-26: **84/84 pruebas Release aprobadas**, sin fallos ni omisiones (nueve nuevas con matrices, 75 de regresión). Diagnóstico previo: 7/8 aprobadas y fallo reproducido de creación sin peso. [Evidencia y límites](../qa/S2-065/reporte.md).

## S2-066 / TI3-697 — Definir interfaz de balanza

[IMPLEMENTADO] Contrato TypeScript independiente del dispositivo en `globalmart-frontend/src/hardware/scale-contract.ts`: conectar, desconectar, consultar estado y leer peso estable; estados cerrados de conexión/lectura, errores tipados y lectura canónica en kg. `ScaleRendererApi` no contiene ruta serial, baud rate ni objetos Node. Typecheck y prueba estática aprobados el 2026-10-24; la prueba confirma que renderer y contrato no acceden a `serialport`, `navigator.serial` ni `SerialPort`. El IPC se implementó en S2-068 y la estructura serial intercambiable en S2-069; compatibilidad física [NO VERIFICADA]. [Detalle](integraciones/hardware.md).

## S2-067 / TI3-698 — Implementar adaptador de balanza simulada

[IMPLEMENTADO] `SimulatedScaleAdapter` implementa el contrato común con escenarios deterministas de peso estable, desconexión y error, sin hardware. Convierte `g` a `kg`, valida mínimo de 10 g y precisión de 1 g, y devuelve estados/errores serializables. La fábrica selecciona `simulator` por defecto mediante `GLOBALMART_SCALE_ADAPTER` y admite la inyección posterior del driver `serial` sin cambiar consumidores. Typecheck y pruebas dirigidas aprobados el 2026-10-24. La entrega mediante IPC se implementó en S2-068; su consumo visual sigue pendiente. [Detalle](integraciones/hardware.md).

## S2-068 / TI3-699 — Exponer IPC mínimo y validado

[IMPLEMENTADO] Electron main registra exclusivamente `scale:connect`, `scale:disconnect`, `scale:read-stable` y `scale:get-state`; preload los expone como `window.api.scale` con `ScaleRendererApi`. No existe invocación genérica ni parámetros de puerto/comando; el renderer mantiene aislamiento, sandbox y Node.js deshabilitado. La lectura admite solo `timeoutMs` y `stabilityWindowMs` enteros entre 0 y 60.000 ms; cualquier otra propiedad se rechaza antes del adaptador. Typecheck y 10/10 pruebas de contrato, simulador e IPC aprobadas el 2026-10-25. Integración visual React y hardware físico [NO VERIFICADOS]. [Detalle](frontend/electron-ipc.md).

## S2-069 / TI3-700 — Preparar adaptador serial intercambiable

[IMPLEMENTADO SIN HARDWARE] `SerialScaleAdapter` implementa el contrato común con `SerialScaleTransport` y `SerialScaleParser` inyectables. Puerto, baud rate e identificador de parser permanecen en Electron main; el renderer conserva sin cambios `window.api.scale`. El driver predeterminado sigue siendo `simulator`; `serial` requiere configuración completa y un proveedor que permanece deshabilitado. No se agregó `serialport` ni un protocolo de fabricante, por lo que no se afirma compatibilidad física. Typecheck y 15/15 pruebas de balanza/IPC aprobadas el 2026-10-25; hardware real [NO VERIFICADO]. [Detalle](integraciones/hardware.md).

## S2-059 / TI3-690 — Agregar logs de correlación de cobro

[IMPLEMENTADO] En MS-5 (`VentasController.cs` y `PaymentGatewayClient.cs`), se incorporaron logs estructurados con el prefijo `[COBRO_CORRELATION]` que registran `Entrada`, `Llamada pasarela` y `Respuesta pasarela` asociando identificadores opacos (`CorrelationId`, `IntentoId`, `VentaId`, `TenantId`). Se verificó mediante prueba automatizada con expresiones regulares en `CobroTarjetaCorrelationIntegrationTests` que nunca se registran números de tarjeta (PAN de 16 dígitos), códigos de seguridad (CVV/CVC) ni secretos.

## S2-060 / TI3-691 — Ejecutar smoke de despliegue de tarjeta

[IMPLEMENTADO] Verificación automatizada mediante el script `scripts/smoke-tarjeta.sh` y la suite `CobroTarjetaCorrelationIntegrationTests`. Ejercita los tres estados del simulador:
1. *Aprobado:* La venta transiciona a `COMPLETADA`, persiste el pago `TARJETA` y emite el evento en outbox.
2. *Rechazado:* Retorna HTTP 422 y conserva la venta en estado `PENDIENTE`.
3. *Timeout / No disponible:* Retorna HTTP 504 / 503 y conserva la venta en estado `PENDIENTE` sin registrar pagos no confirmados.

## S2-086 / TI3-717 — Definir límites del terminal simulado

[IMPLEMENTADO] Especificación documentada en `docs/context/integraciones/limites-terminal-simulado.md`. Establece la matriz de operaciones soportadas (`iniciar`, `cancelar`, `autorizar`, `obtenerEstado`), delimita el desacoplamiento estricto respecto a la pasarela bancaria y prohíbe declarar compatibilidad con hardware o marcas físicas sin validación empírica.

## S2-087 / TI3-718 — Preparar configuración de adaptadores

[IMPLEMENTADO] Implementación modular en `globalmart-frontend/src/devices/`:
- `DeviceManager`: Orquestador que selecciona `MockTerminalAdapter` y `MockScaleAdapter` por defecto sin requerir drivers ni privilegios de hardware.
- `RealTerminalAdapter`: Reservado como adaptador físico que rechaza ejecuciones no homologadas devolviendo error seguro (`DEVICE_NOT_CERTIFIED`).
- Estructura modular desacoplada de la UI que permite incorporar dispositivos futuros sin alterar las vistas.

## S2-088 / TI3-719 — Validar seguridad de IPC

[IMPLEMENTADO] Endurecimiento integral de Electron:
- Ventana POS con `contextIsolation: true`, `nodeIntegration: false` y `sandbox: true`.
- Validación previa de tipos, límites y rangos numéricos en `index.ts` antes de invocar cualquier adaptador (`validateIniciarParams`, `validateSimulacionAction`, `validatePeso`).
- Prohibición absoluta de APIs genéricas de ejecución o acceso directo a shell/sistema operativo desde el renderer.

## S2-089 / TI3-720 — Integrar smoke de simuladores

[IMPLEMENTADO] Flujo de prueba automatizado mediante `scripts/smoke-simuladores-frontend.sh` y `globalmart-frontend/tests/smoke-simulators.ts`, ejecutable en CI o local sin periféricos conectados. Comprueba lecturas de balanza, pesaje, tara y el ciclo de vida del terminal (inicio, aprobación con token opaco, rechazo y cancelación). Incluye widget interactivo de balanza integrado en la interfaz POS (`Pos.tsx`).

---
id: estado-actual
tipo: indice
titulo: Estado Real de Implementación — GlobalMart OS
estado: vigente
fuentes: [docs/context/_reports/inventario.md, src/, git log, docs/context/_reports/preguntas-abiertas.md]
verificado_contra_codigo: true
ultima_revision: 2026-10-06
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

| Microservicio | Controllers | Kafka | Tests | Estado General |
|---------------|-------------|-------|-------|----------------|
| MS-1 Identity | ✅ 3 controllers | ❌ No Kafka | ❌ Solo seed | **PARCIAL** |
| MS-2 Tax | ✅ 1 controller | ❌ No Kafka | ✅ ManualTest | **PARCIAL** |
| MS-3 Catalog | ✅ 2 controllers | ❌ No Kafka | ✅ Manual .http | **PARCIAL** |
| MS-4 Warehouse | ✅ 1 controller | ✅ Consumer sale.completed | ❌ No tests | **PARCIAL** |
| MS-5 POS | ✅ 2 controllers | ✅ Producer sale.completed | ✅ AgregarItemTest + CobroCuadreTest | **PARCIAL** |
| MS-6 SupplyChain | ❌ Sin controllers | ❌ Sin Kafka | ❌ Sin tests | **PLANIFICADO** |
| MS-7 Analytics | ❌ Sin controllers | ❌ Sin consumers | ❌ Sin tests | **PLANIFICADO** |
| MS-8 Loyalty | ❌ Sin controllers | ⚠️ Registrado en DI sin consumer loop | ❌ Sin tests | **PLANIFICADO** |

---

## Detalle por Microservicio

### MS-1 · Tenant & Identity Service [PARCIAL]
**Implementado:**
- `POST /auth/login` — genera JWT con tenant_id, roles, active_role (expiración: 8 horas por defecto en appsettings.json) [IMPLEMENTADO]
- `GET/POST/PUT/DELETE /users` (alias `/api/v1/users`, `/api/users`) — CRUD usuarios con RBAC [IMPLEMENTADO]
- `POST /users/{id}/roles` (alias `/api/v1/users/{id}/roles`) — asignar roles (sin SUPER_ADMIN) [IMPLEMENTADO]
- `GET/PUT /tenants/{id}/config` — config de tenant (país, moneda, IVA) [IMPLEMENTADO]
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
- TenantMiddleware — inyecta tenant_id del JWT en TaxDbContext para los HasQueryFilter [IMPLEMENTADO]
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
- `POST /ventas` — crear venta en estado PENDIENTE [IMPLEMENTADO en controller y service]
- `POST /ventas/{id}/items` — agregar ítem (con tax via MS-2) [IMPLEMENTADO]
- `PUT /ventas/{id}/items/{itemId}` — modificar cantidad [IMPLEMENTADO]
- `DELETE /ventas/{id}/items/{itemId}` — eliminar ítem [IMPLEMENTADO]
- `GET /ventas/{id}` — obtener venta [IMPLEMENTADO]
- `GET /ventas/turno/{turnoId}` — ventas por turno [IMPLEMENTADO]
- `POST /ventas/{id}/cobrar` — valida `monto_recibido` ≥ total, completa la venta, retorna vuelto y publica `sale.completed` a Kafka con `event_id` único [IMPLEMENTADO]
- VentaService con métodos `CompletarAsync` y `AnularAsync` [IMPLEMENTADO en service]
- KafkaProducerService — publica eventos Kafka con key=tenant_id y event_id único [IMPLEMENTADO]
- test de integración abre turno con JWT de cajero y verifica persistencia en estado `ABIERTO` [IMPLEMENTADO]
- test de integración crea venta, agrega dos productos y verifica subtotal, IVA y total con handlers simulados para MS-2/MS-3 [IMPLEMENTADO]
- test de integración cobra una venta en efectivo, calcula vuelto y verifica persistencia de venta completada y pago [IMPLEMENTADO]
- test de integración consume `sale.completed` desde Kafka y verifica `VentaId`, `TenantId` y `Total` del payload [IMPLEMENTADO]
- test de integración abre turno, registra venta y pago efectivo, y verifica el cuadre con diferencia calculada [IMPLEMENTADO]

**No implementado / CRÍTICO:**
- Endpoint HTTP `POST /ventas/{id}/anular` aún no está expuesto en `VentasController.cs`
- Integración con pasarela de cobro externa (planificada simulación con pasarelas tipo Mercado Pago)
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
| 3 | Integración con pasarela de cobro (Mercado Pago o simulador) | RN-02, RF-07, PC-27/28 | 🔴 Crítica |
| 4 | FEFO en recepción e inventario en MS-4 | RN-05, RF-10, WI-04/05 | 🟠 Alta |
| 5 | Integrar frontend `Pos.tsx` con backend real | RF-04, RF-05 | 🟠 Alta |
| 6 | Endpoint POST /auth/refresh | RF-01 | 🟠 Alta |
| 7 | Gestión de sucursales en backend | TI-11, RF-19 | 🟠 Alta |

---

## Estado del Frontend

### Guía visual aprobada

El archivo `docs/GlobalMart OS — Guía de estilo.html` fue aprobado como referencia visual del frontend el 2026-10-06. Define tokens CSS, tipografías `Inter`/`IBM Plex Mono`, paleta de ceniza/malva (primario sombra malva), estados verde bosque/ámbar/ladrillo/azul acero, modo oscuro, reglas de contraste y composición de referencia para el POS. La guía es normativa para nuevas implementaciones, pero no cambia el estado funcional de las pantallas: POS sigue [PARCIAL] y Admin sigue [PARCIAL].

Ver el nodo [[guia-estilo]] para el resumen y las reglas aplicables.

| Pantalla | Estado | Integración API |
|----------|--------|-----------------|
| Login | [IMPLEMENTADO] | Llama directo a `http://127.0.0.1:5124` |
| Abrir Turno (`AbrirTurnoPage`) | [IMPLEMENTADO] | Llama a MS-5 `/api/turnos/activo` y `POST /api/turnos/abrir` (redirección con confirmación a POS y manejo 409) |
| Cerrar Turno (`CerrarTurnoPage`) | [IMPLEMENTADO] | Tabla de denominaciones (billetes/monedas) con conteo y subtotales automáticos. Llama a MS-5 `POST /api/turnos/cuadre` para obtener efectivo esperado. Muestra resultado: efectivo esperado vs declarado, diferencia en **verde** si es $0, en **rojo** si hay discrepancia (sobrante o faltante). Botón `POST /api/turnos/cerrar` con confirmación. |
| Productos Admin (`ProductosPage`) | [IMPLEMENTADO] | `GET /products?page=1&pageSize=500` + `GET /categories` en MS-3 (puerto 5203). Tabla con nombre, código de barras, categoría, precio base, tipo (peso variable/unidad) y estado. Filtro instantáneo en frontend por texto (nombre, código, descripción, categoría), filtro por categoría dropdown y toggle "solo activos". Accesible desde `/admin/productos`. |
| Admin | [PARCIAL] | Panel con pestañas: Lista usuarios con `UsuariosList` (`GET /users` con soporte `/api/v1/users` y `/api/users`, nombre, correo, roles y estado), botón de refresco y formulario modal `CrearUsuarioModal` (`POST /users` + `POST /users/{id}/roles`). Pestaña "Configuración del Tenant" con `TenantConfig` (`GET` y `PUT /tenants/{id}/config` para país, moneda, idioma, IVA y zona horaria). Sin edición/desactivación individual de usuarios. |

**Nota de integración Electron:** `preload.ts` expone únicamente funciones de autenticación y sesión (`ping`, `getToken`, `setToken`, `logout`). No expone hardware serial ni actualización automática por ahora.

---

## Conexiones
- Servicios: [[ms1-identity]], [[ms2-tax]], [[ms3-catalog]], [[ms4-inventory]], [[ms5-pos]]
- Eventos: [[kafka-topics]]
- Planificación: [[sprint-actual]]
- Reporte de discrepancias: `docs/context/_reports/discrepancias.md`

## Fuentes
- `docs/context/_reports/inventario.md`
- `docs/context/_reports/preguntas-abiertas.md`
- `src/POSCartService/Controllers/VentasController.cs`
- `src/POSCartService/Services/VentaService.cs`
- `src/WarehouseInventoryService/Messaging/KafkaConsumerService.cs`
- `src/TenantIdentityService/appsettings.json`

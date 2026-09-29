---
id: estado-actual
tipo: indice
titulo: Estado Real de Implementación — GlobalMart OS
estado: implementado
fuentes: [docs/context/_reports/inventario.md, src/, git log]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: []
publica: []
consume: []
reglas: []
---
# Estado Real de Implementación — GlobalMart OS

> Nodo más importante para cualquier IA nueva. Refleja el estado REAL del código al 28-09-2026, no el diseño.

**Sprint en curso:** Sprint 3 (estimado por git log — trabajo activo en MS-4 Warehouse + MS-5 POS avanzado)

---

## Matriz de Estado por Microservicio

| Microservicio | Controllers | Kafka | Tests | Estado General |
|---------------|-------------|-------|-------|----------------|
| MS-1 Identity | ✅ 3 controllers | ❌ No Kafka | ❌ Solo seed | **PARCIAL** |
| MS-2 Tax | ✅ 1 controller | ❌ No Kafka | ✅ ManualTest | **PARCIAL** |
| MS-3 Catalog | ✅ 2 controllers | ❌ No Kafka | ✅ Manual .http | **PARCIAL** |
| MS-4 Warehouse | ✅ 1 controller | ✅ Consumer sale.completed | ❌ No tests | **PARCIAL** |
| MS-5 POS | ✅ 2 controllers | ❌ SIN productor sale.completed | ✅ AgregarItemTest | **PARCIAL** |
| MS-6 SupplyChain | ❌ Sin controllers | ❌ Sin Kafka | ❌ Sin tests | **PLANIFICADO** |
| MS-7 Analytics | ❌ Sin controllers | ❌ Sin consumers | ❌ Sin tests | **PLANIFICADO** |
| MS-8 Loyalty | ❌ Sin controllers | ⚠️ Registrado en DI pero sin handlers | ❌ Sin tests | **PLANIFICADO** |

---

## Detalle por Microservicio

### MS-1 · Tenant & Identity Service [PARCIAL]
**Implementado:**
- `POST /auth/login` — genera JWT con tenant_id, roles, active_role [IMPLEMENTADO]
- `GET/POST/PUT/DELETE /api/v1/users` — CRUD usuarios con RBAC [IMPLEMENTADO]
- `POST /api/v1/users/{id}/roles` — asignar roles (sin SUPER_ADMIN) [IMPLEMENTADO]
- `GET/PUT /tenants/{id}/config` — config de tenant (país, moneda, IVA) [IMPLEMENTADO]
- TenantMiddleware — extrae tenant_id del JWT e inyecta en DbContext [IMPLEMENTADO]
- Seed de desarrollo: cajero@demo.cl / demo1234 [IMPLEMENTADO]

**No implementado (PLANIFICADO):**
- `POST /auth/refresh` — renovar token
- `GET /tenants/{id}/sucursales` — gestión de sucursales
- `GET /fx/rates` — tipos de cambio
- `TI-03` MFA (Multi-Factor Authentication)

**Brechas críticas:**
- No hay endpoint de refresh token → sesiones expiradas sin solución
- No hay gestión de sucursales (`sucursal_id` en JWT es campo manual, no gestionado)
- No hay integración con Fixer.io para tipos de cambio

---

### MS-2 · Tax & Compliance Service [PARCIAL]
**Implementado:**
- `POST /tax/calculate` — calcula IVA para lista de items usando config del tenant [IMPLEMENTADO]
- TaxCalculatorService — IVA simple (configurable por tenant) [IMPLEMENTADO]
- TenantConfigClient — llama a MS-1 para obtener PorcentajeIva [IMPLEMENTADO]

**No implementado (PLANIFICADO):**
- `POST /dte/solicitar-folio` — integración con entidad fiscal
- `POST /dte/emitir` — emisión de boleta/factura electrónica
- `GET /dte/{id}/estado` — estado de DTE
- `GET /reportes/declaracion-fiscal` — reportes fiscales
- IVA compuesto en cascada
- Integración con SII/AFIP/IRS

**Brechas críticas:**
- Sin folio electrónico → RF-08 (emitir DTE) no implementado → ventas sin comprobante tributario
- RN-03 (toda venta genera DTE) NO se cumple en el código actual

---

### MS-3 · Catalog & Pricing Service [PARCIAL]
**Implementado:**
- `GET/POST/PUT /products` — CRUD productos [IMPLEMENTADO]
- `GET /products/search?q=` — búsqueda full-text con índice trigram pg_trgm [IMPLEMENTADO]
- `GET /products/lookup?barcode=` — lookup por código de barras [IMPLEMENTADO]
- `GET/POST /categories` — CRUD categorías con jerarquía (parentId) [IMPLEMENTADO]
- Soporte para `EsPesoVariable`, `CodigoQrUrl`, `UomBaseId` en modelo [IMPLEMENTADO]

**No implementado (PLANIFICADO):**
- `GET/POST /prices` — precios dinámicos por sucursal
- `GET/POST /promotions` — promociones con fechas
- `POST /uom/convert` — conversión de unidades
- Publicación de eventos Kafka (`catalog.updated`, `product.price_updated`)

**Brechas:**
- Sin precios por sucursal (RN-11 no cumplido)
- Sin promociones por fechas (RF-21 parcial)

---

### MS-4 · Warehouse & Inventory Service [PARCIAL]
**Implementado:**
- `GET /stock/{productId}?sucursal_id=` — consulta stock [IMPLEMENTADO]
- `KafkaConsumerService` — consume `sale.completed`, descuenta stock con idempotencia [IMPLEMENTADO]
- Tabla `EventosKafkaProcesados` — garantiza exactly-once processing [IMPLEMENTADO]
- TenantMiddleware [IMPLEMENTADO]

**No implementado (PLANIFICADO):**
- `POST /stock/ajuste` — ajuste manual
- `POST /recepciones` — recepción de mercancía (FEFO)
- `GET /lotes` — gestión de lotes
- `POST /mermas`, `POST /transferencias`, `POST /conteos`
- Generación de alertas Kafka (`stock.alert`, `expiry.alert`)
- FEFO en selección de lotes para venta

**Brechas críticas:**
- MS-5 NO produce `sale.completed` → el consumer en MS-4 no recibe eventos
- RN-05 FEFO: no hay lógica de selección por fecha de vencimiento implementada
- WI-09/WI-10 (alertas): sin implementar

---

### MS-5 · POS & Cart Service [PARCIAL]
**Implementado:**
- `POST /turnos/abrir` — abre turno [IMPLEMENTADO]
- `GET /turnos/activo` — turno activo del cajero [IMPLEMENTADO]
- `POST /turnos/cerrar` — cierra turno [IMPLEMENTADO]
- `POST /ventas` — crear venta [IMPLEMENTADO]
- `POST /ventas/{id}/items` — agregar ítem (con tax via MS-2) [IMPLEMENTADO]
- `PUT /ventas/{id}/items/{itemId}` — modificar cantidad (con recálculo IVA) [IMPLEMENTADO]
- `DELETE /ventas/{id}/items/{itemId}` — eliminar ítem (con recálculo IVA) [IMPLEMENTADO]
- `GET /ventas/{id}` — obtener venta [IMPLEMENTADO]
- `GET /ventas/turno/{turnoId}` — ventas por turno [IMPLEMENTADO]
- `POST /ventas/{id}/anular` — anular venta [IMPLEMENTADO]
- VentaService con estados PENDIENTE/COMPLETADA/ANULADA/CANCELADA [IMPLEMENTADO]

**No implementado / CRÍTICO:**
- `POST /ventas/{id}/cobrar` — el endpoint existe pero VentaService.CompletarAsync **NO publica** `sale.completed` a Kafka [PARCIAL — falta Kafka]
- Integración con pasarela de pago (RN-02, PC-27, PC-28) — NO implementado [PLANIFICADO]
- Integración con hardware: balanza serial, impresora, cajón de dinero [PLANIFICADO]
- Cálculo de vuelto al cobrar en efectivo [PLANIFICADO en VentaService, no en endpoint]

**Brechas críticas:**
- RN-02 (pagos por pasarela) — sin implementar
- PC-24 (publicar sale.completed) — sin implementar → cadena Kafka rota

---

### MS-6 · Supply Chain & Import Service [PLANIFICADO]
- Modelos: OrdenCompra, OrdenCompraItem, Proveedor, Envio, CostoLandedHistorico [IMPLEMENTADO]
- DbContext y migración inicial [IMPLEMENTADO]
- **Sin controllers, sin endpoints, sin lógica de negocio**
- Sprint 6 según roadmap

---

### MS-7 · Analytics & Notification Service [PLANIFICADO]
- Modelos: Alerta, HistorialEnvio, KPIVenta [IMPLEMENTADO]
- DbContext y migración inicial [IMPLEMENTADO]
- **Sin controllers, sin Kafka consumers, sin lógica**
- Sprint 5 según roadmap

---

### MS-8 · Loyalty & Customer Service [PLANIFICADO]
- Modelos: ClienteAfiliado, MovimientoPuntos, SaldoPuntos, TierMembresia [IMPLEMENTADO]
- DbContext y migración inicial [IMPLEMENTADO]
- Kafka Consumer y Producer registrados en DI (sin handlers implementados) [PARCIAL]
- **Sin controllers, sin endpoints, sin lógica de negocio**
- Sprint 7 según roadmap

---

## Funcionalidades Críticas NO Implementadas

| # | Funcionalidad | RN/RF afectado | Gravedad |
|---|---------------|----------------|----------|
| 1 | Publicación de `sale.completed` desde MS-5 | RN-04, RF-09, PC-24 | 🔴 Crítica |
| 2 | Emisión de DTE (boleta/factura) | RN-03, RF-08, TC-08/09 | 🔴 Crítica |
| 3 | Integración pasarela de pago | RN-02, RF-07, PC-27/28 | 🔴 Crítica |
| 4 | FEFO en recepción e inventario | RN-05, RF-10, WI-04/05 | 🟠 Alta |
| 5 | Endpoint POST /auth/refresh | RF-01 | 🟠 Alta |
| 6 | Gestión de sucursales | TI-11, RF-19 | 🟠 Alta |
| 7 | Precios dinámicos por sucursal | RN-11, RF-21, CP-14 | 🟡 Media |
| 8 | MS-7 Analytics completo | RF-17, AN-01..24 | 🟡 Sprint 5 |
| 9 | MS-6 Supply Chain completo | RF-18, SC-01..24 | 🟡 Sprint 6 |
| 10 | MS-8 Loyalty completo | RF-23, LC-01..17 | 🟡 Sprint 7 |

---

## Estado del Frontend

| Pantalla | Estado | Integración API |
|----------|--------|-----------------|
| Login | [IMPLEMENTADO] | Directo a port 5124 (sin Kong) |
| POS/Carrito | [PARCIAL] | Datos hardcodeados, sin API real |
| Admin | [PARCIAL] | Contenido NO verificado |

**Hallazgo:** El frontend NO pasa por Kong (API Gateway), llama directo a `http://127.0.0.1:5124`.

---

## Conexiones
- Detalle de servicios: [[ms1-identity]], [[ms2-tax]], [[ms3-catalog]], [[ms4-inventory]], [[ms5-pos]]
- Brechas en Kafka: [[kafka-topics]]
- Sprint actual: [[sprint-actual]]
- Discrepancias: ver `docs/context/_reports/discrepancias.md`

## Fuentes
- `docs/context/_reports/inventario.md`
- `src/POSCartService/Services/VentaService.cs`
- `src/WarehouseInventoryService/Messaging/KafkaConsumerService.cs`
- `src/TaxComplianceService/Controllers/TaxController.cs`

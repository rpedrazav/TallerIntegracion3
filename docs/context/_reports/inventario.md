---
id: inventario
tipo: reporte
titulo: Inventario del Proyecto GlobalMart OS
estado: vigente
fuentes: [src/, Docker/, GlobalMart_ContextMaster.md]
verificado_contra_codigo: true
ultima_revision: 2026-09-29
---
# Inventario del Proyecto GlobalMart OS
**Fecha de revisión:** 2026-09-28  
**Rama:** docs/context-graph  
**Fuentes verificadas contra código:** sí

---

## (a) Árbol de servicios y proyectos reales

```
src/
├── TenantIdentityService/       MS-1 · Tenant & Identity
│   ├── Controllers/  AuthController.cs · UsuarioController.cs · TenantConfigController.cs
│   ├── Data/  TenantDbContext + Migrations (InitialCreate)
│   ├── Models/  Tenant · Usuario · UsuarioRol · Rol
│   ├── Repositories/  ITenantRepository · IUsuarioRepository · IUserRepository
│   ├── Services/  IAuthService · IJwtService
│   ├── Middleware/  TenantMiddleware.cs
│   ├── Validators/  LoginRequestValidator.cs
│   └── tests/  (ningún proyecto de test encontrado)
│
├── TaxComplianceService/        MS-2 · Tax & Compliance
│   ├── Controllers/  TaxController.cs
│   ├── Data/  TaxDbContext + Migrations (InitialCreate)
│   ├── Models/  ConfiguracionFiscal · DocumentoTributario · TaxCalculateRequest · TaxCalculateRequestValidator
│   ├── Services/  ITaxCalculatorService · ITenantConfigClient · TaxBreakdown · TaxCalculatorService · TaxItem · TenantConfigClient · TenantConfigResponse
│   └── tests/  TaxComplianceService.ManualTest/
│
├── CatalogPricingService/       MS-3 · Catalog & Pricing
│   ├── Controllers/  ProductoController.cs · CategoriaController.cs
│   ├── Data/  CatalogDbContext + Migrations (InitialCreate · SyncCodigoQrUrl · AddCategoriaHierarchy · AddProductoNombreTrgmIndex)
│   ├── Models/  Categoria · Precio · Producto
│   ├── DTOs/  CategoriaResponseDto · CreateCategoriaDto · CreateProductoDto · UpdateProductoDto
│   ├── Repositories/  IProductoRepository · ProductoRepository · ICategoriaRepository · CategoriaRepository
│   ├── Services/  IProductoService · ProductoService · ICategoriaService · CategoriaService
│   ├── Validators/  CreateCategoriaDtoValidator · CreateProductoDtoValidator · UpdateProductoDtoValidator
│   └── tests/  tests_manual_producto.http
│
├── WarehouseInventoryService/   MS-4 · Warehouse & Inventory
│   ├── Controllers/  StockController.cs
│   ├── Data/  WarehouseDbContext + Migrations (InitialCreate · AddEventosKafkaProcesados)
│   ├── Models/  Stock · Lote · MovimientoStock · EventoKafkaProcesado
│   ├── Repositories/  IStockRepository · StockRepository
│   ├── Messaging/  KafkaConsumerService.cs (consume: sale.completed) · SaleCompletedEvent.cs
│   └── Middleware/  TenantMiddleware.cs
│
├── POSCartService/              MS-5 · POS & Cart
│   ├── Controllers/  VentasController.cs · TurnosController.cs
│   ├── Data/  PosCartDbContext + Migrations (InitialCreate)
│   ├── Models/  Venta · ItemVenta · Turno · Pago · Anulacion
│   ├── Repositories/  IVentaRepository · VentaRepository · ITurnoRepository · TurnoRepository · IItemVentaRepository · ItemVentaRepository
│   ├── Services/  VentaService · TurnoService · CatalogClient · TaxClient
│   ├── Validators/  AbrirTurnoRequestValidator · AgregarItemRequestValidator · CrearVentaRequestValidator · ModificarCantidadItemRequestValidator
│   ├── Exceptions/  ExternalServiceException · TurnoYaAbiertoException
│   ├── Middleware/  TenantMiddleware.cs
│   └── tests/  POSCartService.AgregarItemTest · POSCartService.ManualTest
│
├── SupplyChainService/          MS-6 · Supply Chain
│   ├── Data/  SupplyChainDbContext + Migrations (InitialCreate)
│   ├── Models/  OrdenCompra · OrdenCompraItem · Proveedor · Envio · CostoLandedHistorico
│   └── (SIN Controllers ni Services implementados)
│
├── AnalyticsNotificationService/ MS-7 · Analytics & Notification
│   ├── Data/  AnalyticsDbContext + Migrations (InitialAnalyticsSchema)
│   ├── Models/  Alerta · HistorialEnvio · KPIVenta
│   └── (SIN Controllers ni Kafka consumers implementados)
│
└── LoyaltyCustomerService/      MS-8 · Loyalty & Customer
    ├── Data/  LoyaltyDbContext + Migrations (InitialLoyaltySchema)
    ├── Models/  ClienteAfiliado · MovimientoPuntos · SaldoPuntos · TierMembresia
    └── (SIN Controllers implementados — tiene Kafka Consumer/Producer registrado en Program.cs)
```

---

## (b) Endpoints reales por servicio

### MS-1 · TenantIdentityService

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| POST | `/auth/login` | Público | Login, genera JWT |
| GET | `/api/v1/users` | JWT + ADMIN | Lista usuarios paginados del tenant |
| POST | `/api/v1/users` | JWT + ADMIN | Crear usuario |
| PUT | `/api/v1/users/{id}` | JWT + ADMIN | Actualizar usuario |
| DELETE | `/api/v1/users/{id}` | JWT + ADMIN | Desactivar usuario |
| POST | `/api/v1/users/{id}/roles` | JWT + ADMIN | Asignar roles |
| GET | `/tenants/{id}/config` | JWT | Obtener config del tenant |
| PUT | `/tenants/{id}/config` | JWT + ADMIN | Actualizar config del tenant |
| GET | `/health` | Público | Health check |

**NO implementados** (están en el ContextMaster pero no en el código):
- `POST /auth/refresh` — NO VERIFICADO en código
- `GET /tenants/{id}/sucursales` — NO VERIFICADO en código
- `GET /fx/rates` — NO VERIFICADO en código

### MS-2 · TaxComplianceService

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| POST | `/tax/calculate` | JWT | Calcula IVA para lista de items |
| GET | `/health` | Público | Health check |

**NO implementados** (en diseño):
- `POST /dte/solicitar-folio` — PLANIFICADO
- `POST /dte/emitir` — PLANIFICADO
- `GET /dte/{id}/estado` — PLANIFICADO
- `GET /reportes/declaracion-fiscal` — PLANIFICADO

### MS-3 · CatalogPricingService

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| GET | `/products` | JWT | Lista productos paginados |
| GET | `/products/search?q=` | JWT | Búsqueda por nombre (trigram index) |
| GET | `/products/lookup?barcode=` | JWT | Lookup por código de barras |
| GET | `/products/{id}` | JWT | Obtener producto por ID |
| POST | `/products` | JWT | Crear producto |
| PUT | `/products/{id}` | JWT | Actualizar producto |
| GET | `/categories` | JWT | Listar categorías |
| POST | `/categories` | JWT | Crear categoría |
| GET | `/health` | Público | Health check |

**NO implementados** (en diseño):
- `GET/POST /prices` — PLANIFICADO
- `GET/POST /promotions` — PLANIFICADO
- `POST /uom/convert` — PLANIFICADO

### MS-4 · WarehouseInventoryService

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| GET | `/stock/{productId}?sucursal_id=` | JWT | Consultar stock por producto y sucursal |
| GET | `/health` | Público | Health check |

**Kafka Consumer:**
- Consume: `sale.completed` → descuenta stock con idempotencia (EventosKafkaProcesados)

**NO implementados** (en diseño):
- `POST /stock/ajuste` — PLANIFICADO
- `POST /recepciones` — PLANIFICADO
- `GET /lotes` — PLANIFICADO
- `POST /mermas` — PLANIFICADO
- `POST /transferencias` — PLANIFICADO
- `POST /conteos` — PLANIFICADO

### MS-5 · POSCartService

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| POST | `/turnos/abrir` | JWT | Abrir turno con fondo inicial |
| GET | `/turnos/activo` | JWT | Ver turno activo del cajero |
| POST | `/turnos/cerrar` | JWT | Cerrar turno activo |
| POST | `/ventas` | JWT | Crear venta (carrito vacío) |
| POST | `/ventas/{id}/items` | JWT | Agregar ítem al carrito |
| PUT | `/ventas/{id}/items/{itemId}` | JWT | Modificar cantidad de ítem |
| DELETE | `/ventas/{id}/items/{itemId}` | JWT | Eliminar ítem del carrito |
| GET | `/ventas/{id}` | JWT | Obtener venta por ID |
| GET | `/ventas/turno/{turnoId}` | JWT | Ventas de un turno |
| POST | `/ventas/{id}/cobrar` | JWT | Completar cobro (PARCIAL — VentaService.CompletarAsync existe; integración con pasarela NO implementada) |
| POST | `/ventas/{id}/anular` | JWT | Anular venta |
| GET | `/health` | Público | Health check |

**Kafka:** NO se encontró producción de `sale.completed` en VentaService.cs (VentaService.CompletarAsync solo cambia estado, SIN publicar a Kafka).

### MS-6 · SupplyChainService

No tiene controllers implementados. Solo tiene:
- Modelos de datos (OrdenCompra, Proveedor, Envio, CostoLandedHistorico)
- DbContext con migración inicial
- Program.cs con auth/swagger configurado

**TODO el diseño de endpoints es PLANIFICADO.**

### MS-7 · AnalyticsNotificationService

No tiene controllers ni Kafka consumers implementados. Solo tiene:
- Modelos (Alerta, HistorialEnvio, KPIVenta)
- DbContext con migración inicial
- Program.cs

**TODO el diseño es PLANIFICADO.**

### MS-8 · LoyaltyCustomerService

No tiene controllers implementados. Tiene:
- Modelos (ClienteAfiliado, MovimientoPuntos, SaldoPuntos, TierMembresia)
- DbContext con migración inicial
- Kafka Consumer/Producer registrados en Program.cs (pero no hay handlers implementados)

**TODO el diseño de endpoints es PLANIFICADO.**

---

## (c) Topics Kafka realmente producidos/consumidos en código

| Topic | Productor (código) | Consumidor (código) |
|-------|-------------------|---------------------|
| `sale.completed` | **NO ENCONTRADO** en código MS-5 | MS-4: KafkaConsumerService ✓ |
| `stock.alert` | NO implementado | NO implementado |
| `expiry.alert` | NO implementado | NO implementado |
| `sale.reversed` | NO implementado | NO implementado |
| `stock.updated` | NO implementado | NO implementado |
| `purchase.received` | NO implementado | NO implementado |
| `fx.rate.updated` | NO implementado | NO implementado |
| `points.updated` | NO implementado | NO implementado |

**Topics definidos en docker-compose (kafka-init-topics) pero SIN código:**
`tenant.created`, `tenant.updated`, `user.registered`, `product.created`, `product.price_updated`, `shift.closed`, `stock.low`, `product.expiring_soon`, `purchase_order.received`

**Hallazgo crítico:** MS-5 NO publica `sale.completed`. VentaService.CompletarAsync solo cambia el estado en DB. MS-4 tiene el consumer listo pero sin productor activo en MS-5.

---

## (d) Pantallas del frontend

```
globalmart-frontend/src/
├── App.tsx              — Router con rutas: / → Login, /pos → Pos, /admin → Admin
├── pages/
│   ├── Login.tsx        — Formulario login (email, password, tenantId). Llama POST http://127.0.0.1:5124/auth/login directo (sin Kong)
│   ├── Pos.tsx          — Carrito de compras (items hardcodeados de ejemplo, sin integración API real)
│   └── Admin.tsx        — Página de admin (contenido NO verificado)
├── components/
│   ├── AppLayout.tsx    — Layout general
│   └── pos/
│       ├── BarcodeInput.tsx — Input con debounce para búsqueda de productos
│       └── CartItem.tsx     — Componente de ítem en carrito
└── hooks/
    └── useAuth.ts       — Hook de autenticación con electron-store
```

**Observación crítica:** La página `Pos.tsx` tiene datos hardcodeados (2 productos de ejemplo) y el botón "Cobrar" muestra `alert('Funcionalidad de cobro se implementará en el futuro.')`. NO está integrada con la API real.

**El frontend llama directo al puerto 5124 sin pasar por Kong (puerto 8000).**

**No hay:** RoleSwitcher, pantalla de gestión de turnos, integración con balanza serial, ni tests Playwright.

---

## (e) Sprint real en curso según git

Basado en `git log`:
- Commits más recientes: TI3-179 a TI3-195 (tareas de Jira/Linear)
- Sprint activo: **Sprint 2 o posterior** (ya que Sprint 1 estaba centrado en MS-1, MS-3, MS-5 básico y el código muestra funcionalidad avanzada como Kafka consumer en MS-4, multi-rol, y TenantConfig)
- Trabajo reciente (últimas 2 semanas):
  - TI3-195: Tests POSCartService (AgregarItemTest)
  - TI3-194, TI3-193, TI3-192, TI3-191: VentasController (modificar/eliminar items)
  - TI3-187, TI3-188: KafkaConsumerService en Warehouse
  - TI3-186: Migración AddEventosKafkaProcesados (idempotencia Kafka)
  - TI3-185: KafkaConsumerService mejorado
  - TI3-183, TI3-184: StockController + TenantMiddleware en Warehouse
  - TI3-181, TI3-180: TenantConfigController
  - TI3-179: UsuarioController (asignar roles)

El ContextMaster menciona "Sprint 1" como el actual. El código muestra que **se está en Sprint 2 o 3** (inventario Kafka, multi-tenant en warehouse, gestión avanzada de ventas).

---

## Notas adicionales

- **Kong:** Solo enruta MS-1 (`/api/auth`, `/api/users`, `/api/tenants`), MS-3 (`/api/products`, `/api/prices`, `/api/promotions`) y MS-5 (`/api/turnos`, `/api/ventas`). MS-2 accedido internamente por MS-5. MS-4, MS-6, MS-7, MS-8 NO tienen rutas Kong.
- **Docker:** 15 servicios en docker-compose: 8 PostgreSQL, Zookeeper, Kafka, kafka-init-topics, Kafdrop, Kong, Prometheus, Grafana.
- **CI:** GitHub Actions en `.github/workflows/ci.yml` — build todos los servicios + docker compose + `dotnet test`.
- **Diagramas:** 9 en `diagramas-casos-uso/nuevo/` (archivos .mmd Mermaid) + ERDs en `diagramas/martin/` + secuencias en `diagramas/nicolas/` + flujos UI en `diagramas/rodrigo/`.
- **Tests existentes:** Solo tests manuales/smoke (.http files y console apps) en MS-2 y MS-5. No hay xUnit unit tests encontrados.

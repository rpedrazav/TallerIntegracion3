---
id: ms3-catalog
tipo: microservicio
titulo: MS-3 · Catalog & Pricing Service
estado: parcial
fuentes: [src/CatalogPricingService/]
verificado_contra_codigo: true
ultima_revision: 2026-10-14
depende_de: [ms1-identity]
publica: []
consume: []
reglas: [RN-11, RF-04, RF-11, CP-01, CP-04, CP-05]
---
# MS-3 · Catalog & Pricing Service

> Gestiona el catálogo de productos del tenant con búsqueda por nombre (full-text) y lookup por código de barras. Soporta categorías jerárquicas y productos de peso variable. Precios dinámicos y promociones son PLANIFICADOS.

## Endpoints reales

| Método | Ruta | Auth | Estado |
|--------|------|------|--------|
| GET | `/products?page=&pageSize=` | JWT | [IMPLEMENTADO] |
| GET | `/products/search?q=&page=&pageSize=` | JWT | [IMPLEMENTADO] |
| GET | `/products/lookup?barcode=` | JWT | [IMPLEMENTADO] |
| GET | `/products/{id}` | JWT | [IMPLEMENTADO] |
| POST | `/products` | JWT | [IMPLEMENTADO] |
| PUT | `/products/{id}` | JWT; ADMIN si cambia IsActive | [IMPLEMENTADO] |
| DELETE | `/products/{id}` | JWT + ADMIN | [IMPLEMENTADO — baja lógica] |
| GET | `/categories` | JWT | [IMPLEMENTADO] |
| POST | `/categories` | JWT | [IMPLEMENTADO] |
| GET | `/health` | Público | [IMPLEMENTADO] |

### No implementados
- `GET/POST /prices` — precios por sucursal [PLANIFICADO]
- `GET/POST /promotions` — promociones con fechas [PLANIFICADO]
- `POST /uom/convert` — conversión de unidades [PLANIFICADO]

### Baja lógica de productos — TI3-642 / S2-011

`ProductoController.Delete` exige `[Authorize(Roles = "ADMIN")]` y un `tenant_id` válido en el JWT. `ProductoService.DesactivarProductoAsync` delega en `ProductoRepository.DeactivateAsync`: la escritura filtra por ID, tenant e `IsActive=true` y actualiza exclusivamente `IsActive=false`, sin borrar la fila ni requerir migraciones.

- Producto propio activo: 204 sin body.
- Producto propio ya inactivo: 204 sin body y sin modificar nuevamente la fila.
- Inexistente o perteneciente a otro tenant: idéntico 404 con `{ "message": "Producto no encontrado." }`.
- Sin JWT: 401. Autenticado sin ADMIN: 403.
- PUT compara el estado solicitado con el persistido y rechaza con 403 cambios de `IsActive` de usuarios no ADMIN. Conserva las ediciones de otros campos; un PUT no administrativo excluye `IsActive` de su escritura para no sobrescribir una baja concurrente.

La baja conserva el producto y sus referencias lógicas. MS-5 conserva nombre y precio en `ItemVenta`, e impuestos/totales en `Venta`; consultar ventas históricas no requiere consultar catálogo. MS-5 no fue modificado. Listado, search y lookup siguen incluyendo inactivos; no se agregó un filtro global por estado.

TI3-643 / S2-012: `GET /products/{id}`, utilizado por el POS para resolver productos, consulta `GetActiveByIdAsync` y solo devuelve productos del tenant con `IsActive=true`. Un producto desactivado o de otro tenant retorna 404. `GetByIdAsync` permanece sin filtro de estado para que PUT pueda localizar y reactivar productos inactivos mediante ADMIN. El test `GetById_ProductoDesactivado_Retorna404YConservaFila` fue aprobado el 2026-10-14 (1/1): verifica 200 activo, DELETE 204, GET posterior 404 y fila conservada con `IsActive=false`.

Pruebas: [DesactivarProductoIntegrationTests.cs](../../../tests/GlobalMart.IntegrationTests/DesactivarProductoIntegrationTests.cs), 11/11 aprobadas el 2026-10-14 con PostgreSQL 16 y esquemas aislados. Cubren fila y datos conservados, segunda baja, 404 indistinguible, tenant ajeno intacto, 401/403, bloqueo de activación/desactivación por PUT sin ADMIN y regresión de ediciones permitidas. Build de MS-3 aprobado.

Cierre TI3-642: test adicional `Delete_ProductoVendido_ConservaVentaHistoricaConsultableYDatosIntactos`, ejecutado individualmente el 2026-10-14: 1/1 aprobado. Prepara una venta COMPLETADA con un ItemVenta referido al producto y consulta `GET /ventas/{id}` en MS-5 antes y después del DELETE ADMIN en MS-3 (204). Verifica que venta e ítem siguen consultables, conservando sus IDs, ProductoId, NombreProducto, PrecioUnitario, Cantidad, Subtotal e impuestos/total de venta. Confirma además la fila de catálogo con IsActive=false y sus demás datos intactos. Usa esquemas PostgreSQL aislados y no modifica código productivo.

## Modelo de datos

```
Producto
  id              GUID PK
  tenant_id       GUID (discriminador multi-tenant)
  nombre          string (con índice trigram pg_trgm para búsqueda fuzzy)
  descripcion     string?
  codigo_barras   string
  codigo_qr_url   string?
  categoria_id    GUID FK → Categoria
  uom_base_id     GUID? (UOM base del producto)
  precio_base     decimal
  es_peso_variable bool (true = producto a granel como frutas/carnes)
  is_active       bool
  created_at      DateTime

Categoria
  id         GUID PK
  tenant_id  GUID
  nombre     string
  parent_id  GUID? → Categoria (jerarquía de categorías)
  activo     bool

Precio
  id          GUID PK
  tenant_id   GUID
  producto_id GUID FK
  sucursal_id GUID?
  precio      decimal
  vigente_desde DateTime
  vigente_hasta DateTime?
```

## Características de búsqueda

- **Trigram index (`pg_trgm`):** migración `AddProductoNombreTrgmIndex` agregada 2026-09-27. Permite búsqueda fuzzy eficiente (`ILIKE '%query%'`) sin full scan.
- **Lookup por código de barras:** búsqueda exacta por `CodigoBarras` filtrada por `tenant_id`.
- **Categorías jerárquicas:** `parent_id` nullable permite árbol de categorías (agregado en migración `AddCategoriaHierarchy` 2026-09-25).

## Multi-tenant

- Extrae `tenant_id` del JWT claim `tenant_id`
- Fallback en Development: header `X-Tenant-ID` (para pruebas manuales sin JWT)
- Todos los queries filtran por `tenant_id` explícitamente en el service

## Casos de uso cubiertos

| ID | Caso de Uso | Estado |
|----|-------------|--------|
| CP-01 | Crear Producto | [IMPLEMENTADO] |
| CP-02 | Editar Producto | [IMPLEMENTADO] |
| CP-03 | Asignar Código Barras/QR | [IMPLEMENTADO] |
| CP-04 | Escanear Código Barras | [IMPLEMENTADO] |
| CP-05 | Buscar Producto | [IMPLEMENTADO] |
| CP-10 | Definir Precio Base | [IMPLEMENTADO] |
| CP-15 | Gestionar Categorías | [IMPLEMENTADO] |
| CP-06 | Configurar UOM | [PARCIAL — campo existe en modelo] |
| CP-07 | Convertir Unidades | [PLANIFICADO] |
| CP-08 | Producto a Granel | [PARCIAL — `es_peso_variable` en modelo] |
| CP-09 | Capturar Peso de Balanza | [PLANIFICADO — requiere frontend] |
| CP-11 | Precio Dinámico | [PLANIFICADO] |
| CP-12 | Descuento/Promoción | [PLANIFICADO] |
| CP-13 | Rango Fechas Promoción | [PLANIFICADO] |
| CP-14 | Precio por Sucursal | [PLANIFICADO] |

## Brechas

| Brecha | Impacto |
|--------|---------|
| Sin precios por sucursal | RN-11 no cumplida |
| Sin promociones activas | RF-21 parcial |
| Sin Kafka (catalog.updated) | MS-7 no recibe cambios de catálogo |

## Conexiones
- Depende de: [[ms1-identity]] (tenant_id en JWT)
- Es consumido por: [[ms5-pos]] (CatalogClient para lookup de productos)
- Reglas: [[multi-tenant]] (RN-01), [[reglas-negocio]] (RN-11)

## Fuentes
- `src/CatalogPricingService/Controllers/ProductoController.cs`
- `src/CatalogPricingService/Controllers/CategoriaController.cs`
- `src/CatalogPricingService/Data/Migrations/20260927164215_AddProductoNombreTrgmIndex.cs`
- `src/CatalogPricingService/Data/Migrations/20260925154800_AddCategoriaHierarchy.cs`

---
id: ms3-catalog
tipo: microservicio
titulo: MS-3 · Catalog & Pricing Service
estado: parcial
fuentes: [src/CatalogPricingService/]
verificado_contra_codigo: true
ultima_revision: 2026-10-02
depende_de: [ms1-identity]
publica: []
consume: []
reglas: [RN-11, RF-04, RF-11, CP-01, CP-04, CP-05]
---
# MS-3 · Catalog & Pricing Service

> Gestiona el catálogo de productos del tenant con búsqueda por nombre (full-text) y lookup por código de barras. Soporta categorías jerárquicas y productos de peso variable. Incluye seed de 30 productos variados con códigos de barras y precios (TI3-256). Precios dinámicos y promociones son PLANIFICADOS.

## Endpoints reales

| Método | Ruta | Auth | Estado |
|--------|------|------|--------|
| GET | `/products?page=&pageSize=` | JWT | [IMPLEMENTADO] |
| GET | `/products/search?q=&page=&pageSize=` | JWT | [IMPLEMENTADO] |
| GET | `/products/lookup?barcode=` | JWT | [IMPLEMENTADO] |
| GET | `/products/{id}` | JWT | [IMPLEMENTADO] |
| POST | `/products` | JWT | [IMPLEMENTADO] |
| PUT | `/products/{id}` | JWT | [IMPLEMENTADO] |
| GET | `/categories` | JWT | [IMPLEMENTADO] |
| POST | `/categories` | JWT | [IMPLEMENTADO] |
| GET | `/health` | Público | [IMPLEMENTADO] |

### No implementados
- `GET/POST /prices` — precios por sucursal [PLANIFICADO]
- `GET/POST /promotions` — promociones con fechas [PLANIFICADO]
- `POST /uom/convert` — conversión de unidades [PLANIFICADO]
- `DELETE /products/{id}` — desactivar producto [PLANIFICADO — solo Update]

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
| DELETE no implementado | Solo desactivar via PUT |
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

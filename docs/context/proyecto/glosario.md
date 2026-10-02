---
id: glosario
tipo: proyecto
titulo: Glosario de Términos — GlobalMart OS
estado: implementado
fuentes: [GlobalMart_ContextMaster.md, src/]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: [vision]
publica: []
consume: []
reglas: []
---
# Glosario de Términos — GlobalMart OS

> Términos del dominio usados en el código y la documentación.

| Término | Definición |
|---------|-----------|
| **Tenant** | Un minimarket cliente de la plataforma. Cada tenant tiene datos, usuarios, configuración fiscal y precios completamente aislados. |
| **tenant_id** | GUID discriminador en cada tabla de cada servicio. Inyectado automáticamente en cada query por el TenantMiddleware. |
| **Turno de caja** | Sesión de trabajo de un cajero. Tiene fondo inicial, registra todas las ventas del período y cierra con cuadre de caja. RN-06 exige turno abierto para vender. |
| **Carrito / Venta PENDIENTE** | Objeto Venta en estado PENDIENTE mientras se agregan ítems. |
| **ItemVenta** | Un producto dentro de una venta, con snapshot de nombre/precio y cantidad. |
| **FEFO** | First-Expired, First-Out. Política de despacho que prioriza el lote con menor fecha de vencimiento. Ver [[fefo]]. |
| **Lote** | Agrupación de unidades de un producto con la misma fecha de vencimiento, recibida en una recepción. |
| **DTE** | Documento Tributario Electrónico. Boleta o factura electrónica con folio validado por la entidad fiscal del país (SII, AFIP, IRS). |
| **Folio** | Número único para un DTE, asignado por la entidad fiscal del país. |
| **Costo Landed** | Costo real de un producto importado incluyendo flete, aranceles, seguros y gastos aduaneros. |
| **CAF** | Código de Autorización de Folios (Chile/SII). Rango de folios para emitir DTEs offline. |
| **CAE** | Código de Autorización Electrónico (Argentina/AFIP). Equivalente al CAF. |
| **RBAC** | Role-Based Access Control. Sistema de control de acceso por roles. |
| **active_role** | El rol activo en la sesión actual (puede ser uno de varios roles asignados). |
| **Encargado de Tienda** | Actor compuesto que hereda CAJERO + REPONEDOR + ADMIN simultáneamente. |
| **UOM** | Unit of Measure. Unidad de medida del producto (kg, L, unidad, etc.). |
| **Peso variable** | Producto cuyo precio se calcula por peso (frutas, carnes). Campo `EsPesoVariable` en modelo Producto. |
| **sucursal_id** | Identificador de una sucursal dentro de un tenant. Presente en JWT (campo libre, sin endpoint de gestión implementado aún). |
| **Anulación** | Cancelación de una venta completada. Genera objeto Anulacion y (en diseño) solicita reembolso a la pasarela. |
| **EventoKafkaProcesado** | Tabla de idempotencia en MS-4: registra VentaId de cada `sale.completed` procesado para evitar descuentos duplicados. |
| **DB-less** | Modo de Kong sin base de datos. La configuración se carga desde `kong.yaml` declarativo. |
| **TrigM** | Índice trigram de PostgreSQL (`pg_trgm`) para búsqueda fuzzy de texto. Usado en búsqueda de productos. |
| **seed** | Datos iniciales de desarrollo cargados al iniciar la app (tenant demo + cajero demo en MS-1). |
| **POS** | Point of Sale. Terminal de punto de venta. También el microservicio MS-5. |

## Conexiones
- Actores: [[actores]]
- Reglas de negocio: [[reglas-negocio]]
- Multi-tenant: [[multi-tenant]]

## Fuentes
- `GlobalMart_ContextMaster.md` §1, §14
- `src/WarehouseInventoryService/Models/EventoKafkaProcesado.cs`

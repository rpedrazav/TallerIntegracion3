---
id: matriz-rutas
tipo: infra
titulo: Matriz Integral de Rutas, Puertos y Endpoints
estado: implementado
fuentes: [Docker/config/kong.yaml, src/*/Controllers/*.cs, src/*/Properties/launchSettings.json, globalmart-frontend/src/]
verificado_contra_codigo: true
ultima_revision: 2026-10-16
depende_de: [ms1-identity, ms2-tax, ms3-catalog, ms4-warehouse, ms5-pos, kong]
publica: []
consume: []
reglas: [RN-01, RN-06, RN-07]
---

# Matriz Integral de Rutas, Puertos y Endpoints — GlobalMart OS

> Tarea: **TI3-657 (Plan ID: S2-026)**  
> Este documento consolida el inventario auditado y verificado de todos los endpoints de la plataforma, comparando las rutas expuestas por el API Gateway (Kong), los atributos de enrutamiento en controladores ASP.NET Core 8, los puertos locales de desarrollo, los puertos internos de contenedor y los componentes consumidores del frontend de escritorio (Electron + React).

---

## 1. Mapeo de Puertos por Microservicio y Entorno

| Servicio | Nombre en Kong / Docker | Puerto Host Dev (`launchSettings.json`) | Puerto Interno Contenedor | Puerto Kong Gateway | Base URL Remota Dev |
|---|---|---|---|---|---|
| **MS-1 Tenant & Identity** | `tenant-identity-service` | `5124` (HTTP) / `7206` (HTTPS) | `5001` / `8080` | `8000` (`/api/auth`, `/api/users`, `/api/tenants`) | `https://auth-rpedraza.dev.censei.cl` |
| **MS-2 Tax & Compliance** | `tax-compliance-service` | `5239` (HTTP) / `7062` (HTTPS) | `5002` / `8080` | N/A (Consumo interno vía MS-5) | N/A |
| **MS-3 Catalog & Pricing** | `catalog-pricing-service` | `5203` (HTTP) / `7250` (HTTPS) | `5003` / `8080` | `8000` (`/api/products`, `/api/categories`, `/api/prices`) | `https://catalog-rpedraza.dev.censei.cl` |
| **MS-4 Warehouse & Inventory**| `warehouse-inventory-service`| `5205` (HTTP) / `7019` (HTTPS) | `5004` / `8080` | N/A (Event-driven vía Kafka) | N/A |
| **MS-5 POS & Cart** | `pos-cart-service` | `5000` (HTTP Kestrel default) | `5005` / `8080` | `8000` (`/api/turnos`, `/api/ventas`) | `https://pos-rpedraza.dev.censei.cl` |
| **MS-6 Supply Chain** | `supply-chain-service` | `[PLANIFICADO]` | `5006` / `8080` | `[PLANIFICADO]` | `[PLANIFICADO]` |
| **MS-7 Analytics & Notif.** | `analytics-notification-service` | `5127` (HTTP) / `7209` (HTTPS) | `5007` / `8080` | `[PLANIFICADO]` | `[PLANIFICADO]` |
| **MS-8 Loyalty & Customer** | `loyalty-customer-service` | `5128` (HTTP) / `7210` (HTTPS) | `5008` / `8080` | `[PLANIFICADO]` | `[PLANIFICADO]` |
| **Simulador de Pagos** | `payment-simulator` | `5050` (HTTP) | `5050` | `8000` (`/simulator/payments`, `/simulator/health`) | N/A (Mock desarrollo) |
| **Kong API Gateway** | `kong` | `8000` (Proxy), `8001` (Admin) | `8000` (Proxy), `8001` (Admin) | `8000` | N/A |

---

## 2. Matriz Detallada de Endpoints y Rutas

### MS-1: Tenant & Identity Service (Dev Host :5124)

| Endpoint Lógico | Método | Ruta Controlador (`[Route]`) | Ruta Kong (`kong.yaml`) | Auth / Requisitos | Consumidor Frontend |
|---|---|---|---|---|---|
| Iniciar Sesión | `POST` | `auth/login`, `api/auth/login` | `/api/auth/login` | ❌ Público (Sin JWT) | `src/api/client.ts` (`identityApi.login`), `Login.tsx` |
| Listar Usuarios | `GET` | `users`, `api/users`, `api/v1/users` | `/api/users` | ✅ JWT (Rol: ADMIN, RN-07) | `src/pages/Admin.tsx` |
| Crear Usuario | `POST` | `users`, `api/users`, `api/v1/users` | `/api/users` | ✅ JWT (Rol: ADMIN, RN-07) | `src/pages/Admin.tsx`, `CrearUsuarioModal.tsx` |
| Modificar Usuario | `PUT` | `users/{id}`, `api/users/{id}`, `api/v1/users/{id}` | `/api/users` | ✅ JWT (Rol: ADMIN, RN-07) | `src/pages/Admin.tsx` |
| Desactivar Usuario | `DELETE` | `users/{id}`, `api/users/{id}`, `api/v1/users/{id}` | `/api/users` | ✅ JWT (Rol: ADMIN, RN-07) | `src/pages/Admin.tsx` |
| Asignar Roles | `POST` | `users/{id}/roles`, `api/users/{id}/roles` | `/api/users` | ✅ JWT (Rol: ADMIN, RN-07) | `src/pages/Admin.tsx` |
| Obtener Config Tenant | `GET` | `tenants/{id}/config`, `api/tenants/{id}/config`, `api/v1/tenants/{id}/config` | `/api/tenants` | ✅ JWT (Tenant autenticado, RN-01) | `src/components/admin/TenantConfig.tsx` |
| Actualizar Config Tenant | `PUT` | `tenants/{id}/config`, `api/tenants/{id}/config`, `api/v1/tenants/{id}/config` | `/api/tenants` | ✅ JWT (Tenant autenticado, RN-01) | `src/components/admin/TenantConfig.tsx` |
| Listar Sucursales | `GET` | `sucursales`, `api/sucursales` | `/api/sucursales` | ✅ JWT (Claim: tenant_id, RN-01) | `src/pages/Pos.tsx` (selector sucursal) |
| Crear Sucursal | `POST` | `sucursales`, `api/sucursales` | `/api/sucursales` | ✅ JWT (Rol: ADMIN, RN-01) | Módulo Administración |

### MS-3: Catalog & Pricing Service (Dev Host :5203)

| Endpoint Lógico | Método | Ruta Controlador (`[Route]`) | Ruta Kong (`kong.yaml`) | Auth / Requisitos | Consumidor Frontend |
|---|---|---|---|---|---|
| Listar Productos | `GET` | `products`, `api/products` | `/api/products` | ✅ JWT (RN-01) | `src/api/client.ts` (`catalogApi.listProducts`), `ProductosPage.tsx`, `Pos.tsx` |
| Buscar por Código Barras | `GET` | `products/lookup`, `api/products/lookup` | `/api/products` | ✅ JWT (RN-01) | `src/api/client.ts` (`catalogApi.lookupProduct`), `BarcodeInput.tsx` |
| Obtener Producto por ID | `GET` | `products/{id}`, `api/products/{id}` | `/api/products` | ✅ JWT (RN-01) | `MS-5 CatalogClient.cs` |
| Crear Producto | `POST` | `products`, `api/products` | `/api/products` | ✅ JWT (Rol: ADMIN, RN-01) | `src/pages/ProductosPage.tsx` |
| Actualizar Producto | `PUT` | `products/{id}`, `api/products/{id}` | `/api/products` | ✅ JWT (Rol: ADMIN, RN-01) | `src/pages/ProductosPage.tsx` |
| Eliminar/Desactivar Producto | `DELETE` | `products/{id}`, `api/products/{id}` | `/api/products` | ✅ JWT (Rol: ADMIN, RN-01) | `src/pages/ProductosPage.tsx` |
| Listar Categorías | `GET` | `categories`, `api/categories` | `/api/categories` | ✅ JWT (RN-01) | `src/pages/ProductosPage.tsx` |
| Crear Categoría | `POST` | `categories`, `api/categories` | `/api/categories` | ✅ JWT (Rol: ADMIN, RN-01) | `src/pages/ProductosPage.tsx` |

### MS-5: POS & Cart Service (Dev Host :5000)

| Endpoint Lógico | Método | Ruta Controlador (`[Route]`) | Ruta Kong (`kong.yaml`) | Auth / Requisitos | Consumidor Frontend |
|---|---|---|---|---|---|
| Consultar Turno Activo | `GET` | `turnos/activo`, `api/turnos/activo` | `/api/turnos` | ✅ JWT (Claims: cajero_id, tenant_id, RN-06) | `src/api/client.ts` (`posApi.activeShift`), `Pos.tsx` |
| Abrir Turno de Caja | `POST` | `turnos/abrir`, `api/turnos/abrir` | `/api/turnos` | ✅ JWT (Claims: cajero_id, tenant_id, RN-06) | `src/api/client.ts` (`posApi.openShift`), `Pos.tsx` |
| Cuadre de Turno | `POST` | `turnos/cuadre`, `api/turnos/cuadre` | `/api/turnos` | ✅ JWT (Claims: cajero_id, tenant_id, RN-06) | `src/api/client.ts` (`posApi.balanceShift`), `Pos.tsx` |
| Cerrar Turno de Caja | `POST` | `turnos/cerrar`, `api/turnos/cerrar` | `/api/turnos` | ✅ JWT (Claims: cajero_id, tenant_id, RN-06) | `src/api/client.ts` (`posApi.closeShift`), `Pos.tsx` |
| Listar Ventas de Turno | `GET` | `ventas/turno/{shiftId}`, `api/ventas/turno/{shiftId}` | `/api/ventas` | ✅ JWT (Claims: cajero_id, tenant_id) | `src/api/client.ts` (`posApi.salesByShift`), `Pos.tsx` |
| Obtener Venta por ID | `GET` | `ventas/{saleId}`, `api/ventas/{saleId}` | `/api/ventas` | ✅ JWT (Claims: cajero_id, tenant_id) | `src/api/client.ts` (`posApi.getSale`), `Pos.tsx` |
| Iniciar/Crear Venta | `POST` | `ventas`, `api/ventas` | `/api/ventas` | ✅ JWT (Turno ABIERTO, RN-06) | `src/api/client.ts` (`posApi.createSale`), `Pos.tsx` |
| Agregar Ítem a Venta | `POST` | `ventas/{id}/items`, `api/ventas/{id}/items` | `/api/ventas` | ✅ JWT (RN-01, RN-06) | `src/api/client.ts` (`posApi.addSaleItem`), `Pos.tsx` |
| Actualizar Ítem en Venta | `PUT` | `ventas/{id}/items/{itemId}`, `api/ventas/{id}/items/{itemId}` | `/api/ventas` | ✅ JWT (RN-01, RN-06) | `src/api/client.ts` (`posApi.updateSaleItem`), `Pos.tsx` |
| Eliminar Ítem de Venta | `DELETE` | `ventas/{id}/items/{itemId}`, `api/ventas/{id}/items/{itemId}` | `/api/ventas` | ✅ JWT (RN-01, RN-06) | `src/api/client.ts` (`posApi.deleteSaleItem`), `Pos.tsx` |
| Cobrar / Completar Venta | `POST` | `ventas/{id}/cobrar`, `api/ventas/{id}/cobrar` | `/api/ventas` | ✅ JWT (RN-01, RN-02 pasarela si tarjeta) | `src/api/client.ts`, `Pos.tsx` |
| Anular Venta | `POST` | `ventas/{id}/anular`, `api/ventas/{id}/anular` | `/api/ventas` | ✅ JWT (RN-01) | `Pos.tsx` |

### MS-2: Tax & Compliance Service (Dev Host :5239 — Inter-servicio)

| Endpoint Lógico | Método | Ruta Controlador (`[Route]`) | Acceso | Requisitos | Consumidor |
|---|---|---|---|---|---|
| Calcular Impuesto / IVA | `POST` | `tax/calculate`, `api/tax/calculate` | Interno / Gateway | ✅ JWT (RN-01) | `MS-5 TaxClient.cs` |
| Emitir Comprobante Fiscal | `POST` | `comprobantes`, `api/comprobantes` | Interno / Gateway | ✅ JWT (RN-01) | `MS-5` / Evento `sale.completed` |
| Consultar Comprobante | `GET` | `comprobantes/{id}`, `api/comprobantes/{id}` | Interno / Gateway | ✅ JWT (RN-01) | `MS-5` / Auditoría |

### MS-4: Warehouse & Inventory Service (Dev Host :5205)

| Endpoint Lógico | Método | Ruta Controlador (`[Route]`) | Acceso | Requisitos | Consumidor |
|---|---|---|---|---|---|
| Consultar Stock Producto | `GET` | `stock/{productId}`, `api/stock/{productId}` | Interno / Gateway | ✅ JWT (RN-01) | POS / Inventario |

### Simulador de Pasarela de Pagos (Dev Host :5050 / Docker `payment-simulator:5050` — TI3-688 / TI3-689)

| Endpoint Lógico | Método | Ruta Controlador / Servidor | Ruta Kong (`kong.yaml`) | Auth / Requisitos | Consumidor |
|---|---|---|---|---|---|
| Procesar Pago Seguro | `POST` | `simulator/payments`, `api/simulator/payments` | `/simulator/payments`, `/api/simulator/payments` | ❌ Sin JWT / Sin datos de tarjeta (RN-02) | MS-5 `PaymentGatewayClient` |
| Health Check | `GET` | `health`, `simulator/health`, `api/simulator/health` | `/simulator/health`, `/api/simulator/health` | ❌ Público | Docker healthcheck, Smoke Test |

---

## 3. Discrepancias Identificadas y Soluciones Aplicadas

1. **Problema de Prefijo Kong (`strip_path: false` vs Controllers sin `/api`):**
   - *Causa:* Kong enruta rutas como `/api/products` y `/api/ventas` con `strip_path: false`, reenviando el path exacto al upstream. Controladores en MS-1, MS-3 y MS-5 solo declaraban `[Route("products")]` o `[Route("ventas")]`, produciendo errores HTTP 404.
   - *Solución:* Implementación de atributos duales en controladores (`[Route("products")]` y `[Route("api/products")]`). Esto asegura retrocompatibilidad tanto para llamadas directas como para enrutamiento a través de Kong.

2. **Ruta Faltante en Kong para Categorías:**
   - *Causa:* `ProductosPage.tsx` consulta `GET /categories` para el formulario de productos, pero Kong solo tenía `products-route`, `prices-route` y `promotions-route`.
   - *Solución:* Incorporación de `categories-route` en `Docker/config/kong.yaml` bajo MS-3.

3. **Dispersión de URLs Hardcodeadas en Frontend:**
   - *Causa:* Componentes y páginas (`Admin.tsx`, `ProductosPage.tsx`, `TenantConfig.tsx`, `client.ts`) contenían cadenas literales `https://*-rpedraza.dev.censei.cl`.
   - *Solución:* Centralización de configuración en `src/config/api.ts` con soporte para modos `gateway`, `local` y `remote`, y variables de entorno documentadas en `.env.example`.

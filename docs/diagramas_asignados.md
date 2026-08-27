# 📐 Tareas de Diagramación — Base Técnica del Proyecto GlobalMart OS
*(Stack: Electron + React | Kong 3.6 | ASP.NET Core 8 | Entity Framework Core | PostgreSQL 16 | Apache Kafka | xUnit)*

> **Propósito:** Antes de escribir una sola línea de código, cada integrante del equipo debe producir los diagramas técnicos asignados. Estos diagramas son la fuente de verdad de la arquitectura, el dominio, la base de datos, el frontend, los flujos de negocio y la infraestructura del proyecto. Una vez aprobados en equipo, los diagramas guían cada Sprint.
>
> **Formato recomendado:** draw.io (`.drawio.png`), Mermaid (`.md`) o PlantUML (`.puml`) según la herramienta disponible.
>
> **Carpeta destino:** `diagramas/` en la raíz del repositorio, organizada por integrante: `diagramas/diego/`, `diagramas/sebastian/`, etc.

---

## 👤 Diego — Arquitectura General, Infraestructura y Despliegue

> **Perfil:** DevOps / Arquitecto de Solución

### D1 — Diagrama de Contexto del Sistema (C4 Nivel 1)
**Herramienta recomendada:** draw.io o C4 PlantUML
**Descripción:** Vista de altísimo nivel que muestra GlobalMart OS como una "caja negra" y sus relaciones con los actores externos.
**Incluir:**
- Actores: `Administrador`, `Cajero`, `Reponedor`, `Super Admin`
- El sistema: `GlobalMart OS (Desktop + Backend)`
- Sistemas externos: `Proveedor ERP externo`, `SII/AFIP/IRS (tributario)`, `Servicios de pago` (si aplican en el futuro), `Impresora de tickets`
- Flujos de comunicación entre actores y el sistema con etiquetas descriptivas
**Entregable:** `diagramas/diego/D1_context_c4.drawio.png`

---

### D2 — Diagrama de Contenedores (C4 Nivel 2)
**Herramienta recomendada:** draw.io o C4 PlantUML
**Descripción:** Vista detallada de todos los contenedores del sistema y cómo se comunican entre sí.
**Incluir:**
- `Electron Desktop App` (React + TypeScript)
- `Kong API Gateway 3.6` (puerto 8000)
- Los 7 microservicios ASP.NET Core 8 con sus puertos (5001–5007)
- Las 7 instancias de PostgreSQL 16 (postgres-tenant, postgres-catalog, etc.)
- `Apache Kafka Broker` + `Zookeeper`
- Flechas de comunicación: HTTP/REST (Electron → Kong → servicios), Kafka Topics (servicios → Kafka → servicios)
- Puertos expuestos de cada contenedor
**Entregable:** `diagramas/diego/D2_containers_c4.drawio.png`

---

### D3 — Diagrama de Despliegue con Docker Compose
**Herramienta recomendada:** draw.io
**Descripción:** Mapa visual exacto del archivo `docker-compose.yml` y cómo se levantan los contenedores en el entorno local de desarrollo.
**Incluir:**
- Todos los servicios Docker: `postgres-tenant`, `postgres-catalog` (y demás), `zookeeper`, `kafka`, `kong`, `pgadmin`, `prometheus`, `grafana`
- Red Docker interna `minimarket-net` y qué servicios pertenecen a ella
- Puertos mapeados entre host y contenedor (ej. `5432:5432`, `8000:8000`, `9092:9092`)
- Dependencias entre servicios (`depends_on`) con flechas de orden de arranque
- Volúmenes persistentes (ej. `pgdata-tenant`)
- Health-checks (`pg_isready`, Kafka readiness)
**Entregable:** `diagramas/diego/D3_docker_compose_deploy.drawio.png`

---

### D4 — Diagrama de Red y Enrutamiento Kong 3.6
**Herramienta recomendada:** draw.io
**Descripción:** Cómo fluye el tráfico HTTP desde el cliente Electron hasta cada microservicio a través de Kong.
**Incluir:**
- `Electron App (axios)` → `Kong :8000/api/v1/...`
- Rutas Kong mapeadas a upstreams: `/api/v1/auth/*` → `tenant-identity:5001`, `/api/v1/catalog/*` → `catalog-pricing:5002`, etc.
- Plugins Kong aplicados por ruta: `jwt` (rutas protegidas), `rate-limiting` (login), `cors` (global)
- Respuesta JWT con claims (`tenantId`, `sucursalId`, `role`) en el flujo de autenticación
- Rutas públicas vs. rutas protegidas claramente diferenciadas
**Entregable:** `diagramas/diego/D4_kong_routing.drawio.png`

---

### D5 — Diagrama de Pipeline CI/CD (GitHub Actions)
**Herramienta recomendada:** Mermaid o draw.io
**Descripción:** Flujo completo del pipeline de integración y despliegue continuo.
**Incluir:**
- Triggers: `push` a `feat/*` y `develop`
- Jobs: `build-and-test` (dotnet restore → dotnet format → dotnet build → dotnet test → coverlet), `build-docker` (docker build → docker push)
- Job `deploy-staging` (SSH → docker compose pull → docker compose up)
- Branches: `feat/*` → `develop` → `main`
- Artefactos generados: imagen Docker `tenant-identity:sha`, reporte `coverage.cobertura.xml`, `swagger.json`
**Entregable:** `diagramas/diego/D5_ci_cd_pipeline.drawio.png`

---

## 👤 Sebastián — Modelo de Dominio, Clases y Secuencias de Autenticación

> **Perfil:** Backend Lead / DDD / Seguridad JWT

### S1 — Diagrama de Clases del Dominio (Tenant & Identity Service)
**Herramienta recomendada:** draw.io o PlantUML
**Descripción:** Modelo completo de dominio DDD (objetos de dominio, value objects, enums) del servicio de identidad.
**Incluir:**
- Aggregate Root `Tenant` con sus propiedades (`Id: Guid`, `Name: string`, `CountryCode: string`, `Status: TenantStatus`, `CreatedAt: DateTimeOffset`)
- Value Object `TenantConfig` (`Timezone: string`, `CurrencyCode: string`, `CurrencySymbol: string`, `LanguageCode: string`, `DateFormat: string`) como Owned Entity de EF Core
- Entidad `Sucursal` (`Id`, `TenantId`, `Name`, `Address`, `Phone`, `IsHeadquarters`, `IsActive`, `CreatedAt`)
- Entidad `User` (`Id`, `TenantId`, `SucursalId`, `Email`, `PasswordHash`, `Role`, `IsActive`, `LastLoginAt`, `CreatedAt`)
- Entidad `RefreshToken` (`Id`, `TokenHash`, `UserId`, `TenantId`, `ExpiresAt`, `IsRevoked`, `CreatedAt`)
- Enum `UserRole`: `SUPER_ADMIN`, `ADMINISTRADOR`, `CAJERO`, `REPONEDOR`
- Enum `TenantStatus`: `ACTIVE`, `SUSPENDED`, `INACTIVE`
- Relaciones: `Tenant` 1→N `Sucursal`, `Tenant` 1→N `User`, `Sucursal` 1→N `User`, `User` 1→N `RefreshToken`
- Indicar multiplicidades, restricciones y campos obligatorios
**Entregable:** `diagramas/sebastian/S1_domain_model_tenant_identity.drawio.png`

---

### S2 — Diagrama de Secuencia: Flujo de Login JWT End-to-End
**Herramienta recomendada:** PlantUML o draw.io
**Descripción:** Secuencia completa desde que el usuario presiona "Iniciar sesión" hasta recibir el JWT y navegar al dashboard.
**Participantes:** `Electron (React)`, `Kong 3.6`, `Auth Controller (ASP.NET Core)`, `JwtTokenGenerator`, `UserRepository (EF Core)`, `PostgreSQL (postgres-tenant)`
**Flujo a diagramar:**
1. Usuario presiona "Login" → `POST http://localhost:8000/api/v1/auth/login` con `{email, password}`
2. Kong verifica que la ruta `/auth/login` es pública (sin plugin JWT)
3. Kong enruta a `tenant-identity:5001/api/v1/auth/login`
4. `AuthController` llama a `AuthService.LoginAsync(request)`
5. `UserRepository` consulta `SELECT * FROM users WHERE tenant_id = ? AND email = ?` (EF Core)
6. Se verifica password con `BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)`
7. `JwtTokenGenerator.CreateAccessToken()` emite JWT con claims `tenantId`, `sucursalId`, `role`, `tenantConfig`
8. Se genera `RefreshToken`, se persiste en BD, se retorna `{accessToken, refreshToken, user}`
9. Electron guarda token en `electron-store` vía IPC y navega a `#/dashboard`
**Incluir rutas de error:** usuario no encontrado (401), password incorrecta (401), usuario inactivo (403)
**Entregable:** `diagramas/sebastian/S2_sequence_login_jwt.drawio.png`

---

### S3 — Diagrama de Secuencia: Registro de Nuevo Tenant (Multi-Step)
**Herramienta recomendada:** PlantUML o draw.io
**Descripción:** Secuencia del flujo de registro multi-paso desde Electron hasta la creación completa del tenant y primer administrador.
**Participantes:** `Electron (RegisterPage)`, `Kong 3.6`, `TenantsController`, `TenantService`, `SucursalesController`, `AuthController`, `Kafka Producer`, `PostgreSQL`
**Flujo a diagramar:**
1. `POST /api/v1/tenants` → `TenantService.CreateAsync()` → persistir en BD → publicar `tenant.created` en Kafka
2. `POST /api/v1/sucursales` → crear sede matriz "Casa Matriz" para el tenant recién creado
3. `POST /api/v1/auth/register` → crear usuario ADMINISTRADOR con BCrypt password → publicar `user.registered` en Kafka
4. Respuesta final: redirección a `/login` con toast de confirmación
**Incluir manejo de errores:** rollback si algún paso falla (tenant duplicado → 409, email duplicado → 409)
**Entregable:** `diagramas/sebastian/S3_sequence_register_tenant.drawio.png`

---

### S4 — Diagrama de Secuencia: Refresh Token y Logout
**Herramienta recomendada:** PlantUML o draw.io
**Descripción:** Flujo de renovación silenciosa del access token y cierre de sesión seguro.
**Flujo Refresh:**
1. Interceptor de Axios detecta respuesta 401 (token expirado)
2. Llama automáticamente a `POST /api/v1/auth/refresh` con el refresh token almacenado en `electron-store`
3. `AuthService` valida que el refresh token existe en BD, no está revocado y no expiró
4. Genera nuevo `accessToken` (8h) y nuevo `refreshToken` (7d), revoca el anterior en BD
5. Reintenta la petición original con el nuevo token
**Flujo Logout:**
1. Usuario presiona "Cerrar sesión"
2. `POST /api/v1/auth/logout` → `AuthService` marca `IsRevoked = true` en el `RefreshToken` de BD
3. Electron limpia `electron-store` (token + tenant) y navega a `/login`
**Entregable:** `diagramas/sebastian/S4_sequence_refresh_logout.drawio.png`

---

### S5 — Diagrama de Secuencia: Autorización por Rol en ASP.NET Core
**Herramienta recomendada:** PlantUML o draw.io
**Descripción:** Cómo funciona el pipeline de autorización en ASP.NET Core para una petición protegida.
**Participantes:** `Electron`, `Kong JWT Plugin`, `ASP.NET Core Middleware Pipeline`, `JwtBearerHandler`, `TenantResolutionMiddleware`, `[Authorize] Attribute`, `Controller Action`
**Flujo a diagramar:**
1. Electron envía request con `Authorization: Bearer <JWT>` → Kong
2. Kong verifica firma del JWT (plugin `jwt`) → rechaza con 401 si inválido
3. ASP.NET Core `JwtBearerHandler` valida firma, issuer, audience y expiración
4. `TenantResolutionMiddleware` extrae `tenantId` del claim y lo inyecta en `IRequestContext`
5. `[Authorize(Roles = "ADMINISTRADOR")]` valida el claim `role` → rechaza con 403 si no tiene el rol
6. Controller Action se ejecuta con el contexto del tenant correcto
**Entregable:** `diagramas/sebastian/S5_sequence_authorization_pipeline.drawio.png`

---

## 👤 Martín — Modelo Entidad-Relación de los 7 Microservicios (PostgreSQL 16)

> **Perfil:** Base de Datos / Entity Framework Core Migrations

### M1 — ERD: Tenant & Identity Service (`postgres-tenant`)
**Herramienta recomendada:** draw.io (estilo Crow's Foot Notation)
**Tablas a modelar:**
- `tenants` (id UUID PK, name, country_code, timezone, currency_code, currency_symbol, language_code, date_format, status, created_at)
- `sucursales` (id UUID PK, tenant_id UUID FK→tenants, name, address, phone, is_headquarters, is_active, created_at)
- `users` (id UUID PK, tenant_id UUID FK→tenants, sucursal_id UUID FK→sucursales NULLABLE, email, password_hash, role CHECK, is_active, last_login_at, created_at)
- `refresh_tokens` (id UUID PK, token_hash TEXT, user_id UUID FK→users, tenant_id UUID FK→tenants, expires_at TIMESTAMPTZ, is_revoked BOOLEAN, created_at)
**Incluir:** Índices únicos, CHECK constraints, ON DELETE behavior, tipos de datos exactos de PostgreSQL 16
**Entregable:** `diagramas/martin/M1_erd_tenant_identity.drawio.png`

---

### M2 — ERD: Catalog & Pricing Service (`postgres-catalog`)
**Herramienta recomendada:** draw.io
**Tablas a modelar:**
- `categorias` (id UUID PK, tenant_id, nombre, parent_id UUID FK→categorias NULLABLE, level INT, is_active, created_at)
- `unidades_medida` (id UUID PK, tenant_id, nombre, simbolo, tipo ENUM('PESO','VOLUMEN','UNIDAD'), factor_conversion DECIMAL, created_at)
- `productos` (id UUID PK, tenant_id, nombre, descripcion, codigo_barras VARCHAR(50) UNIQUE, codigo_qr_url, categoria_id FK→categorias, uom_base_id FK→unidades_medida, precio_base DECIMAL(12,4), es_peso_variable BOOLEAN, is_active, created_at)
- `producto_precios` (id UUID PK, producto_id FK→productos, sucursal_id UUID, precio DECIMAL(12,4), currency_code CHAR(3), vigente_desde DATE, vigente_hasta DATE NULLABLE)
- `imagenes_producto` (id UUID PK, producto_id FK→productos, url TEXT, orden INT, is_primary BOOLEAN)
**Incluir:** Índices, constraints de precio no negativo, árbol de categorías con parent_id auto-referenciado
**Entregable:** `diagramas/martin/M2_erd_catalog_pricing.drawio.png`

---

### M3 — ERD: POS & Cart Service (`postgres-pos`)
**Herramienta recomendada:** draw.io
**Tablas a modelar:**
- `turnos_caja` (id UUID PK, cajero_id UUID, sucursal_id UUID, tenant_id UUID, monto_apertura DECIMAL, monto_cierre DECIMAL, estado ENUM('ABIERTO','CERRADO'), abierto_at TIMESTAMPTZ, cerrado_at TIMESTAMPTZ)
- `ventas` (id UUID PK, turno_id FK→turnos_caja, cajero_id UUID, sucursal_id UUID, tenant_id UUID, subtotal DECIMAL, impuestos DECIMAL, total DECIMAL, metodo_pago ENUM('EFECTIVO','TARJETA','MIXTO'), estado ENUM('PENDIENTE','COMPLETADA','ANULADA'), created_at)
- `venta_items` (id UUID PK, venta_id FK→ventas, producto_id UUID, nombre_producto VARCHAR, cantidad DECIMAL, peso_kg DECIMAL NULLABLE, precio_unitario DECIMAL, subtotal DECIMAL)
- `pagos` (id UUID PK, venta_id FK→ventas, metodo ENUM('EFECTIVO','TARJETA'), monto DECIMAL, vuelto DECIMAL)
- `anulaciones` (id UUID PK, venta_id FK→ventas, motivo TEXT, autorizado_por UUID, created_at)
**Entregable:** `diagramas/martin/M3_erd_pos_cart.drawio.png`

---

### M4 — ERD: Warehouse & Inventory Service (`postgres-warehouse`)
**Herramienta recomendada:** draw.io
**Tablas a modelar:**
- `refrigeradores` (id UUID PK, sucursal_id UUID, tenant_id UUID, nombre, capacidad_volumen_m3 DECIMAL, capacidad_area_m2 DECIMAL, temperatura_min DECIMAL, temperatura_max DECIMAL, is_active)
- `lotes` (id UUID PK, producto_id UUID, sucursal_id UUID, tenant_id UUID, numero_lote VARCHAR, fecha_vencimiento DATE, cantidad_inicial DECIMAL, cantidad_actual DECIMAL, refrigerador_id FK→refrigeradores NULLABLE, created_at)
- `productos_stock` (id UUID PK, producto_id UUID, sucursal_id UUID, tenant_id UUID, stock_actual DECIMAL, stock_minimo DECIMAL, stock_maximo DECIMAL, unidad VARCHAR, updated_at)
- `movimientos_inventario` (id UUID PK, producto_id UUID, lote_id FK→lotes NULLABLE, sucursal_id UUID, tenant_id UUID, tipo ENUM('ENTRADA','SALIDA','TRANSFERENCIA','MERMA'), cantidad DECIMAL, referencia_id UUID, motivo TEXT, created_at)
- `mermas` (id UUID PK, producto_id UUID, lote_id FK→lotes, cantidad DECIMAL, motivo TEXT, registrado_por UUID, created_at)
**Entregable:** `diagramas/martin/M4_erd_warehouse_inventory.drawio.png`

---

### M5 — ERD: Tax & Compliance Service (`postgres-tax`)
**Herramienta recomendada:** draw.io
**Tablas a modelar:**
- `categorias_tributarias` (id UUID PK, nombre, descripcion, codigo_sat VARCHAR)
- `reglas_fiscales` (id UUID PK, pais_codigo CHAR(2), region_codigo VARCHAR, categoria_tributaria_id FK→categorias_tributarias, tipo_impuesto VARCHAR, porcentaje DECIMAL, es_compuesto BOOLEAN, aplica_desde DATE, aplica_hasta DATE NULLABLE, is_active)
- `exenciones` (id UUID PK, regla_fiscal_id FK→reglas_fiscales, condicion TEXT, aplica_a TEXT, porcentaje_exencion DECIMAL, descripcion)
- `calculos_impuesto` (id UUID PK, venta_item_id UUID, producto_id UUID, tenant_id UUID, reglas_aplicadas JSONB, base_imponible DECIMAL, impuesto_calculado DECIMAL, total_con_impuesto DECIMAL, created_at)
**Incluir:** Ejemplo de reglas: 0% IVA en vegetales Chile, 19% IVA general, impuesto adicional alcohol
**Entregable:** `diagramas/martin/M5_erd_tax_compliance.drawio.png`

---

### M6 — ERD: Supply Chain & Import Service (`postgres-supply`)
**Herramienta recomendada:** draw.io
**Tablas a modelar:**
- `proveedores` (id UUID PK, tenant_id UUID, nombre, pais_origen CHAR(2), email, telefono, moneda_facturacion CHAR(3), is_active, created_at)
- `ordenes_compra` (id UUID PK, tenant_id UUID, proveedor_id FK→proveedores, sucursal_destino_id UUID, estado ENUM('BORRADOR','ENVIADA','EN_TRANSITO','RECIBIDA','CANCELADA'), moneda CHAR(3), subtotal_proveedor DECIMAL, flete DECIMAL, seguro DECIMAL, aranceles DECIMAL, costo_landed DECIMAL, created_at)
- `orden_compra_items` (id UUID PK, orden_compra_id FK→ordenes_compra, producto_id UUID, cantidad DECIMAL, precio_unitario DECIMAL, subtotal DECIMAL)
- `envios` (id UUID PK, orden_compra_id FK→ordenes_compra, numero_tracking VARCHAR, empresa_logistica, fecha_salida DATE, fecha_estimada_llegada DATE, fecha_llegada_real DATE, estado ENUM('EN_PREPARACION','EN_TRANSITO','ADUANAS','ENTREGADO'))
- `costos_landed_historico` (id UUID PK, producto_id UUID, tenant_id UUID, costo_calculado DECIMAL, detalle JSONB, created_at)
**Entregable:** `diagramas/martin/M6_erd_supply_chain.drawio.png`

---

### M7 — ERD: Analytics & Notification Service (`postgres-analytics`)
**Herramienta recomendada:** draw.io
**Tablas a modelar:**
- `metricas_ventas` (id UUID PK, tenant_id UUID, sucursal_id UUID, fecha DATE, producto_id UUID, unidades_vendidas DECIMAL, ingresos DECIMAL, promedio_movil_7d DECIMAL, created_at)
- `alertas_generadas` (id UUID PK, tenant_id UUID, sucursal_id UUID, tipo ENUM('STOCK_BAJO','VENCIMIENTO_PROXIMO','ANOMALIA_VENTAS'), severidad ENUM('INFO','WARNING','CRITICAL'), mensaje TEXT, producto_id UUID NULLABLE, lote_id UUID NULLABLE, resuelta BOOLEAN, created_at)
- `notificaciones` (id UUID PK, alerta_id FK→alertas_generadas, canal ENUM('PUSH','EMAIL','WEBSOCKET'), destinatario_id UUID, contenido TEXT, enviada BOOLEAN, enviada_at TIMESTAMPTZ)
- `configuraciones_reporte` (id UUID PK, tenant_id UUID, sucursal_id UUID, tipo_reporte VARCHAR, frecuencia ENUM('DIARIO','SEMANAL','MENSUAL'), destinatarios JSONB, is_active)
**Entregable:** `diagramas/martin/M7_erd_analytics_notification.drawio.png`

---

## 👤 Rodrigo — Flujos de Pantallas, Componentes Frontend y Diseño UX

> **Perfil:** Frontend Lead / Electron + React / UX

### R1 — Diagrama de Flujo de Pantallas de la Aplicación Electron
**Herramienta recomendada:** draw.io o Figma (modo diagrama)
**Descripción:** Mapa completo de todas las pantallas de la aplicación desktop y sus transiciones.
**Pantallas a incluir:**
- `Splash / Loading` → (si hay update disponible: Update Screen)
- `Login` → éxito → `Dashboard`; fallo → error en pantalla
- `Registro Multi-Step` (Paso 1: Datos Minimarket, Paso 2: Localización, Paso 3: Admin) → `Login`
- **Dashboard (ADMINISTRADOR):** Inicio (KPIs), Sucursales, Usuarios, Catálogo, Inventario, Configuración
- **POS (CAJERO):** Apertura de Turno → Pantalla de Venta (escaneo + carrito) → Cobro → Ticket → Siguiente Venta → Cierre de Turno
- **Inventario (REPONEDOR):** Lista de productos con stock, alertas de stock bajo, ajuste manual de inventario
- `Configuración Regional` (timezone, moneda, fecha)
- Rutas protegidas vs. rutas públicas diferenciadas claramente
**Indicar:** Qué rol ve cada pantalla (ADMINISTRADOR, CAJERO, REPONEDOR)
**Entregable:** `diagramas/rodrigo/R1_screen_flow_electron.drawio.png`

---

### R2 — Wireframes de Pantallas Principales
**Herramienta recomendada:** draw.io, Figma o Balsamiq
**Pantallas a wireframear (baja fidelidad):**
1. `LoginPage`: logo, campo email, campo password (toggle), botón Login, link a Registro
2. `RegisterPage` (multi-step): stepper de 3 pasos con preview de configuración regional
3. `DashboardLayout`: sidebar fijo con nav y badge de rol, header con breadcrumb y avatar
4. `POS Screen` (pantalla de venta): panel izquierdo (escaneo + búsqueda), panel derecho (carrito + total + cobrar)
5. `SucursalesPage`: tabla con acciones por fila y botón "Nueva Sucursal"
6. `UsuariosPage`: tabla con badges de color por rol y modal de cambio de rol
7. `LocalizacionPage`: secciones Regional, Fecha y Moneda con previews en vivo
**Entregable:** `diagramas/rodrigo/R2_wireframes_pantallas.drawio.png`

---

### R3 — Diagrama de Componentes React (Árbol de Componentes)
**Herramienta recomendada:** draw.io o Mermaid
**Descripción:** Jerarquía completa de componentes React dentro del renderer de Electron.
**Incluir:**
```
App.tsx
├── HashRouter
│   ├── Rutas Públicas
│   │   ├── LoginPage
│   │   └── RegisterPage (Stepper)
│   └── Rutas Protegidas (ProtectedRoute)
│       └── DashboardLayout
│           ├── Sidebar (Logo, NavMenu, TenantBadge, UserAvatar)
│           ├── Header (Breadcrumb, RoleBadge)
│           └── <Outlet>
│               ├── InicioPage (KPIs, últimas ventas)
│               ├── SucursalesPage + SucursalModal
│               ├── UsuariosPage + ChangeRoleModal
│               ├── CatalogoPage + ProductoModal
│               ├── POSScreen (ScanPanel + CarritoPanel)
│               ├── InventarioPage + AjusteModal
│               └── ConfiguracionPage (LocalizacionPage)
```
**Componentes UI compartidos:** `<InputField>`, `<SelectField>`, `<Button>`, `<Spinner>`, `<EmptyState>`, `<Toast>`, `<Modal>`, `<Badge>`, `<Table>`, `<Pagination>`
**Entregable:** `diagramas/rodrigo/R3_react_component_tree.drawio.png`

---

### R4 — Diagrama de Estado Global (Zustand / electron-store)
**Herramienta recomendada:** draw.io o Mermaid
**Descripción:** Cómo se organiza y fluye el estado de la aplicación Electron.
**Stores a diagramar:**
- `authStore`: `{ accessToken, refreshToken, user, isAuthenticated }` — métodos: `login()`, `logout()`, `refreshToken()`
- `tenantStore`: `{ tenant, tenantConfig, activeSucursal }` — métodos: `setTenant()`, `updateConfig()`
- `cartStore` (para POS): `{ items, total, turnoId }` — métodos: `addItem()`, `removeItem()`, `clearCart()`
- Persistencia: qué stores se persisten en `electron-store` y cuáles son solo en memoria
- Flujo de datos: componente → store → IPC (si aplica) → electron-store
**Entregable:** `diagramas/rodrigo/R4_state_management.drawio.png`

---

## 👤 Daniel — Diagramas de Actividad, Estado y Flujos de Negocio

> **Perfil:** QA Lead / Testing / Playwright E2E

### DA1 — Diagrama de Actividad: Flujo Completo de una Venta en el POS
**Herramienta recomendada:** draw.io (notación UML Activity Diagram)
**Descripción:** Proceso de negocio completo de una transacción de venta desde que el cajero abre el turno hasta el cierre.
**Flujo a diagramar:**
1. **Apertura de Turno:** Cajero ingresa monto inicial → `POST /pos/turnos` → turno abierto
2. **Venta — Agregar Ítem (Código de Barras):** escanear → consultar producto → agregar al carrito → recalcular total
3. **Venta — Agregar Ítem (Peso Variable):** escanear producto a granel → activar balanza → capturar peso → calcular precio = peso × precio/kg
4. **Descuento / Eliminar Ítem:** modificar cantidad o eliminar del carrito
5. **Cobro Efectivo:** ingresar monto recibido → calcular vuelto → confirmar pago → `POST /pos/ventas`
6. **Cobro Tarjeta:** confirmar pago → `POST /pos/ventas`
7. **Cobro Mixto:** ingreso de montos parciales hasta completar el total
8. **Emisión de Ticket:** generar formato de impresión → imprimir
9. **Anulación (si aplica):** supervisión del Administrador → `POST /pos/ventas/{id}/anular`
10. **Cierre de Turno:** cuadre de caja → reporte Z → `PATCH /pos/turnos/{id}/cerrar`
**Incluir forks, joins y decision nodes** para cada variante de pago
**Entregable:** `diagramas/daniel/DA1_activity_flujo_venta_pos.drawio.png`

---

### DA2 — Diagrama de Actividad: Flujo de Gestión de Inventario con FEFO
**Herramienta recomendada:** draw.io
**Descripción:** Proceso completo desde la recepción de mercadería hasta la alerta de bajo stock.
**Flujo a diagramar:**
1. **Recepción de Mercadería:** crear lote → ingresar `cantidad`, `fecha_vencimiento`, `refrigerador_destino`
2. **Verificación de Capacidad:** ¿el volumen del lote cabe en el refrigerador? → SÍ: registrar → NO: bloquear y alertar
3. **Asignación por FEFO:** al vender un producto, el sistema selecciona el lote con fecha de vencimiento más próxima
4. **Descuento de Stock:** evento `sale.completed` de Kafka → consumidor actualiza `productos_stock` y `lotes`
5. **Alerta de Bajo Stock:** `stock_actual < stock_minimo` → publicar `stock.low` en Kafka → Analytics recibe y notifica
6. **Alerta de Vencimiento:** job diario verifica lotes con `fecha_vencimiento <= NOW() + 3 días` → publicar `product.expiring_soon`
7. **Ajuste Manual de Merma:** Reponedor registra merma → `POST /warehouse/mermas` → descontar de lote y stock
**Entregable:** `diagramas/daniel/DA2_activity_gestion_inventario_fefo.drawio.png`

---

### DA3 — Diagrama de Estado: Ciclo de Vida de una Venta
**Herramienta recomendada:** draw.io (UML State Machine Diagram)
**Descripción:** Todos los estados posibles de una entidad `Venta` y las transiciones válidas entre ellos.
**Estados y transiciones:**
- `PENDIENTE` (carrito activo) → [confirmar cobro] → `COMPLETADA`
- `PENDIENTE` → [cancelar sin cobrar] → `CANCELADA`
- `COMPLETADA` → [solicitar anulación dentro de X minutos, autorizada por ADMINISTRADOR] → `ANULADA`
- `ANULADA` → (estado final, no hay vuelta)
- `CANCELADA` → (estado final)
**Incluir:** Acciones de entrada/salida de cada estado, condiciones de guardia en las transiciones
**Entregable:** `diagramas/daniel/DA3_state_venta.drawio.png`

---

### DA4 — Diagrama de Estado: Ciclo de Vida de un Turno de Caja
**Herramienta recomendada:** draw.io
**Estados y transiciones:**
- `ABIERTO` → [primera venta registrada] → `EN_USO`
- `EN_USO` → [cajero solicita cierre + cuadre] → `PENDIENTE_CIERRE`
- `PENDIENTE_CIERRE` → [cuadre aprobado] → `CERRADO`
- `PENDIENTE_CIERRE` → [diferencia detectada, requiere aprobación de ADMINISTRADOR] → `EN_REVISION`
- `EN_REVISION` → [ADMINISTRADOR aprueba] → `CERRADO`
**Incluir:** Invariantes (un cajero solo puede tener un turno abierto a la vez por sucursal)
**Entregable:** `diagramas/daniel/DA4_state_turno_caja.drawio.png`

---

### DA5 — Diagrama de Estado: Ciclo de Vida de un Tenant
**Herramienta recomendada:** draw.io
**Estados y transiciones:**
- `ACTIVO` → [SUPER_ADMIN suspende] → `SUSPENDIDO`
- `SUSPENDIDO` → [SUPER_ADMIN reactiva] → `ACTIVO`
- `ACTIVO` → [SUPER_ADMIN desactiva definitivamente] → `INACTIVO`
- `INACTIVO` → (estado final)
**Incluir:** Efectos en cascada de cada estado (ej. en SUSPENDIDO los usuarios no pueden iniciar sesión)
**Entregable:** `diagramas/daniel/DA5_state_tenant.drawio.png`

---

### DA6 — Diagrama de Estrategia de Testing (Pirámide de Pruebas)
**Herramienta recomendada:** draw.io
**Descripción:** Mapa visual de la estrategia de calidad del proyecto.
**Incluir:**
- **Capa 1 (base - unitarios):** xUnit + FluentAssertions + Moq → Value Objects, Services, Validators, Domain Logic
- **Capa 2 (integración):** xUnit + `WebApplicationFactory<Program>` → Controllers + EF Core + PostgreSQL (in-memory o test DB)
- **Capa 3 (E2E):** Playwright → `_electron.launch()` → flujos completos en la app de escritorio
- **Cobertura objetivo:** >= 70% backend, >= 60% frontend crítico
- **CI:** jobs en GitHub Actions para cada capa, falla de CI si cobertura baja del umbral
**Entregable:** `diagramas/daniel/DA6_testing_strategy.drawio.png`

---

## 👤 Nicolás — Flujos de Eventos Kafka, Secuencias de Integración y BPMN

> **Perfil:** Backend / Kafka Integration / CI-CD Pipelines

### N1 — Diagrama de Topología Apache Kafka (Topics, Producers y Consumers)
**Herramienta recomendada:** draw.io
**Descripción:** Mapa completo del bus de eventos del sistema: qué servicio produce qué topic y qué servicio lo consume.
**Topics y flujos a diagramar:**

| Topic Kafka | Producer | Consumer(s) | Payload resumido |
|---|---|---|---|
| `tenant.created` | Tenant & Identity | Analytics | `{tenantId, name, config}` |
| `user.registered` | Tenant & Identity | Analytics | `{userId, tenantId, role}` |
| `tenant.updated` | Tenant & Identity | Catalog, POS, Warehouse | `{tenantId, config}` |
| `product.created` | Catalog & Pricing | Warehouse | `{productId, tenantId, uomBase}` |
| `product.price_updated` | Catalog & Pricing | POS | `{productId, sucursalId, newPrice}` |
| `sale.completed` | POS & Cart | Warehouse, Analytics, Tax | `{ventaId, items[], total, tenantId}` |
| `shift.closed` | POS & Cart | Analytics | `{turnoId, cajeroId, resumen}` |
| `stock.low` | Warehouse | Analytics | `{productId, stockActual, stockMinimo}` |
| `product.expiring_soon` | Warehouse | Analytics | `{productId, loteId, diasRestantes}` |
| `purchase_order.received` | Supply Chain | Warehouse, Catalog | `{ordenId, items[], costoLanded}` |

**Entregable:** `diagramas/nicolas/N1_kafka_topology.drawio.png`

---

### N2 — Diagrama de Secuencia: Flujo del Evento `sale.completed` End-to-End
**Herramienta recomendada:** PlantUML o draw.io
**Descripción:** Qué ocurre en el sistema completo cuando se completa una venta.
**Participantes:** `POS & Cart Service`, `Kafka Broker`, `Warehouse Service (Consumer)`, `Analytics Service (Consumer)`, `Tax & Compliance (Consumer)`, `PostgreSQL postgres-warehouse`, `PostgreSQL postgres-analytics`
**Flujo a diagramar:**
1. POS completa la venta → publica `sale.completed` en Kafka con payload completo
2. **Warehouse Consumer:** consume evento → descuenta stock por producto → si `stock_actual < stock_minimo`: publica `stock.low`
3. **Warehouse Consumer:** verifica regla FEFO → descuenta del lote más próximo a vencer
4. **Analytics Consumer:** actualiza `metricas_ventas` del día → recalcula `promedio_movil_7d`
5. **Analytics Consumer:** si consume `stock.low`: genera `alerta_generada` y `notificacion` para el Administrador
6. **Tax Consumer:** crea registro en `calculos_impuesto` para auditoría tributaria
**Incluir:** Manejo de errores en el consumer (dead-letter queue si falla el procesamiento)
**Entregable:** `diagramas/nicolas/N2_sequence_sale_completed_kafka.drawio.png`

---

### N3 — Diagrama de Secuencia: Flujo de Alerta de Bajo Stock y Vencimiento
**Herramienta recomendada:** PlantUML o draw.io
**Descripción:** Cómo el sistema detecta y notifica alertas críticas de inventario.
**Participantes:** `Warehouse Scheduler (job diario)`, `Warehouse Service`, `Kafka Broker`, `Analytics Consumer`, `PostgreSQL postgres-analytics`, `Notification Service`, `Electron App (WebSocket/Push)`
**Flujo Alerta Bajo Stock:**
1. Warehouse Consumer recibe `sale.completed` → evalúa `stock_actual < stock_minimo`
2. Publica `stock.low` → Analytics lo consume → genera `alerta_generada` (severidad WARNING) → genera `notificacion`
3. Electron recibe notificación vía WebSocket → muestra alerta en badge de la navegación

**Flujo Alerta Vencimiento:**
1. Warehouse job diario (IHostedService en ASP.NET Core) verifica lotes con `fecha_vencimiento <= NOW() + 3d`
2. Por cada lote próximo: publica `product.expiring_soon` → Analytics genera alerta CRITICAL
3. Notificación push al Administrador y Reponedor de la sucursal
**Entregable:** `diagramas/nicolas/N3_sequence_alert_stock_expiry.drawio.png`

---

### N4 — BPMN: Proceso de Importación de Mercadería Internacional
**Herramienta recomendada:** draw.io (notación BPMN 2.0)
**Descripción:** Proceso de negocio completo desde la decisión de comprar al proveedor hasta la actualización del stock en bodega.
**Participantes (Lanes):** `Administrador`, `Supply Chain Service`, `Proveedor Externo`, `Aduana`, `Warehouse Service`
**Flujo BPMN:**
1. **Administrador:** crea Orden de Compra en GlobalMart → selecciona proveedor → agrega ítems y cantidades
2. **Supply Chain Service:** calcula `Costo Landed` = Precio Proveedor + Flete + Seguro + Aranceles Estimados
3. **Administrador:** aprueba la orden → sistema envía notificación al proveedor (email)
4. **Proveedor Externo:** confirma la orden → genera envío → entrega número de tracking
5. **Supply Chain Service:** registra `Envio` con tracking → actualiza estado periódicamente
6. **Aduana:** proceso de liberación aduanera → actualiza aranceles reales
7. **Supply Chain Service:** recalcula `Costo Landed` con aranceles reales → actualiza `costos_landed_historico`
8. **Warehouse Service:** recibe `purchase_order.received` de Kafka → registra entrada de mercadería → crea lotes con fecha de vencimiento
9. **Analytics Service:** actualiza métricas de costos y márgenes
**Entregable:** `diagramas/nicolas/N4_bpmn_importacion.drawio.png`

---

### N5 — Diagrama de Secuencia: Proceso de Cálculo Fiscal en el POS
**Herramienta recomendada:** PlantUML o draw.io
**Descripción:** Cómo el Tax & Compliance Service calcula el impuesto correcto en tiempo real durante la venta.
**Participantes:** `POS Screen (Electron)`, `Kong`, `POS & Cart Service`, `Tax & Compliance Service`, `PostgreSQL postgres-tax`
**Flujo a diagramar:**
1. Cajero agrega producto al carrito → `POST /cart/items` con `{productId, cantidad}`
2. POS Service consulta precio a Catalog Service (HTTP síncrono)
3. POS Service consulta impuesto a Tax Service: `GET /tax/calculate?productId=&tenantId=&pais=CL`
4. Tax Service busca reglas fiscales para el país y categoría tributaria del producto
5. Evalúa exenciones: ¿es vegetal? → 0% IVA; ¿es alcohol? → 19% IVA + 10% adicional
6. Retorna `{baseImponible, impuesto, total}` con desglose por tipo
7. POS Service actualiza el carrito con el total calculado
8. El ticket final muestra desglose de impuestos por ítem
**Entregable:** `diagramas/nicolas/N5_sequence_tax_calculation.drawio.png`

---

## 📋 Resumen de Asignaciones y Entregables

| Integrante | N° Diagramas | Tipos | Entregables |
|---|---|---|---|
| **Diego** | 5 | Contexto C4, Contenedores C4, Docker Deploy, Kong Routing, CI/CD Pipeline | D1–D5 en `diagramas/diego/` |
| **Sebastián** | 5 | Domain Model Clases, 4 Secuencias (Login, Registro, Refresh, Autorización) | S1–S5 en `diagramas/sebastian/` |
| **Martín** | 7 | ERD de las 7 bases de datos PostgreSQL 16 (una por microservicio) | M1–M7 en `diagramas/martin/` |
| **Rodrigo** | 4 | Screen Flow Electron, Wireframes, Árbol Componentes React, Estado Global | R1–R4 en `diagramas/rodrigo/` |
| **Daniel** | 6 | 2 Actividad, 3 Estado, 1 Estrategia Testing | DA1–DA6 en `diagramas/daniel/` |
| **Nicolás** | 5 | Topología Kafka, 3 Secuencias de Integración, 1 BPMN | N1–N5 en `diagramas/nicolas/` |
| **TOTAL** | **32 diagramas** | Todos los tipos UML + C4 + BPMN + Kafka | `diagramas/` organizado por persona |

---

## ✅ Criterios de Aceptación de los Diagramas

Antes de aprobar un diagrama en revisión de equipo, debe cumplir:

- [ ] **Nomenclatura correcta:** Archivo nombrado con el código asignado (ej. `D2_containers_c4.drawio.png`)
- [ ] **Coherencia tecnológica:** El diagrama refleja el stack oficial (.NET 8, EF Core, Kong 3.6, Kafka, Electron + React)
- [ ] **Cardinalidades y restricciones** indicadas en ERDs (PK, FK, UNIQUE, NOT NULL, CHECK)
- [ ] **Participantes correctos** en diagramas de secuencia (nombres de servicios y clases reales)
- [ ] **Legibilidad:** Fuente mínima 11pt, flechas sin cruzarse en exceso, leyenda si hay colores
- [ ] **Revisado en equipo:** Cada diagrama debe ser presentado en una reunión de revisión de 15 minutos antes del Sprint 1

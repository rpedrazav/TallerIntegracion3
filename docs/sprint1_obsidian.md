---
title: "Sprint 1 — Infraestructura Base, Identidad y Multi-Tenant"
aliases:
  - Sprint 1
  - S1 Planning
tags:
  - proyecto/minimarket
  - scrum/sprint-1
  - estado/en-progreso
  - tipo/planificacion
created: 2026-08-17
sprint: 1
estado: En Progreso
capacidad_total: 120h
equipo:
  - Diego
  - Sebastian
  - Martin
  - Rodrigo
  - Daniel
  - Nicolas
semanas: 4
horas_por_persona: 20
---

# Sprint 1 — Infraestructura Base, Identidad y Configuracion Multi-Tenant

> [!IMPORTANT] Objetivo del Sprint
> Sentar las bases arquitectonicas — API Gateway, aislamiento de BD por servicio, Tenant & Identity Service completo con login, perfiles de sucursales, gestion de roles (Cajero, Administrador, Reponedor) y configuracion de localizacion (idioma, zona horaria, formato de fecha, moneda y simbolo monetario) por Tenant.
>
> **Capacidad:** 6 personas x 5 h/semana x 4 semanas = **120 horas totales**

---

## Navegacion Rapida

- [[#Equipo]]
- [[#Sprint Backlog Epicas del Sprint 1]]
- [[#Semana 1 Arranque]]
- [[#Semana 2 API Gateway operativo]]
- [[#Semana 3 Usuarios Roles y Localizacion]]
- [[#Semana 4 Cierre del Sprint]]
- [[#Resumen de Capacidad]]
- [[#Criterios de Aceptacion del Sprint 1]]
- [[#Vista Macro Sprint 2]]

---

## Equipo

| Integrante | Perfil principal |
|---|---|
| **Diego** | Arquitecto / DevOps |
| **Sebastian** | Backend / DDD / Seguridad |
| **Martin** | Base de datos / Backend / Frontend |
| **Rodrigo** | Frontend / UX |
| **Daniel** | QA / Testing |
| **Nicolas** | DevOps / Backend |

---

## Sprint Backlog Epicas del Sprint 1

| Epica | Descripcion | Meta |
|---|---|---|
| **E1** | Infraestructura y entorno base (Docker, monorepo, CI/CD) | G2 — Aislamiento BD |
| **E2** | Diseno del dominio — Tenant, Sucursal & Identity Service | G3 — Tenant & Identity |
| **E3** | Base de datos exclusiva del servicio (PostgreSQL) | G2 — Aislamiento BD |
| **E4** | API Gateway (Kong) — enrutamiento y seguridad base | G1 — API Gateway |
| **E5** | API REST: Tenants, Sucursales, Auth, Usuarios | G3, G4 |
| **E6** | Gestion de roles: Cajero, Administrador, Reponedor | G4 — Roles basicos |
| **E7** | Configuracion de localizacion: idioma, timezone, fecha, moneda, simbolo | G5, G6 |
| **E8** | Frontend — Login, Registro, Dashboard, Sucursales, Roles, Localizacion | G4, G5, G6 |
| **E9** | QA — tests unitarios, integracion y E2E | Transversal |

---

## Semana 1 Arranque

> [!NOTE] Meta de la semana
> Repositorio estructurado, Docker Compose corriendo con PostgreSQL aislado, modelo de dominio completo con Sucursal, roles correctos y TenantConfig con localizacion creado y migrado.

### Diego — Arquitecto / DevOps

- [ ] **T1** `dificultad:: medio` `area:: DevOps` Inicializar monorepo: `/services/tenant-identity/`, `/services/catalog-pricing/`, `/gateway/`, `/infra/`, `/frontend/`. `.gitignore`, README, convencion de ramas, CODEOWNERS. *(1.5 h)*
- [ ] **T2** `dificultad:: medio` `area:: DevOps` `docker-compose.yml`: postgres-tenant (postgres:16-alpine), pgadmin, kong placeholder, red `minimarket-net`. Documentar patron de aislamiento. *(2 h)*
- [ ] **T3** `dificultad:: facil` `area:: DevOps` `.env.example` con todas las variables. Script `setup-local.sh` de copia y validacion. *(1 h)*
- [ ] **T4** `dificultad:: facil` `area:: QA` Verificar `docker compose up -d`, health-check postgres, documentar en `LOCAL_SETUP.md`. *(0.5 h)*

**Total: ~5 h**

---

### Sebastian — Backend / DDD

- [ ] **T1** `dificultad:: dificil` `area:: Backend` Modelar Aggregates: `Tenant` + `TenantConfig` (timezone, currency, currencySymbol, language, dateFormat), `Sucursal`, `User` con sucursalId. Roles: SUPER_ADMIN, ADMINISTRADOR, CAJERO, REPONEDOR. Diagrama UML. *(2.5 h)*
- [ ] **T2** `dificultad:: medio` `area:: Backend` Inicializar NestJS `tenant-identity`. Instalar dependencias base. Configurar ConfigModule global. *(1.5 h)*
- [ ] **T3** `dificultad:: facil` `area:: BaseDatos` Migracion `CreateTenantsTable`: incluir currency_symbol y date_format DEFAULT 'DD/MM/YYYY'. *(1 h)*

**Total: ~5 h**

---

### Martin — Base de datos / Backend

- [ ] **T1** `dificultad:: medio` `area:: BaseDatos` Migracion `CreateSucursalesTable`: tenant_id FK, is_headquarters, constraint unica sede por tenant. *(2 h)*
- [ ] **T2** `dificultad:: medio` `area:: BaseDatos` Migracion `CreateUsersTable`: sucursal_id FK, role CHECK enum, UNIQUE(tenant_id, email). Seed demo: MiniMart CL + sede + ADMINISTRADOR. *(2 h)*
- [ ] **T3** `dificultad:: facil` `area:: DevOps` Dockerfile multi-stage para `tenant-identity`. depends_on postgres-tenant. *(1 h)*

**Total: ~5 h**

---

### Rodrigo — Frontend / UI

- [ ] **T1** `dificultad:: medio` `area:: Frontend` Inicializar React + Vite (react-ts). Instalar dependencias. Estructura de carpetas. Alias `@/`. *(1.5 h)*
- [ ] **T2** `dificultad:: medio` `area:: Frontend` `tokens.css`: paleta verde/ambar/error, Inter + Poppins, espaciados, sombras. *(1.5 h)*
- [ ] **T3** `dificultad:: facil` `area:: Frontend` `InputField.tsx` y `SelectField.tsx` con CSS Modules. *(1 h)*
- [ ] **T4** `dificultad:: facil` `area:: Frontend` `Button.tsx`: variantes, isLoading spinner, micro-animacion hover. *(1 h)*

**Total: ~5 h**

---

### Daniel — QA / Backend

- [ ] **T1** `dificultad:: medio` `area:: QA` Configurar Jest + Supertest, ts-jest, coverageThreshold 70%, configs unit y e2e separados. *(1.5 h)*
- [ ] **T2** `dificultad:: medio` `area:: QA` Tests unitarios Value Objects TenantConfig: timezone valido, currency invalida, currencySymbol vacio, dateFormat fuera del enum. *(2 h)*
- [ ] **T3** `dificultad:: facil` `area:: Backend` Modulos placeholder NestJS: TenantModule, SucursalModule, AuthModule, UserModule. Swagger en /api-docs. *(1.5 h)*

**Total: ~5 h**

---

### Nicolas — DevOps / CI

- [ ] **T1** `dificultad:: dificil` `area:: DevOps` GitHub Actions CI: lint-and-test (Node 20, npm ci, lint, test:cov) + build-docker (push imagen). Activa en develop y feat/*. *(2.5 h)*
- [ ] **T2** `dificultad:: medio` `area:: Backend` TypeOrmModule.forRootAsync con variables de entorno, synchronize: false, migrationsRun: true. *(1.5 h)*
- [ ] **T3** `dificultad:: facil` `area:: DevOps` ESLint + Prettier + husky + lint-staged pre-commit. *(1 h)*

**Total: ~5 h**

---

## Semana 2 API Gateway operativo

> [!NOTE] Meta de la semana
> Kong enruta al Tenant & Identity Service, login JWT funciona end-to-end, CRUD de Sucursales disponible, pantalla de login consume la API real.

### Diego — DevOps / API Gateway

- [ ] **T1** `dificultad:: dificil` `area:: DevOps` Kong DB-less en docker-compose. `gateway/kong.yml`: upstream tenant-identity:3001, rutas /api/v1/auth/*, /tenants, /sucursales, /users. Verificar con curl. *(2.5 h)*
- [ ] **T2** `dificultad:: medio` `area:: DevOps` Plugin jwt en rutas protegidas, rate-limiting 10/min en /auth/login, cors global para localhost:5173. *(1.5 h)*
- [ ] **T3** `dificultad:: facil` `area:: DevOps` `infra/GATEWAY.md`: URL base, como agregar rutas, verificar con :8001/services. *(1 h)*

**Total: ~5 h**

---

### Sebastian — Backend / Autenticacion

- [ ] **T1** `dificultad:: dificil` `area:: Backend` Login JWT: passport-local + passport-jwt, JwtModule 8h. Retornar accessToken + user con tenantConfig en payload. *(2.5 h)*
- [ ] **T2** `dificultad:: medio` `area:: Backend` JwtStrategy, JwtAuthGuard, RolesGuard, @CurrentUser(), @Roles(). Proteger rutas de recursos. *(1.5 h)*
- [ ] **T3** `dificultad:: facil` `area:: BaseDatos` CREATE UNIQUE INDEX CONCURRENTLY idx_users_tenant_email. EXPLAIN ANALYZE. *(1 h)*

**Total: ~5 h**

---

### Martin — Backend / Dominio

- [ ] **T1** `dificultad:: medio` `area:: Backend` CreateTenantUseCase + POST /tenants: DTO con currencySymbol y dateFormat enum. Sin duplicados. *(2 h)*
- [ ] **T2** `dificultad:: medio` `area:: Backend` RegisterUserUseCase + POST /auth/register: roles ADMINISTRADOR/CAJERO/REPONEDOR, bcrypt cost 12, sucursalId opcional. *(2 h)*
- [ ] **T3** `dificultad:: facil` `area:: DevOps` HealthModule GET /health: check BD TypeORM. Bypass en Kong. *(1 h)*

**Total: ~5 h**

---

### Rodrigo — Frontend / Login

- [ ] **T1** `dificultad:: medio` `area:: Frontend` LoginPage.tsx: degradado verde/gris, email, password toggle, loading. Conectar POST /auth/login, guardar en Zustand, redirigir. *(2.5 h)*
- [ ] **T2** `dificultad:: medio` `area:: Frontend` authStore.ts con persist. ProtectedRoute.tsx. Router con rutas publicas y protegidas. *(1.5 h)*
- [ ] **T3** `dificultad:: facil` `area:: Frontend` Spinner.tsx (CSS puro) y EmptyState.tsx (SVG + slots). *(1 h)*

**Total: ~5 h**

---

### Daniel — QA / Testing

- [ ] **T1** `dificultad:: dificil` `area:: QA` Tests E2E: login (200/401/403) + register (CAJERO ok, REPONEDOR ok, password 400, email dup 409, rol invalido 400). SQLite in-memory. *(2.5 h)*
- [ ] **T2** `dificultad:: medio` `area:: QA` Tests unitarios CreateTenantUseCase: exito con currencySymbol/dateFormat, timezone invalida, symbol vacio, duplicado, dateFormat invalido. *(1.5 h)*
- [ ] **T3** `dificultad:: facil` `area:: QA` Validar Zod login en navegador. Verificar Network que request va al gateway :8000. *(1 h)*

**Total: ~5 h**

---

### Nicolas — Backend / Sucursales

- [ ] **T1** `dificultad:: medio` `area:: Backend` CRUD Sucursales: Create, GetByTenant, Update, Deactivate. Solo ADMINISTRADOR. No desactivar unica sede. *(2.5 h)*
- [ ] **T2** `dificultad:: medio` `area:: DevOps` Secrets GitHub Actions. Documentar en infra/SECRETS.md. *(1.5 h)*
- [ ] **T3** `dificultad:: facil` `area:: QA` Coleccion Postman/Bruno completa. curl en docs/api-examples.md. *(1 h)*

**Total: ~5 h**

---

## Semana 3 Usuarios Roles y Localizacion

> [!NOTE] Meta de la semana
> Gestion de usuarios por rol (Cajero, Administrador, Reponedor), asignacion de roles en sucursales y configuracion de localizacion del Tenant. Frontend con pantallas completas.

### Diego — Backend / Usuarios y Roles

- [ ] **T1** `dificultad:: medio` `area:: Backend` GET /users con paginacion y scope por tenantId del JWT. Solo ADMINISTRADOR. *(2 h)*
- [ ] **T2** `dificultad:: medio` `area:: Backend` PATCH /users/:id/role: enum ADMINISTRADOR/CAJERO/REPONEDOR. No auto-cambio. No asignar SUPER_ADMIN. *(1.5 h)*
- [ ] **T3** `dificultad:: facil` `area:: Backend` PATCH /users/:id/deactivate + GET /auth/me con sucursalName y tenantConfig. *(1.5 h)*

**Total: ~5 h**

---

### Sebastian — Backend / Localizacion

- [ ] **T1** `dificultad:: medio` `area:: Backend` GetTenantByIdUseCase + GET /tenants/:id: scope JWT, retornar config completo. *(1.5 h)*
- [ ] **T2** `dificultad:: medio` `area:: Backend` UpdateTenantConfigUseCase + PATCH /tenants/:id/config: validar timezone IANA, currency ISO, symbol no vacio, dateFormat enum. *(2 h)*
- [ ] **T3** `dificultad:: facil` `area:: QA` Tests unitarios UpdateTenantConfigUseCase: exito, timezone invalida, symbol vacio, dateFormat invalido, 404, 403. *(1.5 h)*

**Total: ~5 h**

---

### Martin — Frontend / Sucursales y Layout

- [ ] **T1** `dificultad:: medio` `area:: Frontend` DashboardLayout.tsx: sidebar (logo, nav, tenant, avatar dropdown), header (breadcrumb, badge rol), Outlet. Responsive hamburger. *(2 h)*
- [ ] **T2** `dificultad:: medio` `area:: Frontend` SucursalesPage.tsx: tabla con Spinner/EmptyState. Boton nueva solo para ADMINISTRADOR. *(2 h)*
- [ ] **T3** `dificultad:: facil` `area:: Frontend` SucursalModal.tsx: form + validacion + POST/PATCH. Toast exito. *(1 h)*

**Total: ~5 h**

---

### Rodrigo — Frontend / Gestion de Usuarios

- [ ] **T1** `dificultad:: medio` `area:: Frontend` UsuariosPage.tsx: badges ADMINISTRADOR azul / CAJERO verde / REPONEDOR ambar. Filtros y paginacion. *(2 h)*
- [ ] **T2** `dificultad:: medio` `area:: Frontend` ChangeRoleModal.tsx: select roles, descripcion permisos, confirmacion, PATCH + actualizar tabla. *(2 h)*
- [ ] **T3** `dificultad:: facil` `area:: Frontend` tenantStore.ts con config. Sidebar: Intl.NumberFormat con currency y locale del tenant. *(1 h)*

**Total: ~5 h**

---

### Daniel — QA / E2E

- [ ] **T1** `dificultad:: dificil` `area:: QA` Playwright: login valido (dashboard + badge rol), credenciales invalidas (error), token expirado (redirect). *(2.5 h)*
- [ ] **T2** `dificultad:: medio` `area:: QA` Playwright sucursales: ADMINISTRADOR crea sucursal, CAJERO NO ve boton "Nueva Sucursal". *(1.5 h)*
- [ ] **T3** `dificultad:: facil` `area:: QA` CI: coverageReporters html + lcov, artifact en Actions, badge en README. *(1 h)*

**Total: ~5 h**

---

### Nicolas — Frontend / Localizacion

- [ ] **T1** `dificultad:: medio` `area:: Frontend` LocalizacionPage.tsx: timezone autocomplete, idioma select, dateFormat radio con preview, currency + simbolo preview. Solo ADMINISTRADOR. *(2 h)*
- [ ] **T2** `dificultad:: medio` `area:: Frontend` Conectar GET /tenants/:id + PATCH /tenants/:id/config. Actualizar tenantStore. Toast. *(2 h)*
- [ ] **T3** `dificultad:: facil` `area:: DevOps` Job deploy-staging en CI para rama develop. *(1 h)*

**Total: ~5 h**

---

## Semana 4 Cierre del Sprint

> [!NOTE] Meta de la semana
> Flujo de registro de tenants completo end-to-end, todos los tests pasan en CI, documentacion al dia, demo del Sprint Review preparada.

### Diego — DevOps / Monitoreo

- [ ] **T1** `dificultad:: medio` `area:: DevOps` Prometheus + Grafana en docker-compose. Scraping /metrics. Dashboard Node.js. *(2 h)*
- [ ] **T2** `dificultad:: medio` `area:: Documentacion` ADR-001 (JWT vs session), ADR-002 (BD aislada), ADR-003 (Kong DB-less). *(2 h)*
- [ ] **T3** `dificultad:: facil` `area:: QA` Correr test:cov + playwright test. Corregir rotos. Cobertura >= 70%. *(1 h)*

**Total: ~5 h**

---

### Sebastian — Backend / Refresh Token

- [ ] **T1** `dificultad:: medio` `area:: Backend` Migracion refresh_tokens + POST /auth/refresh (8h+7d, revocar anterior) + POST /auth/logout. *(2.5 h)*
- [ ] **T2** `dificultad:: facil` `area:: Backend` SwaggerModule con BearerAuth y decoradores en todos los endpoints. *(1 h)*
- [ ] **T3** `dificultad:: facil` `area:: QA` Tests unitarios ChangeUserRoleUseCase: exito, auto-cambio 403, sin permiso 403, SUPER_ADMIN 400, 404. *(1.5 h)*

**Total: ~5 h**

---

### Martin — Frontend / Registro Multi-Tenant

- [ ] **T1** `dificultad:: medio` `area:: Frontend` RegisterPage.tsx multi-step 3 pasos: Paso1 minimarket, Paso2 localizacion (dateFormat radio + preview, currencySymbol), Paso3 propietario. Stepper. *(2.5 h)*
- [ ] **T2** `dificultad:: medio` `area:: Frontend` Flujo: POST /tenants -> POST /sucursales -> POST /auth/register (ADMINISTRADOR). Error en paso correspondiente. Toast bienvenida. *(2 h)*
- [ ] **T3** `dificultad:: facil` `area:: Frontend` Hook useCurrentUser(). Sidebar: tenant, sucursal, avatar, badge rol. Util formatDate(date, dateFormat). *(0.5 h)*

**Total: ~5 h**

---

### Rodrigo — Frontend / UX Final y Demo

- [ ] **T1** `dificultad:: medio` `area:: Frontend` ConfiguracionPage.tsx: info general read-only + localizacion editable + historial de cambios. Solo ADMINISTRADOR. *(2 h)*
- [ ] **T2** `dificultad:: medio` `area:: Frontend` Guion de demo docs/sprint1-review.md: registro, login, sucursal, roles, cambio idioma. Grabar con OBS/Loom. *(1.5 h)*
- [ ] **T3** `dificultad:: facil` `area:: QA` Responsive en 375/768/1440px. Corregir >= 3 problemas. docs/responsive-qa.md. *(1.5 h)*

**Total: ~5 h**

---

### Daniel — QA / Plan de Pruebas

- [ ] **T1** `dificultad:: dificil` `area:: QA` docs/test-plan-sprint1.md: casos funcionales happy path + edge cases, PASS/FAIL. Defectos en Issues con label bug. *(2.5 h)*
- [ ] **T2** `dificultad:: medio` `area:: QA` Test Supertest encadenado: POST tenants -> POST sucursales -> POST register -> POST login -> PATCH config -> GET me. *(1.5 h)*
- [ ] **T3** `dificultad:: facil` `area:: QA` Verificar headers seguridad Kong: X-Content-Type-Options, X-Frame-Options. response-transformer si faltan. infra/SECURITY.md. *(1 h)*

**Total: ~5 h**

---

### Nicolas — DevOps / Cierre

- [ ] **T1** `dificultad:: medio` `area:: DevOps` Estabilizar docker-compose: health-checks, restart: unless-stopped, override.yml dev. Stack < 90s desde cero. *(1.5 h)*
- [ ] **T2** `dificultad:: medio` `area:: DevOps` CI publica swagger.json como artifact. README raiz con diagrama Mermaid 7 servicios, badges CI y cobertura. *(2 h)*
- [ ] **T3** `dificultad:: facil` `area:: QA` Merge feat/* a develop. Verificar pipeline verde. Screenshot para la demo. *(1.5 h)*

**Total: ~5 h**

---

## Resumen de Capacidad

| Semana | Diego | Sebastian | Martin | Rodrigo | Daniel | Nicolas | Total |
|---|---|---|---|---|---|---|---|
| **1** | DevOps/QA | Backend/DDD/BD | BD/Backend/DevOps | Frontend | QA/Backend | DevOps/Backend | **30 h** |
| **2** | DevOps/GW | Backend/Seg/BD | Backend/Dom/DevOps | Frontend/UX | QA | Backend | **30 h** |
| **3** | Backend | Backend | Frontend | Frontend | QA/E2E | Frontend/DevOps | **30 h** |
| **4** | DevOps/Docs/QA | Backend/QA | Frontend | Frontend/QA | QA/Backend | DevOps/QA | **30 h** |
| **TOTAL** | 20 h | 20 h | 20 h | 20 h | 20 h | 20 h | **120 h** |

### Disciplinas por integrante

| Integrante | FE | BE | BD | QA | DevOps |
|---|---|---|---|---|---|
| Diego | — | S3 | — | S1, S4 | S1, S2, S4 |
| Sebastian | — | S1-S4 | S1, S2 | S3, S4 | — |
| Martin | S3, S4 | S2, S3 | S1 | — | S1, S2 |
| Rodrigo | S1-S4 | — | — | S4 | — |
| Daniel | S2 | S1, S4 | — | S1-S4 | — |
| Nicolas | S3 | S2 | — | S4 | S1-S4 |

> [!TIP] Full-Stack garantizado
> Cada integrante toca minimo 3 disciplinas. Daniel es el QA lider con 3 tareas dificultad alta.

---

## Criterios de Aceptacion del Sprint 1

### G1 — API Gateway
- [ ] Kong DB-less enruta peticiones correctamente
- [ ] Plugin JWT valida tokens (401 si invalido)
- [ ] Rate-limiting /auth/login: 429 al superar 10 req/min

### G2 — Aislamiento de Base de Datos
- [ ] tenant-identity usa exclusivamente postgres-tenant
- [ ] Cero conexiones directas entre servicios
- [ ] Migraciones corren automaticamente al iniciar el contenedor

### G3 — Tenant & Identity Service
- [ ] POST /api/v1/tenants crea un tenant con localizacion completa
- [ ] Usuario Tenant A no puede acceder a datos Tenant B

### G4 — Login, Sucursales y Roles
- [ ] POST /auth/login retorna JWT con role, tenantId, sucursalId, tenantConfig
- [ ] Roles operativos: CAJERO, ADMINISTRADOR, REPONEDOR
- [ ] CAJERO y REPONEDOR no acceden a endpoints de gestion
- [ ] CRUD de sucursales funciona end-to-end

### G5 — Localizacion
- [ ] TenantConfig almacena timezone, language, dateFormat
- [ ] PATCH /tenants/:id/config valida y actualiza
- [ ] Preview de fecha y moneda en tiempo real en la pantalla de configuracion

### G6 — Moneda Base y Simbolo
- [ ] TenantConfig almacena currency (ISO 4217) y currencySymbol
- [ ] Sidebar formatea con Intl.NumberFormat
- [ ] Cambio de currencySymbol se refleja sin recargar la pagina

---

---

## Vista Macro Sprint 2

> [!IMPORTANT] Prerrequisito
> Sprint 1 completado: usuarios en el sistema, API Gateway operativo, TenantConfig con localizacion completa.

**Objetivo del Sprint 2:** Con los usuarios creados, el sistema debe registrar productos y procesar una venta real en el mostrador. Al finalizar, un Cajero puede abrir caja, escanear o pesar productos y cobrar una venta completa.

### Nuevos Microservicios

| Microservicio | Puerto | Base de datos propia |
|---|---|---|
| **Catalog & Pricing Service** | 3002 | postgres-catalog |
| **POS & Cart Service** | 3003 | postgres-pos |

> [!WARNING] Aislamiento de datos
> Cada servicio tiene su propia BD aislada. El API Gateway (Kong) debera actualizarse con las rutas de ambos servicios.

### Epica A — Catalog & Pricing Service

| Area | Actividades macro |
|---|---|
| **Dominio de Productos** | Entidades Producto, Categoria, UnidadDeMedida (UOM). Cada producto pertenece a un Tenant. |
| **CRUD de Productos** | Crear, listar, editar, desactivar. Campos: nombre, codigo de barras/QR, precio base, categoria, UOM, imagen, estado. |
| **Categorias Jerarquicas** | CRUD padre -> subcategoria. Ej: Bebidas -> Gaseosas, Lacteos -> Yogures. |
| **Generacion de Codigo QR** | QR vinculado al productId del Tenant. Descargable en PNG. |
| **Conversion UOM** | Motor por Tenant: kg/lb, L/oz, unidad/docena. Sistema metrico o imperial configurable. |
| **Precios Dinamicos** | Un producto puede tener precios distintos por sucursal/pais. Tabla ProductoPrecio. |
| **Frontend Catalogo** | Listado con filtros, formulario creacion/edicion, detalle con QR, categorias. |

### Epica B — POS & Cart Service

| Area | Actividades macro |
|---|---|
| **Apertura/Cierre de Turno** | Cajero abre caja (monto inicial) y cierra turno (ventas totales, diferencias, reporte). |
| **Carrito de Compras** | Agregar por codigo de barras, busqueda o QR. Precio de la sucursal. Subtotales en moneda del Tenant. |
| **Procesamiento de Pago** | Efectivo (vuelto automatico), tarjeta (monto manual), mixto. Boleta/ticket imprimible. |
| **Integracion con Balanza** | Serial (RS-232), USB (HID) o red (TCP/IP). Peso capturado -> precio kg/lb automatico. |
| **Historial de Ventas** | Ventas del dia por turno/cajero. Anulacion con rol ADMINISTRADOR. |
| **Frontend POS** | Interfaz touch/tablet: busqueda izquierda, carrito + totales + pago derecha. Lector USB. |

### Modelo de Datos Macro

#### Catalog & Pricing Service (postgres-catalog)

| Tabla | Campos clave |
|---|---|
| categorias | id, tenant_id, nombre, parent_id, is_active |
| productos | id, tenant_id, nombre, codigo_barras, codigo_qr_url, categoria_id, uom_base, precio_base, imagen_url, is_active |
| producto_precios | id, producto_id, sucursal_id, precio, currency_code, vigente_desde |
| unidades_medida | id, tenant_id, nombre, simbolo, tipo (PESO/VOLUMEN/UNIDAD), factor_conversion |

#### POS & Cart Service (postgres-pos)

| Tabla | Campos clave |
|---|---|
| turnos_caja | id, cajero_id, sucursal_id, monto_apertura, monto_cierre, estado, abierto_at, cerrado_at |
| ventas | id, turno_id, cajero_id, sucursal_id, total, metodo_pago, estado, created_at |
| venta_items | id, venta_id, producto_id, cantidad, precio_unitario, subtotal, peso_kg |
| pagos | id, venta_id, metodo (EFECTIVO/TARJETA), monto, vuelto |

### Criterios de Aceptacion Macro Sprint 2

#### Catalog & Pricing Service
- [ ] ADMINISTRADOR crea productos con categoria, UOM, codigo de barras y precio base
- [ ] QR generado automaticamente y descargable en PNG
- [ ] Conversion kg -> lb funciona segun configuracion del Tenant
- [ ] Un producto tiene precios distintos por sucursal
- [ ] Pantalla de catalogo permite buscar, filtrar y editar

#### POS & Cart Service
- [ ] CAJERO abre turno, procesa venta completa y cierra turno
- [ ] Carrito agrega por codigo de barras y calcula total en moneda del Tenant
- [ ] Metodos de pago (efectivo/tarjeta/mixto) generan ticket imprimible
- [ ] Balanza captura peso automaticamente y lo aplica al producto
- [ ] ADMINISTRADOR puede anular ventas y ver historial del turno

> [!CAUTION] Dependencia critica Sprint 2
> El POS no puede calcular precios sin que el Catalog Service este operativo. La integracion debe completarse antes de la **Semana 3 del Sprint 2**.
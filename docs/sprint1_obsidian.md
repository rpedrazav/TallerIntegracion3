---
title: "Sprint 1 — Infraestructura Base, Identidad y Multi-Tenant (.NET 8 & Microservicios)"
aliases:
  - Sprint 1
  - S1 Planning .NET
tags:
  - proyecto/minimarket
  - scrum/sprint-1
  - estado/en-progreso
  - stack/dotnet
  - tipo/planificacion
created: 2026-08-17
updated: 2026-08-20
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

# Sprint 1 — Infraestructura Base, Identidad y Configuracion Multi-Tenant (.NET 8)

> [!IMPORTANT] Objetivo del Sprint
> Sentar las bases arquitectonicas empresariales: API Gateway (**Kong 3.6 DB-less**), aislamiento estricto de BD por servicio (**PostgreSQL 16**), Tenant & Identity Service completo en **ASP.NET Core 8** con login JWT, perfiles de sucursales, gestion de roles (**Administrador, Cajero, Reponedor**), configuracion de localizacion internacional (idioma, zona horaria, fecha, moneda y simbolo monetario) por Tenant, y shell de escritorio en **Electron + React (TypeScript)**.
>
> **Capacidad:** 6 personas x 5 h/semana x 4 semanas = **120 horas totales**

---

## Navegacion Rapida

- [[#Equipo]]
- [[#Sprint Backlog Epicas del Sprint 1]]
- [[#Semana 1 Arranque]]
- [[#Semana 2 API Gateway Kong y Autenticacion]]
- [[#Semana 3 Usuarios Roles Localizacion y Kafka]]
- [[#Semana 4 Cierre del Sprint y Demo]]
- [[#Resumen de Capacidad]]
- [[#Criterios de Aceptacion del Sprint 1]]
- [[#Vista Macro Sprint 2]]

---

## Equipo

| Integrante | Perfil Principal |
|---|---|
| **Diego** | DevOps / Arquitectura .NET / API Gateway Kong |
| **Sebastian** | Backend Lead / DDD / Seguridad JWT / EF Core |
| **Martin** | Base de Datos / EF Core Migrations / Backend / UI |
| **Rodrigo** | Frontend Lead / Electron / React / UX |
| **Daniel** | QA Lead / Testing xUnit / Playwright E2E |
| **Nicolas** | Backend / Kafka Integration / CI-CD Pipelines |

---

## Sprint Backlog Epicas del Sprint 1

| Epica | Descripcion | Meta |
|---|---|---|
| **E1** | Infraestructura base, Docker Compose, solucion .NET 8 y CI/CD | G2 — Aislamiento BD |
| **E2** | Diseno del dominio DDD — Tenant, Sucursal & Identity Service | G3 — Tenant & Identity |
| **E3** | Base de datos exclusiva (PostgreSQL 16 + EF Core Migrations) | G2 — Aislamiento BD |
| **E4** | API Gateway (Kong 3.6 DB-less) — Enrutamiento y seguridad base | G1 — API Gateway |
| **E5** | API REST: Tenants, Sucursales, Auth, Usuarios (ASP.NET Core Controllers) | G3, G4 |
| **E6** | Gestion de roles: Administrador, Cajero, Reponedor, Super Admin | G4 — Roles basicos |
| **E7** | Configuracion de localizacion: idioma, timezone, fecha, moneda, simbolo | G5, G6 |
| **E8** | Frontend Electron + React: Login, Registro, Dashboard, Sucursales, Roles | G4, G5, G6 |
| **E9** | QA: tests unitarios con xUnit, integracion con WebApplicationFactory y E2E | Transversal |

---

## Semana 1 Arranque

> [!NOTE] Meta de la semana
> Solucion .NET 8 (`GlobalMart.sln`) creada con Clean Architecture, Docker Compose corriendo con PostgreSQL y Kafka, TenantDbContext con Fluent API y migraciones de EF Core, proyecto Electron + React inicializado y pipeline de CI para .NET 8.

### Diego — Arquitecto / DevOps
- [ ] **T1** `dificultad:: medio` `area:: DevOps` Inicializar monorepo y solucion `GlobalMart.sln`: proyectos Domain, Application, Infrastructure, Api, `.gitignore` .NET, `.editorconfig` C# y convencion de ramas. *(1.5 h)*
- [ ] **T2** `dificultad:: medio` `area:: DevOps` `docker-compose.yml`: postgres-tenant (postgres:16-alpine, health-check pg_isready), pgadmin, zookeeper, kafka, kong placeholder, red `minimarket-net`. *(2 h)*
- [ ] **T3** `dificultad:: facil` `area:: DevOps` `.env.example` con ConnectionStrings, Jwt (SecretKey, Issuer, Audience), Kafka (BootstrapServers). Script `setup-local.ps1`. *(1 h)*
- [ ] **T4** `dificultad:: facil` `area:: QA` Verificar `docker compose up -d`, health-check postgres y kafka, documentar en `LOCAL_SETUP.md`. *(0.5 h)*
**Total: ~5 h**

---

### Sebastian — Backend / DDD
- [ ] **T1** `dificultad:: dificil` `area:: Backend` Modelar entidades de dominio en C#: Aggregate `Tenant` + `TenantConfig` (Timezone, CurrencyCode, CurrencySymbol, LanguageCode, DateFormat), `Sucursal`, `User`, Enum `UserRole` (SUPER_ADMIN, ADMINISTRADOR, CAJERO, REPONEDOR). UML en `domain-model.md`. *(2.5 h)*
- [ ] **T2** `dificultad:: medio` `area:: Backend` Inicializar proyectos .NET 8: instalar EF Core, Npgsql, FluentValidation, JwtBearer, BCrypt.Net-Next, Swashbuckle. Configurar DI en `Program.cs`. *(1.5 h)*
- [ ] **T3** `dificultad:: facil` `area:: BaseDatos` Configurar `TenantDbContext` y Fluent API (`IEntityTypeConfiguration<Tenant>`): Owned Entity `TenantConfig`, gen_random_uuid(), defaults. *(1 h)*
**Total: ~5 h**

---

### Martin — Base de datos / EF Core
- [ ] **T1** `dificultad:: medio` `area:: BaseDatos` Configurar `IEntityTypeConfiguration<Sucursal>`: FK a Tenants ON DELETE CASCADE, partial unique index para sede principal (`is_headquarters = true`). Migracion `InitialCreate_TenantsAndSucursales`. *(2 h)*
- [ ] **T2** `dificultad:: medio` `area:: BaseDatos` Configurar `IEntityTypeConfiguration<User>`: FK a Sucursales, unique index `(TenantId, Email)`. Seeder `TenantIdentityDbSeeder` con MiniMart CL, Casa Matriz y admin con BCrypt. *(2 h)*
- [ ] **T3** `dificultad:: facil` `area:: DevOps` `Dockerfile` multi-stage: `dotnet/sdk:8.0` build + `dotnet/aspnet:8.0` runtime (puerto 5001). Integrar al docker-compose. *(1 h)*
**Total: ~5 h**

---

### Rodrigo — Frontend / UI
- [ ] **T1** `dificultad:: medio` `area:: Frontend` Inicializar Electron + React con TypeScript (Electron Forge): `main/index.ts`, `preload/index.ts`, `renderer/App.tsx` con `HashRouter`, axios, zustand, react-hook-form, zod. *(1.5 h)*
- [ ] **T2** `dificultad:: medio` `area:: Frontend` `tokens.css`: paleta verde (#1B4332), ambar (#F59E0B), fondo (#F8F9FA), fuentes Inter/Poppins, espaciados y sombras. *(1.5 h)*
- [ ] **T3** `dificultad:: facil` `area:: Frontend` Componentes UI: `<InputField>` y `<SelectField>` con CSS Modules, estados focus y error. *(1 h)*
- [ ] **T4** `dificultad:: facil` `area:: Frontend` Componente `<Button>` reutilizable: variantes primary/secondary/danger, spinner animado, hover elevation. *(1 h)*
**Total: ~5 h**

---

### Daniel — QA / xUnit Testing
- [ ] **T1** `dificultad:: medio` `area:: QA` Configurar framework de pruebas xUnit: proyecto `GlobalMart.TenantIdentity.UnitTests`, paquetes xunit, FluentAssertions, Moq, coverlet.collector. *(1.5 h)*
- [ ] **T2** `dificultad:: medio` `area:: QA` Tests unitarios con xUnit para `TenantConfig`: timezone IANA, currency ISO 4217, CurrencySymbol requerido, DateFormat permitido. `[Fact]` y `[Theory]`. *(2 h)*
- [ ] **T3** `dificultad:: facil` `area:: Backend` Controladores placeholder en ASP.NET Core: Tenants, Sucursales, Auth, Users. Swagger UI en `/swagger`. *(1.5 h)*
**Total: ~5 h**

---

### Nicolas — DevOps / CI
- [ ] **T1** `dificultad:: dificil` `area:: DevOps` GitHub Actions CI: `setup-dotnet@v4`, `dotnet restore`, `dotnet format --verify-no-changes`, `dotnet test`, build y push de imagen Docker. *(2.5 h)*
- [ ] **T2** `dificultad:: medio` `area:: Backend` Ejecutar migraciones automaticas al inicio: `db.Database.MigrateAsync()` + seeding en `Program.cs`. Probar con `dotnet run`. *(1.5 h)*
- [ ] **T3** `dificultad:: facil` `area:: DevOps` `.editorconfig` con reglas de estilo C# y hook de pre-commit para verificar `dotnet format`. *(1 h)*
**Total: ~5 h**

---

## Semana 2 API Gateway Kong y Autenticacion

> [!NOTE] Meta de la semana
> Kong 3.6 DB-less enruta al servicio ASP.NET Core, login JWT con claims de Tenant operativo, CRUD de Sucursales listo en API, y pantalla de Login en Electron conectada a Kong.

### Diego — DevOps / API Gateway Kong
- [ ] **T1** `dificultad:: dificil` `area:: DevOps` Kong 3.6 DB-less en docker-compose. `gateway/kong.yml`: upstream tenant-identity:5001, rutas /api/v1/auth/*, /tenants, /sucursales, /users. Probar con curl a :8000. *(2.5 h)*
- [ ] **T2** `dificultad:: medio` `area:: DevOps` Plugins Kong: jwt en rutas protegidas, rate-limiting (10 req/min) en /auth/login, cors global para Electron. *(1.5 h)*
- [ ] **T3** `dificultad:: facil` `area:: DevOps` `infra/GATEWAY.md`: URLs base (:8000), estructura kong.yml, verificacion con curl y Postman. *(1 h)*
**Total: ~5 h**

---

### Sebastian — Backend / Autenticacion JWT
- [ ] **T1** `dificultad:: dificil` `area:: Backend` Login JWT: `IJwtTokenGenerator` con `System.IdentityModel.Tokens.Jwt`, verificacion BCrypt, claims tenantId/sucursalId/role/tenantConfig. Endpoint `POST /api/v1/auth/login`. *(2.5 h)*
- [ ] **T2** `dificultad:: medio` `area:: Backend` Autorizacion por roles: `[Authorize(Roles = "...")]`, `TenantResolutionMiddleware` para extraccion de claims de Tenant en cada request. *(1.5 h)*
- [ ] **T3** `dificultad:: facil` `area:: BaseDatos` Migracion EF Core: unique index `(TenantId, Email)` en tabla Users. Probar con EXPLAIN ANALYZE en PostgreSQL. *(1 h)*
**Total: ~5 h**

---

### Martin — Backend / Casos de Uso
- [ ] **T1** `dificultad:: medio` `area:: Backend` `CreateTenant` en ASP.NET Core: DTOs, FluentValidation, verificacion de no duplicados y persistencia EF Core. *(2 h)*
- [ ] **T2** `dificultad:: medio` `area:: Backend` `RegisterUser` y `POST /api/v1/auth/register`: validacion tenant activo, email unico, BCrypt cost 12. Solo ADMINISTRADOR. *(2 h)*
- [ ] **T3** `dificultad:: facil` `area:: DevOps` Health Checks: `AspNetCore.HealthChecks.Npgsql`, endpoint `/health`, bypass en Kong. *(1 h)*
**Total: ~5 h**

---

### Rodrigo — Frontend / Login
- [ ] **T1** `dificultad:: medio` `area:: Frontend` `LoginPage.tsx`: formulario centrado, validacion react-hook-form + zod, axios a `http://localhost:8000/api/v1/auth/login`, persistir en Zustand + electron-store. *(2.5 h)*
- [ ] **T2** `dificultad:: medio` `area:: Frontend` `apiClient.ts` (Axios interceptor con Bearer token), `ProtectedRoute.tsx` y rutas en `HashRouter`. *(1.5 h)*
- [ ] **T3** `dificultad:: facil` `area:: Frontend` Componentes UI: `<Spinner>` animado y `<EmptyState>` con SVG y slot para acciones. *(1 h)*
**Total: ~5 h**

---

### Daniel — QA / Integracion
- [ ] **T1** `dificultad:: dificil` `area:: QA` Tests de integracion con xUnit y `WebApplicationFactory`: `POST /auth/login` (200/401/403) y `POST /auth/register` (201/409/400). *(2.5 h)*
- [ ] **T2** `dificultad:: medio` `area:: QA` Tests unitarios con xUnit y Moq para `CreateTenant`: FluentValidation, timezone IANA, simbolo vacio, duplicados. *(1.5 h)*
- [ ] **T3** `dificultad:: facil` `area:: QA` Validar formulario de login en Electron. Verificar en DevTools Network que el trafico va a Kong (:8000). *(1 h)*
**Total: ~5 h**

---

### Nicolas — Backend / Sucursales
- [ ] **T1** `dificultad:: medio` `area:: Backend` CRUD Sucursales en ASP.NET Core: `POST /sucursales`, `GET /sucursales`, `PATCH /sucursales/{id}`, `PATCH /sucursales/{id}/deactivate`. Solo ADMINISTRADOR. *(2.5 h)*
- [ ] **T2** `dificultad:: medio` `area:: DevOps` Secrets GitHub Actions (`JWT_SECRET`, `DB_PASSWORD`, etc.) y documentacion en `infra/SECRETS.md`. *(1.5 h)*
- [ ] **T3** `dificultad:: facil` `area:: QA` Coleccion Postman/Bruno completa apuntando a Kong (:8000/api/v1/) con scripts de prueba. *(1 h)*
**Total: ~5 h**

---

## Semana 3 Usuarios Roles Localizacion y Kafka

> [!NOTE] Meta de la semana
> Administracion de usuarios y cambio de roles, modificacion de localizacion del Tenant, publicacion de eventos en Apache Kafka y vistas completas en Electron.

### Diego — Backend / Usuarios y Roles
- [ ] **T1** `dificultad:: medio` `area:: Backend` `GET /api/v1/users` con paginacion (`Skip/Take`), filtros por rol/sucursal y scope por `TenantId` del JWT. *(2 h)*
- [ ] **T2** `dificultad:: medio` `area:: Backend` `PATCH /api/v1/users/{id}/role`: cambio de rol (ADMINISTRADOR, CAJERO, REPONEDOR). Bloquear auto-modificacion y SUPER_ADMIN. *(1.5 h)*
- [ ] **T3** `dificultad:: facil` `area:: Backend` `PATCH /api/v1/users/{id}/deactivate` y `GET /api/v1/auth/me` con perfil completo y tenantConfig. *(1.5 h)*
**Total: ~5 h**

---

### Sebastian — Backend / Localizacion y Kafka
- [ ] **T1** `dificultad:: medio` `area:: Backend` `GET /api/v1/tenants/{id}`: control de acceso multi-tenant, retornar DTO con `TenantConfigDto`. *(1.5 h)*
- [ ] **T2** `dificultad:: medio` `area:: Backend` `PATCH /api/v1/tenants/{id}/config`: validacion timezone IANA/currency/symbol/date_format y publicacion en Kafka topic `tenant.updated`. *(2 h)*
- [ ] **T3** `dificultad:: facil` `area:: QA` Tests unitarios con xUnit para `UpdateTenantConfig`: exito, timezone invalida (400), symbol vacio (400), rechazo multi-tenant (403). *(1.5 h)*
**Total: ~5 h**

---

### Martin — Frontend / Dashboard y Sucursales
- [ ] **T1** `dificultad:: medio` `area:: Frontend` `DashboardLayout.tsx`: sidebar (logo, nav, tenant activo, sucursal, avatar), header con breadcrumb y badge rol, `<Outlet />`. *(2 h)*
- [ ] **T2** `dificultad:: medio` `area:: Frontend` `SucursalesPage.tsx`: tabla de sucursales con badges de estado y sede matriz. Boton nueva solo para ADMINISTRADOR. *(2 h)*
- [ ] **T3** `dificultad:: facil` `area:: Frontend` `SucursalModal.tsx`: formulario react-hook-form + zod, POST y PATCH via Axios, toasts de feedback. *(1 h)*
**Total: ~5 h**

---

### Rodrigo — Frontend / Usuarios y Roles
- [ ] **T1** `dificultad:: medio` `area:: Frontend` `UsuariosPage.tsx`: tabla de usuarios con badges de color (ADMINISTRADOR azul, CAJERO verde, REPONEDOR ambar), paginacion. *(2 h)*
- [ ] **T2** `dificultad:: medio` `area:: Frontend` `ChangeRoleModal.tsx`: selector de rol, descripcion de permisos, confirmacion y PATCH via Axios. *(2 h)*
- [ ] **T3** `dificultad:: facil` `area:: Frontend` `tenantStore.ts` con Zustand: formateador `formatCurrency()` con `Intl.NumberFormat` usando currency y simbolo del Tenant. *(1 h)*
**Total: ~5 h**

---

### Daniel — QA / E2E Playwright
- [ ] **T1** `dificultad:: dificil` `area:: QA` Playwright E2E sobre Electron: lanzamiento con `_electron.launch`, login valido, redireccion a dashboard, badge rol y logout. *(2.5 h)*
- [ ] **T2** `dificultad:: medio` `area:: QA` Playwright sucursales: ADMINISTRADOR crea sucursal, CAJERO NO ve boton "Nueva Sucursal". *(1.5 h)*
- [ ] **T3** `dificultad:: facil` `area:: QA` Cobertura con Coverlet en CI: reporte `coverage.cobertura.xml`, artifact en GitHub Actions y badge en README. *(1 h)*
**Total: ~5 h**

---

### Nicolas — Frontend / Localizacion y CD
- [ ] **T1** `dificultad:: medio` `area:: Frontend` `LocalizacionPage.tsx`: timezone autocomplete IANA, idioma, DateFormat radio con preview en vivo, moneda y simbolo con preview. *(2 h)*
- [ ] **T2** `dificultad:: medio` `area:: Frontend` Conectar con `PATCH /api/v1/tenants/{id}/config`: carga inicial, mutacion via Axios, actualizar Zustand store y toast. *(2 h)*
- [ ] **T3** `dificultad:: facil` `area:: DevOps` Job deploy-staging en GitHub Actions para rama develop via SSH y docker compose. *(1 h)*
**Total: ~5 h**

---

## Semana 4 Cierre del Sprint y Demo

> [!NOTE] Meta de la semana
> Registro multi-tenant multi-step completo, refresh tokens con rotacion en ASP.NET Core, documentacion OpenAPI enriquecida, ADRs tecnicos y demo del Sprint 1 validada.

### Diego — DevOps / Monitoreo y Documentacion
- [ ] **T1** `dificultad:: medio` `area:: DevOps` Prometheus + Grafana: `prometheus-net.AspNetCore` en /metrics, metricas de logins por tenant, dashboard en Grafana. *(2 h)*
- [ ] **T2** `dificultad:: medio` `area:: Documentacion` ADRs tecnicos: ADR-001 (.NET 8), ADR-002 (EF Core Database-per-Service), ADR-003 (Kong 3.6), ADR-004 (Apache Kafka). *(2 h)*
- [ ] **T3** `dificultad:: facil` `area:: QA` Ejecutar suite completo (`dotnet test` + `npx playwright test`). Cobertura global >= 70%. *(1 h)*
**Total: ~5 h**

---

### Sebastian — Backend / Refresh Tokens y Swagger
- [ ] **T1** `dificultad:: medio` `area:: Backend` Refresh Tokens en EF Core: entidad `RefreshToken`, `POST /auth/refresh` (8h + 7d con rotacion) y `POST /auth/logout` (revocacion). *(2.5 h)*
- [ ] **T2** `dificultad:: facil` `area:: Backend` Swagger enriquecido: esquema Bearer JWT, XML comments de DTOs y controladores, UI interactiva en `/swagger`. *(1 h)*
- [ ] **T3** `dificultad:: facil` `area:: QA` Tests unitarios con xUnit para Refresh Tokens: rotacion, rechazo de token revocado y expirado. *(1.5 h)*
**Total: ~5 h**

---

### Martin — Frontend / Registro Multi-Tenant
- [ ] **T1** `dificultad:: medio` `area:: Frontend` `RegisterPage.tsx` multi-step (Stepper 3 pasos): Paso 1 Minimarket, Paso 2 Localizacion (fecha y moneda con preview), Paso 3 Administrador. *(2.5 h)*
- [ ] **T2** `dificultad:: medio` `area:: Frontend` Flujo encadenado: POST /tenants -> POST /sucursales (sede matriz) -> POST /auth/register. Toasts y redireccion a login. *(2 h)*
- [ ] **T3** `dificultad:: facil` `area:: Frontend` Hook `useCurrentUser()`, sincronizacion de tenant y funcion `formatDate(date, tenant.config.dateFormat)`. *(0.5 h)*
**Total: ~5 h**

---

### Rodrigo — Frontend / UX Final y Demo
- [ ] **T1** `dificultad:: medio` `area:: Frontend` `ConfiguracionPage.tsx`: pestana info general (read-only) + pestana regional (editable por admin) + historial. *(2 h)*
- [ ] **T2** `dificultad:: medio` `area:: Frontend` Guion y grabacion de demo (`docs/sprint1-review.md`): registro tenant -> login admin -> sucursal -> roles -> cambio moneda/fecha. *(1.5 h)*
- [ ] **T3** `dificultad:: facil` `area:: QA` Pruebas de responsividad en Electron (1024x768, 1366x768, 1920x1080). Documentar en `docs/responsive-qa.md`. *(1.5 h)*
**Total: ~5 h**

---

### Daniel — QA / Plan de Pruebas
- [ ] **T1** `dificultad:: dificil` `area:: QA` `docs/test-plan-sprint1.md`: matriz de trazabilidad con las 6 metas, casos funcionales y edge cases, registro en GitHub Issues. *(2.5 h)*
- [ ] **T2** `dificultad:: medio` `area:: QA` Test de integracion encadenado E2E con xUnit (`CompleteFlowIntegrationTests.cs`): register -> login -> patch config -> verify /auth/me. *(1.5 h)*
- [ ] **T3** `dificultad:: facil` `area:: QA` Auditoria de seguridad de Kong: headers X-Content-Type-Options/X-Frame-Options, rate-limiting y rechazo JWT invalido. *(1 h)*
**Total: ~5 h**

---

### Nicolas — DevOps / Release
- [ ] **T1** `dificultad:: medio` `area:: DevOps` Estabilizar docker-compose: health-checks, restart: unless-stopped. Arranque en frio del stack en < 90 segundos. *(1.5 h)*
- [ ] **T2** `dificultad:: medio` `area:: DevOps` Exportar `swagger.json` en CI, actualizar `README.md` raiz con diagrama Mermaid, guia `dotnet run` y badges. *(2 h)*
- [ ] **T3** `dificultad:: facil` `area:: QA` Merge a develop, verificacion de pipeline verde en GitHub Actions y release candidate del Sprint 1. *(1.5 h)*
**Total: ~5 h**

---

## Resumen de Capacidad

| Semana | Diego | Sebastian | Martin | Rodrigo | Daniel | Nicolas | Total |
|---|---|---|---|---|---|---|---|
| **1** | DevOps/QA | Backend/DDD/BD | BD/EF Core/DevOps | Frontend/UI | QA/xUnit | DevOps/CI | **30 h** |
| **2** | DevOps/Gateway | Backend/Auth JWT | Backend/Casos Uso | Frontend/Login | QA/Integracion | Backend/Sucursales | **30 h** |
| **3** | Backend/Users | Backend/Kafka | Frontend/Dashboard | Frontend/Roles | QA/Playwright E2E | Frontend/Localizacion | **30 h** |
| **4** | DevOps/Docs | Backend/Refresh | Frontend/Registro | Frontend/UX Demo | QA/Plan Pruebas | DevOps/Release | **30 h** |
| **TOTAL** | **20 h** | **20 h** | **20 h** | **20 h** | **20 h** | **20 h** | **120 h** |

### Disciplinas por Integrante

| Integrante | Frontend (Electron/React) | Backend (ASP.NET Core 8) | Base de Datos (EF Core/Postgres) | QA (xUnit / Playwright) | DevOps (Docker/Kong/CI) |
|---|---|---|---|---|---|
| **Diego** | — | S3 | — | S1, S4 | S1, S2, S4 |
| **Sebastian** | — | S1, S2, S3, S4 | S1, S2 | S3, S4 | — |
| **Martin** | S3, S4 | S2 | S1, S2 | — | S1 |
| **Rodrigo** | S1, S2, S3, S4 | — | — | S4 | — |
| **Daniel** | S2 | S1 | — | S1, S2, S3, S4 | — |
| **Nicolas** | S3 | S2 | — | S4 | S1, S2, S4 |

> [!TIP] Full-Stack Garantizado
> Cada integrante asume tareas en minimo 3 disciplinas tecnicas distintas a lo largo del Sprint.

---

## Criterios de Aceptacion del Sprint 1

### G1 — API Gateway (Kong 3.6)
- [ ] Kong 3.6 DB-less enruta peticiones hacia `tenant-identity:5001` sin errores
- [ ] Plugin JWT valida tokens (retorna 401 si es invalido o expiro)
- [ ] Rate-limiting en `/auth/login` restringe a maximo 10 req/min (retorna 429)

### G2 — Aislamiento de Base de Datos
- [ ] tenant-identity usa exclusivamente `postgres-tenant` (PostgreSQL 16)
- [ ] Cero conexiones directas o tablas compartidas entre microservicios
- [ ] Migraciones de EF Core corren automaticamente al arrancar el contenedor

### G3 — Tenant & Identity Service (ASP.NET Core 8)
- [ ] `POST /api/v1/tenants` registra un tenant con localizacion completa
- [ ] Usuario de Tenant A no puede acceder a datos de Tenant B

### G4 — Login, Sucursales y Roles
- [ ] `POST /api/v1/auth/login` retorna JWT con claims de tenantId, sucursalId, role y tenantConfig
- [ ] Roles operativos: ADMINISTRADOR, CAJERO, REPONEDOR, SUPER_ADMIN
- [ ] CAJERO y REPONEDOR tienen denegado acceso a gestion de usuarios/sucursales (403)
- [ ] CRUD de sucursales funciona end-to-end

### G5 — Configuracion de Localizacion
- [ ] TenantConfig almacena Timezone (IANA), LanguageCode (BCP-47), DateFormat
- [ ] `PATCH /api/v1/tenants/{id}/config` valida y actualiza
- [ ] Preview de fecha y moneda en vivo en pantalla de configuracion

### G6 — Moneda Base y Simbolo
- [ ] TenantConfig almacena CurrencyCode (ISO 4217) y CurrencySymbol
- [ ] Dashboard formatea con `Intl.NumberFormat` usando currency y simbolo del Tenant
- [ ] Cambio de simbolo se refleja de inmediato sin recargar

---

---

## Vista Macro Sprint 2 (Catálogo y POS en ASP.NET Core)

> [!IMPORTANT] Prerrequisito
> Sprint 1 completado: usuarios creados en ASP.NET Core, Kong 3.6 operativo y TenantConfig con localizacion completa.

### Nuevos Microservicios en ASP.NET Core 8

| Microservicio | Puerto API | Base de Datos Propia |
|---|---|---|
| **Catalog & Pricing Service** | 5002 | `postgres-catalog` |
| **POS & Cart Service** | 5003 | `postgres-pos` |

### Epica A — Catalog & Pricing Service (ASP.NET Core 8)
- **Dominio:** Producto, Categoria, UnidadMedida (UOM). Cada producto pertenece a un Tenant.
- **CRUD:** Crear, listar, editar, desactivar con codigo de barras, precio base, UOM e imagen.
- **Codigo QR:** Generacion automatica vinculada al producto, descargable en PNG.
- **Conversion UOM:** Motor por Tenant (`kg <-> lb`, `L <-> oz`, unidad/docena) con recalculo de precios.
- **Precios Dinamicos:** Tabla `ProductoPrecios` en EF Core para listas diferenciadas por sucursal.
- **Frontend Catalogo:** Vistas en Electron para productos, categorias y listas de precios.

### Epica B — POS & Cart Service (ASP.NET Core 8)
- **Turnos de Caja:** Apertura con fondo inicial y cierre con cuadre y reporte Z.
- **Carrito de Compras:** Sesion activa con escaneo de codigo de barras y calculo en moneda del Tenant.
- **Integracion con Balanza:** Puerto Serial (RS-232), USB HID o TCP/IP para peso exacto en tiempo real.
- **Pagos y Boletas:** Efectivo (vuelto automatico), tarjeta y mixto. Boleta/ticket imprimible.
- **Frontend POS:** Interfaz touch/escritorio optimizada para venta rapida en mostrador.

### Criterios de Aceptacion Macro Sprint 2
- [ ] ADMINISTRADOR crea productos con categoria, UOM, codigo de barras y precio base.
- [ ] QR generado automaticamente y descargable en PNG.
- [ ] Conversion UOM funciona correctamente (`kg <-> lb`).
- [ ] Un producto puede tener precios distintos por sucursal.
- [ ] CAJERO abre turno, procesa venta completa con balanza fisica y cierra turno.
- [ ] Pagos calculan vuelto y emiten ticket termico imprimible.

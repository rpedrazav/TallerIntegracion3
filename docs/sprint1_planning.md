# 🏃 Sprint 1 — Infraestructura Base, Identidad y Configuración Multi-Tenant
*(Stack Oficial: ASP.NET Core 8 + Entity Framework Core + PostgreSQL 16 + Kong 3.6 + Apache Kafka + xUnit + Electron + React)*

> **Capacidad del Sprint:** 6 personas × 5 h/semana × 4 semanas = **120 horas totales**
> **Objetivo del Sprint:** Sentar las bases arquitectónicas empresariales — API Gateway (Kong 3.6), aislamiento estricto de base de datos por servicio (PostgreSQL 16), Tenant & Identity Service completo en **ASP.NET Core 8 Web API** con login JWT, perfiles de sucursales, gestión de roles operativos (`ADMINISTRADOR`, `CAJERO`, `REPONEDOR`), configuración de localización internacional (idioma, huso horario, formato de fecha, moneda y símbolo monetario) por Tenant, y shell de escritorio en **Electron + React (TypeScript)**.

---

## 🏗️ Stack Tecnológico Oficial del Sprint 1

| Componente / Capa | Tecnología | Propósito en el Proyecto |
|---|---|---|
| **Lenguaje & Framework Backend** | C# / ASP.NET Core 8 Web API | Servicios REST de alto rendimiento con Clean Architecture y DDD |
| **ORM & Migraciones** | Entity Framework Core 8 + Npgsql | Mapeo objeto-relacional, Fluent API, `dotnet ef migrations` |
| **Base de Datos Relacional** | PostgreSQL 16 (Alpine en Docker) | Base de datos exclusiva (`postgres-tenant`) por servicio |
| **API Gateway** | Kong 3.6 (DB-less declarativo) | Enrutamiento unificado (`http://localhost:8000/api/v1/`), JWT, Rate Limiting, CORS |
| **Bus de Mensajes Asíncrono** | Apache Kafka + Confluent.Kafka | Publicación de eventos de dominio (`tenant.created`, `user.registered`, `tenant.updated`) |
| **Autenticación & Seguridad** | JWT Bearer (.NET 8) + BCrypt.Net-Next | Tokens stateless enriquecidos con claims y hashing seguro de contraseñas |
| **Validación de Datos** | FluentValidation 11.x | Validación declarativa de DTOs y reglas de negocio |
| **Documentación de API** | Swashbuckle / OpenAPI (Swagger UI) | Contratos OpenAPI y UI interactiva en `/swagger` |
| **Testing Backend** | xUnit + FluentAssertions + Moq | Pruebas unitarias y de integración (`WebApplicationFactory`) |
| **Cliente Frontend** | Electron 29+ con React 18 + TypeScript | Shell de escritorio nativo con SPA moderna (`HashRouter`, Axios, Zustand) |
| **Testing Frontend E2E** | Playwright (Target Electron) | Automatización E2E sobre la ventana de escritorio |
| **DevOps & CI/CD** | Docker Compose + GitHub Actions | Contenedores oficiales `mcr.microsoft.com/dotnet/aspnet:8.0` y pipeline automatizado |

---

## 👥 Equipo y Asignaciones

| ID | Integrante | Rol Base y Disciplinas Principales |
|---|---|---|
| **Diego** | Diego | DevOps / Arquitectura .NET / API Gateway Kong |
| **Sebastián** | Sebastián | Backend Lead / DDD / Seguridad JWT / EF Core |
| **Martín** | Martín | Base de Datos / EF Core Migrations / Backend / UI |
| **Rodrigo** | Rodrigo | Frontend Lead / Electron / React / UX |
| **Daniel** | Daniel | QA Lead / Testing xUnit / Playwright E2E |
| **Nicolás** | Nicolás | Backend / Kafka Integration / CI-CD Pipelines |

---

## 📋 Sprint Backlog — Épicas del Sprint 1

| Épica | Descripción | Meta del Sprint Cubierta |
|---|---|---|
| **E1** | Infraestructura base, Docker Compose, solución .NET 8 y CI/CD | G2 — Aislamiento de BD |
| **E2** | Diseño del dominio DDD — Tenant, Sucursal & Identity Service | G3 — Tenant & Identity |
| **E3** | Base de datos exclusiva (PostgreSQL 16 + EF Core Migrations) | G2 — Aislamiento de BD |
| **E4** | API Gateway (Kong 3.6 DB-less) — Enrutamiento y seguridad centralizada | G1 — API Gateway |
| **E5** | API REST: Tenants, Sucursales, Auth, Usuarios (ASP.NET Core Controllers) | G3, G4 |
| **E6** | Gestión de roles: Administrador, Cajero, Reponedor, Super Admin | G4 — Roles básicos |
| **E7** | Configuración de localización: idioma, timezone, formato de fecha, moneda y símbolo | G5, G6 |
| **E8** | Frontend Electron + React: Login, Registro, Dashboard, Sucursales, Roles, Localización | G4, G5, G6 |
| **E9** | QA: Tests unitarios con xUnit, integración con WebApplicationFactory y E2E con Playwright | Transversal |

---

## 📅 Semana 1 — Arranque: Entorno local, solución .NET, monorepo y modelo de dominio

**Meta de la semana:** El repositorio está estructurado con la solución .NET 8 (`GlobalMart.sln`), Docker Compose corre con PostgreSQL aislado y Kafka, el servicio Tenant & Identity está inicializado con Entity Framework Core 8, las migraciones iniciales (`Tenants`, `Sucursales`, `Users`) están creadas y el proyecto base de Electron + React está operativo.

---

### Diego — Rol base: Arquitecto / DevOps

**Tarea 1** *(Dificultad: medio)* — **[DevOps] Inicializar el monorepo y la solución .NET 8**
Crear el repositorio Git. Crear la solución `GlobalMart.sln` y la estructura de carpetas: `/services/tenant-identity/` (`src/Domain`, `src/Application`, `src/Infrastructure`, `src/Api`), `/services/catalog-pricing/` (placeholder), `/gateway/`, `/infra/`, `/frontend/`, `/docs/`. Agregar `.gitignore` optimizado para .NET (`bin/`, `obj/`, `.vs/`, `node_modules/`), `.editorconfig` con reglas de estilo C# y convención de ramas (`main`, `develop`, `feat/*`, `fix/*`). *(~1.5 h)*

**Tarea 2** *(Dificultad: medio)* — **[DevOps] Crear el `docker-compose.yml` base del entorno local**
Definir los servicios iniciales: `postgres-tenant` (imagen `postgres:16-alpine`, variables de entorno `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`, volumen persistente `pgdata-tenant`, health-check `pg_isready`), `pgadmin`, `zookeeper`, `kafka` (`confluentinc/cp-kafka`), y `kong` (placeholder). Exponer puertos y configurar la red Docker interna `minimarket-net`. Documentar el patrón de aislamiento de BD. *(~2 h)*

**Tarea 3** *(Dificultad: fácil)* — **[DevOps] Crear archivo `.env.example` y script de configuración local**
Listar todas las variables requeridas: `ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=tenant_identity_db;Username=postgres;Password=postgres`, `Jwt__SecretKey`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__ExpiryHours=8`, `Kafka__BootstrapServers=localhost:9092`, `ASPNETCORE_ENVIRONMENT=Development`. Crear script `scripts/setup-local.ps1` que valide dependencias y copie `.env.example` a `.env`. *(~1 h)*

**Tarea 4** *(Dificultad: fácil)* — **[QA] Verificar arranque de infraestructura con Docker Compose**
Ejecutar `docker compose up -d postgres-tenant zookeeper kafka pgadmin`, validar health-checks de contenedores. Documentar los pasos de arranque en `infra/LOCAL_SETUP.md`. *(~0.5 h)*

> **Total estimado: ~5 h**

---

### Sebastián — Rol base: Backend / DDD

**Tarea 1** *(Dificultad: difícil)* — **[Backend/DDD] Modelar las entidades de dominio en C# (Domain Layer)**
Crear proyecto `GlobalMart.TenantIdentity.Domain` (.NET 8 Class Library). Modelar los Aggregates y Value Objects:
- `Tenant` (Id, Name, CountryCode, Status, CreatedAt) con Value Object `TenantConfig` (`Timezone`: IANA string, `CurrencyCode`: ISO 4217, `CurrencySymbol`: string, `LanguageCode`: BCP-47, `DateFormat`: string ej. `DD/MM/YYYY`).
- `Sucursal` (Id, TenantId, Name, Address, Phone, IsHeadquarters, IsActive, CreatedAt).
- `User` (Id, TenantId, SucursalId, Email, PasswordHash, Role, IsActive, LastLoginAt, CreatedAt).
- Enum `UserRole`: `SUPER_ADMIN`, `ADMINISTRADOR`, `CAJERO`, `REPONEDOR`.
Crear diagrama de clases UML en `docs/domain-model.md`. *(~2.5 h)*

**Tarea 2** *(Dificultad: medio)* — **[Backend] Inicializar proyectos de la solución y paquetes NuGet**
Crear proyectos de Clean Architecture: `GlobalMart.TenantIdentity.Application`, `GlobalMart.TenantIdentity.Infrastructure`, `GlobalMart.TenantIdentity.Api` (`dotnet new webapi`). Instalar paquetes NuGet: `Microsoft.EntityFrameworkCore` (8.x), `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design`, `FluentValidation.AspNetCore`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `BCrypt.Net-Next`, `Swashbuckle.AspNetCore`. Configurar referencias de proyectos y DI en `Program.cs`. *(~1.5 h)*

**Tarea 3** *(Dificultad: fácil)* — **[Base de datos] Configurar `TenantDbContext` y Fluent API para `Tenants`**
En `Infrastructure/Persistence`, crear `TenantDbContext : DbContext`. Implementar `IEntityTypeConfiguration<Tenant>` con Fluent API: mapear tabla `tenants`, columnas UUID con `HasDefaultValueSql("gen_random_uuid()")`, Value Object `TenantConfig` como Owned Entity (`builder.OwnsOne(t => t.Config, ...)`), `currency_symbol VARCHAR(10)`, `date_format VARCHAR(20)` con default `'DD/MM/YYYY'`. *(~1 h)*

> **Total estimado: ~5 h**

---

### Martín — Rol base: Base de datos / EF Core

**Tarea 1** *(Dificultad: medio)* — **[Base de datos] Configurar mapeo de `Sucursales` y generar migración inicial**
Implementar `IEntityTypeConfiguration<Sucursal>`: clave foránea a `Tenants` con eliminación en cascada, índice `(TenantId, Name)`, índice único parcial para asegurar una sola sede principal por tenant (`builder.HasIndex(s => new { s.TenantId, s.IsHeadquarters }).IsUnique().HasFilter("is_headquarters = true")`). Instalar herramienta global `dotnet-ef` y generar migración inicial `dotnet ef migrations add InitialCreate_TenantsAndSucursales --project src/Infrastructure --startup-project src/Api`. *(~2 h)*

**Tarea 2** *(Dificultad: medio)* — **[Base de datos] Configurar mapeo de `Users` y script de Seeding**
Implementar `IEntityTypeConfiguration<User>`: clave foránea opcional a `Sucursales`, índice compuesto único `(TenantId, Email)`, check constraint para roles válidos. Crear clase `TenantIdentityDbSeeder` que inserte datos de prueba si la base está vacía: Tenant demo "MiniMart CL" (CLP, $, America/Santiago, DD/MM/YYYY, es-CL), sucursal "Casa Matriz" y usuario `ADMINISTRADOR` con password hasheada con `BCrypt.Net.BCrypt.HashPassword("Admin123!")`. *(~2 h)*

**Tarea 3** *(Dificultad: fácil)* — **[DevOps] Configurar `Dockerfile` multi-stage para ASP.NET Core**
Crear `services/tenant-identity/Dockerfile`: etapa `build` con `mcr.microsoft.com/dotnet/sdk:8.0` (`dotnet restore`, `dotnet publish -c Release -o /app/publish`), etapa `runtime` con `mcr.microsoft.com/dotnet/aspnet:8.0` (expone puerto 5001, `ENTRYPOINT ["dotnet", "GlobalMart.TenantIdentity.Api.dll"]`). Integrar al `docker-compose.yml` con `depends_on: { postgres-tenant: { condition: service_healthy } }`. *(~1 h)*

> **Total estimado: ~5 h**

---

### Rodrigo — Rol base: Frontend / UI

**Tarea 1** *(Dificultad: medio)* — **[Frontend] Inicializar proyecto Electron + React con TypeScript**
Inicializar proyecto Electron (Electron Forge / electron-vite con template React + TypeScript). Estructura de carpetas: `src/main/index.ts` (BrowserWindow y ciclo de vida), `src/preload/index.ts` (contextBridge seguro), `src/renderer/` (React SPA, react-router-dom con `HashRouter`, axios, zustand, react-hook-form, zod). Configurar alias `@/` apuntando a `src/renderer/src`. *(~1.5 h)*

**Tarea 2** *(Dificultad: medio)* — **[Frontend] Definir el sistema de diseño base (tokens CSS y tipografía)**
Crear `src/renderer/styles/tokens.css` con variables CSS: colores corporativos (verde oscuro `#1B4332`, acento ámbar `#F59E0B`, fondo `#F8F9FA`, error `#DC2626`, éxito `#16A34A`), fuentes de Google (Inter para cuerpo, Poppins para encabezados), escala de espaciados, radios de borde y elevaciones. Importar en `main.tsx`. *(~1.5 h)*

**Tarea 3** *(Dificultad: fácil)* — **[Frontend] Maquetar componentes base: `<InputField>` y `<SelectField>`**
`InputField.tsx`: props `label`, `name`, `type`, `placeholder`, `error`, `register`. Estado focus con borde ámbar, estado error con borde rojo y texto de ayuda. `SelectField.tsx`: mismas props + `options: { value: string, label: string }[]`. Implementados con CSS Modules. *(~1 h)*

**Tarea 4** *(Dificultad: fácil)* — **[Frontend] Maquetar componente reutilizable `<Button>`**
Variantes: `primary`, `secondary`, `danger`, `ghost`. Props: `label`, `isLoading` (spinner CSS animado), `disabled`, `onClick`, `type`, `fullWidth`. Micro-animación en hover (`transform: translateY(-1px)`). *(~1 h)*

> **Total estimado: ~5 h**

---

### Daniel — Rol base: QA / Testing

**Tarea 1** *(Dificultad: medio)* — **[QA] Configurar framework de pruebas xUnit en .NET 8**
Crear proyecto de tests unitarios `GlobalMart.TenantIdentity.UnitTests` (`dotnet new xunit`). Instalar paquetes NuGet: `xunit` (2.6+), `xunit.runner.visualstudio`, `FluentAssertions` (6.x), `Moq` (4.x), `coverlet.collector`. Configurar archivo `xunit.runner.json` con ejecución en paralelo y añadir referencia al proyecto `Domain` y `Application`. *(~1.5 h)*

**Tarea 2** *(Dificultad: medio)* — **[QA] Escribir tests unitarios con xUnit para Value Objects y validaciones**
Crear `TenantConfigTests.cs`. Probar con xUnit (`[Fact]` y `[Theory]` con `[InlineData]`):
(a) Instanciación válida de `TenantConfig` con timezone IANA (`America/Santiago`);
(b) Excepción ante código de moneda ISO 4217 inválido;
(c) Excepción ante `CurrencySymbol` vacío o nulo;
(d) Excepción ante formato de fecha no soportado (diferente de `DD/MM/YYYY`, `MM/DD/YYYY`, `YYYY-MM-DD`);
(e) Validación de código de idioma BCP-47 con FluentAssertions (`result.Should().NotBeNull()`). *(~2 h)*

**Tarea 3** *(Dificultad: fácil)* — **[Backend] Crear controladores placeholder en ASP.NET Core**
Crear controladores en `src/Api/Controllers`: `TenantsController`, `SucursalesController`, `AuthController`, `UsersController`. Mapear endpoints base `[HttpGet]` retornando colecciones vacías con `Ok(new List<object>())`. Configurar Swagger en `Program.cs` (`builder.Services.AddSwaggerGen()`, `app.UseSwagger()`, `app.UseSwaggerUI()`) y verificar visualmente en `/swagger`. *(~1.5 h)*

> **Total estimado: ~5 h**

---

### Nicolás — Rol base: DevOps / CI

**Tarea 1** *(Dificultad: difícil)* — **[DevOps/CI] Configurar pipeline de CI en GitHub Actions para .NET 8**
Crear `.github/workflows/ci.yml`. Jobs:
(1) `build-and-test`: Checkout, setup .NET 8 (`actions/setup-dotnet@v4`), `dotnet restore`, `dotnet format --verify-no-changes` (linter/estilo), `dotnet build --no-restore -c Release`, `dotnet test --no-build -c Release --collect:"XPlat Code Coverage"`;
(2) `build-docker`: Login a registry con secrets `DOCKER_USERNAME`/`DOCKER_PASSWORD`, build y push de imagen `tenant-identity:${{ github.sha }}`. Activar en `push` a `develop` y `feat/*`. *(~2.5 h)*

**Tarea 2** *(Dificultad: medio)* — **[Backend] Configurar ejecución automática de migraciones al inicio**
En `Program.cs` de `src/Api`, configurar bloque de migración al arrancar: `using (var scope = app.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>(); await db.Database.MigrateAsync(); await TenantIdentityDbSeeder.SeedAsync(db); }`. Probar levantando la API con `dotnet run` contra el contenedor PostgreSQL. *(~1.5 h)*

**Tarea 3** *(Dificultad: fácil)* — **[DevOps] Configurar `.editorconfig` y pre-commit hooks**
Crear `.editorconfig` en la raíz con reglas estrictas de formateo C# (indentación de 4 espacios, orden de `using`, directivas `var`, reglas de nomenclatura). Configurar script de pre-commit con Husky / script PowerShell que ejecute `dotnet format --verify-no-changes` antes de permitir el commit. *(~1 h)*

> **Total estimado: ~5 h**

---

## 📅 Semana 2 — API Gateway Kong, Autenticación JWT y CRUD de Sucursales

**Meta de la semana:** Kong 3.6 enruta peticiones hacia el servicio ASP.NET Core, la autenticación JWT con claims de Tenant está operativa, el CRUD de Sucursales y registro de usuarios funciona en la API, y la pantalla de login en Electron consume el API Gateway exitosamente.

---

### Diego — Rol base: DevOps / API Gateway Kong

**Tarea 1** *(Dificultad: difícil)* — **[DevOps/Gateway] Configurar Kong API Gateway 3.6 DB-less**
Agregar el servicio `kong` a `docker-compose.yml` (imagen `kong:3.6-alpine`, `KONG_DATABASE=off`, `KONG_DECLARATIVE_CONFIG=/etc/kong/kong.yml`). Crear `gateway/kong.yml` con: upstream `tenant-identity` apuntando a `http://tenant-identity:5001`, rutas registradas: `/api/v1/auth/login`, `/api/v1/auth/register`, `/api/v1/tenants`, `/api/v1/sucursales`, `/api/v1/users`. Probar con curl hacia `http://localhost:8000/api/v1/auth/login`. *(~2.5 h)*

**Tarea 2** *(Dificultad: medio)* — **[DevOps/Gateway] Configurar plugins de seguridad en Kong**
Configurar en `kong.yml`: plugin `jwt` para verificar tokens en rutas protegidas (todo excepto `/api/v1/auth/*`), plugin `rate-limiting` en la ruta de login (`minute: 10, policy: local`), y plugin `cors` global permitiendo llamadas desde el cliente Electron (`origins: ['*']`, headers `Authorization, Content-Type`). *(~1.5 h)*

**Tarea 3** *(Dificultad: fácil)* — **[DevOps] Documentar el API Gateway en `infra/GATEWAY.md`**
Describir: URL base del gateway (`http://localhost:8000/api/v1`), estructura declarativa de `kong.yml`, puertos expuestos (8000 proxy, 8001 admin API) y guía de verificación con curl y Postman. *(~1 h)*

> **Total estimado: ~5 h**

---

### Sebastián — Rol base: Backend / Autenticación JWT

**Tarea 1** *(Dificultad: difícil)* — **[Backend] Implementar emisión y validación de tokens JWT en ASP.NET Core**
Crear servicio `IJwtTokenGenerator` y su implementación con `System.IdentityModel.Tokens.Jwt`. En `Program.cs`, configurar `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)` con `TokenValidationParameters` (validar firma HMAC-SHA256, issuer, audience y lifetime). Endpoint `POST /api/v1/auth/login`: validar email y contraseña con `BCrypt.Net.BCrypt.Verify()`, retornar `{ accessToken, tokenType: "Bearer", expiresIn: 28800, user: { id, email, role, tenantId, sucursalId, tenantConfig } }`. Payload de JWT enriquecido con claims personalizados (`tenantId`, `sucursalId`, `role`). *(~2.5 h)*

**Tarea 2** *(Dificultad: medio)* — **[Backend] Configurar autorización y roles en ASP.NET Core**
Configurar `AddAuthorization()` con políticas de roles: `[Authorize(Roles = "ADMINISTRADOR,CAJERO,REPONEDOR,SUPER_ADMIN")]`. Crear middleware `TenantResolutionMiddleware` o filtro de acción que extraiga el `TenantId` del token JWT y lo inyecte en el contexto de la petición (`IRequestContext`). Proteger controladores con `[Authorize]`. *(~1.5 h)*

**Tarea 3** *(Dificultad: fácil)* — **[Base de datos] Agregar índice único compuesto para `Users`**
En `UserConfiguration.cs`, asegurar `builder.HasIndex(u => new { u.TenantId, u.Email }).IsUnique()`. Ejecutar `dotnet ef migrations add AddUniqueIndexUsersTenantEmail` y probar el plan de ejecución con `EXPLAIN ANALYZE` en PostgreSQL. *(~1 h)*

> **Total estimado: ~5 h**

---

### Martín — Rol base: Backend / Casos de Uso

**Tarea 1** *(Dificultad: medio)* — **[Backend] Implementar caso de uso `CreateTenant` y `POST /api/v1/tenants`**
Crear DTOs `CreateTenantRequest` y validador con FluentValidation `CreateTenantRequestValidator` (nombre requerido, `CountryCode` de 2 caracteres, timezone válido, `CurrencyCode` ISO 4217, `CurrencySymbol` no vacío, `DateFormat` en enum permitido). Implementar servicio `ITenantService.CreateAsync()`. Verificar que no exista un tenant con el mismo nombre y persistir con EF Core. Retornar DTO estandarizado con status 201 Created. *(~2 h)*

**Tarea 2** *(Dificultad: medio)* — **[Backend] Implementar caso de uso `RegisterUser` y `POST /api/v1/auth/register`**
Crear `RegisterUserRequest` y `RegisterUserValidator`: validar que el tenant exista y esté activo, email único por tenant, contraseña segura (mínimo 8 caracteres, mayúscula, número, carácter especial), rol válido (`ADMINISTRADOR`, `CAJERO`, `REPONEDOR`). Hashear contraseña con BCrypt (work factor 12) y persistir en BD. Restricción: solo `ADMINISTRADOR` puede registrar cajeros y reponedores. *(~2 h)*

**Tarea 3** *(Dificultad: fácil)* — **[DevOps] Configurar Health Checks en ASP.NET Core**
Instalar paquete NuGet `AspNetCore.HealthChecks.Npgsql`. En `Program.cs`, registrar `builder.Services.AddHealthChecks().AddNpgsql(connectionString)`. Exponer endpoint `app.MapHealthChecks("/health")`. Retornar JSON con estado de la BD. Configurar bypass en Kong para que `/health` sea público. *(~1 h)*

> **Total estimado: ~5 h**

---

### Rodrigo — Rol base: Frontend / Login y Estado

**Tarea 1** *(Dificultad: medio)* — **[Frontend] Maquetar y conectar la página de Login en Electron**
Crear `src/renderer/pages/LoginPage.tsx`. Layout centrado con degradado corporativo, logo del minimarket, campos de email y password (con botón toggle para ver contraseña), botón de envío con estado `isLoading`. Validar formulario con `react-hook-form` + `zod`. Al enviar, consumir `POST http://localhost:8000/api/v1/auth/login` vía Axios. Guardar token en store de Zustand y persistir con `electron-store`. Redirigir a `/dashboard`. *(~2.5 h)*

**Tarea 2** *(Dificultad: medio)* — **[Frontend] Implementar `ProtectedRoute` y cliente Axios con interceptores**
Crear `src/renderer/services/apiClient.ts`: instancia de Axios con `baseURL: 'http://localhost:8000/api/v1'` e interceptor de request que inyecte `Authorization: Bearer [token]`. Crear componente `ProtectedRoute.tsx`: si no existe token válido en Zustand, redirigir a `/login`. Configurar `App.tsx` con rutas públicas y privadas bajo `HashRouter`. *(~1.5 h)*

**Tarea 3** *(Dificultad: fácil)* — **[Frontend] Crear componentes UI de feedback: `<Spinner>` y `<EmptyState>`**
`Spinner.tsx`: spinner animado en CSS con color corporativo. `EmptyState.tsx`: contenedor con ilustración SVG, título, descripción y slot para botón de acción (usado en listas vacías de sucursales y usuarios). *(~1 h)*

> **Total estimado: ~5 h**

---

### Daniel — Rol base: QA / Testing de Integración

**Tarea 1** *(Dificultad: difícil)* — **[QA] Tests de integración con xUnit y `WebApplicationFactory` para Auth**
Crear proyecto `GlobalMart.TenantIdentity.IntegrationTests` con `Microsoft.AspNetCore.Mvc.Testing`. Crear `AuthIntegrationTests.cs`. Probar con `HttpClient`:
(a) `POST /api/v1/auth/login` con credenciales válidas -> 200 OK + JWT válido;
(b) Login con password incorrecta -> 401 Unauthorized;
(c) Login con usuario inactivo -> 403 Forbidden;
(d) `POST /api/v1/auth/register` con rol `CAJERO` -> 201 Created;
(e) Registro con email duplicado en el mismo tenant -> 409 Conflict. *(~2.5 h)*

**Tarea 2** *(Dificultad: medio)* — **[QA] Tests unitarios con xUnit y Moq para `CreateTenant`**
Crear `CreateTenantTests.cs`. Mockear `ITenantRepository` / `TenantDbContext` usando Moq. Probar: creación exitosa con `CurrencySymbol` y `DateFormat`, validación de timezone IANA, error 400 ante símbolo vacío y error 409 ante tenant duplicado. *(~1.5 h)*

**Tarea 3** *(Dificultad: fácil)* — **[Frontend/QA] Validar formulario de login en Electron**
Verificar en la app Electron: visualización de errores de validación de Zod en tiempo real, inspección de llamadas de red en DevTools (Ctrl+Shift+I) verificando que el tráfico pasa por Kong (`:8000`) y no directo a la API (`:5001`). *(~1 h)*

> **Total estimado: ~5 h**

---

### Nicolás — Rol base: Backend / Sucursales

**Tarea 1** *(Dificultad: medio)* — **[Backend] Implementar CRUD completo de Sucursales en ASP.NET Core**
Crear `SucursalesController` en `src/Api`:
- `POST /api/v1/sucursales`: crear sucursal (solo `ADMINISTRADOR`);
- `GET /api/v1/sucursales`: listar sucursales del tenant autenticado;
- `PATCH /api/v1/sucursales/{id}`: actualizar dirección, teléfono o nombre;
- `PATCH /api/v1/sucursales/{id}/deactivate`: desactivar sucursal (validando que no sea la única sede matriz activa). *(~2.5 h)*

**Tarea 2** *(Dificultad: medio)* — **[DevOps] Configurar gestión de secrets en CI/CD y documentación**
Configurar secrets en GitHub Actions: `JWT_SECRET`, `DB_PASSWORD`, `DOCKER_USERNAME`, `DOCKER_PASSWORD`. Actualizar `ci.yml` para inyectar variables de entorno en tests. Crear `infra/SECRETS.md` con lineamientos de rotación de credenciales. *(~1.5 h)*

**Tarea 3** *(Dificultad: fácil)* — **[QA] Crear colección Postman/Bruno para el API Gateway**
Crear archivo `docs/GlobalMart_Sprint1.postman_collection.json` con todos los endpoints de Semana 2 apuntando a `http://localhost:8000/api/v1/`. Incluir variables de entorno (`{{jwt_token}}`) y scripts de test para validar status codes. *(~1 h)*

> **Total estimado: ~5 h**

---

## 📅 Semana 3 — Gestión de Usuarios, Roles, Configuración de Localización y Publicación Kafka

**Meta de la semana:** El sistema permite administrar usuarios con roles diferenciados (`ADMINISTRADOR`, `CAJERO`, `REPONEDOR`), gestionar la configuración regional de cada Tenant (timezone, fecha, moneda), publicar eventos en Kafka y operar todas las pantallas administrativas desde Electron.

---

### Diego — Rol base: Backend / Usuarios y Roles

**Tarea 1** *(Dificultad: medio)* — **[Backend] Implementar `GET /api/v1/users` con paginación y filtros en EF Core**
En `UsersController`, crear endpoint paginado `GET /api/v1/users?page=1&pageSize=20&role=CAJERO&sucursalId=&isActive=true`. Usar EF Core con `IQueryable<User>`, aplicar filtro obligatorio `Where(u => u.TenantId == currentTenantId)`, aplicar `Skip((page-1)*pageSize).Take(pageSize)`. Retornar DTO `PagedResult<UserDto>` con metadata de paginación (`TotalCount`, `TotalPages`). Restringido a rol `ADMINISTRADOR`. *(~2 h)*

**Tarea 2** *(Dificultad: medio)* — **[Backend] Implementar `PATCH /api/v1/users/{id}/role` para cambio de rol**
Implementar servicio `ChangeUserRole`: recibir nuevo rol (`CAJERO`, `REPONEDOR`, `ADMINISTRADOR`). Validaciones: el usuario actual debe ser `ADMINISTRADOR`, no puede cambiar su propio rol (evitar auto-bloqueo), no puede asignar `SUPER_ADMIN`. Persistir con EF Core y retornar usuario actualizado. *(~1.5 h)*

**Tarea 3** *(Dificultad: fácil)* — **[Backend] Implementar `PATCH /api/v1/users/{id}/deactivate` y `GET /api/v1/auth/me`**
`PATCH /api/v1/users/{id}/deactivate`: marcar `IsActive = false`. Endpoint `GET /api/v1/auth/me`: retornar perfil completo `{ id, email, role, tenantId, sucursalId, sucursalName, tenantConfig }` leyendo los claims del JWT actual y consultando datos complementarios. *(~1.5 h)*

> **Total estimado: ~5 h**

---

### Sebastián — Rol base: Backend / Localización y Kafka

**Tarea 1** *(Dificultad: medio)* — **[Backend] Implementar `GET /api/v1/tenants/{id}` con control de acceso**
Endpoint `GET /api/v1/tenants/{id}`: validar que el `TenantId` del claim JWT coincide con el `{id}` solicitado (aislamiento multi-tenant) o que el rol es `SUPER_ADMIN`. Retornar DTO con `TenantConfigDto` completo (`Timezone`, `CurrencyCode`, `CurrencySymbol`, `LanguageCode`, `DateFormat`). *(~1.5 h)*

**Tarea 2** *(Dificultad: medio)* — **[Backend] Implementar `PATCH /api/v1/tenants/{id}/config` y publicar en Kafka**
Crear `UpdateTenantConfigRequest` y validador: validar timezone en catálogo IANA, código de moneda ISO 4217, símbolo monetario no vacío, formato de fecha permitido. Actualizar entidad `TenantConfig` en EF Core. Inyectar productor de Kafka (`IProducer<string, string>`) y publicar mensaje JSON en topic `tenant.updated` con payload `{ tenantId, config, updatedAt }`. *(~2 h)*

**Tarea 3** *(Dificultad: fácil)* — **[QA] Tests unitarios con xUnit para actualización de localización**
Crear `UpdateTenantConfigTests.cs`. Probar con xUnit: actualización exitosa, validación de timezone inválida (error 400), símbolo de moneda vacío (error 400), formato de fecha no soportado (error 400) y rechazo de acceso entre tenants distintos (403 Forbidden). *(~1.5 h)*

> **Total estimado: ~5 h**

---

### Martín — Rol base: Frontend / Dashboard y Sucursales

**Tarea 1** *(Dificultad: medio)* — **[Frontend] Maquetar layout base del Dashboard (`DashboardLayout.tsx`)**
Crear `src/renderer/layouts/DashboardLayout.tsx`. Sidebar izquierdo: logo de GlobalMart, navegación con iconos (Inicio, Sucursales, Usuarios, Configuración), nombre del Tenant activo, sucursal actual, avatar del usuario con dropdown (Perfil, Cerrar sesión). Header superior: breadcrumb dinámico y badge de rol coloreado. Área de contenido principal con `<Outlet />`. Sidebar colapsable en pantallas reducidas. *(~2 h)*

**Tarea 2** *(Dificultad: medio)* — **[Frontend] Maquetar página de gestión de Sucursales (`SucursalesPage.tsx`)**
Crear `src/renderer/pages/sucursales/SucursalesPage.tsx`. Tabla con columnas: Nombre, Dirección, Teléfono, Sede Principal (badge), Estado, Acciones (Editar, Desactivar). Consumir `GET /api/v1/sucursales` vía Axios. Mostrar `<Spinner>` durante carga y `<EmptyState>` si no hay sucursales registradas. Botón "Nueva Sucursal" visible solo para `ADMINISTRADOR`. *(~2 h)*

**Tarea 3** *(Dificultad: fácil)* — **[Frontend] Modal de creación y edición de Sucursal (`SucursalModal.tsx`)**
Crear `src/renderer/components/sucursales/SucursalModal.tsx`. Formulario con campos: nombre, dirección, teléfono, checkbox "Sede Principal". Validado con `react-hook-form` + `zod`. Llamar `POST /api/v1/sucursales` en creación y `PATCH /api/v1/sucursales/{id}` en edición. Toast de feedback con éxito o error. *(~1 h)*

> **Total estimado: ~5 h**

---

### Rodrigo — Rol base: Frontend / Usuarios y Roles

**Tarea 1** *(Dificultad: medio)* — **[Frontend] Maquetar página de gestión de Usuarios (`UsuariosPage.tsx`)**
Crear `src/renderer/pages/usuarios/UsuariosPage.tsx`. Tabla con columnas: Email, Rol (badge de color: `ADMINISTRADOR` azul, `CAJERO` verde, `REPONEDOR` ámbar), Sucursal Asignada, Estado, Acciones (Cambiar Rol, Desactivar). Filtros por rol y sucursal. Consumir `GET /api/v1/users` con paginación interactiva. *(~2 h)*

**Tarea 2** *(Dificultad: medio)* — **[Frontend] Modal para cambio de rol de usuario (`ChangeRoleModal.tsx`)**
Crear `src/renderer/components/usuarios/ChangeRoleModal.tsx`. Selector con opciones del enum (`ADMINISTRADOR`, `CAJERO`, `REPONEDOR`). Tooltip con descripción de permisos de cada rol. Modal de confirmación antes de guardar. Consumir `PATCH /api/v1/users/{id}/role` y refrescar la fila en la tabla sin recargar toda la página. *(~2 h)*

**Tarea 3** *(Dificultad: fácil)* — **[Frontend] Store de Tenant y formateo internacional de monedas**
Crear `src/renderer/store/tenantStore.ts` con Zustand. Guardar `TenantConfig`. Crear función utilitaria `formatCurrency(amount: number, config: TenantConfig)` que use `Intl.NumberFormat` con el locale y moneda del Tenant, anteponiendo o posponiendo el `currencySymbol`. Mostrar la moneda activa formateada en el sidebar del dashboard. *(~1 h)*

> **Total estimado: ~5 h**

---

### Daniel — Rol base: QA / E2E con Playwright

**Tarea 1** *(Dificultad: difícil)* — **[QA] Automatizar tests E2E de Login y Dashboard en Electron**
Instalar `@playwright/test`. Crear `tests/e2e/auth.spec.ts`. Configurar lanzamiento de Electron con Playwright (`_electron.launch`). Casos:
(a) Lanzar app Electron, ingresar credenciales de `ADMINISTRADOR`, verificar redirección a `/dashboard` y presencia de badge de rol;
(b) Ingreso de credenciales inválidas muestra mensaje de error en pantalla;
(c) Cierre de sesión redirige a `/login` y limpia credenciales almacenadas. *(~2.5 h)*

**Tarea 2** *(Dificultad: medio)* — **[QA] Tests E2E para flujo de gestión de Sucursales**
Crear `tests/e2e/sucursales.spec.ts`. Casos:
(a) Login como `ADMINISTRADOR`, abrir modal de sucursales, registrar nueva sucursal y validar aparición inmediata en la tabla;
(b) Login como `CAJERO`, verificar que el botón "Nueva Sucursal" y opciones de edición no están presentes en la interfaz. *(~1.5 h)*

**Tarea 3** *(Dificultad: fácil)* — **[QA] Configurar reporte de cobertura de código en CI**
Configurar `coverlet.collector` en el pipeline de GitHub Actions para generar reporte `coverage.cobertura.xml`. Subir reporte como artifact y añadir badge de porcentaje de cobertura en `README.md`. Cobertura objetivo mínima: 70%. *(~1 h)*

> **Total estimado: ~5 h**

---

### Nicolás — Rol base: Frontend / Localización y CD

**Tarea 1** *(Dificultad: medio)* — **[Frontend] Maquetar página de Configuración de Localización (`LocalizacionPage.tsx`)**
Crear `src/renderer/pages/configuracion/LocalizacionPage.tsx`. Secciones:
- **Regional:** Zona horaria (autocomplete con lista IANA desde `Intl.supportedValuesOf('timeZone')`), idioma base (select con `es-CL`, `es-AR`, `es-MX`, `en-US`, `pt-BR`);
- **Fecha y Hora:** Formato de fecha (radio buttons con preview en vivo: `31/12/2025` vs `12/31/2025` vs `2025-12-31`);
- **Moneda:** Código ISO 4217, símbolo monetario (`$`, `CLP`, `USD`, `€`) y preview de monto formateado en vivo (`$1.290,00`).
Solo editable por `ADMINISTRADOR`. *(~2 h)*

**Tarea 2** *(Dificultad: medio)* — **[Frontend] Conectar pantalla de Localización con `PATCH /api/v1/tenants/{id}/config`**
Cargar configuración actual con `GET /api/v1/tenants/{id}` al montar la vista. Formulario validado con `react-hook-form` + `zod`. Al guardar, llamar `PATCH /api/v1/tenants/{id}/config` vía Axios. Actualizar el store de Zustand `tenantStore` y emitir toast de confirmación. *(~2 h)*

**Tarea 3** *(Dificultad: fácil)* — **[DevOps] Configurar job de despliegue a Staging en GitHub Actions**
En `.github/workflows/ci.yml`, añadir job `deploy-staging` tras el éxito del build en rama `develop`: conexión SSH al servidor de staging (secret `SSH_PRIVATE_KEY`), ejecución de `docker compose pull` y `docker compose up -d tenant-identity`. *(~1 h)*

> **Total estimado: ~5 h**

---

## 📅 Semana 4 — Cierre del Sprint: Registro Multi-Tenant, Refresh Tokens, Estabilización y Demo

**Meta de la semana:** El flujo de registro multi-tenant y sucursal matriz está completo end-to-end, la rotación de Refresh Tokens y logout está implementada, la documentación OpenAPI y ADRs están finalizados, y la suite completa de tests valida la demo del Sprint 1.

---

### Diego — Rol base: DevOps / Monitoreo y Documentación

**Tarea 1** *(Dificultad: medio)* — **[DevOps] Configurar monitoreo con Prometheus y Grafana para ASP.NET Core**
Instalar paquete NuGet `prometheus-net.AspNetCore` en `src/Api`. Exponer métricas estándar y custom en `/metrics` (`app.UseMetricServer()`, `app.UseHttpMetrics()`). Registrar métricas de negocio: contador de logins por tenant (`logins_total{tenant_id, status}`). Agregar servicios `prometheus` y `grafana` en `docker-compose.yml` e importar dashboard estándar de .NET. *(~2 h)*

**Tarea 2** *(Dificultad: medio)* — **[Documentación] Elaborar Architecture Decision Records (ADRs) del Sprint 1**
Crear en `docs/adr/`:
- `ADR-001-dotnet8-aspnet-core.md`: justificación de ASP.NET Core 8 por rendimiento, tipado fuerte y soporte empresarial;
- `ADR-002-database-per-service-efcore.md`: justificación del aislamiento de esquemas en PostgreSQL con EF Core Migrations;
- `ADR-003-kong-api-gateway.md`: elección de Kong 3.6 DB-less para enrutamiento centralizado y plugins de seguridad;
- `ADR-004-kafka-event-bus.md`: justificación de Apache Kafka para sincronización asíncrona inter-servicios. *(~2 h)*

**Tarea 3** *(Dificultad: fácil)* — **[QA] Ejecutar suite de pruebas completa y verificación de calidad**
Ejecutar `dotnet test` (unitarios e integración) y `npx playwright test` (E2E). Corregir cualquier regresión y verificar que la cobertura global de backend supere el 70%. *(~1 h)*

> **Total estimado: ~5 h**

---

### Sebastián — Rol base: Backend / Refresh Tokens y Swagger

**Tarea 1** *(Dificultad: medio)* — **[Backend] Implementar ciclo de Refresh Tokens y Logout**
Crear entidad `RefreshToken` (Id, TokenHash, UserId, TenantId, ExpiresAt, IsRevoked, CreatedAt) y migración en EF Core. Implementar:
- `POST /api/v1/auth/refresh`: validar refresh token en BD, generar nuevo JWT (8h) + nuevo refresh token (7 días con rotación) y revocar el token anterior;
- `POST /api/v1/auth/logout`: marcar como revocado el refresh token activo del usuario autenticado. *(~2.5 h)*

**Tarea 2** *(Dificultad: fácil)* — **[Backend] Enriquecer documentación OpenAPI/Swagger en ASP.NET Core**
En `Program.cs`, configurar `AddSwaggerGen` con esquema de seguridad Bearer JWT (`OpenApiSecurityScheme`), metadata detallada (título, versión, descripción, contacto), e inclusión de comentarios XML de controladores y DTOs (`includeXmlComments`). Verificar interfaz en `/swagger`. *(~1 h)*

**Tarea 3** *(Dificultad: fácil)* — **[QA] Tests unitarios con xUnit para ciclo de Refresh Tokens**
Crear `RefreshTokenTests.cs`. Probar con xUnit: emisión correcta de nuevo par de tokens, rechazo de refresh token revocado o expirado, y revocación efectiva en logout. *(~1.5 h)*

> **Total estimado: ~5 h**

---

### Martín — Rol base: Frontend / Registro Multi-Tenant

**Tarea 1** *(Dificultad: medio)* — **[Frontend] Maquetar página de Registro Multi-Step (`/register`)**
Crear `src/renderer/pages/RegisterPage.tsx` con formulario multi-paso (Stepper):
- **Paso 1 (Minimarket):** Nombre de la empresa, país de operación;
- **Paso 2 (Configuración Regional):** Zona horaria IANA, idioma, formato de fecha (radio con preview en vivo), código de moneda ISO 4217 y símbolo monetario;
- **Paso 3 (Administrador Inicial):** Nombre, email, contraseña y confirmación de contraseña. Indicador visual de progreso y botones `Anterior` / `Siguiente` / `Crear Minimarket`. *(~2.5 h)*

**Tarea 2** *(Dificultad: medio)* — **[Frontend] Conectar flujo de registro con el API Gateway**
Al completar el Paso 3: llamar `POST /api/v1/tenants` con datos de Pasos 1 y 2 -> obtener `tenantId` -> llamar `POST /api/v1/sucursales` para crear la sucursal matriz "Casa Matriz" -> llamar `POST /api/v1/auth/register` con rol `ADMINISTRADOR`. Si alguna llamada falla, mostrar el error en el paso correspondiente. En éxito, redirigir a `/login` con toast "¡Minimarket registrado exitosamente!". *(~2 h)*

**Tarea 3** *(Dificultad: fácil)* — **[Frontend] Hook `useCurrentUser()` y utilidades de fecha**
Crear hook `useCurrentUser()` que lea los datos del usuario autenticado desde Zustand. Crear función `formatDate(date: string | Date, formatPattern: string)` que formatee fechas respetando el `DateFormat` del Tenant (`DD/MM/YYYY`, `MM/DD/YYYY`, `YYYY-MM-DD`). Mostrar en el sidebar: tenant, sucursal matriz, avatar y badge de rol. *(~0.5 h)*

> **Total estimado: ~5 h**

---

### Rodrigo — Rol base: Frontend / UX Final y Demo

**Tarea 1** *(Dificultad: medio)* — **[Frontend] Maquetar página de Configuración General (`/dashboard/configuracion`)**
Crear `src/renderer/pages/configuracion/ConfiguracionPage.tsx`. Dos pestañas:
(1) **Información General:** Datos del Tenant en modo solo lectura;
(2) **Configuración Regional:** Componente de localización editable por el `ADMINISTRADOR`.
Historial de cambios recientes en tabla de auditoría. *(~2 h)*

**Tarea 2** *(Dificultad: medio)* — **[Frontend] Preparar guión y grabación de demo del Sprint Review**
Crear guión en `docs/sprint1-review.md`: (1) Registro de nuevo minimarket "Demo Mart Chile" con CLP, $, Santiago y DD/MM/YYYY; (2) Login como `ADMINISTRADOR`; (3) Creación de "Sucursal Norte"; (4) Creación de usuarios con roles `CAJERO` y `REPONEDOR`; (5) Cambio de idioma y formato de fecha con verificación visual inmediata. Grabar video demo del flujo. *(~1.5 h)*

**Tarea 3** *(Dificultad: fácil)* — **[QA] Pruebas de responsividad y UI en Electron**
Probar la aplicación en distintas resoluciones de escritorio: 1024x768 (pantalla de caja / POS), 1366x768 (laptop estándar), 1920x1080 (Full HD). Corregir desbordamientos y verificar accesibilidad de botones. Documentar en `docs/responsive-qa.md`. *(~1.5 h)*

> **Total estimado: ~5 h**

---

### Daniel — Rol base: QA / Plan de Pruebas y Auditoría

**Tarea 1** *(Dificultad: difícil)* — **[QA] Elaborar Plan de Pruebas formal del Sprint 1 y matriz de regresión**
Crear `docs/test-plan-sprint1.md`. Incluir matriz de trazabilidad: casos de prueba funcionales (happy path + edge cases) para cada una de las 6 metas del Sprint 1, resultados obtenidos (PASS/FAIL) y registro de incidencias en GitHub Issues con etiqueta `bug`. *(~2.5 h)*

**Tarea 2** *(Dificultad: medio)* — **[QA] Test de integración E2E con xUnit (`CompleteFlowIntegrationTests.cs`)**
Crear test encadenado en memoria con `WebApplicationFactory`:
`POST /api/v1/tenants` (crear tenant) -> `POST /api/v1/sucursales` (crear sede) -> `POST /api/v1/auth/register` (crear admin) -> `POST /api/v1/auth/login` (obtener JWT) -> `PATCH /api/v1/tenants/{id}/config` (modificar moneda/formato) -> `GET /api/v1/auth/me` (verificar persistencia en claims). *(~1.5 h)*

**Tarea 3** *(Dificultad: fácil)* — **[QA] Auditoría de seguridad del API Gateway y headers**
Usar curl para validar que Kong 3.6 retorna headers de seguridad: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`. Verificar que peticiones con JWT expirado o firma inválida retornan 401 Unauthorized y que superar 10 logins/minuto retorna 429 Too Many Requests. Documentar en `infra/SECURITY.md`. *(~1 h)*

> **Total estimado: ~5 h**

---

### Nicolás — Rol base: DevOps / Cierre y Release

**Tarea 1** *(Dificultad: medio)* — **[DevOps] Estabilización de `docker-compose.yml` y arranque en frío**
Revisar health-checks y orden de dependencias en `docker-compose.yml`. Agregar `restart: unless-stopped`. Crear `docker-compose.override.yml` para desarrollo local. Validar que `docker compose up --build` levante el stack completo (PostgreSQL, Kafka, Zookeeper, Kong, ASP.NET Core) en menos de 90 segundos desde cero. *(~1.5 h)*

**Tarea 2** *(Dificultad: medio)* — **[DevOps] Publicación de OpenAPI y actualización de `README.md`**
Configurar script para exportar `swagger.json` desde ASP.NET Core en el CI y publicarlo como artifact. Actualizar `README.md` raíz con diagrama Mermaid de microservicios, instrucciones de arranque local (`dotnet run` y `npm run dev`), convenciones de commits y badges de CI. *(~2 h)*

**Tarea 3** *(Dificultad: fácil)* — **[QA] Validación final de CI en rama `develop`**
Realizar merge de todas las ramas `feat/*` a `develop`. Comprobar que el pipeline de GitHub Actions se ejecuta con éxito (tests xUnit pasando, build Docker exitoso y cobertura >= 70%). *(~1.5 h)*

> **Total estimado: ~5 h**

---

## 📊 Resumen de Capacidad y Distribución de Roles

| Semana | Diego | Sebastián | Martín | Rodrigo | Daniel | Nicolás | Total |
|---|---|---|---|---|---|---|---|
| **1** | DevOps / QA | Backend / DDD / BD | BD / EF Core / DevOps | Frontend / UI | QA / xUnit | DevOps / CI | **30 h** |
| **2** | DevOps / Gateway | Backend / Auth JWT | Backend / Casos de Uso | Frontend / Login | QA / Integración | Backend / Sucursales | **30 h** |
| **3** | Backend / Users | Backend / Kafka | Frontend / Dashboard | Frontend / Roles | QA / Playwright E2E | Frontend / Localización | **30 h** |
| **4** | DevOps / Docs | Backend / Refresh | Frontend / Registro | Frontend / UX Demo | QA / Plan de Pruebas | DevOps / Release | **30 h** |
| **TOTAL** | **20 h** | **20 h** | **20 h** | **20 h** | **20 h** | **20 h** | **120 h** |

### Matriz de Rotación Full-Stack por Integrante

| Integrante | Frontend (Electron/React) | Backend (ASP.NET Core 8) | Base de Datos (EF Core/Postgres) | QA (xUnit / Playwright) | DevOps (Docker/Kong/CI) |
|---|---|---|---|---|---|
| **Diego** | — | ✅ S3 | — | ✅ S1, S4 | ✅ S1, S2, S4 |
| **Sebastián** | — | ✅ S1, S2, S3, S4 | ✅ S1, S2 | ✅ S3, S4 | — |
| **Martín** | ✅ S3, S4 | ✅ S2 | ✅ S1, S2 | — | ✅ S1 |
| **Rodrigo** | ✅ S1, S2, S3, S4 | — | — | ✅ S4 | — |
| **Daniel** | ✅ S2 | ✅ S1 | — | ✅ S1, S2, S3, S4 | — |
| **Nicolás** | ✅ S3 | ✅ S2 | — | ✅ S4 | ✅ S1, S2, S4 |

> ✅ **Carácter Full-Stack garantizado:** Cada integrante asume tareas en al menos 3 disciplinas técnicas diferentes durante el Sprint.

---

## 🎯 Criterios de Aceptación del Sprint 1

*(Mapeados 1:1 a las 6 Metas Oficiales del Sprint)*

### G1 — API Gateway (Kong 3.6)
- [ ] Kong 3.6 DB-less está en ejecución y enruta el tráfico hacia `tenant-identity:5001` sin errores.
- [ ] El plugin JWT de Kong valida tokens en rutas protegidas y rechaza llamadas no autorizadas con 401.
- [ ] El rate-limiting en `/api/v1/auth/login` restringe peticiones a un máximo de 10 por minuto (retorna 429 Too Many Requests).

### G2 — Aislamiento de Base de Datos
- [ ] El servicio `tenant-identity` se conecta **exclusivamente** a su instancia `postgres-tenant`.
- [ ] No existen conexiones directas ni esquemas compartidos con otros microservicios.
- [ ] Las migraciones de EF Core (`Tenants`, `Sucursales`, `Users`, `RefreshTokens`) se ejecutan automáticamente al arrancar el contenedor.

### G3 — Tenant & Identity Service (ASP.NET Core 8)
- [ ] `POST /api/v1/tenants` registra un nuevo minimarket con su configuración regional completa.
- [ ] Aislamiento multi-tenant validado: un usuario del Tenant A no puede consultar ni mutar datos del Tenant B.

### G4 — Login, Perfiles de Sucursales y Roles Operativos
- [ ] `POST /api/v1/auth/login` retorna un JWT válido con claims de `tenantId`, `sucursalId`, `role` y `tenantConfig`.
- [ ] Roles operativos soportados: `ADMINISTRADOR`, `CAJERO`, `REPONEDOR`, `SUPER_ADMIN`.
- [ ] Los roles `CAJERO` y `REPONEDOR` tienen denegado el acceso a endpoints de administración de sucursales y usuarios (403 Forbidden).
- [ ] CRUD de sucursales funcional end-to-end (crear, listar, editar, desactivar).
- [ ] Pantalla de usuarios en Electron refleja el rol con badge visual de color diferenciado.

### G5 — Configuración de Localización
- [ ] `TenantConfig` persiste y entrega: `Timezone` (IANA), `LanguageCode` (BCP-47), `DateFormat` (`DD/MM/YYYY`, `MM/DD/YYYY`, `YYYY-MM-DD`).
- [ ] `PATCH /api/v1/tenants/{id}/config` actualiza la configuración y rechaza valores inválidos con 400 Bad Request.
- [ ] La pantalla de configuración en Electron muestra preview en tiempo real del formato de fecha y moneda.

### G6 — Moneda Base y Símbolo Monetario
- [ ] `TenantConfig` almacena `CurrencyCode` (ISO 4217) y `CurrencySymbol` (ej. `$`, `CLP`, `USD`, `€`).
- [ ] El dashboard en Electron formatea montos monetarios usando `Intl.NumberFormat` con el símbolo y código del Tenant.
- [ ] Cualquier cambio de `CurrencySymbol` se refleja de inmediato en la interfaz del cliente.

---

---

# 🔭 Vista Macro — Sprint 2: Catálogo de Productos y Punto de Venta (MVP en ASP.NET Core)

> **Prerrequisito:** Sprint 1 completado. Los usuarios existen en el sistema, el API Gateway Kong 3.6 está operativo y cada Tenant tiene su configuración de localización completa.
>
> **Objetivo del Sprint 2:** Con los usuarios ya creados, el sistema debe ser capaz de registrar productos y procesar una venta real en el mostrador. Al finalizar este Sprint, un Cajero puede abrir una caja en la app Electron, escanear o pesar productos en una balanza externa y cobrar una venta completa.

---

## 📦 Nuevos Microservicios a Inicializar en ASP.NET Core 8

| Microservicio | Puerto API | Base de Datos Propia (PostgreSQL) |
|---|---|---|
| **Catalog & Pricing Service** | 5002 | `postgres-catalog` |
| **POS & Cart Service** | 5003 | `postgres-pos` |

> Cada servicio mantendrá su propia base de datos aislada y su solución en **ASP.NET Core 8 con Entity Framework Core**. Kong 3.6 será actualizado con las nuevas rutas `/api/v1/catalog/*` y `/api/v1/pos/*`.

---

## 🗂️ Épicas del Sprint 2

### Épica A — Catalog & Pricing Service (ASP.NET Core 8)
- **Dominio de Productos:** Entidades `Producto`, `Categoria` y `UnidadMedida (UOM)`. Cada producto pertenece a un Tenant y se asocia a categorías jerárquicas.
- **CRUD de Productos:** Crear, listar (con paginación y búsqueda), editar y desactivar productos con código de barras, precio base, UOM e imagen.
- **Generación de Código QR:** Generación automática de código QR vinculado al producto, descargable en PNG para etiquetado.
- **Conversión de UOM:** Motor de conversión de unidades por Tenant (`kg ↔ lb`, `L ↔ oz`, unidades/docenas) con recálculo automático de precios.
- **Precios Dinámicos por Sucursal:** Tabla `ProductoPrecios` en EF Core permitiendo listas de precios diferenciadas por sucursal física.
- **Frontend Catálogo en Electron:** Vistas de catálogo de productos, árbol de categorías y gestión de listas de precios.

### Épica B — POS & Cart Service (ASP.NET Core 8)
- **Apertura y Cierre de Turno:** Registro de apertura de caja con fondo inicial y cierre de turno con cuadre de efectivo, resumen de tarjetas y reporte Z.
- **Carrito de Compras de Alto Rendimiento:** Sesión de venta activa con escaneo de código de barras, búsqueda predictiva y cálculo de subtotales en la moneda del Tenant.
- **Integración con Balanza Física:** Comunicación vía puerto Serial (RS-232), USB HID o red (TCP/IP) para capturar el peso exacto en tiempo real para productos a granel.
- **Procesamiento de Pagos:** Soporte para Efectivo (con cálculo de vuelto automático), Tarjeta y Pago Mixto. Generación de ticket de venta imprimible.
- **Frontend POS Touch:** Pantalla optimizada para terminal de punto de venta (operable con teclado numérico, lector de código de barras y pantalla táctil).

---

## 📐 Modelo de Datos Macro — Sprint 2

### Catalog & Pricing Service (`postgres-catalog`)
- `Categorias` (`Id`, `TenantId`, `Nombre`, `ParentId`, `IsActive`)
- `Productos` (`Id`, `TenantId`, `Nombre`, `CodigoBarras`, `CodigoQrUrl`, `CategoriaId`, `UomBase`, `PrecioBase`, `IsActive`)
- `ProductoPrecios` (`Id`, `ProductoId`, `SucursalId`, `Precio`, `CurrencyCode`, `VigenteDesde`)
- `UnidadesMedida` (`Id`, `TenantId`, `Nombre`, `Simbolo`, `Tipo`, `FactorConversion`)

### POS & Cart Service (`postgres-pos`)
- `TurnosCaja` (`Id`, `CajeroId`, `SucursalId`, `MontoApertura`, `MontoCierre`, `Estado`, `AbiertoAt`, `CerradoAt`)
- `Ventas` (`Id`, `TurnoId`, `CajeroId`, `SucursalId`, `Total`, `MetodoPago`, `Estado`, `CreatedAt`)
- `VentaItems` (`Id`, `VentaId`, `ProductoId`, `Cantidad`, `PrecioUnitario`, `Subtotal`, `PesoKg`)
- `Pagos` (`Id`, `VentaId`, `MetodoPago`, `Monto`, `Vuelto`)

---

## 🎯 Criterios de Aceptación Macro del Sprint 2

### Catalog & Pricing Service
- [ ] Un `ADMINISTRADOR` puede crear productos con categoría, UOM, código de barras y precio base.
- [ ] El código QR se genera automáticamente al crear un producto y es descargable en PNG.
- [ ] La conversión de UOM funciona correctamente (`kg ↔ lb`) adaptándose al sistema del Tenant.
- [ ] Un mismo producto puede tener precios diferentes según la sucursal seleccionada.

### POS & Cart Service
- [ ] Un `CAJERO` puede abrir turno, procesar ventas sucesivas y realizar el cierre de caja con reporte.
- [ ] El carrito de compras agrega productos por código de barras y calcula el total en la moneda del Tenant.
- [ ] La balanza física transmite el peso en tiempo real al POS para calcular el precio de productos a granel.
- [ ] Los pagos en efectivo calculan el vuelto exacto y emiten ticket de venta en formato estándar de impresión térmica.

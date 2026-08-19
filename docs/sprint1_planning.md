# 🏃 Sprint 1 — Infraestructura Base, Identidad y Configuración Multi-Tenant
*(Stack: Python + FastAPI + SQLAlchemy + Electron — Aplicación de Escritorio Offline)*

> **Capacidad del Sprint:** 6 personas × 5 h/semana × 4 semanas = **120 horas totales**
> **Objetivo del Sprint:** Sentar las bases arquitectónicas — aplicación de escritorio offline con Electron que spawna FastAPI localmente, aislamiento de BD por servicio, Tenant & Identity Service completo con login, perfiles de sucursales, gestión de roles (Cajero, Administrador, Reponedor) y configuración de localización (idioma, zona horaria, formato de fecha, moneda y símbolo monetario) por Tenant.

---

## ⚠️ Cambios de Stack aplicados

| Área | Stack anterior | Stack nuevo |
|---|---|---|
| **Runtime backend** | Node.js 20 + NestJS | Python 3.12 + FastAPI |
| **ORM** | TypeORM | SQLAlchemy 2.x (async) |
| **Migraciones** | TypeORM migrations | Alembic |
| **Validación** | class-validator + class-transformer | Pydantic v2 |
| **Auth JWT** | @nestjs/passport + passport-jwt | python-jose + passlib[bcrypt] + FastAPI Depends() |
| **Configuración** | @nestjs/config | pydantic-settings (BaseSettings) |
| **API Docs** | @nestjs/swagger + SwaggerModule | FastAPI built-in OpenAPI en /docs y /redoc |
| **Testing backend** | Jest + Supertest | pytest + httpx + pytest-asyncio + pytest-cov |
| **Linting** | ESLint + Prettier + husky | black + flake8 + isort + pre-commit |
| **Package manager** | npm + package.json | pip + pyproject.toml |
| **Contenedor** | node:20-alpine | python:3.12-slim + uvicorn |
| **Frontend** | React + Vite (web SPA) | Electron Forge + React + TypeScript (app de escritorio) |
| **Estado frontend** | Zustand | electron-store + IPC |
| **Build frontend** | Vite | electron-vite / electron-forge |
| **Tests E2E** | Playwright (browser) | Playwright con _electron.launch() |
| **Monitoreo** | prom-client (npm) | prometheus-client (pip) |
| **API Gateway** | Kong DB-less | Eliminado — no necesario para app offline |

---

## Arquitectura Offline — Como funciona sin internet

El proceso principal de Electron (Node.js, `src/main/bridge.ts`) lanza FastAPI (uvicorn) como subproceso mediante `child_process.spawn()`. Antes de mostrar la `BrowserWindow`, hace polling a `http://127.0.0.1:3001/health`. El renderer (React) llama a `http://127.0.0.1:3001` via axios. Al cerrar la app, el proceso uvicorn es terminado con SIGTERM. PostgreSQL corre en Docker (dev) o instalado localmente. **No se requiere internet en ningún paso.**

**Entorno de desarrollo:**
- Terminal 1: `docker compose up postgres-tenant -d`
- Terminal 2: `cd services/tenant-identity && alembic upgrade head && uvicorn app.main:app --reload --port 3001`
- Terminal 3: `cd frontend && npm run dev`

---

## Equipo

| ID | Integrante |
|---|---|
| **Diego** | Diego |
| **Sebastian** | Sebastián |
| **Martin** | Martín |
| **Rodrigo** | Rodrigo |
| **Daniel** | Daniel |
| **Nicolas** | Nicolás |

---

## Sprint Backlog — Epicas del Sprint 1

| Epica | Descripción | Meta del Sprint cubierta |
|---|---|---|
| **E1** | Infraestructura y entorno base (Docker, monorepo, CI/CD) | G2 — Aislamiento de BD |
| **E2** | Diseño del dominio — Tenant, Sucursal & Identity Service | G3 — Tenant & Identity |
| **E3** | Base de datos exclusiva del servicio (PostgreSQL + Alembic) | G2 — Aislamiento de BD |
| **E4** | Arquitectura offline Electron — main spawna FastAPI, renderer llama localhost | G1 — Arquitectura Offline |
| **E5** | API REST: Tenants, Sucursales, Auth, Usuarios (FastAPI routers) | G3, G4 |
| **E6** | Gestión de roles: Cajero, Administrador, Reponedor | G4 — Roles básicos |
| **E7** | Configuración de localización: idioma, timezone, fecha, moneda, símbolo | G5, G6 |
| **E8** | Frontend Electron — Login, Registro, Dashboard, Sucursales, Roles, Localización | G4, G5, G6 |
| **E9** | QA — tests unitarios, integración y E2E con Playwright+Electron | Transversal |

---

## Semana 1 — Arranque: Entorno local, monorepo y modelo de dominio completo

**Meta de la semana:** El repositorio está estructurado, Docker Compose corre con PostgreSQL aislado por servicio, el proyecto FastAPI está inicializado con su estructura de carpetas, Alembic está configurado con las migraciones de `tenants`, `sucursales` y `users`, el proyecto Electron Forge está creado y el pipeline de CI funciona para Python.

---

### Diego — Rol base: Arquitecto / DevOps

**Tarea 1** *(Dificultad: medio)* — **[DevOps] Inicializar el monorepo con estructura de microservicios**
Crear el repositorio Git (GitHub/GitLab). Definir la estructura de carpetas raíz: `/services/tenant-identity/`, `/services/catalog-pricing/` (placeholder), `/services/warehouse-inventory/` (placeholder), `/infra/`, `/frontend/`, `/docs/`. Agregar `.gitignore` global (incluir `__pycache__/`, `*.pyc`, `.env`, `dist/`, `node_modules/`), `README.md` con convención de ramas (`main`, `develop`, `feat/*`, `fix/*`) y `CODEOWNERS`. *(~1.5 h)*

**Tarea 2** *(Dificultad: medio)* — **[DevOps] Crear el `docker-compose.yml` base del entorno local**
Definir los servicios iniciales: `postgres-tenant` (imagen `postgres:16-alpine`, variables de entorno `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`, volumen persistente `pgdata-tenant`, health-check `pg_isready`), `pgadmin` para inspección visual. Exponer puertos, definir red Docker interna `minimarket-net`. Cada microservicio futuro tendrá su propio `postgres-[servicio]`; documentar este patrón de aislamiento en un comentario inline. *(~2 h)*

**Tarea 3** *(Dificultad: facil)* — **[DevOps] Crear archivo `.env.example` y documentar variables de entorno**
Listar todas las variables requeridas: `DATABASE_URL=postgresql+asyncpg://user:pass@localhost:5432/tenant_db`, `JWT_SECRET`, `JWT_ALGORITHM=HS256`, `JWT_EXPIRY_HOURS=8`, `APP_ENV=development`, `APP_PORT=3001`. Agregar `.env` al `.gitignore`. Crear script `scripts/setup-local.sh` que copie `.env.example` a `.env` si no existe. *(~1 h)*

**Tarea 4** *(Dificultad: facil)* — **[QA] Verificar que `docker-compose up` levante correctamente**
Ejecutar `docker compose up -d postgres-tenant pgadmin`, validar health-check (`pg_isready`). Documentar los pasos en `infra/LOCAL_SETUP.md`, incluyendo cómo aplicar las migraciones con `alembic upgrade head` y cómo verificar tablas con `psql`. *(~0.5 h)*

> **Total estimado: ~5 h**

---

### Sebastián — Rol base: Backend / DDD

**Tarea 1** *(Dificultad: dificil)* — **[Backend/DDD] Modelar las entidades de dominio completas del Tenant & Identity Service**
Identificar y documentar los Aggregates raíz: `Tenant` (id, name, country, status, createdAt) con Value Object `TenantConfig` (timezone: IANA string, currency: ISO 4217, currencySymbol: str, language: BCP-47, dateFormat: str ej. `DD/MM/YYYY`). `Sucursal` (id, tenantId, name, address, phone, isHeadquarters, isActive). `User` (id, tenantId, sucursalId, email, passwordHash, role, isActive). `UserRole` enum: `SUPER_ADMIN`, `ADMINISTRADOR`, `CAJERO`, `REPONEDOR`. Crear diagrama UML en `services/tenant-identity/docs/domain-model.md` con relaciones: Tenant 1->N Sucursal, Tenant 1->N User, Sucursal 1->N User. *(~2.5 h)*

**Tarea 2** *(Dificultad: medio)* — **[Backend] Inicializar proyecto FastAPI para el Tenant & Identity Service**
Crear `services/tenant-identity/pyproject.toml`. Ejecutar: `pip install fastapi uvicorn[standard] sqlalchemy[asyncio] asyncpg alembic python-jose[cryptography] passlib[bcrypt] pydantic-settings httpx`. Crear `app/main.py` con `app = FastAPI(title='Tenant & Identity Service', version='1.0', docs_url='/api-docs', redoc_url='/redoc')`. Crear `app/core/config.py` con `class Settings(BaseSettings): db_url: str = Field(..., env='DATABASE_URL'); jwt_secret: str = Field(..., env='JWT_SECRET')`. Crear estructura completa: `app/core/`, `app/models/`, `app/schemas/`, `app/routers/`, `app/services/`. *(~1.5 h)*

**Tarea 3** *(Dificultad: facil)* — **[Base de datos] Configurar Alembic y crear migración inicial para `tenants`**
Ejecutar `alembic init alembic` dentro de `services/tenant-identity/`. Editar `alembic.ini` con la URL asyncpg. Configurar `alembic/env.py` con `target_metadata = Base.metadata` y soporte async. Crear migración: `alembic revision --autogenerate -m 'create_tenants_table'`. Columnas: `id UUID PRIMARY KEY DEFAULT gen_random_uuid()`, `name VARCHAR(100) NOT NULL`, `country_code CHAR(2)`, `timezone VARCHAR(50)`, `currency_code CHAR(3)`, `currency_symbol VARCHAR(10)`, `language_code VARCHAR(10)`, `date_format VARCHAR(20) DEFAULT 'DD/MM/YYYY'`, `status VARCHAR(20) DEFAULT 'ACTIVE'`, `created_at TIMESTAMPTZ DEFAULT NOW()`. *(~1 h)*

> **Total estimado: ~5 h**

---

### Martín — Rol base: Base de datos / Backend

**Tarea 1** *(Dificultad: medio)* — **[Base de datos] Crear migración Alembic para `sucursales`**
Migración `alembic revision --autogenerate -m 'create_sucursales_table'`. Columnas: `id UUID PK DEFAULT gen_random_uuid()`, `tenant_id UUID FK -> tenants(id) ON DELETE CASCADE`, `name VARCHAR(100) NOT NULL`, `address TEXT`, `phone VARCHAR(20)`, `is_headquarters BOOLEAN DEFAULT FALSE`, `is_active BOOLEAN DEFAULT TRUE`, `created_at TIMESTAMPTZ DEFAULT NOW()`. Agregar índice `(tenant_id, name)`. Constraint: solo una sucursal por tenant puede tener `is_headquarters = TRUE` (partial unique index en PostgreSQL). *(~2 h)*

**Tarea 2** *(Dificultad: medio)* — **[Base de datos] Crear migración para `users` y script de semillas Python**
Migración `alembic revision --autogenerate -m 'create_users_table'`. Columnas: `id UUID PK`, `tenant_id UUID FK`, `sucursal_id UUID FK -> sucursales(id) ON DELETE SET NULL`, `email VARCHAR(255) NOT NULL`, `password_hash TEXT NOT NULL`, `role VARCHAR(30) NOT NULL CHECK (role IN ('SUPER_ADMIN','ADMINISTRADOR','CAJERO','REPONEDOR'))`, `is_active BOOLEAN DEFAULT TRUE`, `last_login_at TIMESTAMPTZ`, `created_at TIMESTAMPTZ`. Índice compuesto UNIQUE `(tenant_id, email)`. Crear `seeds/seed_demo.py` usando SQLAlchemy AsyncSession para crear tenant "MiniMart CL" (CLP, $, America/Santiago, DD/MM/YYYY, es-CL), sucursal "Casa Matriz" y un usuario ADMINISTRADOR. *(~2 h)*

**Tarea 3** *(Dificultad: facil)* — **[DevOps] Configurar `Dockerfile` para el servicio `tenant-identity`**
Crear `services/tenant-identity/Dockerfile` con imagen base `python:3.12-slim`: `WORKDIR /app`, `COPY pyproject.toml .`, `RUN pip install --no-cache-dir .`, `COPY app/ ./app/`, `COPY alembic/ ./alembic/`, `COPY alembic.ini .`, `CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "3001"]`. Agregar el servicio al `docker-compose.yml` con `depends_on: { postgres-tenant: { condition: service_healthy } }`. *(~1 h)*

> **Total estimado: ~5 h**

---

### Rodrigo — Rol base: Frontend / UI

**Tarea 1** *(Dificultad: medio)* — **[Frontend] Inicializar proyecto Electron Forge con React + TypeScript**
Ejecutar `npm init electron-app@latest frontend -- --template=vite-typescript`. Instalar: `react react-dom @types/react @types/react-dom react-router-dom axios react-hook-form zod electron-store`. Crear estructura de carpetas: `src/main/index.ts` (BrowserWindow + IPC handlers), `src/main/bridge.ts` (spawn uvicorn), `src/preload/index.ts` (contextBridge), `src/renderer/src/App.tsx` (React entry + HashRouter), `src/renderer/src/pages/`, `src/renderer/src/components/ui/`, `src/renderer/src/services/` (axios calls a http://127.0.0.1:3001), `src/renderer/src/store/` (electron-store wrapper hooks). Configurar `<HashRouter>` en App.tsx (requerido para file:// de Electron). *(~1.5 h)*

**Tarea 2** *(Dificultad: medio)* — **[Frontend] Definir el sistema de diseño base (tokens CSS y tipografía)**
Crear `src/renderer/src/styles/tokens.css` con variables CSS: colores primarios (verde oscuro `#1B4332`, blanco roto `#F8F9FA`, acento ámbar `#F59E0B`, error `#DC2626`, success `#16A34A`), tipografía (Inter para body, Poppins para headings), espaciados (--space-1 a --space-16), radios de borde, sombras de elevación (--shadow-sm a --shadow-xl). Importar en `main.tsx`. *(~1.5 h)*

**Tarea 3** *(Dificultad: facil)* — **[Frontend] Maquetar los componentes UI base: InputField y SelectField**
`InputField.tsx`: props `label`, `name`, `type`, `placeholder`, `error`, `register`. Estado focus con borde ámbar, estado error con borde rojo + mensaje debajo. `SelectField.tsx`: mismas props + `options: {value, label}[]`. Ambos con CSS Modules. *(~1 h)*

**Tarea 4** *(Dificultad: facil)* — **[Frontend] Maquetar el componente Button reutilizable**
Variantes: `primary`, `secondary`, `danger`, `ghost`. Props: `label`, `isLoading` (spinner CSS inline), `disabled`, `onClick`, `type`, `fullWidth`. Micro-animación en hover: `transform: translateY(-1px)`, sombra elevada. *(~1 h)*

> **Total estimado: ~5 h**

---

### Daniel — Rol base: QA / Backend

**Tarea 1** *(Dificultad: medio)* — **[QA] Configurar el framework de testing para el backend (pytest + httpx)**
En `services/tenant-identity/pyproject.toml`, agregar sección `[tool.pytest.ini_options]` con `asyncio_mode = "auto"`, `testpaths = ["tests"]`, `addopts = "--cov=app --cov-report=xml --cov-report=term-missing"`. En `[project.optional-dependencies]`: `dev = ["pytest", "pytest-asyncio", "httpx", "pytest-cov"]`. Instalar con `pip install -e '.[dev]'`. Crear `tests/conftest.py` con fixture `async def client(): async with AsyncClient(app=app, base_url='http://test') as c: yield c`. Crear `tests/unit/` y `tests/integration/` con `__init__.py`. *(~1.5 h)*

**Tarea 2** *(Dificultad: medio)* — **[QA] Escribir tests unitarios para los esquemas Pydantic del dominio**
Crear `tests/unit/test_tenant_schemas.py`. Cubrir con `pytest.raises(ValidationError)`: (a) `TenantConfigSchema` válido con timezone IANA correcto (`America/Santiago`); (b) `currency` inválida (no ISO 4217) -> `ValidationError`; (c) `currencySymbol` vacío (`""`) -> `ValidationError` (campo con `min_length=1`); (d) `dateFormat` fuera del `Literal['DD/MM/YYYY', 'MM/DD/YYYY', 'YYYY-MM-DD']` -> `ValidationError`; (e) `language_code` con código BCP-47 inválido -> `ValidationError`. Estructurar con `class TestTenantConfig`. *(~2 h)*

**Tarea 3** *(Dificultad: facil)* — **[Backend] Crear routers placeholder en FastAPI**
Crear `app/routers/tenants.py` con `router = APIRouter(prefix='/tenants', tags=['Tenants'])` y `@router.get('/', response_model=list[TenantResponse]) async def list_tenants(): return []`. Crear routers análogos para `sucursales`, `users` y `auth`. Registrar en `app/main.py` con `app.include_router(tenants.router)`. Verificar que `GET /api-docs` (Swagger UI automático de FastAPI) está disponible y refleja todos los endpoints. *(~1.5 h)*

> **Total estimado: ~5 h**

---

### Nicolás — Rol base: DevOps / CI

**Tarea 1** *(Dificultad: dificil)* — **[DevOps/CI] Configurar el pipeline de CI en GitHub Actions para Python**
Crear `.github/workflows/ci.yml`. Jobs: (1) `lint-and-test` — checkout, setup Python 3.12, `pip install -e '.[dev]'`, `black --check app/`, `flake8 app/`, `isort --check-only app/`, `pytest tests/ --cov=app --cov-report=xml`; (2) `build-docker` — login a registry con secrets `DOCKER_USERNAME`/`DOCKER_PASSWORD`, build y push de imagen `tenant-identity:${{ github.sha }}`. Activar en `push` a `develop` y `feat/*`. *(~2.5 h)*

**Tarea 2** *(Dificultad: medio)* — **[Backend] Crear `app/core/database.py` con AsyncSession**
Crear `engine = create_async_engine(settings.db_url, echo=settings.app_env == 'development')`, `AsyncSessionLocal = async_sessionmaker(engine, expire_on_commit=False)`. Implementar `async def get_db() -> AsyncGenerator[AsyncSession, None]:` con try/commit/rollback. Agregar `Base = declarative_base()` para que Alembic detecte los modelos. Verificar la conexión levantando el servidor con `uvicorn app.main:app --reload --port 3001` y ejecutando `GET /api-docs`. *(~1.5 h)*

**Tarea 3** *(Dificultad: facil)* — **[DevOps] Configurar linter, formatter y hook de pre-commit**
Crear `.pre-commit-config.yaml` con repos para black (rev: 24.3.0), flake8 (rev: 7.0.0, args: --max-line-length=88), e isort (rev: 5.13.2, args: --profile=black). Ejecutar `pre-commit install` para activar los hooks localmente. Agregar `setup.cfg` con configuración de flake8. *(~1 h)*

> **Total estimado: ~5 h**

---
## Semana 2 — Arquitectura Offline Electron, autenticación JWT y CRUD de Sucursales

**Meta de la semana:** La arquitectura offline Electron está operativa (main spawna FastAPI, renderer llama localhost), el login JWT funciona end-to-end con FastAPI, el CRUD de Sucursales y Usuarios está disponible en la API, y la pantalla de login del frontend ya consume la API real a través de IPC y axios.

---

### Diego — Rol base: DevOps / Arquitectura Offline Electron

**Tarea 1** *(Dificultad: dificil)* — **[DevOps/Electron] Configurar el main process para spawnear FastAPI**
Crear `src/main/bridge.ts`. Importar `spawn, ChildProcess` de `child_process`. Función `startFastAPI()`: ejecutar `spawn('uvicorn', ['app.main:app', '--port', '3001', '--host', '127.0.0.1'], { cwd: path.join(app.getAppPath(), '..', 'services', 'tenant-identity'), stdio: ['ignore', 'pipe', 'pipe'] })`. Capturar stdout y stderr del proceso hijo. Función `waitForAPI(retries=30)`: hacer polling GET a `http://127.0.0.1:3001/health` cada 500ms hasta obtener 200. Función `stopFastAPI()`: llamar `apiProcess?.kill('SIGTERM')`. En `src/main/index.ts`: llamar `await startFastAPI()` antes de `win.show()`; registrar `app.on('will-quit', () => stopFastAPI())`. *(~2.5 h)*

**Tarea 2** *(Dificultad: medio)* — **[Electron] Configurar seguridad de Electron con contextBridge e IPC**
Crear `src/preload/index.ts` con `contextBridge.exposeInMainWorld('api', { getToken: () => ipcRenderer.invoke('store:getToken'), setToken: (token) => ipcRenderer.invoke('store:setToken', token), clearToken: () => ipcRenderer.invoke('store:clearToken'), getTenant: () => ipcRenderer.invoke('tenant:get'), setTenant: (tenant) => ipcRenderer.invoke('tenant:set', tenant) })`. Registrar handlers en main con `ipcMain.handle('store:getToken', () => store.get('authToken'))`. Configurar `BrowserWindow` con `webPreferences: { nodeIntegration: false, contextIsolation: true, preload: path.join(__dirname, 'preload.js') }`. *(~1.5 h)*

**Tarea 3** *(Dificultad: facil)* — **[Documentación] Crear `infra/OFFLINE_ARCHITECTURE.md`**
Documentar: (1) por qué la app no necesita internet, (2) cómo Electron spawna FastAPI y el ciclo de vida del subproceso, (3) cómo el renderer llama a localhost vía axios, (4) diferencia entre entorno dev (uvicorn con --reload) y producción (binary PyInstaller detectado por `app.isPackaged`), (5) setup inicial para desarrolladores (instalar Python 3.12, pip, Docker para postgres), (6) cómo depurar el backend desde Electron (logs del stdout del child process). *(~1 h)*

> **Total estimado: ~5 h**

---

### Sebastián — Rol base: Backend / Autenticación

**Tarea 1** *(Dificultad: dificil)* — **[Backend] Implementar la lógica de autenticación JWT con FastAPI**
Crear `app/core/security.py` con `pwd_context = CryptContext(schemes=['bcrypt'], deprecated='auto')`, `create_access_token(data: dict) -> str` usando python-jose y `verify_token(token: str) -> dict` que lanza `HTTPException(401)` ante JWTError. Crear `app/core/auth.py` con `oauth2_scheme = OAuth2PasswordBearer(tokenUrl='/auth/login')` y dependency `async def get_current_user(token: str = Depends(oauth2_scheme), db: AsyncSession = Depends(get_db)) -> User`. Endpoint `POST /auth/login` retorna `{ access_token, token_type, user: { id, email, role, tenant_id, sucursal_id, tenant_config } }`. Incluir el `tenantConfig` completo en el payload del JWT para evitar llamadas adicionales desde el renderer. *(~2.5 h)*

**Tarea 2** *(Dificultad: medio)* — **[Backend] Implementar dependency de roles**
Implementar `def require_roles(*roles: UserRole)` que retorna una async dependency `checker`. Si `current_user.role` no está en la lista de roles permitidos, lanzar `HTTPException(status_code=403, detail=f'Se requiere uno de los roles: {[r.value for r in roles]}')`. Usar como: `@router.post('/', dependencies=[Depends(require_roles(UserRole.ADMINISTRADOR))])`. Proteger todos los routers de recursos con la dependency correspondiente. *(~1.5 h)*

**Tarea 3** *(Dificultad: facil)* — **[Base de datos] Agregar índice de rendimiento a la tabla `users`**
Crear migración `alembic revision -m 'add_index_users_tenant_email'` con `op.create_index('idx_users_tenant_email', 'users', ['tenant_id', 'email'], unique=True, postgresql_concurrently=True)`. Verificar con `EXPLAIN ANALYZE SELECT * FROM users WHERE tenant_id = '...' AND email = '...'` que el índice es utilizado (Seq Scan -> Index Scan). *(~1 h)*

> **Total estimado: ~5 h**

---

### Martín — Rol base: Backend / Dominio

**Tarea 1** *(Dificultad: medio)* — **[Backend] Implementar caso de uso CreateTenant y POST /tenants**
Crear `app/schemas/tenants.py` con `CreateTenantSchema(BaseModel)`: `name: str = Field(..., min_length=2, max_length=100)`, `country_code: str = Field(..., min_length=2, max_length=2)`, `timezone: str`, `currency: str = Field(..., min_length=3, max_length=3)`, `currency_symbol: str = Field(..., min_length=1)`, `language: str`, `date_format: Literal['DD/MM/YYYY', 'MM/DD/YYYY', 'YYYY-MM-DD'] = 'DD/MM/YYYY'`. Crear `app/services/tenant_service.py` con `async def create_tenant(db: AsyncSession, data: CreateTenantSchema) -> Tenant`. Verificar que no exista tenant con el mismo nombre. Persistir con `db.add(tenant); await db.commit(); await db.refresh(tenant)`. Retornar respuesta estandarizada `{ data, status_code, message }`. *(~2 h)*

**Tarea 2** *(Dificultad: medio)* — **[Backend] Implementar caso de uso RegisterUser y POST /auth/register**
Crear `app/schemas/users.py` con `class UserRole(str, Enum)` con valores SUPER_ADMIN, ADMINISTRADOR, CAJERO, REPONEDOR. `RegisterUserSchema(BaseModel)`: `tenant_id: UUID`, `sucursal_id: UUID | None = None`, `email: EmailStr`, `password: str = Field(..., min_length=8, pattern=r'^(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).+$')`, `role: Literal[UserRole.ADMINISTRADOR, UserRole.CAJERO, UserRole.REPONEDOR]`. Validaciones en `user_service.py`: tenant activo, email único en el tenant, sucursal pertenece al tenant. Hash con `pwd_context.hash(password)` (bcrypt cost 12). Solo un ADMINISTRADOR puede registrar CAJEROS y REPONEDORES. *(~2 h)*

**Tarea 3** *(Dificultad: facil)* — **[DevOps] Agregar health-check endpoint al servicio**
En `app/routers/health.py`: `@router.get('/health') async def health(db: AsyncSession = Depends(get_db)):` que ejecuta `await db.execute(text('SELECT 1'))` y retorna `{'status': 'ok', 'checks': {'db': {'status': 'ok'}}}`. En caso de error, retornar `JSONResponse(status_code=503, content=...)`. Este endpoint es el que usa `bridge.ts` para hacer polling al inicio de la app Electron. No requiere autenticación JWT. *(~1 h)*

> **Total estimado: ~5 h**

---

### Rodrigo — Rol base: Frontend / Login y Registro

**Tarea 1** *(Dificultad: medio)* — **[Frontend] Maquetar y conectar la página de Login en Electron**
Crear `src/renderer/src/pages/LoginPage.tsx`. Layout centrado con fondo degradado sutil (verde oscuro -> gris oscuro), adaptado a ventana de escritorio. Formulario: logo placeholder, campo email, campo password (toggle show/hide), botón "Iniciar sesión" con estado loading. Validar con `react-hook-form` + `zod`. Al enviar: `const res = await axios.post('http://127.0.0.1:3001/auth/login', { email, password })`, luego `await window.api.setToken(res.data.access_token)` (IPC -> electron-store), luego `navigate('/dashboard')`. *(~2.5 h)*

**Tarea 2** *(Dificultad: medio)* — **[Frontend] Implementar ProtectedRoute y estado de auth con IPC**
Crear `src/renderer/src/store/useAuthStore.ts` con hook que lee el token desde `window.api.getToken()` via IPC en `useEffect`. `ProtectedRoute.tsx`: si no hay token, mostrar la pantalla de login. Configurar router en `App.tsx` con `<HashRouter>` (obligatorio en Electron) con rutas públicas (`/login`, `/register`) y protegidas (`/dashboard/*`). *(~1.5 h)*

**Tarea 3** *(Dificultad: facil)* — **[Frontend] Crear componentes Spinner y EmptyState**
`Spinner.tsx`: animación CSS pura (círculo giratorio con `border-color: var(--color-primary)`). `EmptyState.tsx`: SVG inline minimalista (carpeta vacía), `title` y `subtitle` configurables, slot para botón de acción. *(~1 h)*

> **Total estimado: ~5 h**

---

### Daniel — Rol base: QA / Testing

**Tarea 1** *(Dificultad: dificil)* — **[QA] Tests de integración para POST /auth/login y POST /auth/register**
Crear `tests/integration/test_auth.py` con `AsyncClient` + `httpx`. Cubrir login: (a) credenciales válidas -> 200 + `access_token`; (b) password incorrecta -> 401; (c) usuario inactivo -> 403; (d) email inexistente -> 401. Cubrir register: (a) registro exitoso con rol CAJERO -> 201; (b) registro exitoso con rol REPONEDOR -> 201; (c) password débil (sin mayúscula) -> 422; (d) email duplicado en el mismo tenant -> 409; (e) rol inválido -> 422. Usar `pytest.mark.asyncio` y override del `get_db()` dependency con BD SQLite in-memory. *(~2.5 h)*

**Tarea 2** *(Dificultad: medio)* — **[QA] Tests unitarios para CreateTenantService**
Crear `tests/unit/test_tenant_service.py`. Mockear `AsyncSession` con `AsyncMock`. Cubrir: (a) creación exitosa con todos los campos incluyendo currencySymbol y dateFormat; (b) timezone IANA inválida -> `HTTPException(400)`; (c) currencySymbol vacío -> `ValidationError` de Pydantic; (d) tenant duplicado -> `HTTPException(409)`; (e) dateFormat fuera del Literal -> `ValidationError` de Pydantic. *(~1.5 h)*

**Tarea 3** *(Dificultad: facil)* — **[Frontend/QA] Validar formulario de login en la app Electron**
Verificar que el esquema Zod cubre: email con formato válido, password no vacío, mensajes de error en español. Abrir DevTools en Electron (Ctrl+Shift+I) -> Network: verificar que el request va a `http://127.0.0.1:3001/auth/login` (localhost, no internet). Verificar que el token se persiste en `electron-store` revisando `%APPDATA%/[app-name]/store.json`. *(~1 h)*

> **Total estimado: ~5 h**

---

### Nicolás — Rol base: Backend / Sucursales

**Tarea 1** *(Dificultad: medio)* — **[Backend] Implementar CRUD completo de Sucursales**
Crear `app/routers/sucursales.py`. Para listar: `result = await db.execute(select(Sucursal).where(Sucursal.tenant_id == current_user.tenant_id).where(Sucursal.is_active == True)); sucursales = result.scalars().all()`. Para crear: `sucursal = Sucursal(**data.model_dump(), tenant_id=current_user.tenant_id); db.add(sucursal); await db.commit(); await db.refresh(sucursal)`. Endpoints: `POST /sucursales`, `GET /sucursales`, `PATCH /sucursales/{id}`, `PATCH /sucursales/{id}/deactivate`. Solo ADMINISTRADOR puede crear/modificar. No se puede desactivar la sede si es la única activa. *(~2.5 h)*

**Tarea 2** *(Dificultad: medio)* — **[DevOps] Configurar gestión de secrets con GitHub Actions**
Agregar secrets: `JWT_SECRET`, `DB_PASSWORD`, `DOCKER_USERNAME`, `DOCKER_PASSWORD`. Actualizar `ci.yml` para inyectar secrets como variables de entorno en el job de testing. Crear `infra/SECRETS.md` explicando qué secrets existen, quién tiene acceso y cómo rotarlos de forma segura. *(~1.5 h)*

**Tarea 3** *(Dificultad: facil)* — **[QA] Crear colección Postman/Bruno con los endpoints de la Semana 2**
Incluir: `POST /auth/login`, `POST /auth/register`, `POST /tenants`, `GET|POST|PATCH /sucursales`, `GET /health`. Documentar `curl` equivalentes en `docs/api-examples.md`. Nota: todas las URLs son `http://127.0.0.1:3001` (localhost, sin gateway). Incluir caso de prueba de acceso entre tenants (debe retornar 403). *(~1 h)*

> **Total estimado: ~5 h**

---

## Semana 3 — Gestión de Usuarios, Roles y Configuración de Localización

**Meta de la semana:** El sistema permite gestionar usuarios por rol (Cajero, Administrador, Reponedor), asignar roles dentro de una sucursal, y modificar la configuración de localización del Tenant. El frontend Electron tiene las pantallas correspondientes usando HashRouter y electron-store.

---

### Diego — Rol base: Backend / Usuarios y Roles

**Tarea 1** *(Dificultad: medio)* — **[Backend] Implementar GET /users con paginación, filtros y scope por tenant**
Endpoint `GET /users` con parámetros `page: int = Query(1, ge=1)`, `limit: int = Query(20, ge=1, le=100)`, `role: UserRole | None = None`, `sucursal_id: UUID | None = None`, `is_active: bool | None = None`. Query base: `select(User).where(User.tenant_id == current_user.tenant_id)`. Aplicar filtros opcionales. Calcular total con `select(func.count()).select_from(query.subquery())`. Aplicar `offset((page - 1) * limit).limit(limit)`. Retornar `PaginatedResponse(data=..., total=..., page=..., limit=...)`. Solo ADMINISTRADOR puede listar usuarios. *(~2 h)*

**Tarea 2** *(Dificultad: medio)* — **[Backend] Implementar PATCH /users/{id}/role para cambiar rol**
Service `change_user_role(db, user_id, new_role, current_user)`. Restricciones: solo ADMINISTRADOR puede cambiar roles; no puede cambiar su propio rol (user_id == current_user.id -> 403); no puede asignar SUPER_ADMIN desde este endpoint (-> 400). Guardar cambio con `await db.commit(); await db.refresh(user)` y retornar el usuario actualizado. *(~1.5 h)*

**Tarea 3** *(Dificultad: facil)* — **[Backend] Implementar PATCH /users/{id}/deactivate y GET /auth/me**
`deactivate_user`: marcar `is_active = False`. Solo ADMINISTRADOR puede desactivar. No puede desactivarse a sí mismo. `GET /auth/me`: retornar perfil completo `{ id, email, role, tenant_id, sucursal_id, sucursal_name, tenant_config }` usando `Depends(get_current_user)` directamente. *(~1.5 h)*

> **Total estimado: ~5 h**

---

### Sebastián — Rol base: Backend / Localización

**Tarea 1** *(Dificultad: medio)* — **[Backend] Implementar GET /tenants/{id} con control de acceso por tenant**
Service `get_tenant_by_id(db, tenant_id, current_user)`. Validar que el `tenant_id` del JWT coincide con el `{id}` solicitado. SUPER_ADMIN puede ver cualquier tenant. Retornar el tenant con su configuración completa en `TenantDetailResponse` incluyendo `config: TenantConfigResponse` con timezone, currency, currency_symbol, language, date_format. *(~1.5 h)*

**Tarea 2** *(Dificultad: medio)* — **[Backend] Implementar PATCH /tenants/{id}/config para actualizar localización**
Crear `UpdateTenantConfigSchema(BaseModel)` con todos los campos opcionales: `timezone: str | None = None`, `currency: str | None = Field(None, min_length=3, max_length=3)`, `currency_symbol: str | None = Field(None, min_length=1)`, `language: str | None = None`, `date_format: Literal['DD/MM/YYYY', 'MM/DD/YYYY', 'YYYY-MM-DD'] | None = None`. Service `update_tenant_config`: validar timezone contra `zoneinfo.available_timezones()`. Solo ADMINISTRADOR del tenant puede actualizar (o SUPER_ADMIN). Retornar la configuración actualizada. *(~2 h)*

**Tarea 3** *(Dificultad: facil)* — **[QA] Tests unitarios para update_tenant_config**
Crear `tests/unit/test_tenant_config_service.py`. Cubrir: (a) actualización exitosa de timezone; (b) timezone IANA inválida -> HTTPException(400); (c) currencySymbol vacío -> ValidationError de Pydantic; (d) dateFormat fuera del Literal -> ValidationError; (e) tenant no encontrado -> HTTPException(404); (f) usuario CAJERO intenta actualizar -> HTTPException(403). *(~1.5 h)*

> **Total estimado: ~5 h**

---

### Martín — Rol base: Frontend / Layout y Sucursales

**Tarea 1** *(Dificultad: medio)* — **[Frontend] Maquetar el layout base del Dashboard en Electron**
Crear `src/renderer/src/layouts/DashboardLayout.tsx`. Sidebar izquierdo: logo, navegación (Inicio, Sucursales, Usuarios, Configuración), nombre del tenant activo (leído desde `window.api.getTenant()` via IPC), avatar del usuario con dropdown (Ver perfil, Cerrar sesión). Header: breadcrumb dinámico, badge del rol activo. Área de contenido con `<Outlet />`. Usar `<HashRouter>` y `Link` de react-router-dom (requerido para file:// de Electron). Para actualizar el título de la BrowserWindow, usar `ipcRenderer.invoke('window:setTitle', nuevoTitulo)`. *(~2 h)*

**Tarea 2** *(Dificultad: medio)* — **[Frontend] Maquetar la página de Sucursales (#/dashboard/sucursales)**
Crear `src/renderer/src/pages/sucursales/SucursalesPage.tsx`. Tabla: Nombre, Dirección, Teléfono, Sede (badge), Estado, Acciones (Ver, Editar, Desactivar). Consumir `axios.get('http://127.0.0.1:3001/sucursales')` con el `Authorization: Bearer [token]` leído desde `window.api.getToken()`. Mostrar Spinner/EmptyState. Botón "Nueva Sucursal" visible solo para ADMINISTRADOR. *(~2 h)*

**Tarea 3** *(Dificultad: facil)* — **[Frontend] Modal para crear/editar Sucursal**
Crear `src/renderer/src/components/sucursales/SucursalModal.tsx`. Campos: nombre, dirección, teléfono, checkbox "Es sede principal". Validado con react-hook-form + zod. Llamar `axios.post('http://127.0.0.1:3001/sucursales', data)` en creación y `axios.patch(...)` en edición. Toast de confirmación al éxito. *(~1 h)*

> **Total estimado: ~5 h**

---

### Rodrigo — Rol base: Frontend / Gestión de Usuarios

**Tarea 1** *(Dificultad: medio)* — **[Frontend] Maquetar la página de gestión de Usuarios (#/dashboard/usuarios)**
Crear `src/renderer/src/pages/usuarios/UsuariosPage.tsx`. Tabla: Email, Rol (badge de color: ADMINISTRADOR azul, CAJERO verde, REPONEDOR ámbar), Sucursal, Estado, Acciones (Cambiar rol, Desactivar). Filtros: por rol, por sucursal, por estado. Consumir `GET http://127.0.0.1:3001/users?page=1&limit=20` con paginación y axios. *(~2 h)*

**Tarea 2** *(Dificultad: medio)* — **[Frontend] Implementar el modal de cambio de rol**
`src/renderer/src/components/usuarios/ChangeRoleModal.tsx`. Select con las opciones del enum (CAJERO, REPONEDOR, ADMINISTRADOR). Descripción de permisos de cada rol. Confirmación antes de guardar. Llamar `axios.patch('http://127.0.0.1:3001/users/{id}/role', { role })`. Actualizar el item en la tabla sin recargar la página. *(~2 h)*

**Tarea 3** *(Dificultad: facil)* — **[Frontend] Crear el store de Tenant activo usando electron-store via IPC**
Crear `src/renderer/src/store/useTenantStore.ts` con hook que lee el tenant desde `window.api.getTenant()` en useEffect. Función `updateTenant` que llama `window.api.setTenant(t)` y actualiza el estado local. Usar el tenantConfig para formatear moneda con `Intl.NumberFormat(tenant.language_code, { style: 'currency', currency: tenant.currency_code })` en el sidebar. *(~1 h)*

> **Total estimado: ~5 h**

---

### Daniel — Rol base: QA / E2E con Playwright + Electron

**Tarea 1** *(Dificultad: dificil)* — **[QA] Escribir tests E2E con Playwright para el flujo de login en Electron**
Instalar: `npm install -D @playwright/test`. Crear `tests/e2e/auth.spec.ts`. Importar `{ _electron as electron, test, expect } from '@playwright/test'`. Test 1: `const app = await electron.launch({ args: ['dist/main/index.js'] })`, `const window = await app.firstWindow()`, fill email y password, click login, expect dashboard-layout visible, `await app.close()`. Test 2: credenciales inválidas, expect error-msg visible. Test 3: verificar que la BrowserWindow muestra la pantalla de login si no hay token en electron-store. *(~2.5 h)*

**Tarea 2** *(Dificultad: medio)* — **[QA] Tests E2E para el flujo de Sucursales en la app Electron**
Crear `tests/e2e/sucursales.spec.ts`. Casos: (a) login como ADMINISTRADOR, navegar a #/dashboard/sucursales, verificar que la tabla carga con al menos una fila; (b) click en "Nueva Sucursal", completar el formulario, verificar que aparece en la tabla; (c) login como CAJERO, verificar que el botón "Nueva Sucursal" no está en el DOM. *(~1.5 h)*

**Tarea 3** *(Dificultad: facil)* — **[QA] Configurar reporte de cobertura en el CI**
Configurar `pytest --cov=app --cov-report=html --cov-report=xml` en el pipeline. Agregar paso que sube el directorio `htmlcov/` como artifact de GitHub Actions. Agregar badge de cobertura en el `README.md` del servicio. Verificar que la cobertura global supera el 70%. *(~1 h)*

> **Total estimado: ~5 h**

---

### Nicolás — Rol base: Frontend / Configuración de Localización

**Tarea 1** *(Dificultad: medio)* — **[Frontend] Maquetar la página de Configuración de Localización**
Crear `src/renderer/src/pages/configuracion/LocalizacionPage.tsx`. Secciones: Regional (zona horaria — autocomplete con lista IANA desde `Intl.supportedValuesOf('timeZone')`, idioma — select con opciones es-CL, es-AR, es-MX, en-US, pt-BR), Fecha y Hora (formato de fecha — radio buttons con preview live: 31/12/2025 vs 12/31/2025 vs 2025-12-31), Moneda (moneda ISO 4217, símbolo monetario, preview de monto formateado con `Intl.NumberFormat`). Solo visible para ADMINISTRADOR. *(~2 h)*

**Tarea 2** *(Dificultad: medio)* — **[Frontend] Conectar la pantalla de Localización con PATCH /tenants/{id}/config**
Cargar la configuración actual con `axios.get('http://127.0.0.1:3001/tenants/{id}')` al montar el componente. Formulario validado con react-hook-form + zod. Al guardar, llamar `axios.patch('http://127.0.0.1:3001/tenants/{id}/config', payload)` con solo los campos modificados. En éxito: actualizar el store con `window.api.setTenant(updatedTenant)` (IPC -> electron-store), mostrar toast de confirmación. *(~2 h)*

**Tarea 3** *(Dificultad: facil)* — **[DevOps] Agregar job de CD básico al pipeline**
En el workflow, agregar job `deploy-staging` que corre después de `build-docker` (solo en rama `develop`): SSH al servidor de staging (secret `SSH_PRIVATE_KEY`), `docker pull` de la nueva imagen, `docker-compose up -d tenant-identity`. Agregar `infra/STAGING.md` documentando el entorno. *(~1 h)*

> **Total estimado: ~5 h**

---

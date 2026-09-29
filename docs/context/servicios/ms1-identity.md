---
id: ms1-identity
tipo: microservicio
titulo: MS-1 · Tenant & Identity Service
estado: parcial
fuentes: [src/TenantIdentityService/, Docker/config/kong.yaml]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: []
publica: []
consume: []
reglas: [RN-01, RN-07, RN-08, RN-12]
---
# MS-1 · Tenant & Identity Service

> Responsable de autenticación, gestión de usuarios/roles, configuración del tenant y (en diseño) tipos de cambio FX. Es el único microservicio que no requiere JWT para su endpoint de login. Todos los demás microservicios dependen de él para obtener config de tenant.

## Estructura del proyecto

```
src/TenantIdentityService/
├── Controllers/
│   ├── AuthController.cs          POST /auth/login
│   ├── UsuarioController.cs       CRUD /api/v1/users + asignar roles
│   └── TenantConfigController.cs  GET+PUT /tenants/{id}/config + /api/v1/tenants/{id}/config
├── Data/  TenantDbContext.cs + Migrations/
├── Models/  Tenant · Usuario · UsuarioRol · Rol
├── Repositories/  IUsuarioRepository · UsuarioRepository · ITenantRepository · TenantRepository
├── Services/  IAuthService · AuthService · IJwtService · JwtService
├── Middleware/  TenantMiddleware.cs
└── Validators/  LoginRequestValidator.cs
```

## Endpoints reales

| Método | Ruta | Auth | Estado |
|--------|------|------|--------|
| POST | `/auth/login` | Público (sin JWT) | [IMPLEMENTADO] |
| GET | `/api/v1/users?page=&pageSize=` | JWT + ADMIN | [IMPLEMENTADO] |
| POST | `/api/v1/users` | JWT + ADMIN | [IMPLEMENTADO] |
| PUT | `/api/v1/users/{id}` | JWT + ADMIN | [IMPLEMENTADO] |
| DELETE | `/api/v1/users/{id}` | JWT + ADMIN | [IMPLEMENTADO] |
| POST | `/api/v1/users/{id}/roles` | JWT + ADMIN | [IMPLEMENTADO] |
| GET | `/tenants/{id}/config` | JWT | [IMPLEMENTADO] |
| PUT | `/tenants/{id}/config` | JWT + ADMIN | [IMPLEMENTADO] |
| GET | `/health` | Público | [IMPLEMENTADO] |

**Nota:** TenantConfigController registra doble prefijo: `/api/v1/tenants` y `/tenants`.

### Endpoints diseñados pero NO implementados
- `POST /auth/refresh` — renovar JWT [PLANIFICADO]
- `GET /tenants/{id}/sucursales` — gestión de sucursales [PLANIFICADO]
- `GET /fx/rates` — tipos de cambio actuales [PLANIFICADO]
- `POST /fx/convert` — conversión entre monedas [PLANIFICADO]

## Modelo de datos

```
Tenant
  id             GUID PK
  nombre         string
  pais           string (ej: "CL", "AR", "US")
  moneda         string (ej: "CLP", "ARS", "USD")
  idioma         string (ej: "es", "en")
  zona_horaria   string (ej: "America/Santiago")
  porcentaje_iva decimal
  (tenant_id NO aplica aquí — Tenant ES el root)

Usuario
  id             GUID PK
  tenant_id      GUID FK → Tenant (discriminador multi-tenant)
  nombre         string
  email          string
  password_hash  string (BCrypt)
  activo         bool
  creado_en      DateTime
  ultimo_login   DateTime?

Rol
  id    GUID PK
  nombre string ("CAJERO"|"REPONEDOR"|"ADMIN"|"SUPER_ADMIN"|"CLIENTE_AFILIADO")

UsuarioRol
  usuario_id  GUID FK
  rol_id      GUID FK
```

## JWT generado (estructura real)

```json
{
  "sub": "<usuario_id>",
  "tenant_id": "<tenant_id>",
  "roles": ["CAJERO"],
  "active_role": "CAJERO",
  "exp": 1725820800,
  "iat": 1725791200
}
```

**Notas de implementación:**
- `active_role` se determina por prioridad: SUPER_ADMIN > ADMIN > CAJERO > REPONEDOR > CLIENTE_AFILIADO
- `sucursal_id` mencionado en el ContextMaster NO está en el JWT real generado [DISCREPANCIA]
- Expiración: configurable via `Jwt:ExpirationHours` (default 8 horas)
- SUPER_ADMIN no puede ser asignado por un ADMIN (protección en UsuarioController)

## Multi-tenant en MS-1

El TenantMiddleware extrae `tenant_id` del JWT e inyecta en `TenantDbContext.CurrentTenantId`. El EF Core aplica filtro global automáticamente en todas las queries de usuarios. Tenants no tienen `tenant_id` propio (son el raíz).

## Seguridad

- Passwords: BCrypt hash [IMPLEMENTADO]
- JWT: validado en cada endpoint excepto `/auth/login` [IMPLEMENTADO]
- Seed de dev: `cajero@demo.cl` / `demo1234` / tenantId: `aaaaaaaa-0000-0000-0000-000000000001` [SOLO EN DESARROLLO]
- Un admin NO puede desactivarse a sí mismo [IMPLEMENTADO]

## Kong Gateway

```yaml
# Ruta pública (sin JWT):
POST /api/auth/login

# Rutas protegidas (con plugin JWT):
GET/PUT/DELETE /api/auth
GET/POST/PUT/DELETE /api/users
GET/POST/PUT/DELETE /api/tenants
```

## Tests

- **Seed de desarrollo:** crea tenant y cajero demo al iniciar [IMPLEMENTADO]
- **Tests unitarios:** NO encontrados
- **Tests de integración:** NO encontrados
- **Tests manuales:** vía Swagger UI en `/swagger`

## Brechas respecto al diseño

| Brecha | Impacto |
|--------|---------|
| Sin endpoint `/auth/refresh` | Sesiones no renovables |
| Sin gestión de sucursales | `sucursal_id` en JWT es campo libre |
| Sin integración FX (Fixer.io) | TI-15..19 no implementados |
| `sucursal_id` falta en JWT | RN-08 no cumplido al 100% |
| Sin log de auditoría | RNF-05 no implementado |

## Casos de uso cubiertos

| ID | Caso de Uso | Estado |
|----|-------------|--------|
| TI-01 | Iniciar Sesión (Login) | [IMPLEMENTADO] |
| TI-02 | Cerrar Sesión (Logout) | [PARCIAL — solo lado frontend] |
| TI-03 | Autenticar con MFA | [PLANIFICADO] |
| TI-04 | Gestionar Perfil de Usuario | [IMPLEMENTADO] |
| TI-05 | Crear / Editar / Desactivar Usuario | [IMPLEMENTADO] |
| TI-06 | Asignar Roles y Permisos | [IMPLEMENTADO] |
| TI-07 | Configurar Tenant (Localización) | [IMPLEMENTADO] |
| TI-08 | Definir Idioma del Sistema | [IMPLEMENTADO via TenantConfig] |
| TI-09 | Configurar Moneda Base | [IMPLEMENTADO via TenantConfig] |
| TI-10 | Configurar País y Zona Horaria | [IMPLEMENTADO via TenantConfig] |
| TI-11 | Gestionar Sucursales | [PLANIFICADO] |
| TI-14 | Auditar Log de Accesos | [PLANIFICADO] |
| TI-15 | Obtener Tipo de Cambio en Tiempo Real | [PLANIFICADO] |
| TI-16 | Actualizar Tabla de Tipos de Cambio | [PLANIFICADO] |
| TI-17 | Convertir Monto entre Monedas | [PLANIFICADO] |
| TI-18 | Configurar Umbral Actualización FX | [PLANIFICADO] |
| TI-19 | Registrar Historial de Tasas FX | [PLANIFICADO] |
| TI-20 | Asignar Múltiples Roles a Usuario | [IMPLEMENTADO] |
| TI-21 | Cambiar Rol Activo en Sesión | [PLANIFICADO] |
| TI-22 | Escalar Rol Temporalmente | [PLANIFICADO] |
| TI-23 | Configurar Separación de Funciones | [PLANIFICADO] |

## Conexiones
- Dependido por: [[ms2-tax]] (TenantConfigClient), [[ms5-pos]], [[ms3-catalog]], [[ms4-inventory]]
- Publica eventos: ninguno implementado (`fx.rate.updated` es PLANIFICADO)
- Reglas: [[rbac-multirol]] (RN-07, RN-08, RN-12), [[multi-tenant]] (RN-01)
- Kong: [[kong]]

## Fuentes
- `src/TenantIdentityService/Controllers/AuthController.cs`
- `src/TenantIdentityService/Controllers/UsuarioController.cs`
- `src/TenantIdentityService/Controllers/TenantConfigController.cs`
- `src/TenantIdentityService/Program.cs`
- `Docker/config/kong.yaml`

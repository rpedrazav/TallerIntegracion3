---
id: ms1-identity
tipo: microservicio
titulo: MS-1 · Tenant & Identity Service
estado: parcial
fuentes: [src/TenantIdentityService/, Docker/config/kong.yaml]
verificado_contra_codigo: true
ultima_revision: 2026-10-01
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
| GET | `/sucursales` | JWT | [IMPLEMENTADO] |
| GET | `/health` | Público | [IMPLEMENTADO] |

### GET /sucursales

```
GET /sucursales
Auth: Bearer <JWT con tenant_id>

200 → [ { id, tenantId, nombre, direccion, activa, zonaHoraria } ]
401 → sin JWT, JWT inválido o sin claim tenant_id
```

El tenant se obtiene **solo** del claim `tenant_id` del JWT: no se recibe como parámetro de ruta ni
de query, para que no se puedan pedir sucursales de otro tenant. El aislamiento lo aplica el
`HasQueryFilter` global de `TenantDbContext` alimentado por `TenantMiddleware` (`SucursalRepository`
no filtra manualmente). Devuelve las sucursales **inactivas también**, ordenadas por nombre, sin
paginación; la lista puede estar vacía.

`zonaHoraria` en la respuesta es la **efectiva**: `Sucursal.ZonaHoraria ?? Tenant.ZonaHoraria`.
Para resolverla el repository hace `Include(s => s.Tenant)`. Así queda ejecutada la regla de negocio
que hasta ahora estaba solo documentada.

**Discrepancia de contrato:** el ContextMaster y este nodo documentaban `GET /tenants/{id}/sucursales`.
El endpoint implementado es `GET /sucursales`, porque el tenant viene del JWT y el `{id}` en la ruta
sería redundante además de permitir consultar otro tenant. La ruta con `{id}` queda [NO IMPLEMENTADA].

**Nota:** TenantConfigController registra doble prefijo: `/api/v1/tenants` y `/tenants`.

### POST /sucursales

```
POST /sucursales
Auth: Bearer <JWT con tenant_id y rol ADMIN>
Body: { "nombre": "...", "direccion": "...", "zonaHoraria": "America/Santiago" | "" | null }

201 → { id, tenantId, nombre, direccion, activa, zonaHoraria }
400 → body inválido o zona horaria que no es un id IANA real
401 → sin JWT, JWT inválido o sin claim tenant_id
403 → el usuario no tiene el rol ADMIN
409 → ya existe una sucursal con ese nombre en el tenant (case-insensitive)
```

Crea una sucursal del tenant del JWT. `201` **sin** header `Location` porque no existe
`GET /sucursales/{id}`, solo el listado.

| Decisión | Valor | Motivo |
|---|---|---|
| Rol requerido | `ADMIN` | Crear una sucursal es una operación de nivel tenant, igual que crear usuarios o editar la config del tenant. Un CAJERO no abre sucursales |
| `tenant_id` | Solo del JWT | El body no lo acepta. Enviarlo se ignora en silencio: la sucursal queda en el tenant del token (RN-01) |
| `zonaHoraria` | Opcional | Vacía o `null` = hereda `Tenant.ZonaHoraria`; informada = específica de la sucursal |
| `activa` | Siempre `true` | No se pide en el request |
| Nombre duplicado | `409 Conflict` | Índice único como garantía real |

**Validación de zona horaria.** `CrearSucursalRequestValidator` exige prefijo de área IANA
(`Africa`, `America`, `Antarctica`, `Arctic`, `Asia`, `Atlantic`, `Australia`, `Europe`, `Indian`,
`Pacific`, `Etc`, o el literal `UTC`) y luego que la zona exista en el runtime.

> [!IMPORTANT]
> El prefijo es obligatorio porque `TimeZoneInfo.FindSystemTimeZoneById` **no** es un validador IANA
> en .NET 8: con ICU también resuelve ids de Windows (`Chile/Continental`,
> `SA Pacific Standard Time`) y los acepta. Sin el filtro por prefijo, un id de Windows se
> guardaría y `GET /sucursales` devolvería una zona no IANA. La comparación es **ordinal**: la base
> IANA distingue mayúsculas, así que `america/santiago` se rechaza con 400.

**Unicidad de nombre (case-insensitive).** Garantizada por el índice único
`IX_Sucursales_TenantId_NombreLower` sobre `("TenantId", lower("Nombre"))`, creado por la migración
`20261001194639_AddSucursalUniqueNombreIndex` con SQL crudo. `"Centro"` y `"centro"` colisionan, y
también `"Centro"` y `"  centro  "` porque `CreateAsync` aplica `Trim()` antes de insertar.

> [!NOTE]
> El índice **no** está en el modelo de EF: EF Core 8 no modela índices de expresión, y un
> `HasIndex` normal sobre `("TenantId","Nombre")` sería case-**sensitive**, más débil de lo pedido.
> Consecuencia: no aparece en el snapshot, y EF no lo eliminará en migraciones futuras.
> `TenantDbContext` lo documenta en un comentario junto a la entidad.

`SucursalRepository.CreateAsync` hace un chequeo previo para el caso común (mensaje claro sin
viaje de ida y vuelta) y además captura la violación de índice único (SQLSTATE `23505`) para
traducir la carrera de dos POST simultáneos. Verificado: 8 POST concurrentes con el mismo nombre
→ 1 `201`, 7 `409`, 1 fila en la base.

### Endpoints diseñados pero NO implementados
- `POST /auth/refresh` — renovar JWT [PLANIFICADO]
- `GET /tenants/{id}/sucursales` — ruta con `{id}`; superseded por `GET /sucursales` [PLANIFICADO]
- `GET /fx/rates` — tipos de cambio actuales [PLANIFICADO]
- `POST /fx/convert` — conversión entre monedas [PLANIFICADO]
- `PUT`/`DELETE /sucursales/{id}` — desactivar y eliminar sucursales [PLANIFICADO]

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

Sucursal
  id             GUID PK
  tenant_id      GUID FK → Tenant (discriminador multi-tenant)
  nombre         string
  direccion      string
  zona_horaria   string? (IANA; null = hereda Tenant.zona_horaria)
  activa         bool
  creada_en      DateTime
```

**Zona horaria por sucursal:** la zona efectiva es
`Sucursal.ZonaHoraria ?? Tenant.ZonaHoraria`. Si `Sucursal.ZonaHoraria` es `null` la sucursal hereda
la del tenant, lo que evita hardcodear una zona por sucursal y permite que un tenant opere sucursales
en zonas horarias distintas. La columna es nullable a propósito y no tiene valor por defecto.

**Alcance actual:** la columna nullable y la resolución de la zona horaria efectiva están
implementadas, y ambas rutas de creación están cerradas: `GET /sucursales` ejecuta el fallback en
lectura y `POST /sucursales` decide si una sucursal nueva nace con zona propia o heredada, validando
que el id sea IANA real. Lo que sigue pendiente es la parte destructiva del CRUD: no hay `PUT` ni
`DELETE /sucursales/{id}`, así que no se puede **editar ni desactivar** una sucursal creada.

Consumidores actuales de `SucursalId` (como `Guid`, sin FK por ser otro microservicio):
- MS-4 `WarehouseDbContext` lo usa como parte de la clave primaria de `Stock`
- MS-4 `SaleCompletedEvent` lo transporta en `sale.completed` para descontar stock por sucursal
- MS-5 `Turno` lo transporta al crear una venta

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
- Seed de dev: `cajero@demo.cl` (ver contraseña en [[como-ejecutar]]) / tenantId: `aaaaaaaa-0000-0000-0000-000000000001` [SOLO EN DESARROLLO]
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
| No hay `PUT`/`DELETE /sucursales/{id}` | No se puede editar ni desactivar una sucursal ya creada. `Activa` solo se puede leer |
| Sin asignación de usuarios a sucursales | No existe `POST /sucursales/{id}/usuarios`; la tabla `UsuarioSucursales` no tiene endpoint |
| `sucursal_id` en JWT es campo libre | No lo emite el token (D-04). Un cajero no puede indicar su sucursal al autenticar |
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
| TI-11 | Gestionar Sucursales | [PARCIAL — GET y POST /sucursales; sin PUT/DELETE] |
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

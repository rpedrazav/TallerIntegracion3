---
id: auth-y-roles
tipo: frontend
titulo: Autenticación y Roles en el Frontend
estado: parcial
fuentes: [globalmart-frontend/src/hooks/useAuth.ts, globalmart-frontend/src/pages/Login.tsx, globalmart-frontend/src/api/client.ts, globalmart-frontend/src/App.tsx, globalmart-frontend/src/components/AppLayout.tsx, globalmart-frontend/src/store/useTenantStore.ts, globalmart-frontend/src/pages/AdminRoles.tsx, globalmart-frontend/src/components/admin/CrearUsuarioModal.tsx, globalmart-frontend/tests/admin-access.test.cjs, globalmart-frontend/e2e/admin.spec.ts]
verificado_contra_codigo: true
ultima_revision: 2026-11-11
depende_de: [estructura, rbac-multirol, ms1-identity]
publica: []
consume: []
reglas: [RN-07, RN-08]
---
# Autenticación y Roles en el Frontend

ADMIN/SUPER_ADMIN con ese valor en `active_role` entran en `/admin` y los demás roles en `/pos`. Desde TI3-732, `useTenantStore` deriva `esAdmin` exclusivamente de `active_role`: los roles asignados en `roles` no conceden acceso administrativo alternativo. Por tanto, un usuario con `roles: ["ADMIN", "CAJERO"]` y `active_role: "CAJERO"` no ve el enlace de Administración y recibe acceso denegado al abrir `/admin/*`. Los roles propios del tenant heredan permisos de CAJERO, REPONEDOR o ADMIN; MS-1 incluye el rol base y el nombre propio en el JWT y `active_role` refleja el permiso base. La cuenta nueva muestra tenant ID, correo, contraseña inicial y rol para copiar durante la sesión de creación.

> Manejo del JWT en el frontend: login, almacenamiento con electron-store, y protección de rutas. El RoleSwitcher multi-rol NO está implementado.

## useAuth Hook [IMPLEMENTADO]

```typescript
// globalmart-frontend/src/hooks/useAuth.ts
// Proporciona:
//   login(token: string): void  — guarda el JWT en electron-store
//   logout(): void              — limpia el JWT
//   token: string | null        — token actual
//   isAuthenticated: boolean    — !!token
```

El JWT se guarda mediante `window.api.setToken` en electron-store. Login también lo copia a `localStorage`; los clientes protegidos leen el token desde `window.api.getToken` en cada petición.

## Flujo de login

```
1. Usuario ingresa email + password + tenantId en Login.tsx
2. `identityApi.login` ejecuta POST /auth/login en MS-1 con timeout de 15 s
3. Respuesta: { token: "eyJ...", expiresAt: "...", usuario: { ... } }
4. `window.api.setToken(token)` guarda el JWT en electron-store
5. navigate('/pos')
```

## Protección de rutas

`App.tsx` declara `AdminRoute` como guard común de `/admin` y todas sus rutas hijas (usuarios, roles, inventario, sucursales, productos y configuración). Sin sesión redirige a `/login`; con sesión cuyo `active_role` no sea `ADMIN` o `SUPER_ADMIN`, muestra `Acceso denegado` sin montar `AdminLayout`. `AppLayout` usa el mismo `esAdmin` para ocultar el enlace de Administración. Las pruebas de TI3-732 cubren ADMIN activo, el caso multirol con CAJERO activo, acceso directo a `/admin` y `/admin/usuarios`, y ausencia de sesión.

Este control mantiene coherente la interfaz, pero no sustituye la autorización del backend: los endpoints administrativos conservan sus atributos y políticas de autorización independientes.

## JWT decodificado (estructura real)

```json
{
  "sub": "<user_id>",
  "tenant_id": "<tenant_id>",
  "roles": ["CAJERO"],
  "active_role": "CAJERO",
  "exp": 1725820800
}
```

## Multi-rol (PLANIFICADO)

- **RoleSwitcher:** NOT FOUND en el código actual
- **TI-21 (cambiar rol activo):** sin implementar en frontend
- **TI-22 (escalar rol temporalmente):** sin implementar

## Brechas

| Brecha | Impacto |
|--------|---------|
| URL base de MS-1 | Configurada en `src/api/client.ts`; configuración por entorno [NO VERIFICADO] |
| Sin refresh token | Al expirar el JWT, hay que hacer login nuevamente |
| Sin RoleSwitcher | Usuarios multi-rol no pueden cambiar su rol activo |
| Expiración del token | Las llamadas protegidas redirigen al login si reciben 401; no hay renovación de JWT |
| Validación local del JWT | El frontend solo decodifica el payload; no valida criptográficamente el token ni su expiración antes de renderizar. El backend sigue siendo la autoridad de seguridad |

## Conexiones
- Login backend: [[ms1-identity]]
- RBAC: [[rbac-multirol]]
- Estructura: [[estructura]]

## Fuentes
- `globalmart-frontend/src/hooks/useAuth.ts`
- `globalmart-frontend/src/pages/Login.tsx`
- `globalmart-frontend/src/App.tsx`
- `globalmart-frontend/src/components/AppLayout.tsx`
- `globalmart-frontend/src/store/useTenantStore.ts`
- `globalmart-frontend/tests/admin-access.test.cjs`
- `globalmart-frontend/e2e/admin.spec.ts`

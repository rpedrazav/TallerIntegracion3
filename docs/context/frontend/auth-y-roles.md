---
id: auth-y-roles
tipo: frontend
titulo: Autenticación y Roles en el Frontend
estado: parcial
fuentes: [globalmart-frontend/src/hooks/useAuth.ts, globalmart-frontend/src/pages/Login.tsx, globalmart-frontend/src/api/client.ts, globalmart-frontend/src/pages/AdminRoles.tsx, globalmart-frontend/src/components/admin/CrearUsuarioModal.tsx]
verificado_contra_codigo: true
ultima_revision: 2026-10-27
depende_de: [estructura, rbac-multirol, ms1-identity]
publica: []
consume: []
reglas: [RN-07, RN-08]
---
# Autenticación y Roles en el Frontend

ADMIN/SUPER_ADMIN entran en `/admin` y los demás roles en `/pos`. Los roles propios del tenant heredan permisos de CAJERO, REPONEDOR o ADMIN; MS-1 incluye el rol base y el nombre propio en el JWT y `active_role` refleja el permiso base. `AdminLayout` ofrece navegación propia y `AdminRoute` protege usuarios, roles, sucursales, inventario, productos y configuración. La cuenta nueva muestra tenant ID, correo, contraseña inicial y rol para copiar durante la sesión de creación.

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

`App.tsx` declara las rutas; las llamadas protegidas de POS y turnos redirigen a `/login` tras un 401. No hay guard de rutas en `App.tsx` [VERIFICADO].

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

## Conexiones
- Login backend: [[ms1-identity]]
- RBAC: [[rbac-multirol]]
- Estructura: [[estructura]]

## Fuentes
- `globalmart-frontend/src/hooks/useAuth.ts`
- `globalmart-frontend/src/pages/Login.tsx`

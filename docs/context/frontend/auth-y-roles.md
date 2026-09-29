---
id: auth-y-roles
tipo: frontend
titulo: Autenticación y Roles en el Frontend
estado: parcial
fuentes: [globalmart-frontend/src/hooks/useAuth.ts, globalmart-frontend/src/pages/Login.tsx]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: [estructura, rbac-multirol, ms1-identity]
publica: []
consume: []
reglas: [RN-07, RN-08]
---
# Autenticación y Roles en el Frontend

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

El JWT se almacena localmente usando `electron-store` (no sessionStorage/localStorage del browser, porque Electron no tiene esos contextos confiables).

## Flujo de login

```
1. Usuario ingresa email + password + tenantId en Login.tsx
2. POST http://127.0.0.1:5124/auth/login  ← directo a MS-1 (sin Kong)
3. Respuesta: { token: "eyJ...", expiresAt: "...", usuario: { ... } }
4. login(token) → guarda en electron-store
5. navigate('/pos')
```

## Protección de rutas

Las rutas están protegidas en `App.tsx`. Sin autenticación, el usuario es redirigido a `/`. **Implementación exacta de la protección NO VERIFICADA** en detalle.

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
| URL hardcodeada a port 5124 | No pasa por Kong |
| Sin refresh token | Al expirar el JWT, hay que hacer login nuevamente |
| Sin RoleSwitcher | Usuarios multi-rol no pueden cambiar su rol activo |
| Sin manejo de expiración del token | La app puede seguir intentando llamadas con JWT vencido |

## Conexiones
- Login backend: [[ms1-identity]]
- RBAC: [[rbac-multirol]]
- Estructura: [[estructura]]

## Fuentes
- `globalmart-frontend/src/hooks/useAuth.ts`
- `globalmart-frontend/src/pages/Login.tsx`

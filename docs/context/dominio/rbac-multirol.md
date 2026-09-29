---
id: rbac-multirol
tipo: dominio
titulo: RBAC y Gestión Multi-Rol — GlobalMart OS
estado: parcial
fuentes: [src/TenantIdentityService/Controllers/UsuarioController.cs, src/TenantIdentityService/Services/JwtService.cs, GlobalMart_ContextMaster.md#sec13]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: [ms1-identity, multi-tenant]
publica: []
consume: []
reglas: [RN-07, RN-08, RN-12, RF-01, RF-13]
---
# RBAC y Gestión Multi-Rol — GlobalMart OS

> Sistema de control de acceso basado en roles. Un usuario puede tener múltiples roles; el `active_role` en el JWT determina el contexto de la sesión actual.

## Roles del sistema

| Rol | Permisos Clave | Estado |
|-----|----------------|--------|
| `CAJERO` | Abrir/cerrar turno, gestionar carrito, cobrar | [IMPLEMENTADO] |
| `REPONEDOR` | Recibir mercancía, registrar mermas, conteo físico | [PLANIFICADO — sin endpoints] |
| `ADMIN` | Todo CAJERO + REPONEDOR + config tenant, usuarios, OC, reportes | [IMPLEMENTADO] |
| `SUPER_ADMIN` | Todo ADMIN + crear tenants, revocar tokens globales | [PARCIAL — rol existe, endpoints especiales no] |
| `CLIENTE_AFILIADO` | Identificarse en POS, consultar/canjear puntos | [PLANIFICADO] |

## Permisos granulares (diseñados, NO implementados)

- `discount.apply` — descuentos manuales en carrito
- `refund.process` — devoluciones y anulaciones
- `stock.adjust` — ajuste manual de stock
- `oc.approve` — aprobar órdenes de compra

## Estructura del JWT (real)

```json
{
  "sub": "<user_id>",
  "tenant_id": "<tenant_id>",
  "roles": ["CAJERO", "REPONEDOR"],
  "active_role": "CAJERO",
  "exp": 1725820800,
  "iat": 1725791200
}
```

**Nota:** `sucursal_id` mencionado en el ContextMaster como claim obligatorio (RN-08) **NO está en el JWT real** generado por JwtService. → DISCREPANCIA.

## Prioridad de active_role (implementado en JwtService)

```csharp
string[] prioridadRoles = ["SUPER_ADMIN", "ADMIN", "CAJERO", "REPONEDOR", "CLIENTE_AFILIADO"];
var activeRole = prioridadRoles.FirstOrDefault(r => roles.Contains(r)) ?? "CAJERO";
```

Al hacer login, `active_role` se asigna automáticamente al rol de mayor jerarquía del usuario.

## Asignación de roles (implementado)

```
POST /api/v1/users/{id}/roles
Body: { "roles": ["CAJERO", "REPONEDOR"] }

Restricciones:
- Solo ADMIN puede asignar roles
- ADMIN no puede asignar SUPER_ADMIN (403 Forbidden)
- Se reemplazan todos los roles del usuario (replace, no merge)
- Si algún rol no existe en la BD → 400 Bad Request con lista de roles inválidos
```

## Multi-rol en frontend (diseñado, NO implementado)

- **RoleSwitcher:** componente para cambiar el `active_role` en sesión → NOT FOUND en código frontend
- **TI-21:** Cambiar rol activo → PLANIFICADO
- **TI-22:** Escalar rol temporalmente (una sola acción) → PLANIFICADO

## Protección en Controllers

```csharp
[Authorize(Roles = "ADMIN")]  // Requiere rol ADMIN en token
public class UsuarioController : ControllerBase { ... }

[Authorize]  // Cualquier usuario autenticado
public class TaxController : ControllerBase { ... }
```

## Brechas

| Brecha | Impacto |
|--------|---------|
| `sucursal_id` falta en JWT | RN-08 incumplido parcialmente |
| Sin RoleSwitcher en frontend | TI-21/22 no ejecutables desde UI |
| Permisos granulares no implementados | discount.apply, refund.process, etc. no verificados |
| Sin log de auditoría de roles | RNF-05 no cumplido |

## Casos de uso cubiertos

| ID | Caso de Uso | Estado |
|----|-------------|--------|
| TI-01 | Login (genera JWT) | [IMPLEMENTADO] |
| TI-06 | Asignar roles | [IMPLEMENTADO] |
| TI-20 | Asignar múltiples roles | [IMPLEMENTADO] |
| TI-21 | Cambiar rol activo | [PLANIFICADO] |
| TI-22 | Escalar rol temporalmente | [PLANIFICADO] |
| TI-23 | Separación de funciones | [PLANIFICADO] |

## Conexiones
- Implementación: [[ms1-identity]]
- Multi-tenant: [[multi-tenant]]
- Reglas: [[reglas-negocio]] (RN-07, RN-08, RN-12)

## Fuentes
- `src/TenantIdentityService/Controllers/UsuarioController.cs`
- `src/TenantIdentityService/Controllers/AuthController.cs`
- `GlobalMart_ContextMaster.md` §13

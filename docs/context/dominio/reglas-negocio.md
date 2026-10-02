---
id: reglas-negocio
tipo: dominio
titulo: Reglas de Negocio (MoSCoW) — GlobalMart OS
estado: parcial
fuentes: [GlobalMart_ContextMaster.md#sec7, src/]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: [vision]
publica: []
consume: []
reglas: []
---
# Reglas de Negocio (MoSCoW) — GlobalMart OS

> Todas las reglas de negocio del proyecto con su estado de implementación real.

## MUST — Obligatorias

| ID | Regla | Estado |
|----|-------|--------|
| RN-01 | Aislamiento completo entre tenants. Ningún query puede retornar datos de otro tenant. | [IMPLEMENTADO] — TenantMiddleware + HasQueryFilter |
| RN-02 | Todo pago con tarjeta pasa OBLIGATORIAMENTE por pasarela de pago externa. Nunca localmente. | [PLANIFICADO] — sin integración pasarela |
| RN-03 | Toda venta completada genera documento tributario válido (boleta/factura) según normativa del país. | [PLANIFICADO] — MS-2 solo calcula IVA, sin DTE |
| RN-04 | El stock se descuenta automáticamente al completar una venta exitosa. | [PARCIAL] — MS-4 consumer listo; MS-5 no produce evento |
| RN-05 | Productos perecederos gestionados bajo FEFO: lote menor fecha de vencimiento se despacha primero. | [PLANIFICADO] — modelos de lote existen, sin lógica FEFO |
| RN-06 | Solo se procesan ventas si existe turno de caja abierto por el cajero en sesión activa. | [IMPLEMENTADO] — TurnoService.Abrir verifica turno activo |
| RN-07 | Todo usuario autenticado mediante JWT válido para cualquier operación. | [IMPLEMENTADO] — [Authorize] en todos los controllers |
| RN-08 | JWT debe incluir: user_id, tenant_id, roles[], active_role, sucursal_id, exp. | [PARCIAL] — falta sucursal_id en JWT real |

## SHOULD — Importantes

| ID | Regla | Estado |
|----|-------|--------|
| RN-09 | Costo Landed = Costo Producto + Flete + Aranceles + Seguros + Gastos Aduaneros. | [PLANIFICADO] — MS-6 sin implementar |
| RN-10 | Alertas de stock mínimo antes de llegar a cero. | [PLANIFICADO] — sin alertas en MS-4 |
| RN-11 | Precios pueden variar por sucursal dentro del mismo tenant. | [PLANIFICADO] — tabla Precio existe, sin endpoints |
| RN-12 | Usuario puede tener múltiples roles activos. Sistema registra rol activo por acción. | [PARCIAL] — múltiples roles implementados; log de acciones no |
| RN-13 | Separación de funciones en OC: creador ≠ aprobador. | [PLANIFICADO] — MS-6 sin implementar |
| RN-14 | Devoluciones requieren permiso explícito del Administrador o permiso `refund.process`. | [PLANIFICADO] — anulación sin verificar permiso específico |

## COULD — Deseables

| ID | Regla | Estado |
|----|-------|--------|
| RN-15 | Puntos de lealtad expiran automáticamente si no hay actividad en 12 meses. | [PLANIFICADO] — MS-8 sin implementar |
| RN-16 | Tipo de cambio se actualiza automáticamente cuando variación supera umbral. | [PLANIFICADO] — sin FX integration |
| RN-17 | Multiplicadores de puntos configurables por categoría de producto. | [PLANIFICADO] |
| RN-18 | Descuentos manuales en carrito requieren permiso `discount.apply`. | [PLANIFICADO] — sin descuentos en MS-5 |

## WON'T — Fuera de alcance v1.0

| ID | Regla | Estado |
|----|-------|--------|
| RN-19 | No se gestiona inventario en consignación. | [OBSOLETO — fuera de alcance] |
| RN-20 | No se soporta multi-moneda dentro de una misma transacción. | [OBSOLETO — fuera de alcance] |
| RN-21 | No hay integración con marketplaces online. | [OBSOLETO — fuera de alcance] |
| RN-22 | No se incluye gestión de nómina. | [OBSOLETO — fuera de alcance] |

## Reglas críticas para IAs

Antes de modificar código, verificar:
1. **RN-01:** Todo query DEBE filtrar por `tenant_id`
2. **RN-02:** Pagos con tarjeta NUNCA se procesan localmente
3. **RN-06:** Crear venta solo si hay turno abierto
4. **RN-07:** Todo endpoint (excepto `/auth/login`) requiere JWT válido
5. **RN-08:** JWT debe tener `tenant_id`, `roles[]`, `active_role`

## Conexiones
- RBAC: [[rbac-multirol]] (RN-07, RN-08, RN-12)
- Multi-tenant: [[multi-tenant]] (RN-01)
- FEFO: [[fefo]] (RN-05)
- Costo Landed: [[costo-landed]] (RN-09)
- Estado actual: [[estado-actual]]

## Fuentes
- `GlobalMart_ContextMaster.md` §7
- `src/POSCartService/Services/VentaService.cs`
- `src/TenantIdentityService/Controllers/UsuarioController.cs`

---
id: requerimientos-funcionales
tipo: dominio
titulo: Requerimientos Funcionales (MoSCoW) — GlobalMart OS
estado: parcial
fuentes: [GlobalMart_ContextMaster.md#sec8, src/]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: [vision, reglas-negocio]
publica: []
consume: []
reglas: []
---
# Requerimientos Funcionales (MoSCoW) — GlobalMart OS

> Todos los RFs con estado real de implementación.

## MUST — Obligatorios

| ID | Requerimiento | Estado | Dónde |
|----|---------------|--------|-------|
| RF-01 | Login con JWT (tenant, roles, sucursal, exp) | [IMPLEMENTADO] | MS-1 `/auth/login` |
| RF-02 | Abrir turno de caja con fondo inicial | [IMPLEMENTADO] | MS-5 `/turnos/abrir` |
| RF-03 | Cerrar turno con declaración para cuadre | [PARCIAL] | MS-5 `/turnos/cerrar` (sin declaración billetes) |
| RF-04 | Escanear o buscar productos | [IMPLEMENTADO] | MS-3 `/products/lookup` y `/products/search` |
| RF-05 | Calcular total con impuestos | [IMPLEMENTADO] | MS-5 → MS-2 `/tax/calculate` |
| RF-06 | Cobrar en efectivo con cálculo de vuelto | [PARCIAL] | MS-5 existe, sin vuelto automático |
| RF-07 | Cobrar con tarjeta vía pasarela | [PLANIFICADO] | Sin integración pasarela |
| RF-08 | Emitir DTE al completar venta | [PLANIFICADO] | MS-2 sin DTE |
| RF-09 | Descontar stock asíncronamente vía Kafka | [PARCIAL] | MS-4 consumer listo; MS-5 no produce evento |
| RF-10 | Registrar recepción con FEFO | [PLANIFICADO] | MS-4 sin endpoint recepciones |
| RF-11 | Crear/editar/desactivar productos | [IMPLEMENTADO] | MS-3 `/products` |
| RF-12 | Configurar tenant (país, moneda, idioma, zona horaria, IVA) | [IMPLEMENTADO] | MS-1 `/tenants/{id}/config` |
| RF-13 | Gestionar usuarios (CRUD + roles RBAC) | [IMPLEMENTADO] | MS-1 `/api/v1/users` |
| RF-14 | Aislamiento multi-tenant completo | [IMPLEMENTADO] | TenantMiddleware en todos los servicios |

## SHOULD — Importantes

| ID | Requerimiento | Estado |
|----|---------------|--------|
| RF-15 | Integración con balanza física (USB/Serial) | [PLANIFICADO] |
| RF-16 | Alertas automáticas de stock mínimo y caducidad | [PLANIFICADO] |
| RF-17 | Dashboards en tiempo real (ventas, inventario, KPIs) | [PLANIFICADO] |
| RF-18 | Crear órdenes de compra con Costo Landed | [PLANIFICADO] |
| RF-19 | Gestionar múltiples sucursales con inventarios independientes | [PLANIFICADO] |
| RF-20 | Usuarios con múltiples roles y cambio de rol en sesión | [PARCIAL] |
| RF-21 | Precios por sucursal y promociones con rango de fechas | [PLANIFICADO] |
| RF-22 | Anular ventas y procesar devoluciones con reembolso | [PARCIAL] |

## COULD — Deseables

| ID | Requerimiento | Estado |
|----|---------------|--------|
| RF-23 | Programa de lealtad (puntos, tiers, cupones, canje) | [PLANIFICADO] |
| RF-24 | Integración con Fixer.io para tasas FX automáticas | [PLANIFICADO] |
| RF-25 | Notificaciones SMS/Email/Push | [PLANIFICADO] |
| RF-26 | Exportar reportes a PDF/Excel | [PLANIFICADO] |
| RF-27 | Rastrear importaciones con 3PL | [PLANIFICADO] |
| RF-28 | Emitir y validar cupones en POS | [PLANIFICADO] |

## WON'T — Fuera de alcance

| ID | Requerimiento |
|----|---------------|
| RF-29 | App móvil para clientes |
| RF-30 | Integración e-commerce/marketplaces |
| RF-31 | Gestión de nómina |
| RF-32 | Inventario en consignación |

## Conexiones
- Reglas de negocio: [[reglas-negocio]]
- Estado actual: [[estado-actual]]
- Roadmap: [[roadmap]]

## Fuentes
- `GlobalMart_ContextMaster.md` §8

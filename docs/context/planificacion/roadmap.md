---
id: roadmap
tipo: planificacion
titulo: Roadmap — Sprints 1 a 8
estado: parcial
fuentes: [GlobalMart_ContextMaster.md#sec18]
verificado_contra_codigo: false
ultima_revision: 2026-09-28
depende_de: [sprint-actual]
publica: []
consume: []
reglas: []
---
# Roadmap — Sprints 1 a 8

> Plan original de 8 sprints según el ContextMaster. Los Sprints 1-3 están completados o en curso (ver [[sprint-actual]] para el estado real).

## Sprint 1 — Fundamentos [COMPLETADO]

**Objetivo:** Infraestructura base, autenticación y catálogo mínimo.

| US/TT | Descripción | Servicio |
|-------|-------------|---------|
| US-01 | Login con JWT multi-tenant | MS-1 |
| US-02 | CRUD usuarios con RBAC | MS-1 |
| US-03 | Config de tenant (país, moneda, IVA) | MS-1 |
| US-04 | CRUD productos básico | MS-3 |
| US-05 | Búsqueda y lookup de productos | MS-3 |
| TT-01..08 | CI/CD, Docker Compose, Kong, Kafka | Infra |

## Sprint 2 — POS Core [COMPLETADO]

**Objetivo:** Flujo completo de venta en el POS.

| US/TT | Descripción | Servicio |
|-------|-------------|---------|
| US-06 | Abrir/cerrar turno de caja | MS-5 |
| US-07 | Crear venta y gestionar carrito | MS-5 |
| US-08 | Calcular IVA automáticamente | MS-2 → MS-5 |
| US-09 | Categorías jerárquicas | MS-3 |
| TT-09..14 | Tests de integración, índice trigram | MS-3, MS-5 |

## Sprint 3 — Inventario y Kafka [EN PROGRESO]

**Objetivo:** Descuento de stock asíncrono y FEFO básico.

| US/TT | Descripción | Servicio | Estado |
|-------|-------------|---------|--------|
| US-10 | Consultar stock por producto/sucursal | MS-4 | ✅ |
| US-11 | Kafka consumer sale.completed | MS-4 | ✅ |
| ??? | Kafka producer sale.completed | MS-5 | 🔴 PENDIENTE |
| ??? | Recepciones con FEFO | MS-4 | 🔴 PENDIENTE |

## Sprint 4 — Cobros y Fiscal [PRÓXIMO]

**Objetivo:** Integración pasarela de pago y DTE.

- RN-02: Cobro con tarjeta vía pasarela (Transbank/Stripe)
- RN-03: Emisión de DTE electrónico (SII Chile)
- RF-06: Cobro en efectivo con cálculo de vuelto
- RF-07: Cobro con tarjeta
- RF-08: Emitir boleta electrónica

## Sprint 5 — Analytics [PLANIFICADO]

**Objetivo:** Dashboards en tiempo real.

- MS-7 Analytics: consumers Kafka + dashboards
- KPIs de ventas en tiempo real
- Alertas de stock mínimo y caducidad
- Notificaciones SMS/Email

## Sprint 6 — Supply Chain [PLANIFICADO]

**Objetivo:** Gestión de proveedores y OC.

- MS-6 Supply Chain: controllers completos
- Órdenes de compra con aprobación (RN-13)
- Costo Landed (RN-09)
- Integración 3PL

## Sprint 7 — Loyalty [PLANIFICADO]

**Objetivo:** Programa de lealtad.

- MS-8 Loyalty: controllers + Kafka consumers
- Acumulación de puntos por venta
- Tiers y beneficios
- Canje en POS

## Sprint 8 — Multi-sucursal y Hardening [PLANIFICADO]

**Objetivo:** Escalabilidad y modo offline.

- Gestión de sucursales (TI-11)
- Precios por sucursal (RN-11)
- Modo offline para POS en efectivo (RNF-09)
- Cobertura de tests >= 80% (RNF-07)

## Conexiones
- Sprint actual: [[sprint-actual]]
- Backlog original: [[backlog]]
- Estado real: [[estado-actual]]

## Fuentes
- `GlobalMart_ContextMaster.md` §18

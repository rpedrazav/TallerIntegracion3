---
id: roadmap
tipo: planificacion
titulo: Roadmap — Sprints 1 a 8
estado: vigente
fuentes: [GlobalMart_ContextMaster.md#sec18, docs/context/planificacion/sprint-actual.md]
verificado_contra_codigo: true
ultima_revision: 2026-09-29
depende_de: [sprint-actual]
publica: []
consume: []
reglas: []
---
# Roadmap — Sprints 1 a 8

> Plan de desarrollo alineado con el calendario académico de la Universidad Católica de Temuco (UCT). Cada sprint tiene una duración de **4 semanas** (de miércoles a miércoles).

---

## Sprint 1 — Fundamentos, Backend Core y POS Base [EN CURSO]

**Duración:** 4 semanas | **Estado actual:** Semana 3 de 4 (penúltimo día al 29-09-2026)  
**Objetivo:** Infraestructura base, autenticación multi-tenant, catálogo de productos, flujo inicial de ventas en POS y primer consumidor Kafka con idempotencia.

| Entregable / Hito | Servicio | Estado |
|---|---|---|
| Infraestructura Docker completa (8 PostgreSQL, Kafka, Kong, Grafana) | Infra | ✅ Completado |
| Autenticación JWT, CRUD usuarios y configuración tenant | MS-1 | ✅ Completado |
| Cálculo de IVA integrado con configuración de tenant | MS-2 | ✅ Completado |
| Catálogo de productos, categorías jerárquicas y búsqueda trigram | MS-3 | ✅ Completado |
| Consulta de stock y consumidor Kafka `sale.completed` con idempotencia | MS-4 | ✅ Completado |
| Apertura/cierre de turnos y operaciones de carrito (crear, agregar, modificar) | MS-5 | ✅ Completado |
| Frontend Electron + React: Login funcional y UI base de POS | Frontend | 🟡 Parcial |
| Publicación de `sale.completed` a Kafka al completar venta | MS-5 | 🔴 Pendiente (Semana 4) |
| Exponer endpoints HTTP de cobro y anulación en controller | MS-5 | 🔴 Pendiente (Semana 4) |

---

## Sprint 2 — POS Avanzado, Cobro con Tarjeta y Hardware [PLANIFICADO]

**Objetivo:** Completar el ciclo de venta en mostrador con cobro de tarjetas y soporte para hardware.

- Integración con pasarela de cobro externa (simulada/sandbox tipo Mercado Pago).
- Cobro en efectivo con cálculo automático de vuelto y comprobante.
- Integración de balanza serial/USB para productos de peso variable (`EsPesoVariable`).
- Integración de cajón de dinero e impresora de recibos vía IPC en Electron.

---

## Sprint 3 — Inventario FEFO y Recepciones [PLANIFICADO]

**Objetivo:** Control estricto de mercancía y caducidades en bodega y sala.

- Endpoints de recepción de mercancía y creación de lotes con fecha de caducidad.
- Aplicación de la regla FEFO (*First-Expired, First-Out*) al descontar stock por ventas.
- Generación de alertas automáticas Kafka (`stock.alert`, `expiry.alert`).
- Registro de mermas, transferencias entre sucursales y ajustes manuales.

---

## Sprint 4 — Cumplimiento Fiscal y DTE [PLANIFICADO]

**Objetivo:** Emisión de documentos tributarios electrónicos oficiales (simulados).

- Módulo de folios electrónicos (solicitud y consumo de CAF/CAE).
- Generación y firma digital simulada de XML para boletas y facturas electrónicas (SII/AFIP).
- Reportes tributarios periódicos de ventas e IVA por período.

---

## Sprint 5 — Analytics y Dashboards en Tiempo Real [PLANIFICADO]

**Objetivo:** Visibilidad operacional para administradores y dueños de negocio.

- MS-7 Analytics: consumidores Kafka para todos los eventos del ecosistema.
- Dashboards de ventas, márgenes, productos más vendidos y proyección de demanda.
- Despacho de notificaciones multicanal (alertas de stock crítico vía SMS/Email).

---

## Sprint 6 — Supply Chain y Costo Landed [PLANIFICADO]

**Objetivo:** Gestión integral de compras, proveedores e importaciones.

- MS-6 Supply Chain: controllers y lógica de órdenes de compra con aprobación (RN-13).
- Algoritmo de cálculo de Costo Landed histórico (flete + aranceles + seguros + aduana).
- Integración con APIs de transportistas (3PL) para tracking de guías.

---

## Sprint 7 — Programa de Lealtad y Fidelización [PLANIFICADO]

**Objetivo:** Retención de clientes y promociones personalizadas.

- MS-8 Loyalty: activación de consumidores Kafka y lógica de acumulación de puntos por venta.
- Gestión de tiers de membresía (Bronce, Plata, Oro) y multiplicadores por categoría.
- Validación y canje de cupones y puntos en la interfaz del POS.

---

## Sprint 8 — Multi-Sucursal, Modo Offline y Hardening [PLANIFICADO]

**Objetivo:** Escalabilidad empresarial y resiliencia en mostrador.

- Gestión formal de múltiples sucursales con inventarios y precios independientes (RN-11).
- Modo offline parcial en POS para continuar ventas en efectivo ante caídas de red.
- Cobertura de tests automatizados unitarios y de integración >= 80% (RNF-07).

---

## Conexiones
- Sprint actual: [[sprint-actual]]
- Backlog original: [[backlog]]
- Estado real del sistema: [[estado-actual]]

## Fuentes
- `GlobalMart_ContextMaster.md` §18
- `docs/context/planificacion/sprint-actual.md`

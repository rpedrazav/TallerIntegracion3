---
id: roadmap
tipo: planificacion
titulo: Roadmap — Sprints 1 a 8
estado: vigente
fuentes: [GlobalMart_ContextMaster.md#sec18, docs/context/planificacion/sprint-actual.md, tareas/plan_sprint2_revisado.xlsx, docs/defensa-sprint2/01-resumen.md]
verificado_contra_codigo: true
ultima_revision: 2026-11-14
depende_de: [sprint-actual]
publica: []
consume: []
reglas: []
---
# Roadmap — Sprints 1 a 8

> Plan de desarrollo alineado con el calendario académico de la Universidad Católica de Temuco (UCT). Cada sprint tiene una duración de **4 semanas** (de miércoles a miércoles).

---

## Sprint 1 — Fundamentos, Backend Core y POS Base [COMPLETADO]

**Semanas:** 1 a 4. Detalle en [[sprints-anteriores]].
**Objetivo:** Infraestructura base, autenticación multi-tenant, catálogo de productos, flujo inicial de ventas en POS y primer consumidor Kafka con idempotencia.

---

## Sprint 2 — POS Avanzado, Cobro con Tarjeta y Hardware Simulado [COMPLETADO — Semana 8 trasladada]

**Semanas:** 5 a 8. **Plan:** [plan_sprint2_revisado.xlsx](../../../tareas/plan_sprint2_revisado.xlsx), 120 tareas / 120 h en metas G1–G5 y H.
**Objetivo:** Completar el ciclo de venta en mostrador con cobro de tarjeta y hardware mediante simuladores y contratos reemplazables.

- G1 Administración · G2 Carrito y venta funcional · G3 Cobro con tarjeta (pasarela simulada) · G4 Balanza (simulada) · G5 Terminal POS (simulado) · H Salud técnica.
- Las 30 tareas de la **Semana 8** (`S2-091`..`S2-120`) no se ejecutaron por exigencia del profesor y pasan a la Semana 9 del Sprint 3. Ver [[sprint-actual]].
- Fuera de alcance por decisión del plan: DTE, FEFO, compras, dashboards, devoluciones parciales, pagos mixtos. Cajón de dinero e impresora de recibos no se incluyeron en el plan revisado.

---

## Sprint 3 — Arrastre del Sprint 2, Inventario FEFO y Recepciones [EN CURSO]

**Semanas:** 9 a 12. **Capacidad:** 120 h.
**Objetivo:** dejar todo pulido y funcionando. Prioridad: microservicios → funcionalidad → frontend.

- **Semana 9:** arrastre íntegro de la Semana 8 del Sprint 2 (anulación, consumidor Kafka de MS-4, Administración, terminal POS, salud técnica y release).
- **Semanas 10–12 (por planificar):** alcance del roadmap y pendientes restantes:
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

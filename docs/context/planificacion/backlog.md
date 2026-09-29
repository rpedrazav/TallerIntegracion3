---
id: backlog
tipo: planificacion
titulo: Backlog Original — User Stories y Tareas Técnicas
estado: parcial
fuentes: [GlobalMart_ContextMaster.md#sec16-17]
verificado_contra_codigo: false
ultima_revision: 2026-09-28
depende_de: [roadmap]
publica: []
consume: []
reglas: []
---
# Backlog Original — User Stories y Tareas Técnicas

> Backlog del Sprint 1 según el ContextMaster (escrito en septiembre 2026). Estado actualizado según código real al 28-09-2026.

## User Stories (US-01..11)

| ID | Historia | Criterios de Aceptación | Estado Real |
|----|---------|------------------------|-------------|
| US-01 | Como cajero, quiero iniciar sesión con mi email y contraseña para acceder al POS según mi rol | JWT válido con tenant_id, roles[], active_role | [IMPLEMENTADO] |
| US-02 | Como admin, quiero gestionar usuarios de mi tenant para controlar el acceso | CRUD + asignación de roles | [IMPLEMENTADO] |
| US-03 | Como admin, quiero configurar país, moneda e IVA de mi tenant | PUT /tenants/{id}/config funcional | [IMPLEMENTADO] |
| US-04 | Como admin, quiero crear y editar productos del catálogo | POST/PUT /products | [IMPLEMENTADO] |
| US-05 | Como cajero, quiero buscar productos por nombre o escanear código de barras | /products/search y /products/lookup | [IMPLEMENTADO] |
| US-06 | Como cajero, quiero abrir un turno con fondo inicial | POST /turnos/abrir con monto | [IMPLEMENTADO] |
| US-07 | Como cajero, quiero agregar productos al carrito y ver el total con IVA | Carrito con recálculo de IVA | [IMPLEMENTADO] |
| US-08 | Como cajero, quiero cobrar una venta en efectivo | POST /ventas/{id}/cobrar | [PARCIAL — sin pasarela] |
| US-09 | Como cajero, quiero anular una venta con motivo | POST /ventas/{id}/anular | [IMPLEMENTADO — sin reembolso] |
| US-10 | Como reponedor, quiero consultar el stock de un producto | GET /stock/{productId} | [IMPLEMENTADO] |
| US-11 | Como sistema, quiero descontar stock al completar una venta | Kafka sale.completed → Warehouse | [PARCIAL — sin producer] |

## Tareas Técnicas (TT-01..19)

| ID | Tarea | Estado Real |
|----|-------|-------------|
| TT-01 | Setup Docker Compose (8 PG, Kafka, Kong, Prometheus, Grafana) | [IMPLEMENTADO] |
| TT-02 | Configurar Kong (DB-less, JWT, CORS, rate-limiting) | [IMPLEMENTADO] |
| TT-03 | GitHub Actions CI (build + docker up + test + down) | [IMPLEMENTADO] |
| TT-04 | MS-1: Setup proyecto, auth, JWT | [IMPLEMENTADO] |
| TT-05 | MS-2: Setup proyecto, cálculo IVA | [IMPLEMENTADO] |
| TT-06 | MS-3: Setup proyecto, CRUD, búsqueda | [IMPLEMENTADO] |
| TT-07 | MS-4: Setup proyecto, stock, Kafka consumer | [IMPLEMENTADO] |
| TT-08 | MS-5: Setup proyecto, turnos, ventas, IPC | [IMPLEMENTADO] |
| TT-09 | MS-6: Setup proyecto, modelos | [PARCIAL — sin controllers] |
| TT-10 | MS-7: Setup proyecto, modelos | [PARCIAL — sin controllers] |
| TT-11 | MS-8: Setup proyecto, modelos | [PARCIAL — sin controllers] |
| TT-12 | Frontend: Electron + React + Login | [IMPLEMENTADO] |
| TT-13 | Frontend: POS carrito básico | [PARCIAL — sin API real] |
| TT-14 | Tests unitarios MS-1 | [PLANIFICADO] |
| TT-15 | Tests unitarios MS-2 | [PLANIFICADO] |
| TT-16 | Tests unitarios MS-5 | [PARCIAL — solo smoke tests] |
| TT-17 | Tests integración MS-5 ↔ MS-2 | [PLANIFICADO] |
| TT-18 | Tests integración Kafka | [PLANIFICADO] |
| TT-19 | Kafdrop para monitoreo de topics | [IMPLEMENTADO] |

## Conexiones
- Sprint actual: [[sprint-actual]]
- Roadmap: [[roadmap]]

## Fuentes
- `GlobalMart_ContextMaster.md` §16, §17

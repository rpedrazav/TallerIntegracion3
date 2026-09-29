---
id: sprint-actual
tipo: planificacion
titulo: Sprint Actual — Estado Real del Desarrollo
estado: implementado
fuentes: [git log, GlobalMart_ContextMaster.md#sec18, src/]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: [sprints-anteriores]
publica: []
consume: []
reglas: []
---
# Sprint Actual — Estado Real del Desarrollo

> Basado en análisis de `git log` (commits recientes) y estado del código. El ContextMaster indica "Sprint 1", pero el código real muestra trabajo de Sprint 2-3.

## Sprint en curso (estimado): Sprint 3

**Período:** septiembre 2026 (en curso)

## Tareas completadas (commits recientes — verificadas)

| ID Tarea | Descripción | Servicio | Estado |
|---------|-------------|---------|--------|
| TI3-195 | POSCartService — AgregarItemTest | MS-5 tests | ✅ Completada |
| TI3-194 | VentasController — endpoint completo | MS-5 | ✅ Completada |
| TI3-193 | VentasController — refactors | MS-5 | ✅ Completada |
| TI3-192 | VentasController — más endpoints | MS-5 | ✅ Completada |
| TI3-191 | VentasController — inicio | MS-5 | ✅ Completada |
| TI3-188 | KafkaConsumerService — mejoras | MS-4 | ✅ Completada |
| TI3-187 | KafkaConsumerService — inicio | MS-4 | ✅ Completada |
| TI3-186 | Migración AddEventosKafkaProcesados | MS-4 | ✅ Completada |
| TI3-185 | KafkaConsumerService + SaleCompletedEvent | MS-4 | ✅ Completada |
| TI3-184 | TenantMiddleware en Warehouse | MS-4 | ✅ Completada |
| TI3-183 | StockController | MS-4 | ✅ Completada |
| TI3-181 | TenantConfigController completado | MS-1 | ✅ Completada |
| TI3-180 | TenantConfigController iniciado | MS-1 | ✅ Completada |
| TI3-179 | UsuarioController — AssignRoles | MS-1 | ✅ Completada |

## Tareas en progreso / pendientes

Basado en estado del código (brechas detectadas):

| Tarea Pendiente | Descripción | Prioridad |
|----------------|-------------|-----------|
| TI3-??? | MS-5: Implementar Kafka producer de sale.completed | 🔴 Crítica |
| TI3-??? | MS-2: Implementar DTE (boletas electrónicas) | 🔴 Crítica |
| TI3-??? | MS-5: Integrar pasarela de pago | 🔴 Crítica |
| TI3-??? | Frontend: Integrar POS con API MS-5 | 🟠 Alta |
| TI3-??? | Frontend: Gestión de turnos en UI | 🟠 Alta |
| TI3-??? | MS-1: Agregar sucursal_id al JWT | 🟠 Alta |
| TI3-??? | MS-4: Implementar endpoints recepción FEFO | 🟠 Alta |

## Discrepancia con ContextMaster

El ContextMaster afirma que se está en "Sprint 1" con la primera iteración del backlog. El código muestra:
- MS-1 con CRUD completo de usuarios + config de tenant (Sprint 2 completado)
- MS-4 con Kafka consumer funcional (Sprint 3 en progreso)
- MS-5 con flujo completo de ventas e ítems (Sprint 2-3)

**El ContextMaster tiene ~3 semanas de desactualización** (escrito 8 septiembre 2026, revisado 28 septiembre 2026).

## Conexiones
- Historial: [[sprints-anteriores]]
- Backlog: [[backlog]]
- Roadmap: [[roadmap]]
- Estado actual: [[estado-actual]]

## Fuentes
- `git log --oneline -20` (ejecutado 2026-09-28)
- Análisis de código fuente

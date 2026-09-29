---
id: sprint-actual
tipo: planificacion
titulo: Sprint Actual — Sprint 1 (En Curso)
estado: vigente
fuentes: [git log, GlobalMart_ContextMaster.md#sec17-18, src/, docs/context/_reports/preguntas-abiertas.md]
verificado_contra_codigo: true
ultima_revision: 2026-09-29
depende_de: [sprints-anteriores]
publica: []
consume: []
reglas: []
---
# Sprint Actual — Sprint 1 (En Curso)

> Estado del desarrollo activo del proyecto según el equipo de trabajo y el repositorio al 29-09-2026.

## Contexto Académico y Reglas del Proyecto

- **Institución:** Universidad Católica de Temuco (UCT).
- **Carrera:** Ingeniería Civil Informática — Asignatura: Taller de Integración 3 (INTEGRA3).
- **Simulación de integraciones:** Todos los componentes, hardware y servicios externos que no puedan obtenerse legalmente o requieran trámites fiscales oficiales (como certificados digitales reales del SII o terminales de pago bancario físicos) **serán simulados/mockeados** en software.
- **Cadencia de Sprints:** Cada sprint tiene una duración de **4 semanas**, organizadas de **miércoles a miércoles**.
- **Punto temporal actual:** Penúltimo día de la **Semana 3 del Sprint 1**.

---

## Tareas Completadas en Sprint 1 (Semanas 1 a 3)

Durante las primeras tres semanas de este Sprint 1 se avanzó intensamente en el backend, la infraestructura y los primeros componentes del frontend:

| ID Tarea | Descripción | Componente | Estado |
|---|---|---|---|
| TI3-195 | Tests de integración unitarios/smoke AgregarItem | MS-5 | ✅ Completada |
| TI3-191..194 | VentasController (crear venta, agregar, modificar y eliminar items) | MS-5 | ✅ Completada |
| TI3-185..188 | KafkaConsumerService para `sale.completed` e idempotencia con `EventosKafkaProcesados` | MS-4 | ✅ Completada |
| TI3-183..184 | StockController y TenantMiddleware | MS-4 | ✅ Completada |
| TI3-180..181 | TenantConfigController (configuración fiscal y de localización) | MS-1 | ✅ Completada |
| TI3-179 | Asignación y gestión de roles en UsuarioController | MS-1 | ✅ Completada |
| TI3-143 | Migración de índice trigram `pg_trgm` para búsqueda fuzzy de productos | MS-3 | ✅ Completada |
| TI3-113..114 | Infraestructura Docker: 8 PostgreSQL, Zookeeper, Kafka, Kong DB-less, Prometheus, Grafana | Infra | ✅ Completada |
| Frontend | Setup Electron + React + TypeScript, vista Login funcional y POS base | Frontend | ✅ Parcial |

---

## Tareas Pendientes para Cierre de Sprint 1 (Semana 4)

1. **MS-5 → Kafka:** Implementar la publicación real del evento `sale.completed` en `VentaService.CompletarAsync` para conectar con MS-4.
2. **MS-5 Controllers:** Exponer endpoints HTTP para `POST /ventas/{id}/cobrar` y `POST /ventas/{id}/anular` en `VentasController.cs`.
3. **Frontend POS:** Conectar `Pos.tsx` a las APIs reales de MS-3 (lookup/búsqueda) y MS-5 (turnos y ventas), reemplazando los datos mockeados.
4. **Validación de sucursales:** Diseñar el modelo de validación de `sucursal_id` previsto para las siguientes semanas.

---

## Conexiones
- Roadmap: [[roadmap]]
- Estado real: [[estado-actual]]
- Preguntas abiertas y aclaraciones del equipo: `docs/context/_reports/preguntas-abiertas.md`
- Inventario: `docs/context/_reports/inventario.md`

## Fuentes
- `git log --oneline -30`
- `docs/context/_reports/preguntas-abiertas.md` (Respuestas del equipo UCT)
- `src/`

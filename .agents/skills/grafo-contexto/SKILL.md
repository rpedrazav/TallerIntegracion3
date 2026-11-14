---
name: grafo-contexto
description: Actualiza el grafo de contexto de GlobalMart OS (docs/context) después de un cambio de código. Úsala cuando cambien endpoints, eventos Kafka, reglas, arquitectura o el estado de una funcionalidad.
---

# Actualizar el grafo de contexto

El grafo (`docs/context/`) es la fuente que leen los agentes antes de trabajar. Si queda desactualizado, el siguiente agente parte con información falsa. Se actualiza en la **misma tarea o commit** que el cambio de código.

## Pasos

1. **¿Hace falta?** Solo si cambió algo observable: endpoint, contrato, evento, regla de negocio, arquitectura, pantalla o estado de una funcionalidad. Un refactor interno sin cambio de comportamiento no toca el grafo.
2. **Identifica los nodos afectados** con `docs/context/index.md`:
   - Servicio: `servicios/ms1-identity.md` … `ms5-pos.md`.
   - Dominio: `dominio/*.md` (por ejemplo `fefo.md`, `anulacion-venta.md`, `multi-tenant.md`).
   - Eventos: `eventos/kafka-topics.md`, `eventos/sale-completed-outbox.md`.
   - Frontend: `frontend/pantallas.md`, `frontend/estructura.md`, `frontend/electron-ipc.md`.
   - Infraestructura: `infraestructura/*.md`.
3. **Edita solo esos nodos.** No reescribas secciones ajenas ni actualices todo por rutina.
4. **`estado-actual.md`:** actualízalo si cambian endpoints, eventos, reglas o el estado de una funcionalidad. Agrega una sección breve con el ID de la tarea, la fecha y qué quedó probado.
5. **`ultima_revision`:** pon la fecha de hoy en el frontmatter de cada nodo que modificaste.
6. **Etiquetas de estado:**
   - `IMPLEMENTADO` solo con evidencia en código, pruebas o configuración.
   - `PARCIAL` si falta parte del criterio de aceptación.
   - `PLANIFICADO` si no hay código.
   - `[NO VERIFICADO]` para todo lo que no comprobaste (despliegue remoto, hardware físico, Kafka en el clúster, etc.).
7. **Enlaces:** verifica que los enlaces nuevos apunten a archivos existentes y que endpoints, eventos y nombres coincidan con el código.

## Ejemplos

- Cambio en `VentasController.cs` → `servicios/ms5-pos.md` y `estado-actual.md`.
- Cambio en el consumidor de MS-4 → `servicios/ms4-inventory.md`, `eventos/kafka-topics.md` y `estado-actual.md`.
- Pantalla nueva en Admin → `frontend/pantallas.md`.
- Refactor interno de `VentaService` sin cambio de comportamiento → ningún nodo.

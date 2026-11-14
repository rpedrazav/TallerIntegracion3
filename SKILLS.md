# Skills del proyecto — GlobalMart OS

Las skills son instrucciones que el agente carga cuando la tarea lo requiere. Están en dos carpetas con **el mismo contenido**:

| Herramienta | Carpeta | Cómo invocarla |
|---|---|---|
| Codex | `.agents/skills/<nombre>/SKILL.md` | `$ponytail` o `/skills` |
| Claude Code | `.claude/skills/<nombre>/SKILL.md` | `/ponytail` |

`.agents/skills/` es la fuente. Si cambias una skill, copia el mismo `SKILL.md` a `.claude/skills/`.

## Skills disponibles

| Skill | Cuándo usarla | Obligatoria |
|---|---|---|
| **ponytail** | En **toda** tarea de código: escribir el cambio completo más pequeño posible, sin abstracciones ni código «para después». | **Sí** |
| **simplificar-flujo** | Al tocar o revisar código existente de un flujo (login, caja, tarjeta, anulación, Admin) para quitar vueltas, código muerto y duplicados sin cambiar comportamiento. | En tareas que tocan un flujo existente |
| **grafo-contexto** | Después de cambiar endpoints, eventos, reglas o el estado de una funcionalidad: actualizar los nodos de `docs/context/`. | Si cambia comportamiento |
| **reporte-qa** | Al registrar evidencia de pruebas de una tarea en `docs/qa/<ID>/reporte.md`. | En tareas de testing |

## Cómo pedírselo al agente

- «Usa ponytail e implementa S3-012 según `tareas/plan_sprint3.xlsx`.»
- «Con simplificar-flujo, revisa el flujo de cierre de turno desde el botón hasta la BD.»
- «Actualiza el grafo con grafo-contexto por el cambio en `VentasController.cs`.»
- «Escribe el reporte de S3-030 con reporte-qa.»

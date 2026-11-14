---
titulo: Sprint 3 — Actualización de YouTrack
sprint: 3
responsable: Rodrigo (scrum)
fuente: tareas/plan_sprint3.xlsx
creado: 2026-11-14
---

# Sprint 3 — Qué actualizar en YouTrack

> [!info] Resumen
> - **Paso 1:** mover las 30 tareas arrastradas (`TI3-722`..`TI3-751`) a **Semana 9** y corregir 12 estimaciones.
> - **Paso 2:** importar las **93 tareas nuevas** (`S3-001`..`S3-093`, 95 h) desde `tareas_sprint3_youtrack.csv`.
> - Total del sprint: 25 h de arrastre + 95 h nuevas = **120 h**, 300 min por persona y semana.
> - Detalle completo y criterios de aceptación: [[plan_sprint3.xlsx]].

## Paso 1 — Tareas arrastradas de la Semana 8

No se vuelven a crear: ya existen en YouTrack.

1. En YouTrack, buscar las 30 tareas:

```
issue id: TI3-722, TI3-723, TI3-724, TI3-725, TI3-726, TI3-727, TI3-728, TI3-729, TI3-730, TI3-731, TI3-732, TI3-733, TI3-734, TI3-735, TI3-736, TI3-737, TI3-738, TI3-739, TI3-740, TI3-741, TI3-742, TI3-743, TI3-744, TI3-745, TI3-746, TI3-747, TI3-748, TI3-749, TI3-750, TI3-751
```

2. Seleccionar todas y aplicar el comando masivo (Ctrl+Alt+J o *Apply command*):

```
Semana Semana 9
```

> [!warning] Verificar nombres
> El comando asume que el campo se llama `Semana` y el valor `Semana 9`, igual que en la importación del Sprint 2. Si el valor `Semana 9` no existe todavía, crearlo primero en la configuración del campo.

3. Editar a mano estas **12 estimaciones** (el contenido de la tarea no cambia, solo el tiempo):

| ✔ | YouTrack | ID plan | Tarea | Persona | Antes | Ahora |
|---|---|---|---|---|---:|---:|
| [ ] | TI3-726 | S2-095 | Probar anulación y permisos | Sebastián | 1h30m | 1h |
| [ ] | TI3-731 | S2-100 | Probar duplicados y fallo intermedio | Nicolás | 1h30m | 1h |
| [ ] | TI3-736 | S2-105 | Cerrar regresión de Administración | Martín | 1h30m | 1h |
| [ ] | TI3-741 | S2-110 | Ejecutar demo completa de mostrador | Rodrigo | 1h30m | 1h |
| [ ] | TI3-743 | S2-112 | Ejecutar E2E de Admin y venta | Daniel | 1h | 45m |
| [ ] | TI3-744 | S2-113 | Ejecutar E2E de tarjeta y pesaje | Daniel | 1h | 45m |
| [ ] | TI3-745 | S2-114 | Medir cobertura y compilar todo | Daniel | 1h | 45m |
| [ ] | TI3-746 | S2-115 | Auditar aislamiento y recuperación final | Daniel | 1h30m | 1h |
| [ ] | TI3-748 | S2-117 | Integrar pruebas críticas en CI | Diego | 1h | 45m |
| [ ] | TI3-749 | S2-118 | Actualizar nodos de contexto afectados | Diego | 1h | 30m |
| [ ] | TI3-750 | S2-119 | Publicar guía de demo y límites físicos | Diego | 1h | 30m |
| [ ] | TI3-751 | S2-120 | Ejecutar revisión de release | Diego | 1h30m | 1h |

4. (Opcional) Estas 8 ya tienen evidencia en el repositorio; su responsable debería confirmar si las cierra:

| YouTrack | ID plan | Tarea | Persona | Evidencia |
|---|---|---|---|---|
| TI3-722 | S2-091 | Definir alcance de anulación Sprint 2 | Sebastián | Contrato entregado (2026-11-09) |
| TI3-723 | S2-092 | Exponer anulación de venta | Sebastián | Backend implementado (2026-11-09) |
| TI3-732 | S2-101 | Revisar rutas Admin y rol activo | Martín | Implementada y probada (2026-11-11) |
| TI3-733 | S2-102 | Completar estados de usuarios inactivos | Martín | Implementada; E2E PASS REPORTADO |
| TI3-734 | S2-103 | Validar productos desde Admin | Martín | Implementada; E2E PASS REPORTADO |
| TI3-735 | S2-104 | Verificar accesibilidad y guía visual | Martín | Implementada; E2E PASS REPORTADO |
| TI3-737 | S2-106 | Diseñar pantalla de terminal POS | Rodrigo | Implementada según reporte QA |
| TI3-738 | S2-107 | Conectar terminal simulado al cobro | Rodrigo | Implementada según reporte QA |

## Paso 2 — Importar las tareas nuevas

1. Importar `tareas/tareas_sprint3_youtrack.csv` en YouTrack, igual que en el Sprint 2 (9 columnas: Summary, Description, Assignee, Priority, Sprints, Semana, AreaTrabajo, Tags, Estimation).
2. Verificar que el valor `Sprint 3` exista en el campo Sprints antes de importar.
3. Guardar la correspondencia `S3-xxx` → `TI3-xxx` en `tareas/sprint3_youtrack_resultado.json` si el importador la entrega.

## Tareas nuevas por semana

### Semana 9 — 5 h nuevas

| ID | Tarea | Persona | Área | Min | MoSCoW |
|---|---|---|---|---:|---|
| S3-001 | Inventariar código muerto en el flujo de caja de MS-5 | Sebastián | Backend | 30 | Must |
| S3-002 | Definir contrato mínimo de recepciones y lotes FEFO | Nicolás | Backend | 30 | Must |
| S3-003 | Inventariar repositorios duplicados de usuarios en MS-1 | Martín | Backend | 30 | Must |
| S3-004 | Inventariar archivos basura del repositorio | Rodrigo | DevOps | 30 | Must |
| S3-005 | Tomar línea base de pruebas antes de simplificar | Daniel | Testing | 45 | Must |
| S3-006 | Definir caso E2E de cierre de turno | Daniel | Testing | 30 | Must |
| S3-007 | Revisar manifiestos e imágenes del clúster | Diego | DevOps | 60 | Must |
| S3-008 | Mapear responsabilidades de Pos.tsx y CerrarTurnoPage.tsx | Diego | Frontend | 45 | Must |

### Semana 10 — 30 h nuevas

| ID | Tarea | Persona | Área | Min | MoSCoW |
|---|---|---|---|---:|---|
| S3-009 | Eliminar código muerto del flujo de caja en MS-5 | Sebastián | Backend | 60 | Must |
| S3-010 | Retirar alias de rutas no usados en MS-5 | Sebastián | Backend | 60 | Must |
| S3-011 | Agregar anularVenta al cliente de API | Sebastián | Frontend | 30 | Must |
| S3-012 | Agregar acción Anular venta para ADMIN | Sebastián | Frontend | 90 | Must |
| S3-013 | Mostrar errores de anulación | Sebastián | Frontend | 60 | Must |
| S3-014 | Revisar modelo Lote y migración de MS-4 | Nicolás | Backend | 60 | Must |
| S3-015 | Implementar POST /recepciones | Nicolás | Backend | 90 | Must |
| S3-016 | Validar recepciones | Nicolás | Backend | 60 | Must |
| S3-017 | Probar recepciones con PostgreSQL | Nicolás | Testing | 60 | Must |
| S3-018 | Documentar contrato de recepciones en el grafo | Nicolás | Backend | 30 | Should |
| S3-019 | Unificar repositorios de usuarios en MS-1 | Martín | Backend | 90 | Must |
| S3-020 | Ajustar AuthService al repositorio unificado | Martín | Backend | 60 | Must |
| S3-021 | Implementar PUT /sucursales/{id} | Martín | Backend | 60 | Must |
| S3-022 | Implementar DELETE /sucursales/{id} como baja lógica | Martín | Backend | 60 | Must |
| S3-023 | Probar edición y baja de sucursales | Martín | Testing | 30 | Must |
| S3-024 | Eliminar archivos basura de la raíz | Rodrigo | DevOps | 60 | Must |
| S3-025 | Eliminar scripts temporales del frontend | Rodrigo | Frontend | 30 | Must |
| S3-026 | Dejar solo modos remote y local en config/api.ts | Rodrigo | Frontend | 90 | Must |
| S3-027 | Eliminar config.ts y el interceptor de /api duplicado | Rodrigo | Frontend | 60 | Must |
| S3-028 | Verificar login y POS en modos remote y local | Rodrigo | Testing | 60 | Must |
| S3-029 | Automatizar E2E de cierre de turno | Daniel | Testing | 60 | Must |
| S3-030 | Regresión tras limpieza de archivos y configuración | Daniel | Testing | 60 | Must |
| S3-031 | Regresión de MS-1 tras unificar repositorios | Daniel | Testing | 60 | Must |
| S3-032 | Regresión del POS tras partir Pos.tsx | Daniel | Testing | 60 | Must |
| S3-033 | Registrar defectos de la semana en YouTrack | Daniel | Testing | 60 | Should |
| S3-034 | Extraer el carrito de Pos.tsx | Diego | Frontend | 90 | Must |
| S3-035 | Extraer cobro y recuperación de venta de Pos.tsx | Diego | Frontend | 90 | Must |
| S3-036 | Reemplazar window.confirm por modal en cierre de turno | Diego | Frontend | 60 | Must |
| S3-037 | Partir CerrarTurnoPage en tabla de denominaciones y resultado | Diego | Frontend | 60 | Must |

### Semana 11 — 30 h nuevas

| ID | Tarea | Persona | Área | Min | MoSCoW |
|---|---|---|---|---:|---|
| S3-038 | Probar anulación desde la UI | Sebastián | Testing | 60 | Must |
| S3-039 | Simplificar el cálculo de totales en VentaService | Sebastián | Backend | 90 | Must |
| S3-040 | Revisar ramas muertas del outbox de MS-5 | Sebastián | Backend | 60 | Should |
| S3-041 | Mostrar ventas anuladas en historial y cuadre | Sebastián | Frontend | 60 | Must |
| S3-042 | Verificar cuadre con ventas anuladas | Sebastián | Testing | 30 | Must |
| S3-043 | Descontar stock por FEFO en el consumidor de ventas | Nicolás | Backend | 90 | Must |
| S3-044 | Definir comportamiento con lotes insuficientes | Nicolás | Backend | 60 | Must |
| S3-045 | Implementar GET /lotes?porVencer= | Nicolás | Backend | 60 | Must |
| S3-046 | Probar descuento FEFO | Nicolás | Testing | 60 | Must |
| S3-047 | Simplificar KafkaConsumerService | Nicolás | Backend | 30 | Should |
| S3-048 | Implementar reactivación de usuarios | Martín | Backend | 60 | Must |
| S3-049 | Agregar botón Reactivar en la lista de usuarios | Martín | Frontend | 60 | Must |
| S3-050 | Partir ProductosPage en tabla, filtros y modal | Martín | Frontend | 90 | Must |
| S3-051 | Crear pantalla de recepción de mercadería en Admin | Martín | Frontend | 90 | Must |
| S3-052 | Actualizar imágenes de MS-1 a MS-5 en el clúster | Rodrigo | DevOps | 90 | Must |
| S3-053 | Desplegar MS-4 con recepciones y lotes | Rodrigo | DevOps | 60 | Must |
| S3-054 | Configurar build Setup.exe con Electron Forge | Rodrigo | DevOps | 90 | Must |
| S3-055 | Probar el primer .exe contra el clúster | Rodrigo | Testing | 60 | Must |
| S3-056 | E2E de recepción y FEFO en docker compose | Daniel | Testing | 90 | Must |
| S3-057 | Regresión tras simplificar VentaService | Daniel | Testing | 60 | Must |
| S3-058 | Regresión de Admin tras partir ProductosPage | Daniel | Testing | 60 | Must |
| S3-059 | Probar el primer .exe en un segundo equipo | Daniel | Testing | 60 | Must |
| S3-060 | Auditar aislamiento por tenant en endpoints nuevos | Daniel | Testing | 30 | Must |
| S3-061 | Preparar Dockerfiles y manifiestos para el despliegue | Diego | DevOps | 90 | Must |
| S3-062 | Documentar la limitación de Kafka en el clúster | Diego | DevOps | 30 | Must |
| S3-063 | Aplicar la guía de estilo en POS, apertura y cierre de turno | Diego | Frontend | 90 | Must |
| S3-064 | Quitar estilos inline de apertura y cierre de turno | Diego | Frontend | 90 | Must |

### Semana 12 — 30 h nuevas

| ID | Tarea | Persona | Área | Min | MoSCoW |
|---|---|---|---|---:|---|
| S3-065 | Corregir defectos de regresión en MS-5 | Sebastián | Backend | 90 | Must |
| S3-066 | Validar el flujo de caja contra el clúster | Sebastián | Testing | 60 | Must |
| S3-067 | Revisar mensajes de error y logs de MS-5 | Sebastián | Backend | 60 | Should |
| S3-068 | Ejecutar pruebas de integración de MS-5 tras la simplificación | Sebastián | Testing | 60 | Must |
| S3-069 | Actualizar nodo ms5-pos | Sebastián | Backend | 30 | Should |
| S3-070 | Mantener coherente el stock agregado con los lotes | Nicolás | Backend | 60 | Must |
| S3-071 | Corregir defectos de regresión en MS-4 | Nicolás | Backend | 90 | Must |
| S3-072 | Probar venta a FEFO en docker compose con Kafka | Nicolás | Testing | 60 | Must |
| S3-073 | Agregar lotes de demostración al seed | Nicolás | Backend | 60 | Should |
| S3-074 | Actualizar nodos ms4-inventory y fefo | Nicolás | Backend | 30 | Should |
| S3-075 | Mostrar lotes por vencer en Admin Inventario | Martín | Frontend | 60 | Must |
| S3-076 | Editar y desactivar sucursales en Admin | Martín | Frontend | 90 | Must |
| S3-077 | Aplicar la guía de estilo en pantallas de Admin | Martín | Frontend | 60 | Must |
| S3-078 | Reducir Program.cs de MS-1 | Martín | Backend | 60 | Should |
| S3-079 | Regresión rápida de Admin | Martín | Testing | 30 | Must |
| S3-080 | Desplegar la versión congelada en el clúster | Rodrigo | DevOps | 90 | Must |
| S3-081 | Generar el .exe final | Rodrigo | DevOps | 60 | Must |
| S3-082 | Smoke del .exe final en equipo limpio | Rodrigo | Testing | 60 | Must |
| S3-083 | Cerrar el sprint en YouTrack | Rodrigo | DevOps | 30 | Must |
| S3-084 | Escribir notas de versión del Sprint 3 | Rodrigo | DevOps | 60 | Should |
| S3-085 | Regresión completa antes del congelamiento | Daniel | Testing | 90 | Must |
| S3-086 | Regresión posterior al despliegue final | Daniel | Testing | 60 | Must |
| S3-087 | Verificar el checklist de salida del sprint | Daniel | Testing | 60 | Must |
| S3-088 | Reporte de pruebas del Sprint 3 | Daniel | Testing | 60 | Must |
| S3-089 | Verificar el criterio de simplificación en archivos tocados | Daniel | Testing | 30 | Should |
| S3-090 | Pulir mensajes vacíos y de error en el POS | Diego | Frontend | 90 | Must |
| S3-091 | Apoyar el despliegue final y verificar salud | Diego | DevOps | 60 | Must |
| S3-092 | Corregir defectos visuales de regresión | Diego | Frontend | 90 | Must |
| S3-093 | Revisar navegación por teclado en el POS | Diego | Frontend | 60 | Should |

## Si hay que agregar tareas

- Cada persona tiene exactamente **300 min por semana**. Una tarea nueva debe reemplazar otra de la misma persona y semana; reemplazar primero las **Should**.
- Los defectos de regresión van como tareas nuevas con responsable; se absorben en las tareas *Corregir defectos de regresión* de la Semana 12.
- Tras el **domingo 29-nov** (congelamiento) solo se agregan correcciones de bugs.
- Agregar la tarea también en `plan_sprint3.xlsx` (hoja *Sprint 3*) para que el control de horas del Resumen siga cuadrando.

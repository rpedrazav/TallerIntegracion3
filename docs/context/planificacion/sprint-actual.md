---
id: sprint-actual
tipo: planificacion
titulo: Sprint Actual — Sprint 3 (En Curso)
estado: vigente
fuentes: [tareas/plan_sprint3.xlsx, tareas/plan_sprint2_revisado.xlsx, docs/qa/S2-092/reporte.md, docs/qa/S2-106/reporte.md, docs/qa/S2-107/reporte.md, tareas/sprint2_youtrack_resultado.json, docs/defensa-sprint2/01-resumen.md, docs/context/estado-actual.md, aclaración del equipo (Daniel Prado, 2026-11-14)]
verificado_contra_codigo: false
ultima_revision: 2026-11-14
depende_de: [sprints-anteriores, roadmap]
publica: []
consume: []
reglas: []
---
# Sprint Actual — Sprint 3 (En Curso)

> Estado de planificación al 2026-11-14. El Sprint 3 abarca las **Semanas 9 a 12** del calendario del proyecto.

## Contexto académico y reglas del proyecto

- **Institución:** Universidad Católica de Temuco (UCT), Ingeniería Civil Informática, Taller de Integración 3 (INTEGRA3).
- **Simulación de integraciones:** el hardware y los servicios externos que no pueden obtenerse legalmente (certificados SII, terminales bancarios, balanzas certificadas) se simulan.
- **Cadencia:** sprints de 4 semanas, de miércoles a miércoles.
- **Capacidad:** 6 integrantes × 5 h/semana × 4 semanas = **120 h**. Mismo equipo y usuarios YouTrack que el Sprint 2.
- **Calendario:** semanas de miércoles a martes 23:59. Semana 9 terminó el 10-nov; Semana 10 cierra el martes 17-nov (cada integrante debe llevar 10 h acumuladas). Defensa: 2-dic-2026.

| Persona | Usuario YouTrack |
|---|---|
| Sebastián | spereda2024 |
| Nicolás | nmartinez2024 |
| Martín | msanhueza2024 |
| Rodrigo | admin |
| Daniel | dprado2024 |
| Diego | dleiva2024 |

---

## Arrastre del Sprint 2: Semana 8 → Semana 9

Por exigencia del profesor, las 30 tareas planificadas para la **Semana 8** (cuarta semana del Sprint 2) **no se ejecutaron** (aclaración del equipo, 2026-11-14). Se trasladaron íntegras a la **Semana 9**, primera semana del Sprint 3. Conservan su ID de plan (`S2-091`..`S2-120`), su ID YouTrack (`TI3-722`..`TI3-751`), responsable y criterio de aceptación definidos en [plan_sprint2_revisado.xlsx](../../../tareas/plan_sprint2_revisado.xlsx).

Total trasladado: **30 tareas**, 30 h originales comprimidas a **25 h** (ver Plan del Sprint 3). La columna Min muestra la estimación original.

| ID plan | YouTrack | Tarea | Persona | Meta | MoSCoW | Min | Estado / evidencia |
|---|---|---|---|---|---|---:|---|
| S2-091 | TI3-722 | Definir alcance de anulación Sprint 2 | Sebastián | G2 | Must | 30 | Contrato entregado — [anulacion-venta](../dominio/anulacion-venta.md) (2026-11-09) |
| S2-092 | TI3-723 | Exponer anulación de venta | Sebastián | G2 | Must | 60 | Backend implementado — [reporte](../../qa/S2-092/reporte.md) (2026-11-09); UI y remoto [NO VERIFICADO] |
| S2-093 | TI3-724 | Reversar tarjeta por pasarela simulada | Sebastián | G2 | Must | 60 | Pendiente |
| S2-094 | TI3-725 | Publicar sale.reversed por outbox | Sebastián | G2 | Must | 60 | Pendiente |
| S2-095 | TI3-726 | Probar anulación y permisos | Sebastián | G2 | Must | 90 | Pendiente |
| S2-096 | TI3-727 | Definir política de errores Kafka | Nicolás | G2 | Must | 30 | Pendiente |
| S2-097 | TI3-728 | Agrupar descuento e idempotencia | Nicolás | G2 | Must | 60 | Pendiente |
| S2-098 | TI3-729 | Evitar stock negativo por evento | Nicolás | G2 | Must | 60 | Pendiente |
| S2-099 | TI3-730 | Corregir commit y recuperación | Nicolás | G2 | Must | 60 | Pendiente |
| S2-100 | TI3-731 | Probar duplicados y fallo intermedio | Nicolás | G2 | Must | 90 | Pendiente |
| S2-101 | TI3-732 | Revisar rutas Admin y rol activo | Martín | G1 | Must | 30 | Implementada y probada (2026-11-11), ver [[estado-actual]] |
| S2-102 | TI3-733 | Completar estados de usuarios inactivos | Martín | G1 | Must | 60 | Implementada; E2E PASS REPORTADO (2026-11-11) |
| S2-103 | TI3-734 | Validar productos desde Admin | Martín | G1 | Must | 60 | Implementada; E2E PASS REPORTADO; integración MS-3 no ejecutada (2026-11-11) |
| S2-104 | TI3-735 | Verificar accesibilidad y guía visual | Martín | G1 | Should | 60 | Implementada; E2E PASS REPORTADO (2026-11-11) |
| S2-105 | TI3-736 | Cerrar regresión de Administración | Martín | G1 | Must | 90 | Pendiente |
| S2-106 | TI3-737 | Diseñar pantalla de terminal POS | Rodrigo | G5 | Must | 30 | Implementada según [reporte QA](../../qa/S2-106/reporte.md) (2026-11-11); falta reflejarla en [[estado-actual]] |
| S2-107 | TI3-738 | Conectar terminal simulado al cobro | Rodrigo | G5 | Must | 60 | Implementada según [reporte QA](../../qa/S2-107/reporte.md) (2026-11-11); falta reflejarla en [[estado-actual]] |
| S2-108 | TI3-739 | Mostrar comprobante de pago seguro | Rodrigo | G5 | Must | 60 | Pendiente |
| S2-109 | TI3-740 | Resolver errores y recarga POS | Rodrigo | G5 | Must | 60 | Pendiente |
| S2-110 | TI3-741 | Ejecutar demo completa de mostrador | Rodrigo | G5 | Must | 90 | Pendiente |
| S2-111 | TI3-742 | Congelar criterios de salida del sprint | Daniel | H | Must | 30 | Pendiente |
| S2-112 | TI3-743 | Ejecutar E2E de Admin y venta | Daniel | H | Must | 60 | Pendiente |
| S2-113 | TI3-744 | Ejecutar E2E de tarjeta y pesaje | Daniel | H | Must | 60 | Pendiente |
| S2-114 | TI3-745 | Medir cobertura y compilar todo | Daniel | H | Must | 60 | Pendiente |
| S2-115 | TI3-746 | Auditar aislamiento y recuperación final | Daniel | H | Must | 90 | Pendiente |
| S2-116 | TI3-747 | Revisar Compose y gateway final | Diego | H | Must | 30 | Pendiente |
| S2-117 | TI3-748 | Integrar pruebas críticas en CI | Diego | H | Must | 60 | Pendiente |
| S2-118 | TI3-749 | Actualizar nodos de contexto afectados | Diego | H | Must | 60 | Pendiente |
| S2-119 | TI3-750 | Publicar guía de demo y límites físicos | Diego | H | Should | 60 | Pendiente |
| S2-120 | TI3-751 | Ejecutar revisión de release | Diego | H | Must | 90 | Pendiente |

**Avance de la Semana 9 (al 2026-11-14):** 8 de 30 tareas tienen evidencia en código o en `docs/qa/` (S2-091, S2-092, S2-101..S2-104, S2-106, S2-107); las 22 restantes siguen **Pendientes**. «PASS REPORTADO» indica que el runner E2E se interrumpió sin resumen final ni exit code 0; no equivale a cierre independiente. Los criterios de aceptación de cada tarea están en la hoja `Sprint 2` del plan. Contrastar con [[estado-actual]] antes de cerrar una tarea en YouTrack.

Bloques del arrastre:
- **G2 — Anulación (Sebastián):** contrato y endpoint backend listos (S2-091/S2-092); faltan reversa de tarjeta, `sale.reversed` y pruebas (S2-093..S2-095).
- **G2 — Consumidor Kafka MS-4 (Nicolás):** atomicidad del descuento, stock no negativo, commit de offsets y recuperación.
- **G1 — Administración (Martín):** S2-101..S2-104 con evidencia; falta la regresión de cierre (S2-105).
- **G5 — Terminal POS (Rodrigo):** S2-106/S2-107 con evidencia; faltan comprobante simulado, recarga y demo (S2-108..S2-110).
- **H — Salud técnica (Daniel y Diego):** criterios de salida, E2E, cobertura, auditoría, Compose/gateway, CI, grafo y release.

---

## Plan del Sprint 3 (acordado el 2026-11-14)

Plan completo: [plan_sprint3.xlsx](../../../tareas/plan_sprint3.xlsx). Guía para actualizar YouTrack: [sprint3_actualizar_youtrack.md](../../../tareas/sprint3_actualizar_youtrack.md).

**Capacidad: 120 h.** Semana 9 = arrastre comprimido a **25 h** + **5 h** nuevas. Semanas 10–12 = **90 h** nuevas. Cada persona tiene exactamente 300 min por semana. Tareas nuevas: `S3-001`..`S3-093` (95 h).

**Compresión del arrastre:** las tareas de testing y documentación reducen su tiempo, no su contenido. S2-095, S2-100, S2-105, S2-110, S2-115 y S2-120 bajan de 90 a 60 min; S2-112, S2-113, S2-114 y S2-117 de 60 a 45; S2-118 y S2-119 de 60 a 30.

**Prioridad:** microservicios → funcionalidad → frontend. Objetivo: llegar a la defensa del **2-dic-2026** con el `.exe` construido, los MS en el clúster, sin errores y con una UI agradable e intuitiva.

| Meta | Contenido | Horas |
|---|---|---:|
| Arrastre Sprint 2 | S2-091..S2-120 en la Semana 9 | 25 |
| M1 Microservicios | FEFO mínimo en MS-4 (recepciones, lotes, descuento FEFO junto con S2-097..S2-099, `GET /lotes?porVencer=`); MS-1 con repositorio único de usuarios, `PUT`/`DELETE /sucursales/{id}` y reactivación de usuarios | 20 |
| M2 Funcionalidad | Anulación desde la UI, recepción y lotes por vencer en Admin, cierre de turno con modal React (reemplaza `window.confirm`) | 17,5 |
| M3 Frontend simple | Solo modos `remote` y `local` en `config/api.ts`; eliminar `config.ts`, interceptor `/api` y archivos basura; partir `Pos.tsx`, `CerrarTurnoPage.tsx` y `ProductosPage.tsx`; guía de estilo en todas las pantallas | 18,25 |
| M4 Entrega | Imágenes actualizadas en el clúster y `Setup.exe` (Electron Forge) apuntando al clúster | 14,5 |
| Q Calidad | Línea base, regresión por cambio, E2E de cierre de turno, anulación y FEFO, checklist de salida | 24,75 |

**Simplificación** (pedida por el profesor: el código «da muchas vueltas» al recorrer un flujo). Es conservadora y se hace de paso en cada tarea, sin cambiar la arquitectura. Toda tarea de Backend/Frontend incluye el criterio: *«Los archivos tocados quedan sin código muerto ni duplicado, y el flujo se puede seguir desde el botón hasta la BD sin desvíos.»* Daniel hace una pasada final de simplificación.

**Roles:** Rodrigo (scrum, usuario `admin`) ejecuta todo lo del clúster, la configuración y la limpieza de archivos basura. Diego prepara manifiestos y apoya, y toma la partición de `Pos.tsx`/`CerrarTurnoPage.tsx`. Sebastián: MS-5 y anulación. Nicolás: MS-4 y FEFO. Martín: MS-1 y pantallas Admin. Daniel: regresión y E2E.

**Calendario:** Semana 10 (al martes 17-nov): limpieza, MS-1, recepciones, anulación UI y partición del POS. Semana 11 (al 24-nov): FEFO, simplificación de flujos grandes y primer `.exe`. Semana 12 (al 1-dic): pulido, regresión y despliegue final. **Congelamiento de código: domingo 29-nov**; después solo se corrigen bugs.

**Fuera de alcance:** refresh token y revocación de JWT, `UomBaseId` ↔ kg, mermas, transferencias, alertas Kafka, DTE, compras y dashboards.

**Limitación aceptada:** el namespace del clúster no tiene Kafka, así que allí la venta no descuenta stock. FEFO completo se demuestra con `docker compose`.

**Bug de cierre de turno:** reportado como congelamiento de la app al cerrar turno; no se reproduce en `dev` (2026-11-14). Se reemplaza igual `window.confirm` por un modal y se agrega un E2E.

Pendientes conocidos que no entran en el sprint, según [[estado-actual]]: refresh/revocación de JWT, auditoría de usuarios en la UI y entrega de eventos Kafka en el clúster [NO VERIFICADO].

---

## Conexiones
- Roadmap: [[roadmap]]
- Sprints anteriores: [[sprints-anteriores]]
- Estado real: [[estado-actual]]
- Plan del Sprint 2 y mapeo YouTrack: `tareas/plan_sprint2_revisado.xlsx`, `tareas/sprint2_youtrack_resultado.json`

## Fuentes
- `tareas/plan_sprint2_revisado.xlsx` (hojas `Resumen` y `Sprint 2`, filas con `Semana 8`)
- `tareas/sprint2_youtrack_resultado.json`
- `docs/defensa-sprint2/01-resumen.md`
- Aclaración del equipo (Daniel Prado, 2026-11-14): Semana 8 no ejecutada por exigencia del profesor; capacidad y equipo sin cambios; prioridad microservicios → funcionalidad → frontend.

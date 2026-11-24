---
name: reporte-qa
description: Escribe la evidencia de pruebas de una tarea de GlobalMart OS en docs/qa/<ID>/reporte.md. Úsala al cerrar una tarea de testing o al documentar qué se probó y con qué resultado.
---

# Reporte de pruebas

La evidencia de cada tarea vive en `docs/qa/<ID>/reporte.md` (por ejemplo `docs/qa/S3-030/reporte.md`). En la defensa se cita tal cual, así que solo afirma lo que la ejecución demuestra.

## Plantilla

~~~markdown
# <ID> / <TI3-xxx> — <título de la tarea>

Fecha: AAAA-MM-DD. Meta: <meta del plan>.

## Objetivo y criterio de aceptación
- Objetivo: ...
- Aceptación: ... (copiado de tareas/plan_sprint3.xlsx)

## Qué se probó
- Escenarios cubiertos, uno por línea.

## Cómo reproducir
```bash
<comandos exactos, variables de entorno necesarias, rama o commit>
```

## Resultado
- <suite>: N/N aprobadas, exit code 0.
- Artefactos: <.trx, capturas, JSON> en esta misma carpeta.

## Límites
- Qué es real y qué es simulado o fixture.
- Qué queda [NO VERIFICADO].
~~~

## Reglas

- **Números reales:** `N/N aprobadas` y el exit code de la corrida. No sumes corridas distintas que repiten casos.
- **PASS REPORTADO:** si el runner se interrumpió sin resumen final o sin exit code 0 (por ejemplo, el servidor de Playwright quedó abierto), escribe «PASS REPORTADO» y no lo presentes como cierre completo.
- **Sin cobertura inventada:** no declares un porcentaje de cobertura sin un reporte que lo pruebe.
- **Real vs simulado:** distingue pasarela simulada, balanza simulada, fixtures de catálogo/IVA y APIs interceptadas de las integraciones reales.
- **Clúster sin Kafka:** en el clúster la venta no descuenta stock; lo que dependa de eventos se prueba en `docker compose` y se indica así.
- **Artefactos junto al reporte:** guarda `.trx`, capturas o JSON de resultados en la misma carpeta.
- Si el reporte cambia el estado de una funcionalidad, actualiza también el grafo con la skill `grafo-contexto`.

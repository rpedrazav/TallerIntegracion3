# S3-013 — Mostrar errores de anulación

Fecha: 2026-11-23. Rama: `dev`. Meta: Funcionalidad de punta a punta.

## Objetivo y criterio de aceptación

- Traducir los códigos del backend a mensajes claros en la vista de anulación.
- Cada error del contrato S2-091 muestra un mensaje distinto y no deja la venta en estado ambiguo.
- Mantener el flujo sin código muerto ni duplicado.

## Cambio realizado

`VentasTurnoPage` usa `mensajeErrorAnulacion` para traducir los códigos de negocio, con fallback HTTP para cuerpos vacíos y errores desconocidos. Conserva el motivo y el diálogo ante errores, mantiene el aviso al cerrarlo y evita repetir desde ese diálogo una solicitud fallida. La guarda de envío bloquea solicitudes simultáneas y el cierre mientras se procesa.

Después de cada intento consulta la lista del servidor. Si la lectura falla, retira las filas desactualizadas y muestra que no pudo verificar el estado. No cambia una venta a ANULADA de forma optimista ni afirma un reembolso ante una reversa pendiente. Venta ajena e inexistente comparten mensaje 404. El interceptor existente conserva la salida al login ante 401.

Flujo conservado: botón → confirmarAnulacion → posApi.anularVenta → VentasController.Anular → VentaService.AnularAsync → persistencia (y pasarela para tarjeta). El traductor solo decide texto en la UI; no agrega capas a la escritura. Backend y contratos HTTP no fueron modificados.

## Qué se probó

- CAJERO sin botón de anulación y ADMIN con motivo obligatorio y éxito.
- Los 13 códigos definidos por la anulación, incluidos rechazo y reversa pendiente. Para IDENTIDAD_INVALIDA/401 se verifica la salida al login del interceptor.
- Respuestas sin código: 400, 401, 403, 404, 500; conflicto desconocido 409 y fallo de red.
- Los rechazos conservan motivo, mensaje y estado COMPLETADA de la fixture, sin un segundo POST.
- Respuesta perdida tras una anulación: la lectura posterior recupera ANULADA del servidor.
- Error al refrescar: no quedan filas obsoletas ni se presenta una lista vacía como ausencia real de ventas.

## Cómo reproducir

Desde `globalmart-frontend`, con dependencias y Chromium de Playwright disponibles:

```bash
npm run typecheck
PLAYWRIGHT_JSON_OUTPUT_FILE=../docs/qa/S3-013/resultados.json npx playwright test e2e/ventas-turno.spec.ts --reporter=list,json
```

## Resultado

- Línea base: TypeScript aprobado; 2/2 escenarios existentes aprobados, exit code 0.
- Verificación final: TypeScript aprobado, exit code 0; Playwright **24/24**, exit code 0.
- La primera corrida ampliada tuvo 22 aprobados y 2 fallos por esperar que el diálogo permaneciera tras 401. Se corrigieron esas expectativas para comprobar la redirección global existente; no se debilitó la autorización.
- Evidencia: [playwright.log](playwright.log), [resultados.json](resultados.json), [typecheck.log](typecheck.log).

## Límites

Pruebas de interfaz en Chromium con APIs interceptadas: no certifican persistencia real, pasarela ni aislamiento backend. Electron empaquetado y clúster [NO VERIFICADOS]. No se ejecutó dotnet test porque no cambió código .NET. La pantalla informa reversas pendientes pero no añade consulta/reconciliación de reversa; tampoco implementa devoluciones parciales. El 401 dirige al login, por lo que su mensaje no permanece en la vista de anulación.

## Para la reunión

“Ahora la pantalla explica por qué una venta no pudo anularse, diferenciando turno cerrado, falta de permisos y problemas con la devolución de tarjeta. Además, consulta el estado real al servidor para no mostrar una anulación o un reembolso que no estén confirmados.”

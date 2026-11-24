# S3-038 — Probar anulación desde la UI

Fecha: 2026-11-23. Rama: `dev`. Meta: Funcionalidad de punta a punta.

## Objetivo y aceptación

Cubrir anulación por ADMIN, rechazo para CAJERO y rechazo de tarjeta con una prueba E2E. Los tres escenarios pasan y la venta conserva un estado coherente.

## Spec y escenarios

Se amplió `globalmart-frontend/e2e/ventas-turno.spec.ts`, reutilizando los casos existentes. Los tres casos de esta tarea llevan `S3-038` en el nombre:

1. **ADMIN:** exige motivo, envía el texto normalizado, muestra ANULADA y retira el botón Anular. Al recargar conserva ANULADA e importe $119.
2. **CAJERO:** no ve Anular y no envía solicitudes. Además se abre una pantalla ADMIN en otra página y se cambia el token del cliente a CAJERO: se comprueba el Authorization enviado, se simula HTTP 403 y se verifica el mensaje de permisos. La venta sigue COMPLETADA con $119 al recargar.
3. **TARJETA:** la fila muestra método TARJETA; el POST recibe 409 REVERSA_RECHAZADA. La pantalla explica que no hubo reembolso, realiza un solo POST y conserva COMPLETADA, TARJETA y $119 al recargar.

## Cómo reproducir

Desde `globalmart-frontend`, con dependencias y navegador Playwright instalados:

```bash
npm run typecheck
npx playwright test e2e/ventas-turno.spec.ts --grep S3-038 --reporter=list
PLAYWRIGHT_JSON_OUTPUT_FILE=../docs/qa/S3-038/resultados.json npx playwright test e2e/ventas-turno.spec.ts --reporter=list,json
```

La corrida de evidencia ejecuta el spec completo, incluidos los tres escenarios nuevos/reforzados y la regresión de mensajes S3-013.

## Resultado

- Tres escenarios S3-038: **3/3 aprobados** dentro de la corrida completa.
- Spec completo: **25/25 aprobados**, exit code 0, 19.9 segundos.
- TypeScript: aprobado, exit code 0. `git diff --check`: aprobado.
- La primera corrida detectó un problema de preparación del caso CAJERO: dos scripts de inicio de sesión en una página impedían abrir la vista ADMIN. Se separaron las páginas; la corrida final pasó sin modificar código productivo.
- Artefactos: [playwright.log](playwright.log), [resultados.json](resultados.json), [typecheck.log](typecheck.log).

## Límites

La UI React, handlers y cliente HTTP son reales; el puente Electron, JWT y respuestas HTTP son fixtures interceptadas por Playwright. Las comprobaciones tras recargar validan cómo la UI consume el estado de la fixture, no persistencia PostgreSQL. El 403 verifica la reacción del frontend y el token enviado, no la autorización real del backend. El 409 no invoca la pasarela real ni el simulador externo.

No se modificó código productivo ni se añadieron dependencias. Integración completa con MS-5, Electron empaquetado y clúster [NO VERIFICADOS] en esta tarea. La evidencia previa de autorización y persistencia backend se conserva en [S2-095](../S2-095/reporte.md).

## Para la reunión

“Automatizamos los tres casos de anulación desde la pantalla: un administrador puede anular, un cajero no tiene permiso y una devolución de tarjeta rechazada conserva la venta completada. También comprobamos que al recargar se mantengan el estado y el importe esperados.”

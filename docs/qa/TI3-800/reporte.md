# TI3-800 — Agregar botón Reactivar en la lista de usuarios

Fecha: 2026-11-22. Meta: reactivar una cuenta desde Administración y rehabilitar sus acciones sin recargar la página.

## Objetivo y criterio de aceptación

- Objetivo: incorporar **Reactivar** para usuarios inactivos mediante `POST /users/{id}/reactivate` con el JWT existente.
- Aceptación: ante 204, solo la fila afectada cambia de Inactivo a Activo, conserva las demás filas, no recarga y vuelve a ofrecer Editar y Desactivar; los fallos no alteran el estado.

## Qué se probó

- Helper: POST a la ruta del mismo usuario y actualización exclusiva de su propiedad `activo`.
- Interfaz: Reactivar solo aparece en inactivos; el POST conserva ID y encabezado Bearer.
- Single-flight: un segundo evento durante la petición no crea otro POST.
- Respuesta 204: la misma fila pasa a Activo, Reactivar desaparece, Editar y Desactivar se rehabilitan, las otras filas se conservan y no ocurre otro GET.
- Errores 401, 403, 404 y de red: mensaje visible, fila inactiva conservada y control nuevamente disponible.
- Regresión administrativa: acceso por rol, edición, baja, roles y configuración de tenant.

## Cómo reproducir

```powershell
cd globalmart-frontend
node --test tests/user-deactivation.test.cjs tests/user-edit.test.cjs tests/admin-access.test.cjs
node node_modules/typescript/bin/tsc --noEmit --incremental false --pretty false
npx.cmd playwright test e2e/admin-flows.spec.ts
```

## Resultado

- Pruebas Node administrativas: 13/13 aprobadas, exit code 0.
- TypeScript: sin errores, exit code 0.
- Playwright `admin-flows.spec.ts`: **22/22 aprobadas en 42,6 s, exit code 0**, incluidos los cinco escenarios de reactivación.
- Diagnóstico del bloqueo anterior: Playwright completaba los casos, pero su `taskkill /T /F` recibía `Acceso denegado` al cerrar el servidor en el entorno Windows administrado. En la validación final se cerró exclusivamente el PID que escuchaba en `127.0.0.1:4175`; el runner continuó hasta emitir el resumen y terminar normalmente con exit code 0.
- `playwright.config.ts` y `e2e/serve.cjs` conservaron su configuración original; no fue necesario cambiar el proyecto para resolver una restricción del entorno de ejecución.
- Artefactos temporales de Playwright: eliminados al finalizar.

## Límites

- Playwright usa `page.route()` y fixtures HTTP; no acredita comunicación E2E con MS-1 desplegado.
- El backend de reactivación y su ciclo real de login fueron probados por TI3-799, no repetidos en esta tarea frontend.
- Despliegue remoto: [NO VERIFICADO].

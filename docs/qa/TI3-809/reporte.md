# TI3-809 — Regresión de Admin tras partir ProductosPage

Fecha: 2026-11-24. Meta: Calidad y regresión (Must, dificultad Media).

## Objetivo y criterio de aceptación
- Objetivo: ejecutar E2E de Admin y productos después de partir `ProductosPage` (TI3-801: tabla, filtros y modal).
- Aceptación: los E2E de Admin pasan sin cambios en los specs. [Texto de la tarea; no se verificó contra `tareas/plan_sprint3.xlsx`.]

## Qué se probó
- Productos: crear, editar, filtrar y desactivar actualiza la tabla sin recarga manual.
- Productos: errores 400/401/403/404 y fallo de red al guardar conservan los datos; precio cero se rechaza sin POST; desactivación 403 conserva la fila.
- Productos: el modal contiene el foco, cierra con Escape, devuelve el foco y envía con Enter una sola vez.
- Admin: usuarios (alta, edición, baja, reactivación), roles, inventario, configuración de IVA y acceso por rol.

## Cómo reproducir
```bash
cd globalmart-frontend
npx playwright test e2e/admin.spec.ts e2e/admin-flows.spec.ts e2e/productos-admin.spec.ts e2e/tenant-config.spec.ts
```
Requiere Chrome (channel `chrome`) y puerto 4175 libre. Rama `dev`, sin cambios en `e2e/`.

## Resultado
- Playwright: 40/40 aprobadas, 0 fallidas, 0 omitidas, exit code 0, 36,7 s (`admin.spec.ts` 8, `admin-flows.spec.ts` 22, `productos-admin.spec.ts` 9, `tenant-config.spec.ts` 1).
- Specs sin modificar: `git diff -- globalmart-frontend/e2e` vacío.
- Artefactos en esta carpeta: `salida-playwright.txt`, `results.json`.

## Límites
- Las APIs (identidad, catálogo, inventario) están interceptadas en Playwright; no se probó backend real, JWT real ni PostgreSQL.
- No se ejecutó el resto de la suite (POS, carrito, Electron `ci:local`): [NO VERIFICADO] tras la partición.

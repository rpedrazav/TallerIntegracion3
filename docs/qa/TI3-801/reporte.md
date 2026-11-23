# TI3-801 — Partir ProductosPage en tabla, filtros y modal

Fecha: 2026-11-22. Meta: separar responsabilidades de la administración de productos sin modificar su comportamiento.

## Objetivo y criterio de aceptación

- Objetivo: extraer modal, filtros y tabla desde `ProductosPage.tsx` hacia componentes presentacionales sin HTTP.
- Aceptación: preservar selectores, textos, estilos, permisos, CRUD, filtros, errores, accesibilidad, prevención de doble envío y contratos MS-3; TypeScript y los E2E originales deben finalizar con exit code 0.

## Qué se probó

- Alta, edición, código de barras, filtros y baja lógica con refresco automático.
- Precio mayor que cero y conservación del formulario ante error.
- Errores 400 de FluentValidation, 401, 403, 404 y red.
- Desactivación fallida sin alterar la fila.
- Foco inicial, Tab/Shift+Tab, Escape, retorno de foco y submit con Enter sin doble envío.
- CAJERO activo con ADMIN asignado sin acceso directo a `/admin/productos`.

## Cómo reproducir

```powershell
cd globalmart-frontend
node node_modules/typescript/bin/tsc --noEmit --incremental false --pretty false
npx.cmd playwright test e2e/productos-admin.spec.ts
npx.cmd playwright test e2e/admin.spec.ts --grep "CAJERO activo"
```

## Resultado

- TypeScript: sin errores, exit code 0.
- `e2e/productos-admin.spec.ts`: 9/9 aprobadas en 41,7 s, exit code 0.
- Acceso CAJERO en `e2e/admin.spec.ts`: 1/1 aprobada en 35,5 s, exit code 0.
- `ProductosPage.tsx`: 1.212 → 401 líneas.
- Componentes extraídos: `ProductoModal.tsx` 382 líneas, `ProductosFiltros.tsx` 149 y `ProductosTabla.tsx` 333.
- Los archivos E2E originales no fueron modificados.
- Artefactos temporales de Playwright: eliminados al finalizar.

## Límites

- Los nueve escenarios de productos usan `page.route()` y fixtures HTTP; no acreditan MS-3 desplegado.
- El entorno Windows administrado deniega el `taskkill /T /F` que Playwright usa para su `webServer`. Se cerró exclusivamente el PID que escuchaba en `127.0.0.1:4175`; ambos runners emitieron su resumen y terminaron normalmente con exit code 0.
- No se modificó el grafo de contexto porque la extracción no cambia endpoints, reglas ni comportamiento observable.

# TI3-763 — Agregar acción Anular venta para ADMIN

Fecha: 2026-11-22. Meta: Funcionalidad de punta a punta.

## Objetivo y criterio de aceptación
- Objetivo: Mostrar las ventas del turno con botón Anular visible solo para ADMIN y pedir motivo obligatorio.
- Entrega verificable: Vista de ventas del turno con acción de anulación.
- Aceptación: CAJERO no ve la acción; ADMIN anula una venta en efectivo y la lista refleja ANULADA.
- Simplificación: Los archivos tocados quedan sin código muerto ni duplicado, y el flujo se puede seguir desde el botón hasta la BD sin desvíos.

## Qué se probó
- Endpoint `GET /api/turnos/abiertos` en MS-5: ADMIN autenticado obtiene únicamente turnos abiertos de su tenant; CAJERO recibe 403 Forbidden; turnos de otros tenants quedan aislados (RN-01).
- Integración de MS-5 y suite de Turnos, Cobro y Anulación en .NET (54/54 aprobadas).
- Vista `VentasTurnoPage` (`/#/ventas-turno`): CAJERO visualiza ventas completadas del turno sin botón de Anular.
- Vista `VentasTurnoPage`: ADMIN visualiza botón Anular en ventas COMPLETADA; el botón de confirmación permanece deshabilitado hasta ingresar motivo obligatorio.
- Al confirmar anulación con motivo válido, se despacha `POST /api/ventas/{id}/anular` con `{ motivo }` y la tabla refresca el estado a `ANULADA`, ocultando la acción de anulación.

## Cómo reproducir
```bash
# 1. Pruebas de integración Backend MS-5
dotnet test tests/GlobalMart.IntegrationTests/GlobalMart.IntegrationTests.csproj --filter "FullyQualifiedName~AnulacionVentaIntegrationTests|FullyQualifiedName~TurnoIntegrationTests|FullyQualifiedName~CobroIntegrationTests|FullyQualifiedName~RoutePrefixIntegrationTests"

# 2. Typecheck y prueba E2E Frontend
cd globalmart-frontend
npm run typecheck
npx playwright test e2e/ventas-turno.spec.ts
```

## Resultado
- Backend (.NET): 54/54 aprobadas, exit code 0.
- Frontend (Playwright `e2e/ventas-turno.spec.ts`): 2/2 aprobadas, exit code 0 (duración 5.0s).
- Frontend (TypeScript): 0 errores (`tsc --noEmit`).

## Límites
- Playwright interactúa contra servidor web local (`e2e/serve.cjs`) con rutas HTTP mockeadas vía `page.route` para aislar el comportamiento de la UI.
- La ejecución en clúster remoto de la UI y la conexión con hardware físico quedan fuera del alcance de esta tarea.

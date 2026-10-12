# S2-025 — Regresión de carrito

Fecha: 2026-10-12. Resultado: **NO ACEPTADO**.

Entrega: suite Playwright reproducible sobre el componente real del carrito,
reporte HTML/JSON, capturas y trazas de los fallos. 3 casos: 1 aprobado, 2 fallidos.

| Caso | Resultado | Evidencia |
|---|---|---|
| Agregar A y B, cambiar A a 3, eliminar B, recargar | APROBADO | Queda una fila A con cantidad 3 después de recargar |
| Recibir IVA y totales de MS-5 después del flujo | FALLIDO | Cero respuestas de `/ventas`; no puede compararse con MS-5 |
| Cambiar sesión de tenant A a B y recargar | FALLIDO | Producto A del carrito anterior sigue visible |

Capturas: [sin respuesta de MS-5](ms5-fallo.png),
[persistencia entre sesiones](tenant-fallo.png).
Reporte navegable: [Playwright](playwright-report/index.html).
Resultados estructurados: [JSON](results.json).

## Hallazgos

1. `Pos.tsx` mantiene operaciones en React/localStorage (`pos_cart`) y calcula
   `Math.round(subtotal * 0.19)`. No consulta ventas ni totales de MS-5.
   En la captura final muestra subtotal $300, IVA $57 y total $357 calculados
   localmente. La igualdad con MS-5 queda **NO VERIFICADA** y la aceptación falla.
2. `pos_cart` no se particiona por tenant o usuario. La prueba de cambio de
   sesión conserva el producto anterior. Es un fallo de aislamiento del estado
   del frontend; no demuestra acceso cruzado a datos de MS-5 por HTTP.

## Alcance y reproducción

Ver [instrucciones de la suite](../../../globalmart-frontend/e2e/README.md).
Chrome sobre Windows, renderer real con HashRouter; catálogo y puente de sesión
controlados. No se levantó MS-5 ni se simuló su respuesta para aprobar el caso.
No se probaron cobro, Kafka, autenticación real, IPC Electron ni autorización
del servidor. El criterio de totales del servidor queda pendiente de integrar
el POS y ejecutar contra servicios reales con fixtures de tenant/turno.

No se modificó la lógica de producto: esta tarea entrega la regresión y sus
hallazgos. Corregir la integración y el almacenamiento por sesión/tenant es
necesario antes de aceptar la meta de carrito y venta funcional.

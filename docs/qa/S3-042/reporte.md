# S3-042 / TI3-793 — Verificar cuadre con ventas anuladas

Fecha: 2026-11-24. Meta: Funcionalidad de punta a punta.

## Objetivo y criterio de aceptación
- Objetivo: Prueba de integración que demuestre que el cuadre de caja excluye las ventas anuladas.
- Aceptación: El cuadre coincide con las ventas en efectivo completadas y no anuladas.
- Simplificación: Los archivos tocados quedan sin código muerto ni duplicado, y el flujo se puede seguir desde el botón hasta la BD sin desvíos.

## Qué se probó
- Prueba de integración en `tests/GlobalMart.IntegrationTests/CierreTurnoIntegrationTests.cs` etiquetada con `[Trait("Plan", "S3-042")]`:
  1. Apertura de turno para cajero (`POST /api/turnos/abrir`).
  2. Registro de 2 ventas en efectivo completadas con pago (Venta A neto $119, Venta B neto $60).
  3. Cuadre inicial (`POST /api/turnos/cuadre` con declarado $179): `efectivo_esperado = 179`, `diferencia = 0`.
  4. Anulación de Venta A mediante `POST /api/ventas/{id}/anular` autorizada por ADMIN con motivo obligatorio.
  5. Cuadre posterior con declarado $60: `efectivo_esperado = 60`, `diferencia = 0` (exclusión verificada).
  6. Reintento de anulación de Venta A: responde 409 Conflict y el cuadre posterior permanece en $60.
  7. Verificación directa en PostgreSQL: los registros de pagos de Venta A persisten inmutables en BD y su estado es `ANULADA`.

## Cómo reproducir
```bash
GLOBALMART_TEST_POSTGRES="Host=127.0.0.1;Port=5436;Database=pos_db;Username=admin;Password=admin_password" \
ConnectionStrings__DefaultConnection="Host=127.0.0.1;Port=5436;Database=pos_db;Username=admin;Password=admin_password" \
dotnet test tests/GlobalMart.IntegrationTests/GlobalMart.IntegrationTests.csproj \
  --no-restore -m:1 \
  --filter 'Plan=S3-042'
```

## Resultado
- Suite de integración CierreTurno: 1/1 aprobada (y 9/9 en regresión conjunta), exit code 0.
- Artefactos: `S3-042.trx` en esta misma carpeta.

## Límites
- Ejecutado contra base PostgreSQL real de MS-5 (`pos_db` en Docker).
- Clúster productivo y Kong permanecen [NO VERIFICADO] al ser pruebas de backend a nivel API/servicio.

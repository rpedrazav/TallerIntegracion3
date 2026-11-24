# S2-092 — Exponer anulación de venta

Fecha: 2026-11-09. Plan: S2-092 / TI3-723.

## Cambio

Se implementó `POST /ventas/{id:guid}/anular` y alias `/api/ventas/{id:guid}/anular` conforme al [contrato S2-091](../../context/dominio/anulacion-venta.md).

- JWT válido con identidad y tenant; rol ADMIN tanto en el endpoint como en `VentaService.AnularAsync`. No exige que el administrador sea el cajero original ni tenga turno propio.
- Body estricto: únicamente `motivo`, texto de 1–500 caracteres tras Trim. Rechaza campos adicionales y duplicados para no interpretar una devolución parcial como total.
- `VentaRepository.AnularTotalAsync` filtra por tenant y bloquea turno original y venta durante la transacción. Exige COMPLETADA, efectivo coherente, turno ABIERTO y ausencia de pagos de tarjeta/intentos inciertos.
- Guarda ANULADA y auditoría con usuario autenticado; conserva pagos, ítems, importes e historial outbox. No emite otro `sale.completed`.
- Errores de negocio 400/404/409 con código estable; autorización 401/403. JSON malformado/body ausente usa la respuesta 400 estándar de ASP.NET. Errores de persistencia retornan 500 genérico y revierten la transacción.
- Tarjeta aprobada: 409 `REEMBOLSO_NO_DISPONIBLE`; intento incierto: 409 `PAGO_INCIERTO`. No se contacta la pasarela ni se promete reembolso.
- Sin migraciones, cambios de frontend ni corrección del cuadre preexistente. La devolución de efectivo sigue siendo gestión manual.

## Pruebas

[AnulacionVentaIntegrationTests.cs](../../../tests/GlobalMart.IntegrationTests/AnulacionVentaIntegrationTests.cs) agrega 38 casos con HTTP de MS-5, JWT firmado y PostgreSQL 16 temporal migrado:

- Dos rutas exitosas; ADMIN distinto al cajero y sin turno propio; motivo normalizado; identidad registrada; conservación de ítems, pago con vuelto y outbox; repetición 409 sin cambio adicional.
- Sin token, token vencido o sin identidad: 401; CAJERO, REPONEDOR y SUPER_ADMIN sin ADMIN: 403.
- Dos tenants atacándose en ambos sentidos, incluso con `X-Tenant-ID` falsificado: 404 igual a inexistente y snapshots intactos de ambos tenants.
- 13 entradas inválidas: ausencia, null, tipos incorrectos, espacios, longitud, campos ajenos/parciales y duplicados, JSON malformado.
- 13 conflictos: estados, turno cerrado, método/pago/intento de tarjeta aprobado, incertidumbre, mixto, pago ausente, importe incoherente y vuelto negativo.
- Dos solicitudes simultáneas: una 200 y otra 409, una sola auditoría.
- Llamada al servicio sin identidad ADMIN: rechazo sin mutación.
- Fallo controlado después de SaveChanges y antes de Commit: 500 sin detalles internos, rollback comprobado con contexto nuevo.

Los rechazos comparan snapshots recargados de ventas, ítems, pagos, intentos, anulaciones y outbox. El caso de éxito comprueba además los valores esperados del pago y precio histórico.

La primera ejecución obtuvo 37/38: la configuración del interceptor de fallo no reemplazaba las opciones existentes del DbContext y no provocó el fallo esperado. Se corrigió la configuración de la prueba; no se relajó la aserción de rollback.

## Resultado final

**73/73 pruebas Release aprobadas, sin fallos ni omisiones: 38 nuevas S2-092 y 35 de regresión** de cobro efectivo/tarjeta, cierre, contrato de eventos e aislamiento. Compilación aprobada; advertencias preexistentes de documentación XML y analizadores xUnit en otros archivos. No se ejecutó toda la solución. Evidencia: [S2-092.trx](S2-092.trx).

## Reproducción

Usar una base desechable exclusiva, nunca una base compartida del proyecto:

```bash
docker run --rm -d --name globalmart-s2092-postgres \
  -e POSTGRES_PASSWORD=s2092_test -e POSTGRES_DB=s2092 \
  -p 127.0.0.1:55492:5432 postgres:16
docker exec globalmart-s2092-postgres pg_isready -U postgres -d s2092
export ConnectionStrings__DefaultConnection='Host=127.0.0.1;Port=55492;Database=s2092;Username=postgres;Password=s2092_test'
export GLOBALMART_TEST_POSTGRES="$ConnectionStrings__DefaultConnection"
dotnet test tests/GlobalMart.IntegrationTests -c Release --no-restore \
  --filter 'Plan=S2-092|FullyQualifiedName~CobroIntegrationTests|FullyQualifiedName~CierreTurnoIntegrationTests|FullyQualifiedName~PaymentEventSequenceContractTests|FullyQualifiedName~VentaTenantIsolationTests|FullyQualifiedName~CobrarTarjetaIntegrationTests' \
  --logger 'trx;LogFileName=S2-092.trx' --results-directory docs/qa/S2-092
docker stop globalmart-s2092-postgres
```

La contraseña es exclusiva de la base temporal. Para ejecutar solo la nueva suite, filtrar `Plan=S2-092`. El worker de outbox está deshabilitado por la factory de pruebas. No acredita entrega Kafka, reposición de inventario, devolución física, integración de UI ni despliegue Kubernetes. El cierre concurrente con anulación se coordina mediante bloqueo de turno en código, pero no tiene un escenario específico nuevo en esta suite; sí se prueba la carrera entre dos anulaciones.

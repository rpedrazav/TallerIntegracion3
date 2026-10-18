# Integración de cambios de pagos — 2026-10-18

Ramas integradas: commit local `212cc56` (S2-035) y `f6727db` de `origin/dev` (nueve commits nuevos). Se resolvieron los cinco archivos con conflicto sin descartar globalmente ningún lado.

## Resolución

- `VentasController`: conserva validación estricta de la clave de S2-035, cambio de método de pago, control de identidad de respuesta y `GET /ventas/{id}/intento-tarjeta` de S2-049.
- `VentaRepository`: `IniciarIntentoPagoAsync` usa la reserva de S2-035, con lock breve por venta, índice único por tenant/clave y bloqueo de pagos existentes o inciertos. No se crean dos reservas. Se conserva la actualización condicional de S2-049 y se exige coincidencia de tenant, venta, clave, monto y moneda. La aprobación exige una reserva previa y la actualiza dentro de la misma transacción de venta/pago/outbox.
- `estado-actual.md`, `ms5-pos.md` y `pasarela-pago.md`: combinan fuentes, S2-035 y S2-040, y reflejan la recuperación de S2-049.
- Se conserva íntegra la migración de `Pago.Referencia`, la propagación de esa referencia desde el servicio y los cambios entrantes de frontend, administración, catálogo e infraestructura.
- Se adaptó el doble de repositorio de `PaymentEventSequenceContractTests` a la interfaz combinada. No se eliminó ninguna prueba de las dos ramas.

### Diferencia intencional de estado

S2-035 inicialmente guardaba errores de comunicación como `FALLIDO`. S2-049 conserva `PENDIENTE` hasta obtener un resultado definitivo. Se conserva esta última semántica y se actualiza la expectativa del test S2-035: ambos estados bloquean autorizaciones adicionales, y la consulta también acepta registros `FALLIDO` anteriores.

Se extendió la prueba de fallo de persistencia: tras una aprobación real del simulador y rollback local, se elimina el fallo controlado y se consulta el intento por HTTP. La venta se recupera como aprobada, una segunda consulta conserva un pago y un outbox, y el contador de autorizaciones sigue siendo **un POST**.

## Evidencia

- [merge-pagos.trx](merge-pagos.trx): **54/54** pruebas backend Release aprobadas, sin fallos ni omisiones. Incluye **17 S2-035**, **5 S2-040** y regresión de tarjeta, adaptador/simulador, timeout, persistencia, efectivo y outbox. Usa PostgreSQL 16 temporal y procesos Node reales para S2-035.
- `npm run typecheck`: aprobado.
- `node --test tests/user-edit.test.cjs tests/user-deactivation.test.cjs`: **2/2 archivos aprobados**.
- [merge-ui.json](merge-ui.json): **7/7** pruebas de interfaz aprobadas (`card-search.spec.ts`, `cash-payment.spec.ts`): búsqueda, escaneo, aprobación, rechazo/cambio de método, doble clic/timeout/recarga y efectivo/vuelto/monto insuficiente. Las APIs están controladas en estos tests: no es E2E contra un despliegue.

La configuración original de Playwright no pudo iniciar porque `/opt/google/chrome/chrome` no existe en este equipo. Se descargó Chromium de Playwright en `/tmp/globalmart-merge-browsers` y se usó una configuración temporal con `browserName: 'chromium'`, manteniendo los mismos tests, servidor, viewport y timeouts. No se modificaron dependencias ni configuración del proyecto.

La compilación mostró advertencias XML preexistentes de `ProductoController`; no hubo errores de compilación. No se ejecutó toda la solución ni se validó el despliegue Kubernetes.

## Reproducción de backend

Usar la base temporal descrita en [reporte S2-035](reporte.md), y definir ambas variables hacia esa base exclusiva de prueba:

```bash
export GLOBALMART_TEST_POSTGRES='Host=127.0.0.1;Port=55435;Database=s2035;Username=postgres;Password=s2035_test'
export ConnectionStrings__DefaultConnection="$GLOBALMART_TEST_POSTGRES"
dotnet test tests/GlobalMart.IntegrationTests --configuration Release --no-restore \
  --filter 'Plan=S2-035|Plan=S2-040|FullyQualifiedName~CobrarTarjetaIntegrationTests|FullyQualifiedName~PaymentGatewayClientTests|FullyQualifiedName~PaymentSimulatorIntegrationTests|FullyQualifiedName~PaymentRetrySafetyTests|FullyQualifiedName~IntentoPagoPersistenceTests|FullyQualifiedName~CobroIntegrationTests|FullyQualifiedName~VentaOutboxTransactionTests' \
  --logger 'trx;LogFileName=merge-pagos.trx' --results-directory docs/qa/S2-035
```

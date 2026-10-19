# S2-053 / TI3-684 — Probar autorización e idempotencia

Fecha: 2026-10-19. **3/3 pruebas aprobadas, 0 fallidas, 0 omitidas**, configuración Release. Evidencia automatizada: [S2-053.trx](S2-053.trx).

## Resultado de aceptación

Una clave por tenant produce una única autorización lógica. Dos solicitudes idénticas de cobro a MS-5 generan un único POST externo, un IntentoPago APROBADO, un Pago TARJETA de 119 CLP y un evento outbox pendiente. La primera solicitud devuelve 200 y la repetición 409 conforme al contrato existente. No fue necesario modificar código de producción.

| Caso | Comprobaciones |
|---|---|
| Duplicado después de aprobar | 200/409, un POST al simulador, venta COMPLETADA, un intento y un pago |
| Duplicado con respuesta pendiente | Se retiene la respuesta real de aprobación; el duplicado devuelve 409 mientras solo existe la reserva. Al liberar la respuesta se confirma un único pago |
| Dos POST idénticos directos al simulador | Ambos retornan APPROVED con el mismo request_id, transaction_id, referencia y processed_at |

Los dos casos HTTP correlacionan la transacción, código de autorización y referencia del simulador con la respuesta de aprobación; verifican la clave, monto, moneda y estado del intento, y la referencia del Pago mediante un contexto nuevo de PostgreSQL. Consultar nuevamente la pasarela devuelve exactamente la autorización original.

## Alcance

[CobroSeguroIntegrationTests.cs](../../../tests/GlobalMart.IntegrationTests/CobroSeguroIntegrationTests.cs) usa el pipeline HTTP de MS-5 con JWT validado, PostgreSQL 16 migrado en un esquema exclusivo por caso, el adaptador real y un proceso Node real del simulador. Turno ABIERTO y venta TARJETA se preparan en la base. El observador de tráfico cuenta solicitudes y, para el caso concurrente, retiene la primera respuesta; no inventa respuestas de pasarela.

[PaymentSimulatorIntegrationTests.cs](../../../tests/GlobalMart.IntegrationTests/PaymentSimulatorIntegrationTests.cs) añade al filtro S2-053 la prueba existente de repetición directa, reforzada para exigir aprobación e identificadores no vacíos.

No acredita Electron, creación de venta a través de MS-1/MS-2/MS-3, banco real ni entrega Kafka. El publicador outbox está deshabilitado. La idempotencia del simulador se conserva en memoria durante su proceso; no se prueba reinicio. Despliegue remoto [NO VERIFICADO]: persiste como antecedente el bloqueo de catálogo documentado en [S2-052](../S2-052/reporte.md); esta tarea no lo corrigió ni volvió a evaluarlo.

## Reproducción

Requiere .NET 8, Node, Docker y PostgreSQL listo antes de iniciar las pruebas. Desde la raíz:

```powershell
docker run --rm -d --name globalmart-s2053-postgres -e POSTGRES_PASSWORD=s2053_test -e POSTGRES_DB=s2053 -p 127.0.0.1:55453:5432 postgres:16
$env:GLOBALMART_TEST_POSTGRES = 'Host=127.0.0.1;Port=55453;Database=s2053;Username=postgres;Password=s2053_test'
dotnet test tests/GlobalMart.IntegrationTests --configuration Release --filter 'Plan=S2-053' --logger 'trx;LogFileName=S2-053.trx' --results-directory docs/qa/S2-053
docker stop globalmart-s2053-postgres
```

Las pruebas eliminan sus esquemas y procesos Node. El contenedor es desechable y la conexión se establece solo para la prueba. La compilación mostró advertencias preexistentes de MSBuild/documentación XML; no hubo errores en la ejecución final. No se ejecutó la solución completa. La primera ejecución detectó una expectativa incorrecta en la prueba nueva sobre los campos de la respuesta aprobada; se ajustó al DTO real y se ejecutaron nuevamente los tres casos con éxito.

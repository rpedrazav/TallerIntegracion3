# S2-040 / TI3-671 — Autorización e idempotencia

Fecha: 2026-10-17. Estado de la tarea: **PARCIAL**.

## Alcance verificable

Las pruebas usan `PaymentGatewayClient` real por HTTP contra un proceso Node
aislado de `Docker/simulator/server.js`, iniciado y cerrado por cada prueba.
No requieren conexión bancaria, Docker ni datos de tarjeta.

| Caso | Comprobación |
| --- | --- |
| Dos solicitudes secuenciales idénticas | Una autorización nueva y una reutilización; respuesta completa, referencia, transacción y fecha iguales. |
| Dos solicitudes simultáneas idénticas | Una autorización nueva y una reutilización, con una sola identidad de transacción. |
| Una clave en dos tenants | El segundo tenant no puede consultar la autorización del primero; autoriza independientemente y cada tenant conserva su resultado al repetir. |
| Una clave reutilizada con otro monto | HTTP 409; la consulta posterior conserva la autorización original. |
| Header y request_id distintos | HTTP 409. |

Las trazas JSON incluyen `idempotency_replay`: `false` en una autorización
nueva y `true` cuando se devuelve una respuesta almacenada (repetición o
consulta de estado). Las pruebas cuentan las dos trazas de aprobación antes
de consultar el estado, filtrando por tenant y clave. No se agregan datos de
tarjeta ni secretos a las trazas.

## Reproducción

Requisitos: .NET 8, Node disponible en PATH y dependencias NuGet restauradas.

```powershell
dotnet test tests/GlobalMart.IntegrationTests/GlobalMart.IntegrationTests.csproj --no-restore -m:1 --filter "Plan=S2-040" --logger "trx;LogFileName=S2-040.trx" --results-directory docs/qa/S2-040
```

## Dependencia que impide cerrar la aceptación completa

`VentasController` y `VentaService.CompletarAsync` rechazan el cobro en efectivo
de una venta TARJETA. Ningún endpoint de cobro invoca `IPaymentGatewayClient`.
Por tanto, estas pruebas verifican **una autorización lógica simulada por
clave y tenant**, pero **no verifican un Pago TARJETA persistido ni constituyen
un flujo de extremo a extremo del POS**.

Para cerrar la tarea falta implementar la orquestación de tarjeta con una
clave persistente y protección ante repetición, y ejecutar dos solicitudes
autenticadas al POS sobre la misma venta. La prueba deberá consultar una base
PostgreSQL aislada y comprobar un solo `Pago` TARJETA con la referencia de la
autorización y el monto de la venta, además de venta completada y un único
evento outbox. Insertar ese Pago desde el propio test ocultaría la dependencia.

La idempotencia del simulador dura únicamente mientras vive su proceso.

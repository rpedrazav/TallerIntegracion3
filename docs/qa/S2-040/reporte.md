# S2-040 / TI3-671 — Secuencia de pago y evento

Fecha: 2026-10-18. Estado: **PARCIAL — implementación terminada, integración pendiente de infraestructura**.

## Alcance implementado

- `Pago.Referencia` conserva la referencia opaca de la autorización con tarjeta.
- `CompletarTarjetaAsync` copia exactamente `IntentoPago.Referencia` al `Pago` creado.
- La aprobación persiste venta `COMPLETADA`, `Pago TARJETA`, `IntentoPago APROBADO` y `sale.completed` pendiente en la transacción existente.
- Rechazo y timeout mantienen la venta `PENDIENTE` y no crean `Pago` ni outbox.
- La respuesta `gateway_reference`, `Pago.Referencia` e `IntentoPago.Referencia` deben ser idénticas.
- La migración `20261018040621_AddPagoGatewayReference` agrega una columna nullable de 100 caracteres y no altera pagos existentes.

## Pruebas

| Caso | Resultado 2026-10-18 |
| --- | --- |
| Contrato de referencia opaca y mapeo EF | **1/1 aprobada** |
| Propagación servicio → Pago/IntentoPago/outbox | **1/1 aprobada** |
| Aprobación: respuesta = Pago = IntentoPago, venta completada y outbox pendiente | Implementada y compilada; [NO VERIFICADA] por PostgreSQL no disponible |
| Rechazo: venta pendiente, cero pagos y cero outbox | Implementada y compilada; [NO VERIFICADA] por PostgreSQL no disponible |
| Timeout: venta pendiente, cero pagos y cero outbox | Implementada y compilada; [NO VERIFICADA] por PostgreSQL no disponible |

EF Core informó `No changes have been made to the model since the last migration`. El build enfocado de MS-5 terminó con 0 errores.

La ejecución integrada falló antes de iniciar cada caso con `Connection refused` en `127.0.0.1:5436`. Se intentó iniciar únicamente `postgres-pos`, pero el usuario del entorno no tuvo permiso sobre `/var/run/docker.sock`. No se leyó ni utilizó `Docker/.env`.

## Reproducción

Con PostgreSQL de MS-5 disponible en el puerto configurado:

```bash
dotnet test tests/GlobalMart.IntegrationTests/GlobalMart.IntegrationTests.csproj \
  --filter 'Plan=S2-040' \
  --logger 'trx;LogFileName=S2-040.trx' \
  --results-directory docs/qa/S2-040 \
  -m:1
```

Evidencia contractual: `docs/qa/S2-040/S2-040-contract.trx`.

## Verificación al integrar S2-035 / S2-049 — 2026-10-18

Las cinco pruebas S2-040 (dos de contrato/servicio y tres HTTP con PostgreSQL) aprobaron dentro de la suite combinada **54/54**, con la migración `AddPagoGatewayReference` aplicada en una base temporal. Queda resuelta la limitación de infraestructura de la ejecución anterior. [Evidencia TRX](../S2-035/merge-pagos.trx) y [detalle de la integración](../S2-035/merge.md).

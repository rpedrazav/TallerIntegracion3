# S2-093 — Reversar tarjeta por pasarela simulada

Fecha: 2026-11-14. Alcance: backend y simulador; sin despliegue remoto ni pantalla nueva.

## Qué se implementó

Una venta TARJETA completada se anula únicamente cuando el simulador confirma la reversa del pago original. Se envía su referencia opaca; el monto, moneda, venta y tenant proceden del servidor. ADMIN y turno original abierto se validan antes de reservar.

`ReversaPago` guarda un único caso por tenant/venta en PostgreSQL: PENDIENTE, APROBADA o RECHAZADA, referencia original y de reversa, importe/moneda, motivo, autorizador y fechas. La reserva se confirma antes de llamar a la pasarela. La confirmación local guarda reversa APROBADA, venta ANULADA y auditoría en una transacción. Los registros del cobro original y el outbox se conservan.

Rechazo: 409 `REVERSA_RECHAZADA`, venta COMPLETADA y caso visible. Timeout o respuesta inválida: 409 `REVERSA_PENDIENTE`. Repetir la petición nunca envía otra reversa. La consulta recupera resultados externos confirmados, incluso tras fallo local de escritura.

## API para integrar al frontend

- `POST /ventas/{id}/anular`, body `{"motivo":"Error en venta"}`: JWT + ADMIN; 200 solo con reversa confirmada, `reembolso_tarjeta: CONFIRMADO_SIMULADO`.
- `GET /ventas/{id}/reversa-tarjeta`: JWT + ADMIN del tenant; estado y `reembolso_confirmado`. Si está pendiente consulta la pasarela y puede reconciliar la base local. No reenvía POST y no se almacena en caché HTTP.
- Ambos admiten prefijo `/api`. No se aceptan importes, identidades, escenarios ni datos bancarios desde el renderer.
- Otro tenant: 404; sin JWT: 401; rol no autorizado: 403.

## Archivos principales

- [Orquestación de reversa](../../../src/POSCartService/Services/ReversaTarjetaService.cs): reserva, precondiciones, incertidumbre y confirmación atómica.
- [Contrato gateway](../../../src/POSCartService/Services/IReversalGatewayClient.cs), [cliente HTTP](../../../src/POSCartService/Services/ReversalGatewayClient.cs) y [DTOs](../../../src/POSCartService/DTOs/ReversalDTOs.cs).
- [Modelo persistente](../../../src/POSCartService/Models/ReversaPago.cs) y [migración](../../../src/POSCartService/Data/Migrations/20261114165151_AddReversasPago.cs).
- [Controller](../../../src/POSCartService/Controllers/VentasController.cs) y [delegación desde VentaService](../../../src/POSCartService/Services/VentaService.cs).
- [Simulador](../../../Docker/simulator/reversals.js), [rutas Kong](../../../Docker/config/kong.yaml).
- [Pruebas de backend](../../../tests/GlobalMart.IntegrationTests/ReversaTarjetaIntegrationTests.cs) y [simulador](../../../Docker/simulator/reversals.test.js).

## Reproducción local

Usar PostgreSQL **de pruebas** y Node disponible. Las pruebas de backend inician el simulador en un puerto libre; generan un cobro aprobado real en ese simulador antes de solicitar la reversa. No requieren Kafka: outbox deshabilitado para publicación, conservando persistencia.

```bash
GLOBALMART_TEST_POSTGRES='Host=127.0.0.1;Port=55493;Database=s2093;Username=postgres;Password=s2093' ConnectionStrings__DefaultConnection='Host=127.0.0.1;Port=55493;Database=s2093;Username=postgres;Password=s2093' dotnet test tests/GlobalMart.IntegrationTests --configuration Release --filter 'FullyQualifiedName~ReversaTarjetaIntegrationTests|FullyQualifiedName~AnulacionVentaIntegrationTests|FullyQualifiedName~PaymentGatewayClientTests|FullyQualifiedName~PaymentSimulatorIntegrationTests|FullyQualifiedName~CobroSeguroIntegrationTests'
node --test Docker/simulator/reversals.test.js
dotnet ef migrations has-pending-model-changes --project src/POSCartService --configuration Release --no-build
```

Se utilizó un contenedor PostgreSQL 16 temporal, puerto 55493, separado de la base de desarrollo. Las credenciales del ejemplo son exclusivamente de ese contenedor temporal.

## Verificación ejecutada

| Suite | Resultado |
|---|---|
| `AnulacionVentaIntegrationTests` | 38/38 aprobadas |
| `CobroSeguroIntegrationTests` | 25/25 aprobadas |
| `PaymentGatewayClientTests` | 5/5 aprobadas |
| `PaymentSimulatorIntegrationTests` | 8/8 aprobadas |
| `ReversaTarjetaIntegrationTests` | 17/17 aprobadas |
| Simulador Node `reversals.test.js` | 4/4 aprobadas |

**Total: 93 pruebas .NET y 4 pruebas Node aprobadas, sin omisiones.** Build Release completado. Migración aplicada a PostgreSQL 16 de pruebas y `has-pending-model-changes` sin diferencias. `git diff --check` y enlaces locales del grafo verificados. Existen advertencias previas de documentación XML y analizadores de otras pruebas; no impidieron compilar.

Cobertura S2-093: aprobación, rechazo visible, repetición, concurrencia, timeout recuperable, pasarela desconectada, identidad/referencia/monto/estado de respuesta alterados, turno cerrado, referencia ausente, intento incierto, JWT/ADMIN/tenant y rollback después de escribir pero antes del commit. El fallo de persistencia deja la reserva PENDIENTE y la consulta recupera la aprobación sin otra reversa.

## Límites y despliegue

- Simulación, no devolución bancaria real. Simulador conserva autorizaciones/reversas en memoria: reiniciarlo pierde resultados. MS-5 conserva su reserva y no reenvía una devolución incierta; un caso sin respuesta disponible requiere revisión manual.
- No hay reintento de reversa rechazada ni devoluciones parciales o mixtas. El caso rechazado se conserva consultable.
- Sin reposición de inventario, reversión fiscal o evento de reversión. No se afirma efecto exactamente una vez entre servicios.
- El turno debe estar abierto al reservar. Si cierra mientras llega la respuesta, se registra igualmente el resultado externo confirmado; ajustes de caja cerrada no se implementan aquí.
- El cuadre de efectivo existente no se modifica ni se certifica en esta tarea.
- Requiere publicar MS-5 con migración `AddReversasPago`, desplegar el directorio completo del simulador (incluido `reversals.js`) y actualizar Kong. MS-5 ejecuta migraciones al iniciar. `PasarelaPago:BaseUrl` debe apuntar al simulador/Gateway real del entorno.
- `PasarelaPago:ReversalScenarioId`: `sim-reversal-approved-v1`, `sim-reversal-declined-v1`, `sim-reversal-timeout-v1`; configuración exclusiva del backend. Timeout HTTP menor a 35 segundos para el escenario incierto (predeterminado 30).
- UI de anulación, configuración efectiva y ejecución en cluster universitario: **[NO VERIFICADO]**.

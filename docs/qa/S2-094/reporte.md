# S2-094 — Publicar sale.reversed por outbox

Fecha: 2026-11-14. Implementado en MS-5; pruebas locales con PostgreSQL 16 temporal y simulador Node real para tarjeta.

## Explicación de la tarea

Al anular una venta, los demás servicios necesitan recibir un aviso durable. El outbox es una tabla que guarda ese aviso junto con la anulación; un worker lo envía después a Kafka. Si el envío falla, se reintenta el aviso guardado, con la misma identidad, en lugar de inventar otra anulación.

## Implementación

- [SaleReversedEvent](../../../src/POSCartService/Messaging/SaleReversedEvent.cs) define payload versión 1 y su snapshot outbox: `event_id`, tenant, venta original, anulación, sucursal, autorizador, motivo, método de pago, ítems, importes originales y fecha.
- `event_id = Anulacion.Id`: identifica la única anulación confirmada. `venta_id` la relaciona con la venta original; no se modifica el evento histórico `sale.completed`.
- [VentaRepository.AnularTotalAsync](../../../src/POSCartService/Repositories/VentaRepository.cs) guarda ANULADA, auditoría y outbox juntos para efectivo.
- [ReversaTarjetaService.ConfirmarAsync](../../../src/POSCartService/Services/ReversaTarjetaService.cs) guarda reversa APROBADA, ANULADA, auditoría y outbox juntos cuando la pasarela responde REVERSED validado. Funciona también al recuperar pendientes por GET. Rechazo/timeout no crean `sale.reversed`.
- Se reutiliza [OutboxPublisherService](../../../src/POSCartService/Services/OutboxPublisherService.cs): publica topic `sale.reversed`, clave tenant, JSON guardado y mismos identificadores en reintentos. No hay llamada Kafka dentro de la anulación.
- El índice único existente `(TenantId, VentaId, EventType)` y los locks/guardias de estado impiden eventos lógicos duplicados. No se necesita nueva migración.

## Pruebas y resultados

**60/60 pruebas aprobadas, cero fallos y cero omisiones**, compilación Release exitosa:

| Suite | Casos | Evidencia |
|---|---:|---|
| AnulacionVentaIntegrationTests | 38 | Efectivo, identidad de evento, permisos/tenant, repetición, concurrencia, snapshot intacto ante rechazo y rollback tras escritura |
| ReversaTarjetaIntegrationTests | 17 | Evento solo al aprobar; rechazo/timeout sin evento; recuperación crea uno; concurrencia; fallo local conserva reserva sin evento parcial |
| SaleReversedOutboxIntegrationTests | 1 | Anulación real en PostgreSQL, payload de pesable, venta original, auditoría, publicación fallida y recuperación con mismo EventId y JSON |
| OutboxPublisherWorkerTests | 2 | Regresión de worker, reintento y cancelación |
| VentaOutboxTransactionTests | 2 | Regresión de atomicidad de cobro/outbox |

El test nuevo usa repositorio y publisher de producción, PostgreSQL migrado y contextos nuevos entre intentos. El productor Kafka es un doble controlado que captura el envío y falla una vez: simula incertidumbre de entrega/ACK, verificando dos intentos físicos con la misma identidad lógica. **No es una prueba contra broker Kafka real.** Las pruebas de tarjeta ejecutan el adaptador HTTP y simulador Node reales.

Las pruebas de rollback existentes fallan después de SaveChanges y antes de commit: verifican que no se publique un evento de una anulación que no quedó confirmada.

```bash
GLOBALMART_TEST_POSTGRES='Host=127.0.0.1;Port=55494;Database=s2094;Username=postgres;Password=s2094' ConnectionStrings__DefaultConnection='Host=127.0.0.1;Port=55494;Database=s2094;Username=postgres;Password=s2094' dotnet test tests/GlobalMart.IntegrationTests --configuration Release --filter 'FullyQualifiedName~SaleReversedOutboxIntegrationTests|FullyQualifiedName~AnulacionVentaIntegrationTests|FullyQualifiedName~ReversaTarjetaIntegrationTests|FullyQualifiedName~OutboxPublisherWorkerTests|FullyQualifiedName~VentaOutboxTransactionTests'
```

La conexión del ejemplo corresponde exclusivamente al contenedor temporal de pruebas; para repetir, aprovisionar una base de pruebas y ajustar ambas variables. Hubo advertencias previas de comentarios XML y analizadores de otras pruebas, sin errores de compilación.

## Límites y despliegue

- Requiere desplegar MS-5 actualizado, habilitar `OutboxPublisher:Enabled` y configurar Kafka accesible. El topic ya existe en la lista de aprovisionamiento Docker; verificar su disponibilidad en el cluster.
- No añade UI, consumidor de reversas, reposición MS-4, reversión fiscal ni puntos. Publicar el aviso no implica que otro servicio ya haya aplicado esos efectos.
- Puede haber entrega repetida con el mismo EventId si Kafka recibió el mensaje pero se perdió su confirmación. No se garantiza exactamente una vez ni orden entre topics distintos.
- No rellena outbox para ventas anuladas antes de desplegar este cambio.
- Broker Kafka real y cluster universitario: **[NO VERIFICADO]**.

## Guion breve para la reunión

“Implementé el evento sale.reversed mediante outbox. Ahora la anulación y su evento se guardan juntos, y el worker lo publica después. Si hay un reintento, conserva el mismo identificador y contenido, por lo que no crea otra anulación lógica. En tarjeta solo se genera cuando la pasarela confirma la reversa. Lo validé con 60 pruebas.”

[Contrato completo y ejemplo JSON](../../context/eventos/sale-reversed-outbox.md).

---
id: sale-reversed-outbox
tipo: evento
titulo: Anulación total por outbox — S2-094
estado: implementado
ultima_revision: 2026-11-14
fuentes: [src/POSCartService/Messaging/SaleReversedEvent.cs, src/POSCartService/Repositories/VentaRepository.cs, src/POSCartService/Services/ReversaTarjetaService.cs, src/POSCartService/Services/OutboxPublisherService.cs]
publica: [sale.reversed]
consume: []
---
# sale.reversed — Anulación total confirmada

MS-5 guarda este evento en `OutboxMessages` al confirmar la anulación total. Efectivo: en la transacción de `VentaRepository.AnularTotalAsync`. Tarjeta: solo con REVERSED validado, en `ReversaTarjetaService.ConfirmarAsync`, tanto desde POST como desde recuperación por GET. Rechazos, timeout y autorizaciones inválidas no crean el evento.

**Atomicidad:** venta ANULADA, auditoría y outbox se confirman juntos; en tarjeta también ReversaPago APROBADA. Si falla una escritura o el commit, no queda un evento parcial publicable. La reserva de tarjeta PENDIENTE previa se conserva para recuperación. No se requiere Kafka disponible durante la anulación.

## Contrato versión 1

- `event_id`: UUID de la auditoría `Anulacion.Id`, estable para la única anulación lógica; distinto del evento original de cobro.
- `schema_version`: 1.
- `tenant_id`, `venta_id`: tenant y venta original. `venta_id` relaciona la anulación con `sale.completed`; no se requiere encontrar ni modificar su outbox histórico.
- `anulacion_id`: mismo UUID de auditoría; coincide con `event_id` por diseño de este contrato.
- `sucursal_id`, `autorizado_por`, `motivo`: datos persistidos de venta y autorización ADMIN.
- `metodo_pago`: EFECTIVO o TARJETA. No significa entrega física de efectivo; tarjeta solo se emite tras reversa confirmada.
- `subtotal`, `iva`, `total`: montos originales positivos; es una anulación total, no un nuevo cálculo ni una devolución parcial.
- `items`: `producto_id`, `cantidad`, `precio_unitario`, snapshots originales. En pesables `cantidad` es el peso facturable en kg validado por el contrato; no se consulta nuevamente el catálogo.
- `timestamp`: fecha UTC de la auditoría, no fecha de cada reenvío.

Ejemplo ilustrativo (identidades no corresponden a datos reales):

```json
{
  "event_id": "26ba7bd4-31e5-4ef6-a313-6f39d2d4517a",
  "schema_version": 1,
  "tenant_id": "30e74f8c-3c48-4f85-85d7-16a9a3da6b3f",
  "venta_id": "f55dbb13-e428-4713-85de-96a42cf5a90c",
  "anulacion_id": "26ba7bd4-31e5-4ef6-a313-6f39d2d4517a",
  "sucursal_id": "c3aef268-b50a-4b8c-941f-992313f7b4e5",
  "autorizado_por": "5b120d79-7640-4c88-aea7-a4b7e63fb9e1",
  "motivo": "Error en venta",
  "metodo_pago": "EFECTIVO",
  "subtotal": 750,
  "iva": 142.5,
  "total": 892.5,
  "items": [{ "producto_id": "8427a276-93b5-4dbe-9cca-674b73c06a2c", "cantidad": 0.750, "precio_unitario": 1000 }],
  "timestamp": "2026-11-14T15:00:00Z"
}
```

## Publicación e idempotencia

Topic y EventType: `sale.reversed`. Clave Kafka: tenant UUID, como `sale.completed`. El worker genérico publica el JSON ya persistido; conserva EventId, payload y timestamp en cada intento. El índice único existente `(TenantId, VentaId, EventType)` impide dos filas lógicas para la misma anulación y permite conservar también `sale.completed`. Locks por venta y guardias de estado evitan duplicados por requests concurrentes.

Repetir POST en efectivo devuelve 409; en tarjeta ya APROBADA devuelve el éxito existente. Ninguno crea otro evento. Recuperar una reversa pendiente de tarjeta crea el evento únicamente al confirmar su resultado.

Si Kafka recibió el mensaje pero se perdió la confirmación, puede recibirlo otra vez con **el mismo `event_id`**. No se promete entrega exactamente una vez. Los futuros consumidores deberán deduplicar y procesar transaccionalmente sus efectos. La clave no garantiza orden entre topics diferentes; consumidores deben contemplar llegada desordenada de cobro y anulación.

## Límites

No añade consumidor MS-4 ni reposición de inventario, reversión fiscal o puntos. El topic ya está en el aprovisionamiento Docker existente. Sin migración adicional: se reutilizan tabla e índices de outbox. No rellena eventos para anulaciones históricas anteriores al despliegue. La entrega contra Kafka real y el despliegue universitario quedan [NO VERIFICADO]; las pruebas ejercitan PostgreSQL y publisher real con transporte Kafka simulado que falla una vez.

[MS-5](../servicios/ms5-pos.md) · [Contrato de anulación](../dominio/anulacion-venta.md) · [Topics](kafka-topics.md) · [Reporte de pruebas](../../qa/S2-094/reporte.md)

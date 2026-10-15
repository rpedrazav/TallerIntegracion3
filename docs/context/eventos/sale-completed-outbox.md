---
id: sale-completed-outbox
tipo: decision
titulo: "S2-006 — Contrato de sale.completed, outbox y reintentos"
estado: planificado
fuentes: [src/POSCartService/Messaging/SaleCompletedEvent.cs, src/POSCartService/Services/VentaService.cs, src/POSCartService/Services/KafkaProducerService.cs, src/POSCartService/Repositories/VentaRepository.cs, src/WarehouseInventoryService/Messaging/KafkaConsumerService.cs, tareas/sprint2_youtrack_resultado.json]
verificado_contra_codigo: false
ultima_revision: 2026-10-14
depende_de: [ms5-pos, kafka-topics]
publica: []
consume: []
reglas: [RN-01, RN-02, RN-06, RN-07]
---
# S2-006 — Contrato de evento y reintento

**Plan:** S2-006 · **YouTrack:** TI3-637 · **Prioridad:** Must · **Dificultad:** Baja.  
**Meta:** Carrito y venta funcional.  
**Decisión:** diseño aprobado por el responsable de la tarea en esta conversación el 2026-10-14.  
**Entrega:** diseño de outbox y política de reintentos documentados. **Implementación: [PLANIFICADO].**

Este documento define el comportamiento futuro. No acredita una implementación, migración ni prueba ejecutada del outbox. `verificado_contra_codigo: false` distingue el diseño aprobado del comportamiento vigente descrito a continuación. S2-006 se limita a documentación nueva; no modifica código, otros nodos del grafo ni archivos compartidos.

## Evidencia actual y discrepancia resuelta en el diseño

- `VentaService.CompletarAsync` persiste venta y pago mediante `VentaRepository.ActualizarAsync` antes de construir y publicar el evento. Una caída entre ambas operaciones puede dejar una venta sin evento.
- `SaleCompletedEvent` inicializa `EventId` con `Guid.NewGuid()` y su comentario describe un GUID nuevo por publicación. `VentaService` también asigna explícitamente un GUID al construirlo. Reutilizar el mismo objeto conserva su ID, pero no hay persistencia durable del evento para recuperarlo tras un reinicio.
- El payload actual no contiene versión. Sus nombres JSON están definidos con `JsonPropertyName` en `snake_case`.
- `KafkaProducerService.PublicarAsync` captura `ProduceException` sin relanzarla; su llamador puede registrar publicación aunque haya fallado. Un publicador de outbox necesitará un resultado inequívoco de éxito o error.
- MS-4 deduplica por `EventId`, con fallback a `VentaId`, pero guarda descuentos antes del marcador y confirma offsets incluso ante errores. No se acredita procesamiento exactamente una vez.

La discrepancia entre «GUID nuevo por publicación» y la aceptación de S2-006 fue presentada y aprobada para resolución documental: **la identidad pertenece al evento lógico, no al intento de transporte**. El código y los documentos anteriores conservan su estado actual hasta una tarea de implementación autorizada.

## Contrato de `sale.completed` versión 1 [PLANIFICADO]

Se conserva el topic `sale.completed`, la clave Kafka `tenant_id` como UUID en texto y los campos existentes. Se agrega únicamente `schema_version` al payload. No se incorporan contadores ni estados de publicación al mensaje de negocio.

| Campo JSON | Tipo | Regla |
|---|---|---|
| `event_id` | UUID no vacío | Se genera una sola vez al preparar el evento que se guardará con el cobro. Es inmutable en todos los intentos y reprocesos. |
| `schema_version` | Entero | Valor `1` para este contrato. Se conserva en cada reenvío. |
| `tenant_id` | UUID no vacío | Tenant de la venta, validado mediante el contexto autenticado; coincide con el registro de outbox y la clave Kafka. |
| `venta_id` | UUID no vacío | Venta completada que originó el evento. |
| `cajero_id` | UUID no vacío | Cajero registrado en la venta. |
| `sucursal_id` | UUID no vacío | Sucursal registrada en la venta. |
| `items` | Lista no vacía | Snapshot de los ítems al completar la venta. |
| `items[].producto_id` | UUID no vacío | Producto vendido. |
| `items[].cantidad` | Decimal positivo | Cantidad efectiva persistida por MS-5; para venta a peso conserva la cantidad resuelta por ese flujo. |
| `items[].precio_unitario` | Decimal | Precio autoritativo persistido en el ítem. |
| `subtotal`, `iva`, `total` | Decimal | Importes persistidos de la venta; `iva` corresponde a `Venta.Impuestos`. No se recalculan al publicar. |
| `metodo_pago` | Texto | Conserva el contrato `EFECTIVO`, `TARJETA`, `MIXTO`; actualmente el cobro implementado solo permite `EFECTIVO`. Esto no habilita pagos por tarjeta. |
| `timestamp` | Fecha ISO 8601 UTC | Instante del hecho de negocio registrado al preparar el cobro, no del reintento. |

**Invariante de aceptación:** para cualquier evento E y sus intentos i y j, `EventId(E,i) = EventId(E,j)`. También permanecen idénticos el JSON almacenado, `schema_version`, `timestamp`, topic y clave Kafka. El publicador lee el snapshot persistido; no crea otro `SaleCompletedEvent` ni regenera GUID, fechas o totales.

La versión 1 conserva nombres y significados existentes. La ausencia de `schema_version` identifica un mensaje legacy no versionado, compatible con los campos anteriores; no autoriza a generar una identidad nueva. Cambios incompatibles requieren una versión nueva y coordinación con consumidores. Una versión desconocida no debe procesarse silenciosamente como versión 1. La adaptación y pruebas de consumidores quedan fuera de S2-006; su compatibilidad ejecutada está [NO VERIFICADO].

## Outbox local a MS-5 [PLANIFICADO]

La tabla propuesta `OutboxMessages` reside en la misma base PostgreSQL y unidad transaccional que `Venta` y `Pago`. El nombre es de diseño, todavía no existe como entidad implementada.

| Dato propuesto | Propósito |
|---|---|
| `EventId` | UUID, clave primaria; coincide con `event_id` del JSON. |
| `TenantId`, `VentaId`, `EventType` | Pertenencia y referencia del evento; `EventType = sale.completed`. |
| `SchemaVersion`, `Topic`, `MessageKey` | Versión y destino inmutables. |
| `Payload` | JSON serializado almacenado como texto, para reenviar el mismo contenido sin reconstrucción. |
| `Status` | `PENDIENTE`, `EN_PROCESO`, `PUBLICADO` o `FALLIDO`. |
| `AttemptCount`, `TotalAttemptCount` | Intentos del ciclo automático actual e intentos acumulados, inicialmente cero. |
| `CreatedAt`, `NextAttemptAt`, `LastAttemptAt`, `PublishedAt` | Fechas UTC; las que todavía no correspondan son nulas. |
| `LeaseToken`, `LockedUntil` | Identificador de reserva y vencimiento UTC para coordinación y recuperación de workers. |
| `LastError` | Código y descripción acotada del último fallo, sin credenciales ni payload completo. |

Restricción única adicional: `(TenantId, VentaId, EventType)`. Una repetición del cobro no debe producir otro `sale.completed` para la misma venta ni otro pago. La transición de venta pendiente a completada y esta restricción deben verificarse dentro de la transacción; la protección concurrente será parte de la implementación futura.

Flujo requerido:

1. Validar el cobro mediante las reglas existentes y preparar el snapshot con un solo `EventId`.
2. Guardar venta completada, pago y outbox `PENDIENTE` en una misma transacción PostgreSQL. Si cualquiera falla, revertir todo. Si la transacción no se confirma, no existe un evento publicable.
3. Confirmar la venta al cliente una vez confirmado ese commit. La disponibilidad de Kafka no determina el éxito del cobro ya persistido.
4. Un publicador en segundo plano reserva un registro elegible, confirma la reserva y envía el JSON almacenado. No mantiene abierta la transacción de negocio durante la llamada a Kafka.
5. Solo tras confirmación satisfactoria del broker marca el registro `PUBLICADO`. Si la publicación falla o su resultado es incierto, aplica la política de reintentos.

**RN-01:** las lecturas y mutaciones de registros se ejecutan con un tenant explícito; las actualizaciones incluyen `TenantId` y `EventId`. El worker debe establecer un contexto de tenant confiable antes de consultar su lote; no usa un tenant recibido de un request arbitrario ni deshabilita filtros globales. El mecanismo de enumeración autorizada de tenants debe concretarse en la tarea del worker. No se autoriza mediante este diseño un endpoint público de reproceso.

## Estados y recuperación [PLANIFICADO]

| Origen | Condición | Destino y acción |
|---|---|---|
| Nuevo | Commit atómico con venta y pago | `PENDIENTE`, elegible inmediatamente. |
| `PENDIENTE` | Fecha elegible y reserva exclusiva obtenida | `EN_PROCESO`; asignar reserva temporal e incrementar contadores antes del envío. |
| `EN_PROCESO` | Broker confirma publicación y worker conserva su reserva | `PUBLICADO`; registrar `PublishedAt` y liberar reserva. |
| `EN_PROCESO` | Error transitorio o resultado incierto; quedan intentos | `PENDIENTE`; guardar error, programar siguiente intento y liberar reserva. |
| `EN_PROCESO` | Se agotaron los intentos o hay error no recuperable automáticamente | `FALLIDO`; conservar evento y diagnóstico, liberar reserva. |
| `EN_PROCESO` | Reserva vencida tras caída o interrupción | Recuperar mediante actualización condicional; volver a `PENDIENTE` con espera o a `FALLIDO` si se alcanzó el límite. |
| `FALLIDO` | Reproceso operativo autorizado tras revisar la causa | `PENDIENTE`; iniciar nuevo ciclo automático y registrar la acción, preservando identidad, contenido y contador total. |

`PUBLICADO` es terminal para el publicador normal: significa recepción confirmada por Kafka, no procesamiento confirmado por inventario. No se eliminan registros pendientes o fallidos automáticamente. La retención de publicados requiere una decisión posterior.

La reserva debe ser atómica y su duración configurable, superior al timeout de publicación o renovada mientras se publica. Toda finalización compara `TenantId`, `EventId`, estado y `LeaseToken`; un worker con reserva vencida no puede sobrescribir el resultado del nuevo propietario. La recuperación no incrementa contadores por sí sola; el siguiente intento reservado sí los incrementa. Una interrupción después de reservar consume conservadoramente un intento aunque no pueda saberse si llegó a enviarse.

## Política de reintentos aprobada [PLANIFICADO]

- **Límite configurable:** 10 intentos totales por ciclo, incluidos el inicial y hasta 9 reintentos. Un intento de outbox es una llamada de publicación con resultado o timeout; los reintentos internos del cliente Kafka no son nuevas filas ni nuevos eventos.
- **Primer intento:** inmediato una vez visible el commit y disponible el worker.
- **Espera:** después del intento fallido n, con n empezando en 1, calcular `base_n = min(300 s, 5 s × 2^(n-1))` y esperar un valor aleatorio uniforme entre `base_n / 2` y `base_n`. Bases: 5, 10, 20, 40, 80, 160, 300, 300 y 300 segundos.
- Persistir `NextAttemptAt` al programar la espera. Un reinicio no reinicia contadores ni vuelve a generar la espera ya almacenada.
- Reintentar fallos transitorios de conexión, indisponibilidad o timeout, incluidos resultados inciertos. Errores de payload inválido, autorización o configuración que requieren intervención pasan a `FALLIDO` con diagnóstico. La clasificación concreta de códigos Kafka se verificará al implementar.
- Al llegar al límite, mantener `FALLIDO` visible para revisión operativa. El reproceso manual conserva el `EventId`, payload, topic, clave y versión; reinicia solo `AttemptCount`, mantiene `TotalAttemptCount` y deja registro de quién lo solicitó y cuándo.
- La cancelación por apagado no equivale a éxito. Si no se puede guardar su resultado, la reserva vence y se recupera como publicación incierta.
- No emitir un log de éxito ni marcar `PUBLICADO` cuando el productor devuelve un fallo. El contrato del adaptador Kafka deberá propagar el resultado real.

Estos tiempos son valores iniciales aprobados para el proyecto, no mediciones de producción. El máximo de intentos evita reintentos automáticos indefinidos; la entrega eventual de un evento `FALLIDO` depende de intervención y reproceso.

## Garantía y dependencia de MS-4

La estrategia admite entrega **al menos una vez**, con duplicados posibles. Si Kafka recibió el mensaje y el proceso cae antes de marcar `PUBLICADO`, el evento debe reenviarse con la misma identidad. La clave de particionado por tenant no garantiza por sí sola el orden cronológico de negocio cuando intervienen varios workers o reintentos.

Para que esos duplicados no descuenten stock dos veces, MS-4 necesita deduplicación durable y atómica: comprobar/registrar la identidad y efectuar todos los descuentos en la misma transacción, con unicidad y aislamiento por tenant. También debe resolver su política de offsets ante fallos. Esto es una **dependencia futura en otro microservicio**, no un cambio autorizado de código dentro de S2-006. El outbox de MS-5 no corrige por sí solo las garantías del consumidor actual.

## Escenarios verificables de aceptación

Son criterios para la implementación posterior; **ejecución de estos escenarios: [NO VERIFICADO]**. S2-006 entrega su definición documental.

| Escenario | Resultado esperado y evidencia requerida |
|---|---|
| Kafka falla en los primeros intentos y luego responde | Una fila outbox; capturas de todos los envíos con igual `event_id`, versión, JSON y clave; finalmente `PUBLICADO`. |
| Reinicio entre intentos | Se recuperan el evento, contador y `NextAttemptAt` persistidos; el siguiente envío conserva ID y contenido. |
| Kafka recibe, pero se pierde la confirmación | Se permite una nueva entrega con el mismo ID; nunca se fabrica un evento nuevo. |
| Reinicio después del commit del cobro y antes de publicar | Venta y pago existen junto al outbox recuperable; el worker puede enviarlo sin repetir el cobro. |
| Falla la inserción del outbox | Se revierte también el pago y la transición de la venta; no queda cobro confirmado sin registro del evento. |
| Dos cobros simultáneos de la misma venta | Solo un pago de ese cobro y un evento lógico; la restricción única impide dos filas `sale.completed`. |
| Dos workers o reserva vencida | Solo una reserva vigente; el worker antiguo no puede finalizar con un token obsoleto. Cualquier reentrega conserva identidad. |
| Décimo intento falla | Estado `FALLIDO`, evento y error conservados; no hay intento automático 11 dentro del mismo ciclo. |
| Reproceso autorizado de un fallido | Mismo ID y payload, nuevo ciclo de intentos, contador total y trazabilidad conservados. |
| Aislamiento de tenants | Consulta o mutación con contexto A no accede ni modifica filas de B, incluso al reservar, recuperar o reprocesar. |
| Duplicado recibido en MS-4 — dependencia futura | Un solo descuento por evento; stock y marcador se confirman o revierten juntos. |

## Fuentes y conexiones

- [Estado actual](../estado-actual.md), [MS-5 POS](../servicios/ms5-pos.md) y [topics Kafka](kafka-topics.md): contexto previo; este diseño no declara actualizado su comportamiento implementado.
- [Contrato vigente de MS-5](../../../src/POSCartService/Messaging/SaleCompletedEvent.cs).
- [Completar venta](../../../src/POSCartService/Services/VentaService.cs) y [persistencia de venta](../../../src/POSCartService/Repositories/VentaRepository.cs).
- [Publicador Kafka vigente](../../../src/POSCartService/Services/KafkaProducerService.cs).
- [Consumidor de MS-4](../../../src/WarehouseInventoryService/Messaging/KafkaConsumerService.cs) y [persistencia de stock](../../../src/WarehouseInventoryService/Repositories/StockRepository.cs).
- [Correspondencia S2-006 / TI3-637](../../../tareas/sprint2_youtrack_resultado.json).
- [Transactional Outbox — Microsoft](https://learn.microsoft.com/en-us/azure/architecture/databases/guide/transactional-out-box-cosmos): referencia conceptual del guardado atómico y publicación posterior; no propone incorporar Cosmos DB.
- [Control y límites de reintentos — AWS](https://docs.aws.amazon.com/wellarchitected/latest/framework/rel_mitigate_interaction_failure_limit_retries.html): referencia sobre backoff, jitter y límites; los parámetros concretos de este documento son decisiones del proyecto.

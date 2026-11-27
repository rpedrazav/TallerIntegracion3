# S3-100 — Descontar stock agregado cuando el producto no tiene lotes

Fecha: 2026-11-27. Rama: `dev`. Base: `6182d7f`. Meta: Funcionalidad de punta a punta.

## Objetivo y aceptación

Descontar el stock agregado y registrar el movimiento sin lote cuando un producto no tiene lotes.

Aceptación (tareas/tareas_semana12_youtrack.csv): «Venta de un producto sin lotes con stock suficiente descuenta el agregado y confirma el evento; con stock insuficiente sigue reintentando sin descuentos parciales».

## Cambio

`StockRepository.Descontar` consulta los lotes del producto/tenant/sucursal, incluidos agotados. Solo exige cantidad suficiente en lotes cuando existen. Si no hay ninguno, descuenta del agregado validado previamente. La misma persistencia de `SaleEventProcessor` guarda stock, movimiento VENTA y EventId; no se añade otro SaveChanges ni se crean lotes artificiales.

Los lotes agotados cuentan como existentes: no permiten eludir FEFO. Los lotes de otro tenant o sucursal no interfieren. Si existen lotes pero su suma no alcanza, se mantiene el rechazo anterior.

Flujo del tramo afectado, antes y después: `KafkaConsumerService → SaleEventProcessor → StockRepository → WarehouseDbContext` (4 pasos, sin capas nuevas). El consumidor crea un scope por intento y confirma Processed/Duplicate; TransientFailure mantiene su política de reintento.

## Qué se probó

Suite MS4_Warehouse completa del proyecto unitario, con 7 casos nuevos en `StockSinLotesTests`:

- Stock 10 sin lotes; cantidades 2, 10 y 0.75 dejan 8, 0 y 9.25. Verificación desde un contexto nuevo de movimiento, EventId y ausencia de lotes artificiales. Repetir el mismo payload devuelve Duplicate sin otro descuento.
- Primer ítem suficiente y segundo insuficiente: no persiste ningún descuento, movimiento ni EventId. Reponer el segundo y reintentar con contexto nuevo aplica ambos una vez.
- Lotes y stock de otra sucursal o tenant quedan intactos y no impiden el descuento propio sin lotes (2 casos).
- Lote propio agotado con agregado suficiente mantiene TransientFailure sin cambios.

La regresión existente conserva FEFO entre dos lotes, rechazo por lotes insuficientes, stock insuficiente, política de confirmación del consumidor y pruebas de recepción/consulta.

## Cómo reproducir

Desde la raíz:

```bash
dotnet test tests/GlobalMart.Tests/GlobalMart.Tests.csproj --no-restore --filter FullyQualifiedName~MS4_Warehouse --logger 'console;verbosity=quiet' --logger 'trx;LogFileName=resultado.trx' --results-directory docs/qa/S3-100 -v quiet
```

## Resultado

- Línea base: 45/45 aprobadas, 0 omitidas, exit code 0 (`base.trx`).
- Después: 52/52 aprobadas, 0 omitidas, exit code 0 (`resultado.trx`).
- Duración informada de ejecución de pruebas: 670 ms base, 627 ms después (no incluye compilación).
- El primer intento dentro del sandbox falló antes de ejecutar pruebas por permisos de canales locales de MSBuild. La ejecución autorizada fuera del sandbox terminó correctamente.

## Límites

Procesador y repositorio reales; almacenamiento EF InMemory. No se simuló el repositorio en los siete casos nuevos. Esto verifica decisiones y persistencia lógica, no transacciones/concurrencia de PostgreSQL ni entrega efectiva de Kafka. No se ejecutó la batería completa de otros servicios ni se cambió el frontend.

Clúster, broker y PostgreSQL para esta modificación: [NO VERIFICADOS]. El evento histórico sin stock real observado en S3-099 seguirá pendiente: S3-100 no debe confirmar eventos sin disponibilidad. No se tocaron offsets ni se limpiaron datos. El transporte sin Kafka corresponde a S3-101.

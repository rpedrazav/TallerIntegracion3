# S2-035 — Cobro seguro e idempotente

Fecha: 2026-10-18. Alcance: backend MS-5 con pasarela simulada, sin tarjetas ni dinero reales.

## Qué se probó

`CobroSeguroIntegrationTests` recorre HTTP de MS-5, validación JWT real, servicios/repositorios, PostgreSQL con migraciones reales, `PaymentGatewayClient` y un proceso Node real (`Docker/simulator/server.js`). Cada caso crea un esquema PostgreSQL exclusivo y un simulador en puerto libre; ambos se eliminan al terminar. Requiere explícitamente `GLOBALMART_TEST_POSTGRES` y nunca toma por defecto una base de desarrollo.

El handler de pruebas observa los POST/GET reales. En concurrencia retiene la primera respuesta hasta terminar la segunda solicitud; en pérdida de respuesta la retiene hasta que vence el cliente. No inventa aprobaciones. El caso de rollback induce un error PostgreSQL mediante un trigger exclusivo del esquema de prueba. La publicación Kafka queda deshabilitada: se verifica el evento persistido en outbox, no entrega al broker. Se preparan turno y venta en DB; no es una prueba de Electron ni de creación del carrito vía catálogo/impuestos.

## Diagnóstico previo

- [baseline.trx](baseline.trx): **21/21 aprobadas** en las suites existentes de endpoint, simulador y reintento seguro.
- [antes.trx](antes.trx): **5/12 aprobadas, 7 fallidas** en la primera versión de la suite conjunta, antes de corregir producción. Las primeras ejecuciones tuvieron un problema de configuración del historial de migraciones; este archivo contiene la ejecución posterior con ese problema resuelto.

| Escenario | Antes de la corrección |
|---|---|
| Aprobación real | Pasa: completa venta y registra pago/outbox. |
| Rechazo real | Pasa: conserva venta pendiente sin pago/outbox. |
| Timeout real sin resultado consultable | Pasa: 504 y venta pendiente. |
| Repetición tras aprobación | Pasa: 409 por venta completada, sin otra autorización. |
| Misma clave en tenants distintos | Pasa: operaciones independientes. |
| Repetición tras rechazo o timeout | Falla: nueva llamada a pasarela y violación del índice único local. |
| Dos solicitudes simultáneas | Falla: no existe reserva previa que impida la segunda autorización. |
| Clave nueva después de timeout incierto | Falla: se permite autorizar otra vez y completar la venta. |
| Clave utilizada en otra venta del mismo tenant | Falla: se contacta la pasarela y el registro local colisiona. |
| Clave inválida o UUID vacío | Falla: texto inválido se reemplaza por un UUID nuevo; UUID vacío llega al adaptador y termina como error de comunicación. |

## Corrección

1. Se reserva `IntentoPago=PENDIENTE` **antes** de contactar la pasarela y se confirma esa escritura.
2. La reserva toma brevemente un lock PostgreSQL `FOR UPDATE` sobre la venta del tenant. Revalida estado/monto y bloquea claves repetidas y ventas con intentos no rechazados. No mantiene una transacción abierta durante HTTP. El índice único resuelve también carreras entre ventas distintas con la misma clave del tenant.
3. Aprobación, pago, venta y outbox se confirman juntos, actualizando la reserva existente. Ante error local, la reserva sobrevive y evita una autorización adicional.
4. Rechazo/timeout actualizan esa misma reserva. Solo `DECLINED` es rechazo definitivo; otros estados desconocidos quedan `FALLIDO` y no completan la venta.
5. Clave inválida, UUID vacío o desacuerdo entre header/body retorna 400 antes de contactar la pasarela.

### Contrato de repetición

La repetición bloqueada retorna **409**, sin un nuevo POST a la pasarela; no reproduce el HTTP 200/422 inicial. Esto mantiene el contrato previo de venta completada → 409. Un rechazo definitivo admite un intento nuevo con otra clave. Una venta con intento `PENDIENTE` o `FALLIDO` bloquea también claves nuevas: un error de comunicación no prueba ausencia de cargo.

El adaptador mantiene la consulta inmediata tras timeout. Si esa consulta confirma `APPROVED`, puede completar la venta sin reautorizar. Si no confirma, la venta queda pendiente. La reconciliación posterior de intentos inciertos o de una aprobación externa cuyo commit local falló **no está implementada**; esta tarea prueba el bloqueo seguro, no una recuperación administrativa completa. La idempotencia del simulador vive en memoria; no se acredita persistencia bancaria ni durabilidad de su almacén tras reiniciarlo.

## Validación posterior

[S2-035.trx](S2-035.trx): **17/17 aprobadas**, sin fallos ni omisiones, en Release. Incluye:

- Aprobado, rechazado y timeout: 3 casos.
- Misma clave repetida después de cada resultado: 3 casos.
- Solicitudes simultáneas, con la misma clave y con claves diferentes: 2 casos.
- Clave nueva tras incertidumbre, misma clave en otra venta y misma clave entre tenants: 3 casos.
- Clave inválida y UUID vacío: 2 casos.
- Nuevo intento tras rechazo, aprobación recuperada mediante GET, rollback tras autorización y clave por encabezado/conflicto con cuerpo: 4 casos.

Todos los casos originales fallidos quedan cubiertos y pasan. El resultado de concurrencia requiere un solo POST externo; el simulador conserva la misma transacción en la repetición. Se comprueban estado de venta, cantidad de pagos/intentos/outbox y referencias de autorización.

[regresion.trx](regresion.trx): **35/35 aprobadas**, sin fallos ni omisiones. Cubre las suites existentes de cobro con tarjeta, contrato/adaptador HTTP, simulador, política de timeout, persistencia de intentos, cobro en efectivo y transacción de venta/outbox. Compilación Release aprobada como parte de `dotnet test`; no se ejecutó la solución completa.

## Reproducción

Requisitos: .NET 8, Node y PostgreSQL 16. Ejemplo con contenedor temporal:

```bash
docker run --rm -d --name globalmart-s2035-postgres \
  -e POSTGRES_PASSWORD=s2035_test -e POSTGRES_DB=s2035 \
  -p 127.0.0.1:55435:5432 postgres:16

# Esperar a que pg_isready indique que acepta conexiones.
docker exec globalmart-s2035-postgres pg_isready -U postgres -d s2035

export GLOBALMART_TEST_POSTGRES='Host=127.0.0.1;Port=55435;Database=s2035;Username=postgres;Password=s2035_test'
dotnet test tests/GlobalMart.IntegrationTests --configuration Release \
  --filter 'Plan=S2-035' --logger 'trx;LogFileName=S2-035.trx' \
  --results-directory docs/qa/S2-035

docker stop globalmart-s2035-postgres
```

La contraseña del ejemplo es desechable y exclusiva de pruebas. Para las suites antiguas de cobro, definir también `ConnectionStrings__DefaultConnection` con la misma base **temporal**; esas suites no crean esquemas exclusivos.

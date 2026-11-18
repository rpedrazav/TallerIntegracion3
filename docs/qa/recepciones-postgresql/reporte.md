# Pruebas de recepciones con PostgreSQL

Fecha: 2026-11-18. ID: no informado en la tarea. Meta: Microservicios (FEFO y MS-1).

## Objetivo y criterio de aceptación

- Objetivo: cubrir recepción válida, inválida y de otro tenant con pruebas de integración.
- Aceptación: las pruebas pasan y una recepción de otro tenant no es visible ni modificable.

## Qué se probó

- Una recepción válida responde 201, crea un lote, suma stock y registra un movimiento `RECEPCION`.
- Una recepción con cantidad cero responde 400 y conserva stock, lotes y movimientos.
- Otro tenant recibe 404 al intentar registrar la misma recepción, obtiene una lista de stock vacía y no modifica los datos del dueño.
- Las migraciones reales de MS-4 se aplican en un esquema PostgreSQL aislado, eliminado al finalizar.

## Cómo reproducir

```bash
docker run -d --rm --name globalmart-recepciones-pg \
  -e POSTGRES_PASSWORD=recepciones_test \
  -p 127.0.0.1:55432:5432 postgres:16-alpine
until docker exec globalmart-recepciones-pg pg_isready -U postgres; do sleep 1; done

GLOBALMART_TEST_POSTGRES="Host=127.0.0.1;Port=55432;Database=postgres;Username=postgres;Password=recepciones_test" \
dotnet test tests/GlobalMart.IntegrationTests/GlobalMart.IntegrationTests.csproj \
  --filter 'FullyQualifiedName~RecepcionesPostgresIntegrationTests' \
  --results-directory docs/qa/recepciones-postgresql \
  --logger 'trx;LogFileName=resultados.trx'

docker stop globalmart-recepciones-pg
```

## Resultado

- `RecepcionesPostgresIntegrationTests`: 3/3 aprobadas, 0 fallidas, exit code 0.
- Artefacto: `docs/qa/recepciones-postgresql/resultados.trx`.

## Límites

- PostgreSQL 16, las migraciones, MS-4, JWT y las solicitudes HTTP son reales.
- MS-1 y MS-3 se simulan con un handler que filtra producto y sucursal según el `tenant_id` del JWT reenviado.
- Kafka se elimina de la fixture porque no participa en recepciones.
- Despliegue remoto y servicios MS-1/MS-3 reales [NO VERIFICADOS].

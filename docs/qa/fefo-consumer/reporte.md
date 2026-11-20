# Pruebas de integración del descuento FEFO

Fecha: 2026-11-20. Meta: Microservicios (FEFO y MS-1).

## Objetivo y criterio de aceptación

- Objetivo: cubrir orden de vencimiento, lotes insuficientes y evento duplicado.
- Aceptación: un evento duplicado no descuenta dos veces.

## Qué se probó

- Una venta de 8 unidades sobre dos lotes de 5 agota primero el lote con vencimiento más próximo y deja 2 unidades en el posterior.
- Lotes insuficientes en el segundo ítem no persisten el descuento del primero; al reponer el lote, el mismo evento se recupera completo.
- Publicar dos veces el mismo `EventId` deja un único descuento de stock y lote, un movimiento y un registro de idempotencia.

## Cómo reproducir

```bash
DB_USER=admin DB_PASSWORD=admin_password docker compose -f Docker/docker-compose.yml up -d postgres-warehouse zookeeper kafka kafka-init-topics
mkdir -p docs/qa/fefo-consumer
dotnet test tests/GlobalMart.IntegrationTests/GlobalMart.IntegrationTests.csproj --filter "Feature=FEFO" --logger "trx;LogFileName=fefo-integration.trx" --results-directory docs/qa/fefo-consumer --no-restore
```

## Resultado

- Integración FEFO: 3/3 aprobadas, 0 fallidas, exit code 0.
- Artefacto: `docs/qa/fefo-consumer/fefo-integration.trx`.

## Límites

- PostgreSQL 16 y Kafka 7.6.1 fueron servicios Docker reales; MS-4 se ejecutó en proceso mediante `WebApplicationFactory`.
- Se usó un topic y grupo de consumidor únicos para la corrida.
- El clúster remoto queda [NO VERIFICADO] porque no dispone de Kafka.

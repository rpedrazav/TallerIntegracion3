# S2-100 / TI3-731 — Probar duplicados y fallo intermedio

Fecha: 2026-11-17. Meta: Carrito y venta funcional.

## Objetivo y criterio de aceptación

- Objetivo: publicar eventos duplicados y provocar un fallo a mitad de los ítems.
- Aceptación: se descuenta una vez y no queda stock parcialmente descontado.

## Qué se probó

- Dos mensajes Kafka con el mismo `EventId`: el consumidor confirmó el segundo como duplicado, descontó stock una vez, registró un movimiento y conservó una sola fila de idempotencia.
- Evento de dos ítems con stock insuficiente en el segundo: durante los reintentos ambos stocks permanecieron intactos, sin movimientos ni `EventId`.
- Recuperación del fallo intermedio: tras reponer el segundo producto, el mismo offset se reprocesó y los dos descuentos se confirmaron juntos.
- La fixture usa un topic Kafka único por corrida para no consumir mensajes históricos.

## Cómo reproducir

```bash
DB_USER=admin DB_PASSWORD=admin_password docker compose -f Docker/docker-compose.yml up -d postgres-warehouse zookeeper kafka
dotnet test tests/GlobalMart.IntegrationTests/GlobalMart.IntegrationTests.csproj --no-restore --filter "Plan=S2-100" --logger "trx;LogFileName=$PWD/docs/qa/S2-100/resultados.trx" --verbosity minimal
DB_USER=admin DB_PASSWORD=admin_password docker compose -f Docker/docker-compose.yml stop kafka zookeeper postgres-warehouse
```

Rama: `local`.

## Resultado

- `StockConsumerIntegrationTests` S2-100: 2/2 aprobadas, 0 omitidas, exit code 0.
- Artefacto: `docs/qa/S2-100/resultados.trx`.

## Límites

- PostgreSQL y Kafka fueron contenedores locales reales; los eventos y datos fueron exclusivos de la prueba.
- No se probó el clúster remoto, donde Kafka continúa [NO VERIFICADO].
- La espera asíncrona tiene timeout de 15 segundos; no es una prueba de carga ni de rendimiento.

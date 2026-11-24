# S3-040 / TI3-791 — Revisar ramas muertas del outbox de MS-5

Fecha: 2026-11-24. Meta: Carrito y venta funcional.

## Objetivo y criterio de aceptación
- Objetivo: Eliminar o simplificar el código del publicador de outbox que no se ejecuta en ninguna configuración actual.
- Aceptación: La suite S2-007..S2-010 pasa. Se documenta la separación entre lo implementado y lo futuro (reproceso manual, deduplicación en MS-4).
- Simplificación: Los archivos tocados quedan sin código muerto ni duplicado, y el flujo se puede seguir desde el botón hasta la BD sin desvíos.

## Qué se probó
- Inspección estática del publicador outbox (`OutboxPublisherWorker.cs`, `OutboxPublisherService.cs`, `OutboxRepository.cs`): flujo lineal sin bifurcaciones muertas ni dependencias residuales.
- Ejecución completa de la suite de integración de outbox (S2-007, S2-008, S2-009, S2-010):
  - S2-007: Migración, unicidad y aislamiento multi-tenant de `OutboxMessages`.
  - S2-008: Commit atómico de venta, pago y outbox en PostgreSQL con rollback en conflicto.
  - S2-009: Reserva atómica de lease, publicación controlada, backoff exponencial con jitter y cancelación limpia.
  - S2-010: Resiliencia ante caída y recuperación de broker Kafka con entrega única final sin duplicar IDs.

## Cómo reproducir
```bash
GLOBALMART_TEST_POSTGRES="Host=127.0.0.1;Port=5436;Database=pos_db;Username=admin;Password=admin_password" \
ConnectionStrings__DefaultConnection="Host=127.0.0.1;Port=5436;Database=pos_db;Username=admin;Password=admin_password" \
dotnet test tests/GlobalMart.IntegrationTests/GlobalMart.IntegrationTests.csproj \
  --no-restore -m:1 \
  --filter 'Plan=S2-007|Plan=S2-008|Plan=S2-009|Plan=S2-010'
```

## Resultado
- Suite de outbox (S2-007..S2-010): 6/6 aprobadas, exit code 0.
- Artefactos: `S3-040.trx` en esta misma carpeta.

## Límites
- PostgreSQL 16 local en contenedor Docker (`postgres-pos`, puerto 5436).
- Productor Kafka controlado mediante test fixture determinista (`FailOnceProducer`). Broker Kafka real en clúster y deduplicación en MS-4 continúan como [NO VERIFICADO] / [PLANIFICADO].

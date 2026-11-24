# S3-056 — E2E de recepción y FEFO en docker compose

Fecha: 2026-11-24. Rama: `dev`. Meta: Q — Calidad y regresión. Prioridad MoSCoW: Must.

**Aceptación:** el lote con vencimiento más próximo baja primero. ✅ Cumplida contra PostgreSQL y Kafka reales.

## Qué hace la prueba

[`scripts/e2e-fefo.mjs`](../../../scripts/e2e-fefo.mjs) (Node, sin dependencias, solo HTTP) contra MS-1, MS-3, MS-4 y MS-5 en localhost:

1. ADMIN crea un producto nuevo (ningún otro lote puede vencer antes) y toma la primera sucursal del tenant, o la crea.
2. `POST /recepciones` de dos lotes de 5 unidades: primero el que vence en +60 días y luego el de +10 días, para que el orden de llegada no coincida con FEFO.
3. CAJERO cierra cualquier turno previo, abre uno en esa sucursal y vende 3 unidades en efectivo. La venta queda COMPLETADA.
4. El outbox de MS-5 publica `sale.completed` en Kafka y MS-4 lo consume. La prueba espera hasta 60 s.
5. Comprueba con `GET /lotes?porVencer=365`: el lote temprano baja de 5 a 2, el tardío sigue en 5, y `GET /stock/{id}` da 7.

Flujo: `POST /recepciones → RecepcionService → PostgreSQL` y `POST /api/ventas/{id}/cobrar → outbox → Kafka → SaleEventProcessor → StockRepository (FEFO)`.

## Resultado

Corrida final, exit 0:

```
[1] Lotes recibidos: tardío 145c2831-… (+60 d), temprano 2bcdad3e-… (+10 d)
[2] Venta bbfe1338-… COMPLETADA (3 unidades, total 3570)
[3] OK: temprano 5→2, tardío 5→5, stock agregado 7. FEFO verificado de punta a punta.
```

Entorno: `docker compose` con 5 PostgreSQL, ZooKeeper, Kafka y `kafka-init-topics`; MS-1..MS-5 con `dotnet run`. No hizo falta el simulador de pagos (efectivo). Todo se detuvo al terminar.

## Cómo repetir

```bash
docker compose -f Docker/docker-compose.yml up -d postgres-tenant postgres-catalog postgres-pos postgres-warehouse postgres-tax zookeeper kafka kafka-init-topics
# MS-1, MS-2, MS-3, MS-4 y MS-5 con run-apis
node scripts/e2e-fefo.mjs
```

## Hallazgos de la ejecución

- **Primeras corridas fallaron por el entorno y por la prueba, no por FEFO.** El JWT del cajero demo trae `sucursal_id` vacío y no había sucursales (la prueba ahora usa o crea una); un turno abierto en la sucursal `0000…0001` bloqueaba la venta con «Sucursal no encontrada o inactiva» (la prueba ahora lo cierra primero).
- **Evento viejo bloquea la partición (nuevo defecto, no corregido).** El volumen de Kafka conservaba eventos `sale.completed` de ventas anteriores de productos que la base de MS-4 no conoce. El de offset 20 se clasificó `TransientFailure` («Stock o lotes insuficientes») y MS-4 lo reintentó sin avanzar, así que ningún evento posterior se procesó y el lote no bajó (5 ≠ 2). Es la política de «lotes insuficientes = recuperable» ([fefo](../../context/dominio/fefo.md)) aplicada a una venta de un producto sin recepción. Se resolvió **solo en el entorno**: con MS-4 detenido se movió el grupo `warehouse-inventory-service` a `latest` con `kafka-consumer-groups --reset-offsets --to-latest`. En una base y un Kafka limpios no ocurre. Queda para registrar en YouTrack como defecto Should (responsable propuesto: Nicolás, MS-4).
- La prueba deja datos (producto, sucursal y lotes `E2E-…`); no los borra. Solo sirve en `docker compose` local.

## Límites

- No ejercita la UI; la UI de recepción es otra tarea.
- No cubre la venta que consume dos lotes a la vez (cubierta por la prueba dirigida documentada en [fefo](../../context/dominio/fefo.md): 8 de 10 → 0 y 2).
- El clúster no tiene Kafka, así que esta prueba no corre allí (limitación aceptada del sprint).

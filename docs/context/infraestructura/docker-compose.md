---
id: docker-compose
tipo: infra
titulo: Docker Compose — Infraestructura de Desarrollo
estado: implementado
fuentes: [Docker/docker-compose.yml, Docker/env.example, Docker/scripts/, Docker/simulator/server.js]
verificado_contra_codigo: true
ultima_revision: 2026-11-24
depende_de: [stack]
publica: []
consume: []
reglas: []
---
# Docker Compose — Infraestructura de Desarrollo

> 16 servicios definidos en `Docker/docker-compose.yml`: 8 instancias PostgreSQL 16 aisladas por microservicio (`5433`–`5440`), Zookeeper + Kafka + inicializador de topics + Kafdrop, Simulador de Pasarela de Pagos (`payment-simulator`), Kong 3.6 DB-less, Prometheus + Grafana y un contenedor auxiliar `frontend`. Los microservicios .NET (MS-1 a MS-5) se ejecutan en el host (`./run-apis.sh` / `run-apis.bat`) y son alcanzados desde Kong vía `host.docker.internal`.

## Servicios definidos (verificados en `Docker/docker-compose.yml`)

| # | Contenedor | Imagen | Puerto Host → Contenedor | Rol |
|---|-----------|--------|--------------------------|-----|
| 1 | `postgres-tenant` | `postgres:16-alpine` | `5433:5432` | DB `tenant_db` — MS-1 Identity |
| 2 | `postgres-supply` | `postgres:16-alpine` | `5434:5432` | DB `supply_db` — MS-6 Supply |
| 3 | `postgres-catalog` | `postgres:16-alpine` | `5435:5432` | DB `catalog_db` — MS-3 Catalog |
| 4 | `postgres-pos` | `postgres:16-alpine` | `5436:5432` | DB `pos_db` — MS-5 POS |
| 5 | `postgres-tax` | `postgres:16-alpine` | `5437:5432` | DB `tax_db` — MS-2 Tax |
| 6 | `postgres-warehouse` | `postgres:16-alpine` | `5438:5432` | DB `warehouse_db` — MS-4 Warehouse |
| 7 | `postgres-analytics` | `postgres:16-alpine` | `5439:5432` | DB `analytics_db` — MS-7 Analytics |
| 8 | `postgres-loyalty` | `postgres:16-alpine` | `5440:5432` | DB `loyalty_db` — MS-8 Loyalty |
| 9 | `zookeeper` | `confluentinc/cp-zookeeper:7.6.1` | `2181:2181` | Coordinación Kafka |
| 10 | `kafka` | `confluentinc/cp-kafka:7.6.1` | `9092:9092` (interno `29092`) | Bus de eventos asíncrono |
| 11 | `kafka-init-topics` | `confluentinc/cp-kafka:7.6.1` | — | Aprovisionamiento idempotente de 17 topics |
| 12 | `kafdrop` | `obsidiandynamics/kafdrop:4.0.2` | `9000:9000` | UI web Kafka |
| 13 | `payment-simulator` | `node:20-alpine` (`Docker/simulator/server.js`) | `5050:5050` | Simulador determinista de pasarela RN-02 (S2-057) |
| 14 | `kong` | `kong:3.6` | `8000` (proxy), `8001` (admin), `8002` (GUI) | API Gateway declarativo DB-less |
| 15 | `prometheus` | `prom/prometheus:v2.53.0` | `9090:9090` | Recolección de métricas |
| 16 | `grafana` | `grafana/grafana:11.1.0` | `3000:3000` | Dashboards de observabilidad |
| — | `frontend` | `../globalmart-frontend/Dockerfile` | — | Helper opcional CLI (`lint`/`install` sin Node local) |

## Volúmenes persistentes

```yaml
pgdata-tenant, pgdata-supply, pgdata-catalog, pgdata-pos,
pgdata-tax, pgdata-warehouse, pgdata-analytics, pgdata-loyalty,
zkdata, zklog, kafkadata, prometheus-data, grafana-data
```

## Red y resolución hacia el host

- Todos los contenedores comparten la red bridge `minimarket-net`.
- `kong` declara `extra_hosts: ["host.docker.internal:host-gateway"]` para enrutar tráfico hacia MS-1..MS-5 corriendo en el host en `0.0.0.0`.

## Healthchecks y valores por defecto (S2-116 / TI3-747)

## Topics Kafka inicializados

```
sale.completed, sale.completed.error, stock.alert, expiry.alert, sale.reversed, stock.updated,
purchase.received, fx.rate.updated, points.updated, tenant.created,
tenant.updated, user.registered, product.created, product.price_updated,
shift.closed, stock.low, product.expiring_soon, purchase_order.received
```

## Healthchecks y valores por defecto (S2-116 / TI3-747)

- Los 8 PostgreSQL, Zookeeper, Kafka, `payment-simulator`, Kong y Prometheus cuentan con `healthcheck` activo. Los servicios dependientes esperan `condition: service_healthy`.
- `POSTGRES_USER` y `POSTGRES_PASSWORD` tienen fallback `${DB_USER:-admin}` y `${DB_PASSWORD:-admin_password}`, alineados con `src/*/appsettings.json` para que `docker compose -f Docker/docker-compose.yml up -d` funcione tanto con `Docker/.env` como sin él.

## Variables de entorno clave

Definidas en `Docker/.env` (NO commitear, usar `Docker/env.example` como plantilla):
- `DB_USER`, `DB_PASSWORD` — credenciales PostgreSQL
- `KAFKA_HOST`, `KAFKA_PORT` — configuración Kafka
- `GRAFANA_ADMIN_PASSWORD` — contraseña Grafana

## Notas de operación

- **Kong en Windows Docker Desktop:** límites de memoria removidos (`mem_limit` eliminado) para evitar OOM kills en los workers de Kong.
- **kafka-init-topics:** contenedor `restart: "no"` que inicializa los topics y termina.
- **Migraciones:** se aplican automáticamente al iniciar cada microservicio en modo Development.

## Conexiones
- Stack: [[stack]]
- API Gateway: [[kong]]
- CI/CD: [[ci-cd]]
- Cómo ejecutar: [[como-ejecutar]]

## Fuentes
- `Docker/docker-compose.yml`
- `Docker/env.example`
- `Docker/simulator/server.js`
- `Docker/scripts/init-kafka-topics.sh`

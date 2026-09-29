---
id: docker-compose
tipo: infra
titulo: Docker Compose — Infraestructura de Desarrollo
estado: implementado
fuentes: [Docker/docker-compose.yml, Docker/env.example, Docker/scripts/]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: [stack]
publica: []
consume: []
reglas: []
---
# Docker Compose — Infraestructura de Desarrollo

> 15 contenedores definidos. Incluye 8 PostgreSQL, Kafka stack, Kong, monitoreo y herramientas de UI.

## Servicios definidos

| # | Contenedor | Imagen | Puerto | Rol |
|---|-----------|--------|--------|-----|
| 1 | postgres-tenant | postgres:16 | 5432 | DB MS-1 Identity |
| 2 | postgres-supply | postgres:16 | 5433 | DB MS-6 Supply |
| 3 | postgres-catalog | postgres:16 | 5434 | DB MS-3 Catalog |
| 4 | postgres-pos | postgres:16 | 5435 | DB MS-5 POS |
| 5 | postgres-tax | postgres:16 | 5436 | DB MS-2 Tax |
| 6 | postgres-warehouse | postgres:16 | 5437 | DB MS-4 Warehouse |
| 7 | postgres-analytics | postgres:16 | 5438 | DB MS-7 Analytics |
| 8 | postgres-loyalty | postgres:16 | 5439 | DB MS-8 Loyalty |
| 9 | zookeeper | confluentinc/cp-zookeeper | 2181 | Coordinación Kafka |
| 10 | kafka | confluentinc/cp-kafka:7.6.1 | 9092 | Bus de eventos |
| 11 | kafka-init-topics | cp-kafka:7.6.1 | — | Init topics al arrancar |
| 12 | kafdrop | obsidiandynamics/kafdrop:4.0.2 | 9000 | UI web Kafka |
| 13 | kong | kong:3.6 | 8000 (proxy), 8001 (admin), 8002 (GUI) | API Gateway |
| 14 | prometheus | prom/prometheus:v2.53.0 | 9090 | Métricas |
| 15 | grafana | grafana/grafana:11.1.0 | 3000 | Dashboards |

## Volumes definidos

```yaml
pgdata-tenant, pgdata-supply, pgdata-catalog, pgdata-pos,
pgdata-tax, pgdata-warehouse, pgdata-analytics, pgdata-loyalty,
zkdata, zklog, kafkadata, prometheus-data, grafana-data
```

## Red

Todos los contenedores en red `minimarket-net` (bridge). Comunicación entre servicios por nombre de contenedor.

## Topics Kafka inicializados

```
sale.completed, stock.alert, expiry.alert, sale.reversed, stock.updated,
purchase.received, fx.rate.updated, points.updated, tenant.created,
tenant.updated, user.registered, product.created, product.price_updated,
shift.closed, stock.low, product.expiring_soon, purchase_order.received
```

## Healthchecks

Todos los PostgreSQL, Zookeeper, Kafka y Kong tienen healthchecks definidos. Los servicios dependientes esperan `condition: service_healthy`.

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
- `Docker/scripts/init-kafka-topics.sh`

---
id: arquitectura
tipo: proyecto
titulo: Arquitectura del Sistema — GlobalMart OS
estado: implementado
fuentes: [Docker/docker-compose.yml, Docker/config/kong.yaml, GlobalMart_ContextMaster.md#sec3]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: [stack]
publica: []
consume: []
reglas: [RN-01, RNF-01]
---
# Arquitectura del Sistema — GlobalMart OS

> Patrón de microservicios RESTful con event sourcing vía Kafka. 8 servicios ASP.NET Core 8, cada uno con su propia PostgreSQL. Kong como API Gateway. Electron como frontend de escritorio.

## Vista general

```
[Electron + React Frontend]
        |
        v  (HTTP directo a port 5124 en dev — NO pasa por Kong actualmente)
[Kong API Gateway :8000 — JWT, rate-limiting, routing]
        |
   ┌────┴─────────────────────────────────────────┐
   |        |        |        |        |      |   |
  MS-1    MS-2    MS-3    MS-4    MS-5   MS-6 MS-7 MS-8
  :5001   :5002   :5003   :5004   :5005  :6001 :7001 :8001
   |        |        |        |        |      |   |   |
  [PG]    [PG]    [PG]    [PG]    [PG]  [PG] [PG][PG]
                                |
                         [Apache Kafka :9092]
                         [Zookeeper :2181]
```

## Comunicación entre servicios

### Síncrona (REST)
- MS-5 → MS-2: calcular IVA (TaxClient.CalculateTaxAsync)
- MS-5 → MS-3: lookup de productos (CatalogClient)
- MS-2 → MS-1: obtener config fiscal del tenant (TenantConfigClient)

### Asíncrona (Kafka) — estado actual
- MS-5 →[PLANIFICADO]→ Kafka `sale.completed` → MS-4 (consumer implementado)

### Via Kong (configurado)
- `/api/auth/*` → MS-1
- `/api/users/*` → MS-1
- `/api/tenants/*` → MS-1
- `/api/products/*` → MS-3
- `/api/turnos/*` → MS-5
- `/api/ventas/*` → MS-5

**MS-4, MS-6, MS-7, MS-8 NO tienen rutas Kong configuradas.**

## Patrón Multi-Tenant

Ver [[multi-tenant]] para detalles completos.

Resumen:
1. `tenant_id` en cada tabla
2. JWT con claim `tenant_id`
3. TenantMiddleware inyecta CurrentTenantId en DbContext
4. Kong valida JWT antes de llegar al servicio

## Infraestructura docker-compose

15 contenedores:
- 8 × PostgreSQL (uno por servicio)
- Zookeeper, Kafka, kafka-init-topics, Kafdrop
- Kong
- Prometheus, Grafana

Ver [[docker-compose]] para detalles de puertos y configuración.

## Servicios y puertos (reales)

| Servicio | Puerto interno | Nota |
|----------|---------------|------|
| MS-1 Identity | 5001 | También corre en 5124 en dev |
| MS-2 Tax | 5002 | |
| MS-3 Catalog | 5003 | |
| MS-4 Warehouse | 5004 | |
| MS-5 POS | 5005 | |
| MS-6 Supply | 6001 | NO verificado |
| MS-7 Analytics | 7001 | NO verificado |
| MS-8 Loyalty | 8001 | NO verificado |
| Kong proxy | 8000 | |
| Kafka | 9092 | |
| Kafdrop UI | 9000 | |
| Grafana | 3000 | |
| Prometheus | 9090 | |

## Conexiones
- Stack: [[stack]]
- API Gateway: [[kong]]
- Docker: [[docker-compose]]
- Eventos: [[kafka-topics]]
- Multi-tenant: [[multi-tenant]]

## Fuentes
- `Docker/docker-compose.yml`
- `Docker/config/kong.yaml`
- `GlobalMart_ContextMaster.md` §3

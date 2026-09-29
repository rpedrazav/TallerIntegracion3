---
id: stack
tipo: proyecto
titulo: Stack Tecnológico — GlobalMart OS
estado: implementado
fuentes: [GlobalMartOS.sln, Directory.Build.props, Docker/docker-compose.yml, globalmart-frontend/package.json]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: [arquitectura]
publica: []
consume: []
reglas: []
---
# Stack Tecnológico — GlobalMart OS

> Stack verificado contra el código real. Las versiones exactas provienen de docker-compose.yml y package.json.

## Backend

| Tecnología | Versión | Rol |
|-----------|---------|-----|
| ASP.NET Core | 8 (LTS) | 8 microservicios REST |
| C# | 12 | Lenguaje backend |
| Entity Framework Core | 8 | ORM + migraciones |
| FluentValidation | — | Validación de DTOs |
| BCrypt.Net | — | Hash de contraseñas |
| Confluent.Kafka | — | Producer/Consumer Kafka |
| Npgsql | — | Driver PostgreSQL para EF Core |
| Microsoft.AspNetCore.Authentication.JwtBearer | — | Validación JWT |

## Base de datos

| Tecnología | Versión | Rol |
|-----------|---------|-----|
| PostgreSQL | 16 | Una instancia por microservicio (8 total) |

## Mensajería

| Tecnología | Versión | Rol |
|-----------|---------|-----|
| Apache Kafka | confluentinc/cp-kafka:7.6.1 | Bus de eventos |
| Apache Zookeeper | — | Coordinación Kafka |
| Kafdrop | 4.0.2 | UI web para Kafka |

## API Gateway

| Tecnología | Versión | Rol |
|-----------|---------|-----|
| Kong | 3.6 (DB-less) | Routing, JWT validation, rate-limiting |

## Frontend

| Tecnología | Versión | Rol |
|-----------|---------|-----|
| Electron | Latest | App de escritorio multiplataforma |
| React | 18+ | UI en el renderer de Electron |
| TypeScript | 5+ | Tipado estático |
| axios | — | Cliente HTTP |
| electron-store | — | Persistencia local de tokens |
| React Router | — | Navegación entre pantallas |

## Infraestructura / DevOps

| Tecnología | Versión | Rol |
|-----------|---------|-----|
| Docker + Docker Compose | — | Contenedorización (desarrollo y producción) |
| GitHub Actions | — | CI/CD: build → test → deploy |
| Prometheus | prom/prometheus:v2.53.0 | Métricas |
| Grafana | grafana/grafana:11.1.0 | Dashboards de monitoreo |

## Testing

| Tecnología | Versión | Rol |
|-----------|---------|-----|
| xUnit | — | Framework de tests (en .csproj) |
| WebApplicationFactory | Built-in | Tests de integración |
| Console App tests | — | Tests manuales smoke (AgregarItemTest, ManualTest) |

**Nota:** No se encontraron tests xUnit implementados con asserts reales. Los proyectos de test son console apps de smoke testing.

## Decisiones de stack

- Kafka elegido sobre RabbitMQ: [[001-kafka-vs-rabbitmq]]
- Kong elegido sobre YARP: [[002-kong-vs-yarp]]
- EF Core sin Dapper: [[003-efcore-unico]]
- Electron elegido sobre web: [[005-electron-frontend]]

## Conexiones
- Arquitectura: [[arquitectura]]
- Docker: [[docker-compose]]
- CI/CD: [[ci-cd]]

## Fuentes
- `Docker/docker-compose.yml`
- `globalmart-frontend/package.json`
- `Directory.Build.props`
- `GlobalMartOS.sln`

---
id: ci-cd
tipo: infra
titulo: CI/CD — GitHub Actions
estado: implementado
fuentes: [.github/workflows/ci.yml, .github/workflows/docker-build.yml]
verificado_contra_codigo: true
ultima_revision: 2026-10-27
depende_de: [docker-compose, testing]
publica: []
consume: []
reglas: []
---
# CI/CD — GitHub Actions

`docker-build.yml` construye y publica también `WarehouseInventoryService` en Docker Hub en push a `dev` o `main`, junto con MS-1, MS-2, MS-3 y MS-5.

> Pipeline de build y test automatizado. Se ejecuta en cada push a `main` o `dev` y en pull requests hacia esas ramas. Incluye gate de validación estricta de tipos TypeScript para el frontend antes de empaquetado y tests de backend con infraestructura.

## Triggers

```yaml
on:
  push:
    branches: [main, dev]
  pull_request:
    branches: [main, dev]
```

## Jobs del pipeline

### 1. `frontend-typecheck` (Gate TypeScript — S2-029 / TI3-660)
- Checkout del código
- Setup Node.js 20 con caché de npm
- `npm ci` en `globalmart-frontend/`
- `npm run typecheck` (`tsc --noEmit`), garantizando que cualquier error de tipos bloquee el pipeline.

### 2. `build-and-test` (.NET 8 + Integración)
| # | Paso | Descripción |
|---|------|-------------|
| 1 | Checkout | `actions/checkout@v4` |
| 2 | Setup .NET 8 | `actions/setup-dotnet@v4` |
| 3 | Restore | `dotnet restore GlobalMartOS.sln` |
| 4 | Build (8 servicios) | `dotnet build` por cada microservicio |
| 5 | Docker Compose up | Levanta PostgreSQL x8, Zookeeper, Kafka, Kong |
| 6 | Health check | Espera hasta 120s por contenedor, falla si no pasa |
| 7 | Tests | `dotnet test GlobalMartOS.sln` con PostgreSQL efímero |
| 8 | Teardown | `docker compose down -v --remove-orphans` (siempre) |

## Variables de entorno en CI

```yaml
env:
  DB_USER: ci_user
  DB_PASSWORD: ci_password
  COMPOSE_FILE: Docker/docker-compose.yml
```

## Lo que NO corre en CI

- Frontend Electron (requiere display/GUI)
- Prometheus/Grafana (no necesarios para tests)
- Kafdrop (UI, no necesaria en CI)

## Estado real de los tests

Los tests actuales son console apps de smoke testing (no xUnit con assertions). El pipeline ejecutará `dotnet test` pero con cobertura muy baja.

## Conexiones
- Docker: [[docker-compose]]
- Testing: [[testing]]
- Stack: [[stack]]

## Fuentes
- `.github/workflows/ci.yml`

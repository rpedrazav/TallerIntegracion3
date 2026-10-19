---
id: ci-cd
tipo: infra
titulo: CI/CD — GitHub Actions
estado: implementado
fuentes: [.github/workflows/ci.yml]
verificado_contra_codigo: true
ultima_revision: 2026-10-18
depende_de: [docker-compose, testing]
publica: []
consume: []
reglas: []
---
# CI/CD — GitHub Actions

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

### Workflow independiente `electron-e2e.yml` — S2-024

`.github/workflows/electron-e2e.yml` ejecuta en Windows, Node 22, `npm ci` y `npm run ci:local`, sin secretos, Docker ni datos personales. Arranca Electron real con perfil aislado por prueba y APIs simuladas. Publica HTML, capturas y trazas de fallo durante siete días, excluyendo perfiles. Tiene límite de 15 minutos y triggers push/PR a main/dev y manual. El mismo comando local aprobó typecheck y **2/2 E2E** el 2026-10-18; ejecución hospedada GitHub Actions [NO VERIFICADO]. [Guía reproducible](../../../globalmart-frontend/e2e/electron/README.md).

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

- Electron contra APIs desplegadas (el workflow S2-024 usa APIs simuladas)
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

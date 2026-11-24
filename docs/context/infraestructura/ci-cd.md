---
id: ci-cd
tipo: infra
titulo: CI/CD — GitHub Actions
estado: implementado
fuentes: [.github/workflows/ci.yml, .github/workflows/docker-build.yml, .github/workflows/electron-e2e.yml, globalmart-frontend/package.json]
verificado_contra_codigo: true
ultima_revision: 2026-11-24
depende_de: [docker-compose, kong, testing]
publica: []
consume: []
reglas: []
---
# CI/CD — GitHub Actions

> Pipelines automatizados en `.github/workflows/ci.yml` (integración continua bloqueante por etapas — S2-029 / S2-117), `.github/workflows/docker-build.yml` (construcción y publicación de imágenes con tag inmutable `s2-<YYYYMMDD>-<sha>` + `:latest` — TI3-758), y `.github/workflows/electron-e2e.yml` (regresión de Electron empaquetado/local — S2-024).

`docker-build.yml` construye y publica también `WarehouseInventoryService` en Docker Hub en push a `dev` o `main`, junto con MS-1, MS-2, MS-3 y MS-5.

## 1. Workflow `ci.yml` — Pruebas Críticas Bloqueantes (S2-117 / TI3-748)

Se dispara en `push` y `pull_request` hacia `main` y `dev`. Emite bloques desplegables (`::group::`) y tabla resumen en `$GITHUB_STEP_SUMMARY`. **Cualquier fallo de compilación .NET, tipos TypeScript, esquema Kong o pruebas unitarias/integración bloquea el workflow (`exit != 0`).**

### Job 1: `frontend-gate` (`Etapa 1 · Frontend`)
| Etapa | Comando | Qué valida |
|---|---|---|
| `[1/6]` | `actions/checkout@v4` | Código fuente del repositorio |
| `[2/6]` | Setup Node.js 22 | Entorno para scripts y empaquetado |
| `[3/6]` | `npm ci` | Instalación limpia desde `package-lock.json` |
| `[4/6]` | `npm run typecheck` (`tsc --noEmit`) | Tipos estrictos en React, IPC, adaptadores y contratos |
| `[5/6]` | `npm run test:unit` (`node --test tests/*.test.cjs`) + `smoke:simulators` | Pruebas unitarias frontend y smoke de simuladores de hardware |
| `[6/6]` | `npx playwright install` + `npm run test:e2e` | Regresión de interfaz y renderer Playwright |

### Job 2: `backend-build-and-test` (`Etapa 2 · Backend`)
| Etapa | Comando | Qué valida |
|---|---|---|
| `[1/7]` | Checkout + Setup `.NET 8.0.x` + `Node 22` | Runtime para APIs, tests xUnit y simulador Node |
| `[2/7]` | `cd globalmart-frontend && npm ci --ignore-scripts` | Dependencias para suites S2-082/083/084 que ejecutan TS/React productivo |
| `[3/7]` | `dotnet restore` + `dotnet build -c Release` (8 MS + Test suites) | Compilación limpia de toda la solución |
| `[4/7]` | `kong config parse` (`KONG_DATABASE=off`) | Validez sintáctica y esquema de `Docker/config/kong.yaml` |
| `[5/7]` | `docker compose up -d` + espera de 12 `healthcheck`s | Salud de PostgreSQL x8, Zookeeper, Kafka, Kong y `payment-simulator` |
| `[6/7]` | `dotnet test` (`GlobalMart.Tests` + `GlobalMart.IntegrationTests`) | Suite unitaria y suite completa de integración xUnit con reporte TRX |
| `[7/7]` | `docker compose down -v --remove-orphans` (`if: always()`) | Limpieza garantizada de contenedores efímeros en el runner |

### Workflow independiente `electron-e2e.yml` — S2-024

`.github/workflows/electron-e2e.yml` ejecuta en Windows, Node 22, `npm ci` y `npm run ci:local`, sin secretos, Docker ni datos personales. Arranca Electron real con perfil aislado por prueba y APIs simuladas. Publica HTML, capturas y trazas de fallo durante siete días, excluyendo perfiles. Tiene límite de 15 minutos y se activa con push a `main`, `dev` o `local`, PR hacia esas ramas y ejecución manual. [Guía reproducible](../../../globalmart-frontend/e2e/electron/README.md).

## 2. Workflow `docker-build.yml` — Imágenes del Clúster (TI3-758)

En `push` a `dev` / `main` o ejecución manual (`workflow_dispatch`), construye y sube a Docker Hub las imágenes de **MS-1, MS-2, MS-3, MS-4 y MS-5** etiquetadas simultáneamente con:
- `<DOCKER_USERNAME>/<servicio>:s2-<YYYYMMDD>-<short_sha>` (tag inmutable recomendado para actualizar pods en Kubernetes)
- `<DOCKER_USERNAME>/<servicio>:latest`

Ver el procedimiento de despliegue en [[manifiestos-k8s-cluster]].

## Lo que NO corre aún en CI

- Ventana gráfica interactiva de Electron y suite E2E Playwright contra clúster remoto universitario.
- Recolección automatizada de porcentaje de cobertura de líneas (`coverlet` no impone gate del 80% todavía; ver [[testing]]).

## Conexiones
- Docker: [[docker-compose]]
- Testing: [[testing]]
- API Gateway: [[kong]]

## Fuentes
- `.github/workflows/ci.yml`
- `.github/workflows/docker-build.yml`
- `.github/workflows/electron-e2e.yml`
- `globalmart-frontend/package.json`

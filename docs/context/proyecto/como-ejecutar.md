---
id: como-ejecutar
tipo: proyecto
titulo: Cómo Ejecutar GlobalMart OS
estado: implementado
fuentes: [Docker/docker-compose.yml, DOCKER.md, run-apis.bat, .github/workflows/ci.yml]
verificado_contra_codigo: true
ultima_revision: 2026-10-16
depende_de: [stack, docker-compose]
publica: []
consume: []
reglas: []
---
# Cómo Ejecutar GlobalMart OS — Guía de Arranque del Núcleo

> Comandos y orden verificado para levantar el entorno completo de desarrollo de forma reproducible (TI3-661 / S2-030).
> **Importante:** Docker Compose levanta la infraestructura compartida (PostgreSQL x8, Kafka, Kong Gateway, Grafana). Los microservicios .NET (MS-1 a MS-5) se ejecutan en el host.

## Requisitos previos

- Docker Desktop / Docker Engine con Compose v2
- .NET 8 SDK
- Node.js 20+ (para el frontend Electron)
- Git y curl / bash

## 1. Clonar y configurar variables de entorno

```bash
git clone <repo>
cd TallerIntegracion3

# Copiar variables de entorno de infraestructura
cp Docker/env.example Docker/.env
# Editar Docker/.env si se requieren credenciales personalizadas

# Copiar variables de entorno del frontend (opcional para desarrollo local)
cp globalmart-frontend/.env.example globalmart-frontend/.env
```

## 2. Levantar la infraestructura compartida (Docker Compose)

> Docker Compose **NO** levanta las APIs .NET; únicamente aprovisiona las bases de datos, el broker de mensajería y el API gateway.

```bash
# Levantar PostgreSQL x8, Kafka, Zookeeper, Kong Gateway, Kafdrop, Prometheus y Grafana
docker compose -f Docker/docker-compose.yml up -d

# Verificar que los contenedores estén 'healthy'
docker compose -f Docker/docker-compose.yml ps
```

Puertos de infraestructura:
- **Kong API Gateway (Proxy):** `http://localhost:8000` (Admin: `:8001`)
- **Apache Kafka:** `localhost:9092` (Zookeeper: `:2181`)
- **Kafdrop (Kafka UI):** `http://localhost:9000`
- **PostgreSQL x8:** Puertos `5432` a `5439` (mapeados por microservicio)
- **Grafana:** `http://localhost:3000` (`admin`/`admin`)

## 3. Ejecutar los microservicios del núcleo (MS-1 a MS-5)

Una vez que la infraestructura está saludable, iniciar los 5 microservicios en el host:

```bash
# En Linux / macOS:
./run-apis.sh

# En Windows:
run-apis.bat
```

O manualmente por servicio en terminales separadas:
```bash
dotnet run --project src/TenantIdentityService      # MS-1 (:5124)
dotnet run --project src/TaxComplianceService        # MS-2 (:5239)
dotnet run --project src/CatalogPricingService       # MS-3 (:5203)
dotnet run --project src/WarehouseInventoryService   # MS-4 (:5205)
dotnet run --project src/POSCartService              # MS-5 (:5000)
```

## 4. Validar el núcleo con Smoke Test HTTP

Ejecutar el script automatizado para verificar el flujo crítico completo (health, login de cajero seed, consulta de catálogo y estado de POS):

```bash
./scripts/smoke-test.sh
```

El script valida:
1. Health checks (`/health`) en MS-1 a MS-5.
2. Autenticación pública en `POST /api/auth/login` con credenciales seed (`cajero@demo.cl` / `demo1234`).
3. Consulta autorizada a `GET /api/products` (MS-3 vía Kong) usando el token JWT obtenido.
4. Consulta autorizada a `GET /api/turnos/activo` (MS-5 vía Kong).

## 5. Ejecutar el frontend Electron

```bash
cd globalmart-frontend
npm ci
npm start    # Abre la app Electron
```

Por defecto conecta a los endpoints remotos (`API_MODE=remote`). Para conectar a los servicios locales o Kong:
```bash
API_MODE=gateway npm start   # Tráfico vía Kong (:8000)
API_MODE=local npm start     # Tráfico directo a microservicios
```

**Seed local de desarrollo (credenciales de prueba):**
- Email: `cajero@demo.cl`
- Password: `demo1234`
- Tenant ID: `aaaaaaaa-0000-0000-0000-000000000001`

## 5. Ejecutar tests

```bash
# Todos los tests del solution
dotnet test GlobalMartOS.sln

# Un proyecto específico
dotnet test src/POSCartService/tests/POSCartService.AgregarItemTest/
```

## 6. Aplicar migraciones de BD

Las migraciones se aplican automáticamente al iniciar cada servicio en modo Development (`db.Database.Migrate()`).

Para aplicar manualmente:
```bash
cd src/TenantIdentityService
dotnet ef database update
```

## 7. Detener todo

```bash
docker compose -f Docker/docker-compose.yml down

# Con limpieza de volúmenes (borra todas las BDs)
docker compose -f Docker/docker-compose.yml down -v
```

## Variables de entorno clave (NO commitear valores reales)

Ver `Docker/env.example` para lista completa. Variables principales:
- `DB_USER`, `DB_PASSWORD` — credenciales PostgreSQL
- `JWT_SECRET` — secreto para firmar JWT
- `KAFKA_HOST` — host de Kafka
- `GRAFANA_ADMIN_PASSWORD` — contraseña Grafana

## Conexiones
- Docker: [[docker-compose]]
- Stack: [[stack]]
- Gateway y Rutas: [[kong]], [[matriz-rutas]]
- CI/CD: [[ci-cd]]

## Fuentes
- `Docker/docker-compose.yml`
- `run-apis.sh`
- `run-apis.bat`
- `scripts/smoke-test.sh`
- `DOCKER.md`
- `.github/workflows/ci.yml`

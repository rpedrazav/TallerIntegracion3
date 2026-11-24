---
id: como-ejecutar
tipo: proyecto
titulo: Cómo Ejecutar GlobalMart OS
estado: implementado
fuentes: [Docker/docker-compose.yml, Docker/config/kong.yaml, DOCKER.md, run-apis.bat, .github/workflows/ci.yml]
verificado_contra_codigo: true
ultima_revision: 2026-11-24
depende_de: [stack, docker-compose]
publica: []
consume: []
reglas: []
---
# Cómo Ejecutar GlobalMart OS

Guía rápida para levantar el entorno de desarrollo local.

## Prerrequisitos

- **.NET 8 SDK** (`dotnet --version` >= 8.0)
- **Node.js 20+** y **npm** (`node --version`)
- **Docker** y **Docker Compose v2** (`docker compose version`)

---

## 1. Clonar y configurar variables de entorno

```bash
git clone https://github.com/rpedrazav/TallerIntegracion3.git
cd TallerIntegracion3

# Frontend: copiar variables de entorno de ejemplo
cp globalmart-frontend/.env.example globalmart-frontend/.env
```

---

## 2. Levantar la infraestructura (PostgreSQL x8, Kafka, Kong)

```bash
# Levantar bases de datos, broker de eventos y API Gateway
docker compose -f Docker/docker-compose.yml up -d

# Verificar que los contenedores estén sanos (healthy)
docker compose -f Docker/docker-compose.yml ps
```

Puertos de infraestructura:
- **Kong API Gateway (Proxy):** `http://localhost:8000` (Admin: `:8001`, GUI: `:8002`). Enruta a los microservicios del host (`host.docker.internal:5124/5239/5203/5205/5000/5050`) y valida el JWT de MS-1 con un consumer de desarrollo sincronizado con `GlobalMartOS` (S3-029). Ver [[kong]].
- **Simulador de Pagos:** `http://localhost:5050` (Gateway: `http://localhost:8000/simulator/payments`)
- **Apache Kafka:** `localhost:9092` (Zookeeper: `:2181`)
- **Kafdrop (Kafka UI):** `http://localhost:9000`
- **PostgreSQL por servicio:** puertos `5432` a `5439` (ver [[docker-compose]])

---

## 3. Iniciar los microservicios (.NET 8)

En terminales separadas, o usando el script automatizado:

**En Linux / macOS:**
```bash
chmod +x run-apis.sh
./run-apis.sh
```

**En Windows:**
```bat
run-apis.bat
```

O individualmente con `dotnet run`:
```bash
dotnet run --project src/TenantIdentityService      # :5124
dotnet run --project src/TaxComplianceService       # :5239
dotnet run --project src/CatalogPricingService      # :5203
dotnet run --project src/WarehouseInventoryService  # :5205
dotnet run --project src/POSCartService             # :5000
```

---

## 4. Iniciar el frontend de escritorio (Electron + React)

```bash
cd globalmart-frontend
npm install
npm start    # Abre la app Electron
```

Por defecto conecta a los endpoints remotos (`API_MODE=remote`). Para conectar a los servicios locales o Kong:
```bash
API_MODE=local npm start     # Tráfico vía Kong (:8000) hacia los servicios de run-apis
```

`local` requiere `docker compose` (Kong) y `run-apis` en ejecución. Si Kong responde 503/502, el microservicio de ese puerto no está levantado en el host; si responde 401 «No credentials found for given 'iss'», el `kong.yaml` cargado no tiene el consumer JWT (recrear el contenedor: `docker compose -f Docker/docker-compose.yml up -d --force-recreate kong`).

**Seed local de desarrollo (credenciales de prueba):**
- Cajero: `cajero@demo.cl` / Password: `demo1234`
- Administrador: `admin@demo.cl` / Password: `demo1234`
- Tenant ID: `aaaaaaaa-0000-0000-0000-000000000001`

> Para el guion completo de demostración del Sprint 2 (sin dispositivos físicos) y límites de hardware, consultar [`docs/pautas/manual-ejecucion-demo-sprint2.md`](../../pautas/manual-ejecucion-demo-sprint2.md).

## 5. Ejecutar tests

```bash
# Tests de la solución completa (.NET 8)
dotnet test GlobalMartOS.sln

# Solo tests de integración del flujo de venta
dotnet test tests/GlobalMart.IntegrationTests/GlobalMart.IntegrationTests.csproj

# Pruebas del frontend
cd globalmart-frontend
npm run typecheck       # Gate TypeScript (tsc --noEmit)
npm run test:unit       # Pruebas unitarias de IPC y contratos
npm run smoke:simulators# Smoke de periféricos sin hardware
npm test                # Tests unitarios Jest
```

---

## 6. Monitoreo (Opcional)

Si se levantó el stack de observabilidad:
- **Grafana:** `http://localhost:3000` (admin / admin)
- **Prometheus:** `http://localhost:9090`

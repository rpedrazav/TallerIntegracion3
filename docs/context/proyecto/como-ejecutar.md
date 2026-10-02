---
id: como-ejecutar
tipo: proyecto
titulo: Cómo Ejecutar GlobalMart OS
estado: implementado
fuentes: [Docker/docker-compose.yml, DOCKER.md, run-apis.bat, .github/workflows/ci.yml]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: [stack, docker-compose]
publica: []
consume: []
reglas: []
---
# Cómo Ejecutar GlobalMart OS

> Comandos verificados para levantar el entorno completo de desarrollo.

## Requisitos previos

- Docker Desktop (Windows/macOS/Linux)
- .NET 8 SDK
- Node.js 18+ (para el frontend Electron)
- Git

## 1. Clonar y configurar variables de entorno

```bash
git clone <repo>
cd TallerIntegracion3

# Copiar variables de entorno (NO commitear el .env real)
cp Docker/env.example Docker/.env
# Editar Docker/.env con tus credenciales de BD
```

## 2. Levantar infraestructura con Docker Compose

```bash
# Levantar todo (PostgreSQL x8, Kafka, Zookeeper, Kong, Prometheus, Grafana)
docker compose -f Docker/docker-compose.yml up -d

# Verificar que estén healthy
docker compose -f Docker/docker-compose.yml ps

# Ver logs de un servicio
docker compose -f Docker/docker-compose.yml logs kafka -f
```

## 3. Ejecutar los microservicios (desarrollo local)

```bash
# Opción A: Script automático (Windows)
run-apis.bat

# Opción B: Manualmente por servicio
cd src/TenantIdentityService
dotnet run

cd src/POSCartService
dotnet run

# etc.
```

**Puertos en desarrollo:**
- MS-1 Identity: `http://localhost:5124` (Swagger: `/swagger`)
- MS-2 Tax: puerto según launchSettings
- MS-3 Catalog: puerto según launchSettings
- MS-4 Warehouse: puerto según launchSettings
- MS-5 POS: puerto según launchSettings
- Kong proxy: `http://localhost:8000`
- Kafdrop (UI Kafka): `http://localhost:9000`
- Grafana: `http://localhost:3000` (admin/admin)

## 4. Ejecutar el frontend Electron

```bash
cd globalmart-frontend
npm install
npm start    # Abre la app Electron
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
- CI/CD: [[ci-cd]]

## Fuentes
- `Docker/docker-compose.yml`
- `run-apis.bat`
- `DOCKER.md`
- `.github/workflows/ci.yml`

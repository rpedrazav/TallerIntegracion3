# 🚀 Infraestructura Local — GlobalMart OS

Configuración completa de **Docker Compose** para el entorno de desarrollo local del proyecto GlobalMart OS. Incluye bases de datos, mensajería, API Gateway y monitoreo.

---

## 📋 Tabla de Contenidos

- [Prerequisitos](#-prerequisitos)
- [Arquitectura de Servicios](#-arquitectura-de-servicios)
- [Tabla de Servicios y Puertos](#-tabla-de-servicios-y-puertos)
- [Guía de Instalación y Uso](#-guía-de-instalación-y-uso)
- [Interfaces Web (UIs)](#-interfaces-web-uis)
- [Scripts de Inicialización](#-scripts-de-inicialización)
- [Configuración de Kong API Gateway](#-configuración-de-kong-api-gateway)
- [Variables de Entorno](#-variables-de-entorno)
- [Troubleshooting](#-troubleshooting)

---

## 🔧 Prerequisitos

| Herramienta | Versión Mínima | Instalación |
|:---|:---:|:---|
| **Docker Engine** | 24+ | [Linux](https://docs.docker.com/engine/install/) |
| **Docker Desktop** | 4.25+ | [Windows/macOS](https://www.docker.com/products/docker-desktop/) |
| **Docker Compose** | v2+ | Incluido en Docker Desktop y Docker Engine recientes |
| **RAM disponible** | 6 GB+ | Para correr todos los servicios simultáneamente |

> ⚠️ **Windows/macOS:** Asegúrate de que Docker Desktop tenga asignados al menos **6 GB de RAM** en Settings → Resources.

---

## 🏗️ Arquitectura de Servicios

```
┌─────────────────────────────────────────────────────────────────────┐
│                        minimarket-net (Docker Bridge)                │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │              BASES DE DATOS (PostgreSQL 16)                 │    │
│  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐      │    │
│  │  │ tenant   │ │ supply   │ │ catalog  │ │ pos      │      │    │
│  │  │ :5433    │ │ :5434    │ │ :5435    │ │ :5436    │      │    │
│  │  └──────────┘ └──────────┘ └──────────┘ └──────────┘      │    │
│  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐      │    │
│  │  │ tax      │ │ warehouse│ │ analytics│ │ loyalty  │      │    │
│  │  │ :5437    │ │ :5438    │ │ :5439    │ │ :5440    │      │    │
│  │  └──────────┘ └──────────┘ └──────────┘ └──────────┘      │    │
│  └─────────────────────────────────────────────────────────────┘    │
│                                                                     │
│  ┌───────────────────────┐    ┌───────────────────────────────┐    │
│  │     MENSAJERÍA        │    │         API GATEWAY           │    │
│  │  ┌─────────┐          │    │  ┌──────────────────────┐     │    │
│  │  │Zookeeper│◄─────┐   │    │  │ Kong 3.6 (DB-less)   │     │    │
│  │  │ :2181   │      │   │    │  │ Proxy  :8000         │     │    │
│  │  └─────────┘      │   │    │  │ Admin  :8001         │     │    │
│  │  ┌─────────┐      │   │    │  │ GUI    :8002         │     │    │
│  │  │  Kafka  │──────┘   │    │  └──────────────────────┘     │    │
│  │  │  :9092  │          │    └───────────────────────────────┘    │
│  │  └─────────┘          │                                         │
│  │  ┌─────────┐          │    ┌───────────────────────────────┐    │
│  │  │ Kafdrop │          │    │         MONITOREO             │    │
│  │  │  :9000  │          │    │  ┌──────────┐ ┌───────────┐   │    │
│  │  └─────────┘          │    │  │Prometheus│ │  Grafana  │   │    │
│  └───────────────────────┘    │  │  :9090   │ │  :3000    │   │    │
│                               │  └──────────┘ └───────────┘   │    │
│                               └───────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 📊 Tabla de Servicios y Puertos

### Bases de Datos (PostgreSQL 16)

Credenciales por defecto: **Usuario:** `admin` | **Contraseña:** `admin_password`

| # | Contenedor | Microservicio | Puerto | Base de Datos | Volumen |
|:---:|:---|:---|:---:|:---|:---|
| 1 | `postgres-tenant` | Tenant & Identity (MS-1) | `5433` | `tenant_db` | `pgdata-tenant` |
| 2 | `postgres-supply` | Supply Chain & Import (MS-6) | `5434` | `supply_db` | `pgdata-supply` |
| 3 | `postgres-catalog` | Catalog & Pricing (MS-3) | `5435` | `catalog_db` | `pgdata-catalog` |
| 4 | `postgres-pos` | POS & Cart (MS-5) | `5436` | `pos_db` | `pgdata-pos` |
| 5 | `postgres-tax` | Tax & Compliance (MS-2) | `5437` | `tax_db` | `pgdata-tax` |
| 6 | `postgres-warehouse` | Warehouse & Inventory (MS-4) | `5438` | `warehouse_db` | `pgdata-warehouse` |
| 7 | `postgres-analytics` | Analytics & Notification (MS-7) | `5439` | `analytics_db` | `pgdata-analytics` |
| 8 | `postgres-loyalty` | Loyalty & Customer (MS-8) | `5440` | `loyalty_db` | `pgdata-loyalty` |

### Mensajería (Apache Kafka)

| # | Contenedor | Servicio | Puerto | Descripción |
|:---:|:---|:---|:---:|:---|
| 9 | `zookeeper` | Apache Zookeeper | `2181` | Coordinación del cluster Kafka |
| 10 | `kafka` | Apache Kafka | `9092` | Broker de mensajería (bus de eventos) |
| 11 | `kafka-init-topics` | Inicializador | — | Crea topics automáticamente (one-shot) |
| 12 | `kafdrop` | Kafdrop UI | `9000` | Interfaz web para explorar Kafka |

### API Gateway

| # | Contenedor | Servicio | Puerto | Descripción |
|:---:|:---|:---|:---:|:---|
| 13 | `kong` | Kong 3.6 DB-less | `8000` | Proxy para microservicios |
| — | — | — | `8001` | Admin API |
| — | — | — | `8002` | Kong Manager GUI |

### Monitoreo

| # | Contenedor | Servicio | Puerto | Descripción |
|:---:|:---|:---|:---:|:---|
| 14 | `prometheus` | Prometheus | `9090` | Recolección de métricas |
| 15 | `grafana` | Grafana | `3000` | Dashboards y visualización |

---

## ⚡ Guía de Instalación y Uso

### 1. Clonar el repositorio

```bash
git clone https://github.com/<tu-org>/TallerIntegracion3.git
cd TallerIntegracion3/Docker
```

### 2. Configurar variables de entorno

```bash
# El archivo env.example ya tiene los valores por defecto
# Copiarlo como .env (si no existe)
cp env.example .env
```

> 📝 Edita el `.env` si necesitas cambiar credenciales o puertos.

### 3. Validar la configuración

```bash
docker compose config
```

### 4. Levantar toda la infraestructura

```bash
docker compose up -d
```

> ⏱️ **Primera vez:** La descarga de imágenes puede tomar 5-10 minutos. Los contenedores PostgreSQL ejecutarán automáticamente el script `init-databases.sh` para crear extensiones y tablas base.

### 5. Verificar que todo esté corriendo

```bash
docker compose ps
```

Todos los servicios deben mostrar estado `Up` y `(healthy)`.

### 6. Verificar logs (opcional)

```bash
# Ver logs de todos los servicios
docker compose logs -f

# Ver logs de un servicio específico
docker compose logs -f kafka
docker compose logs -f kong
docker compose logs -f kafka-init-topics
```

---

## 🌐 Interfaces Web (UIs)

Una vez levantada la infraestructura, puedes acceder a las siguientes interfaces:

| Servicio | URL | Credenciales |
|:---|:---|:---|
| **Kafdrop** (Kafka UI) | [http://localhost:9000](http://localhost:9000) | Sin autenticación |
| **Kong Manager** (API Gateway) | [http://localhost:8002](http://localhost:8002) | Sin autenticación |
| **Prometheus** (Métricas) | [http://localhost:9090](http://localhost:9090) | Sin autenticación |
| **Grafana** (Dashboards) | [http://localhost:3000](http://localhost:3000) | `admin` / `admin` |

### Kafdrop — Explorador de Kafka

Kafdrop permite visualizar los topics, mensajes, consumer groups y el estado del cluster Kafka. Al abrir [http://localhost:9000](http://localhost:9000) deberías ver los **17 topics** creados automáticamente.

### Grafana — Dashboards

Al ingresar por primera vez te pedirá cambiar la contraseña. El datasource de **Prometheus** ya está preconfigurado automáticamente.

---

## 📜 Scripts de Inicialización

### `scripts/init-databases.sh` (TI3-116)

Se ejecuta automáticamente la **primera vez** que se crea cada volumen PostgreSQL. Crea:

- **Extensiones:** `uuid-ossp` (para UUIDs) y `pgcrypto` (para hashing)
- **Tabla `__migrations_history`:** Tracking de migraciones de EF Core
- **Tabla `audit_log`:** Log inmutable de auditoría con `tenant_id`, `user_id`, `active_role` (cumple RNF-05)
- **Índices:** Sobre `tenant_id`, `user_id`, `action`, `created_at` y `(entity_type, entity_id)`

> ⚠️ **Importante:** Si ya tienes volúmenes existentes, este script NO se re-ejecutará. Para forzar la reinicialización:
> ```bash
> docker compose down -v    # ¡BORRA TODOS LOS DATOS!
> docker compose up -d
> ```

### `scripts/init-kafka-topics.sh`

Script standalone para inicialización de topics. El compose ya tiene un servicio `kafka-init-topics` que hace esto automáticamente con un entrypoint inline.

### Verificar que las bases se inicializaron correctamente

```bash
# Verificar extensiones en tenant_db
docker exec postgres-tenant psql -U admin -d tenant_db -c "\dx"

# Verificar tabla audit_log existe
docker exec postgres-tenant psql -U admin -d tenant_db -c "\dt"

# Verificar en otra base de datos
docker exec postgres-pos psql -U admin -d pos_db -c "\dt"
```

---

## 🌐 Configuración de Kong API Gateway

Kong está configurado en modo **DB-less** (declarativo) via el archivo `config/kong.yaml`.

### Rutas configuradas (Sprint 1)

| Ruta | Microservicio | Servicio Interno |
|:---|:---|:---|
| `/api/auth/*` | MS-1 Tenant & Identity | `tenant-identity-service:5001` |
| `/api/users/*` | MS-1 Tenant & Identity | `tenant-identity-service:5001` |
| `/api/tenants/*` | MS-1 Tenant & Identity | `tenant-identity-service:5001` |
| `/api/products/*` | MS-3 Catalog & Pricing | `catalog-pricing-service:5003` |
| `/api/prices/*` | MS-3 Catalog & Pricing | `catalog-pricing-service:5003` |
| `/api/promotions/*` | MS-3 Catalog & Pricing | `catalog-pricing-service:5003` |
| `/api/turnos/*` | MS-5 POS & Cart | `pos-cart-service:5005` |
| `/api/ventas/*` | MS-5 POS & Cart | `pos-cart-service:5005` |

### Plugins activos

- **Rate Limiting:** 100 requests/minuto por IP
- **CORS:** Habilitado para desarrollo con Electron

### Verificar Kong

```bash
# Estado del gateway
curl -s http://localhost:8001/status | python3 -m json.tool

# Ver rutas configuradas
curl -s http://localhost:8001/routes | python3 -m json.tool

# Ver servicios registrados
curl -s http://localhost:8001/services | python3 -m json.tool
```

### Agregar nuevas rutas

Edita el archivo `config/kong.yaml` y reinicia Kong:

```bash
docker compose restart kong
```

---

## 🔑 Variables de Entorno

Todas las variables están definidas en `Docker/.env` (ver `env.example` como referencia):

| Variable | Valor por Defecto | Descripción |
|:---|:---:|:---|
| `DB_USER` | `admin` | Usuario PostgreSQL |
| `DB_PASSWORD` | `admin_password` | Contraseña PostgreSQL |
| `ZOOKEEPER_PORT` | `2181` | Puerto de Zookeeper |
| `KAFKA_PORT` | `9092` | Puerto externo de Kafka |
| `KAFKA_INTERNAL_PORT` | `29092` | Puerto interno de Kafka (entre contenedores) |
| `KAFKA_HOST` | `localhost` | Host para listeners externos |
| `KAFKA_INIT_TOPICS` | `sale.completed,...` | Topics creados automáticamente (17 topics) |
| `KAFDROP_PORT` | `9000` | Puerto de Kafdrop UI |
| `KONG_PROXY_PORT` | `8000` | Puerto proxy de Kong |
| `KONG_ADMIN_PORT` | `8001` | Puerto admin de Kong |
| `PROMETHEUS_PORT` | `9090` | Puerto de Prometheus |
| `GRAFANA_PORT` | `3000` | Puerto de Grafana |
| `GRAFANA_ADMIN_USER` | `admin` | Usuario admin de Grafana |
| `GRAFANA_ADMIN_PASSWORD` | `admin` | Contraseña admin de Grafana |

---

## 🔌 Conectarse a una Base de Datos

### Desde la terminal (psql dentro del contenedor)

```bash
# Conectarse a tenant_db
docker exec -it postgres-tenant psql -U admin -d tenant_db

# Conectarse a pos_db
docker exec -it postgres-pos psql -U admin -d pos_db

# Conectarse a cualquier otra base
docker exec -it postgres-catalog psql -U admin -d catalog_db
```

### Desde un cliente SQL externo (DBeaver, DataGrip, pgAdmin, etc.)

| Parámetro | Valor |
|:---|:---|
| Host | `localhost` |
| Puerto | Ver tabla de puertos (5433-5440) |
| Usuario | `admin` |
| Contraseña | `admin_password` |
| Base de datos | Ver tabla de bases de datos |

### Desde una aplicación ASP.NET Core

```
Server=localhost;Port=5433;Database=tenant_db;User Id=admin;Password=admin_password;
```

---

## 🛑 Detener y Limpiar

### Detener manteniendo los datos

```bash
docker compose stop
```

### Detener y eliminar contenedores (datos preservados en volúmenes)

```bash
docker compose down
```

### Eliminar TODO (contenedores + volúmenes + datos)

```bash
docker compose down -v
```

> ⚠️ **Cuidado:** `docker compose down -v` borra **todos** los datos de las bases de datos, Kafka, Prometheus y Grafana.

### Reconstruir desde cero

```bash
docker compose down -v
docker compose up -d
```

---

## 🔧 Troubleshooting

### Error: Puerto ya en uso

Si un puerto está ocupado por otro proceso:

```bash
# Ver qué proceso usa un puerto (ejemplo: 5433)
sudo lsof -i :5433

# O cambiar el puerto en el archivo .env
# Ejemplo: KAFKA_PORT=9093
```

### Kafka no arranca o se reinicia

```bash
# Verificar logs de Zookeeper primero
docker compose logs zookeeper

# Luego logs de Kafka
docker compose logs kafka

# Problema común: memoria insuficiente
# Solución: Reducir KAFKA_HEAP_OPTS en .env
```

### Kong muestra error al arrancar

```bash
# Verificar que kong.yaml sea válido
docker compose logs kong

# Error común: YAML mal formateado
# Solución: Validar el YAML en https://www.yamllint.com/
```

### Las bases de datos no tienen las tablas iniciales

```bash
# El script solo se ejecuta con volúmenes nuevos
# Para re-ejecutar:
docker compose down -v    # Borra datos
docker compose up -d      # Reinicializa todo
```

### Kafdrop no muestra topics

```bash
# Verificar que kafka-init-topics finalizó
docker compose logs kafka-init-topics

# Verificar topics manualmente
docker exec kafka kafka-topics --bootstrap-server localhost:9092 --list
```

### Verificar salud de todos los servicios

```bash
docker compose ps --format "table {{.Name}}\t{{.Status}}\t{{.Ports}}"
```

---

## 📂 Estructura de Archivos

```
Docker/
├── .env                    # Variables de entorno (NO subir a git)
├── env.example             # Plantilla de variables de entorno
├── docker-compose.yml      # Definición de todos los servicios
├── README.md               # Esta documentación
├── config/
│   ├── kong.yaml           # Configuración declarativa de Kong (rutas y plugins)
│   ├── prometheus.yml      # Configuración de Prometheus (scrape targets)
│   └── grafana/
│       └── provisioning/
│           └── datasources/
│               └── datasource.yml  # Auto-provisioning datasource Prometheus
└── scripts/
    ├── init-databases.sh   # Inicialización de bases PostgreSQL (extensiones + audit)
    └── init-kafka-topics.sh # Script standalone de inicialización de topics
```

---

## 📝 Topics de Kafka

Los siguientes **17 topics** se crean automáticamente al levantar la infraestructura:

| Topic | Productor | Consumidor(es) | Descripción |
|:---|:---|:---|:---|
| `sale.completed` | MS-5 POS | MS-4, MS-7, MS-8 | Venta completada exitosamente |
| `sale.reversed` | MS-5 POS | MS-4, MS-7, MS-8 | Venta anulada/revertida |
| `stock.alert` | MS-4 Warehouse | MS-7 Analytics | Stock bajo mínimo configurado |
| `stock.updated` | MS-4 Warehouse | MS-7 Analytics | Cambio en nivel de stock |
| `stock.low` | MS-4 Warehouse | MS-7 Analytics | Alerta de stock bajo |
| `expiry.alert` | MS-4 Warehouse | MS-7 Analytics | Producto próximo a caducar |
| `purchase.received` | MS-6 Supply | MS-4 Warehouse | Recepción de mercancía |
| `purchase_order.received` | MS-6 Supply | MS-4 Warehouse | Orden de compra recibida |
| `fx.rate.updated` | MS-1 Tenant | MS-3, MS-6, MS-7 | Tipo de cambio actualizado |
| `points.updated` | MS-8 Loyalty | MS-7 Analytics | Puntos de lealtad actualizados |
| `tenant.created` | MS-1 Tenant | Todos | Nuevo tenant creado |
| `tenant.updated` | MS-1 Tenant | Todos | Configuración de tenant modificada |
| `user.registered` | MS-1 Tenant | MS-7 Analytics | Nuevo usuario registrado |
| `product.created` | MS-3 Catalog | MS-4, MS-7 | Nuevo producto en catálogo |
| `product.price_updated` | MS-3 Catalog | MS-7 Analytics | Precio de producto modificado |
| `shift.closed` | MS-5 POS | MS-7 Analytics | Turno de caja cerrado |
| `product.expiring_soon` | MS-4 Warehouse | MS-7 Analytics | Producto próximo a vencer |

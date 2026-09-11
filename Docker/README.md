# 🚀 Infraestructura Local de Bases de Datos (PostgreSQL 16)

Este repositorio contiene la configuración de **Docker Compose** para desplegar 8 instancias independientes de PostgreSQL 16 Alpine, siguiendo un esquema de microservicios con aislamiento de bases de datos por dominio.

---

## 🏗️ Arquitectura y Funcionamiento

Cada microservicio cuenta con su propia instancia de PostgreSQL independiente para garantizar aislamiento total, escalabilidad y flexibilidad.

- **Imagen base:** `postgres:16-alpine` (ligera y optimizada)
- **Red compartida:** `minimarket-net` (permite comunicación entre contenedores por nombre de servicio)
- **Persistencia:** Volúmenes de Docker (`pgdata-<servicio>`) para mantener los datos al reiniciar contenedores
- **Healthcheck:** Cada base de datos ejecuta automáticamente `pg_isready` cada 10 segundos

---

## 📊 Tabla de Servicios, Puertos y Bases de Datos

Todas las instancias utilizan por defecto las siguientes credenciales de desarrollo:
- **Usuario:** `admin`
- **Contraseña:** `admin_password`

| Contenedor | Servicio / Dominio | Puerto Host | Base de Datos | Volumen Persistente |
| :--- | :--- | :---: | :--- | :--- |
| `postgres-tenant` | Tenant & Identity | `5433` | `tenant_db` | `pgdata-tenant` |
| `postgres-supply` | Supply Chain & Import | `5434` | `supply_db` | `pgdata-supply` |
| `postgres-catalog` | Catalog & Pricing | `5435` | `catalog_db` | `pgdata-catalog` |
| `postgres-pos` | POS & Cart | `5436` | `pos_db` | `pgdata-pos` |
| `postgres-tax` | Tax & Compliance | `5437` | `tax_db` | `pgdata-tax` |
| `postgres-warehouse` | Warehouse & Inventory | `5438` | `warehouse_db` | `pgdata-warehouse` |
| `postgres-analytics` | Analytics & Notification | `5439` | `analytics_db` | `pgdata-analytics` |
| `postgres-loyalty` | Loyalty & Customer | `5440` | `loyalty_db` | `pgdata-loyalty` |

---

## ⚡ Guía Rápida de Inicialización y Uso

### 1. Validar la configuración YAML
Antes de levantar la infraestructura, confirma que la sintaxis del archivo sea correcta:
```bash
docker compose config
```

### 2. Desplegar los contenedores
Levanta los 8 servicios en segundo plano (`-d`):
```bash
docker compose up -d
```
> ⏱️ **Nota:** Si es la primera vez que inicializas los volúmenes, espera ~15 segundos para que PostgreSQL complete el formateo inicial e initdb.

### 3. Verificar estado y salud
Comprueba que los 8 contenedores estén en estado `Up` y `(healthy)`:
```bash
docker compose ps
```

### 4. Probar conectividad interna
Puedes verificar si una base de datos específica está lista para recibir conexiones usando `pg_isready`:
```bash
# Ejemplo para postgres-tenant
docker exec postgres-tenant pg_isready -U admin -d tenant_db

# Ejemplo para postgres-catalog
docker exec postgres-catalog pg_isready -U admin -d catalog_db
```

### 5. Conectarse a una Base de Datos mediante `psql`
Para ingresar a la terminal interactiva de PostgreSQL dentro de un contenedor:
```bash
docker exec -it postgres-tenant psql -U admin -d tenant_db
```

### 6. Detener la infraestructura
- **Detener manteniendo los datos en los volúmenes:**
  ```bash
  docker compose stop
  ```
- **Eliminar contenedores y red:**
  ```bash
  docker compose down
  ```
- **Eliminar contenedores, red Y VOLÚMENES (borrado total de datos):**
  ```bash
  docker compose down -v
  ```

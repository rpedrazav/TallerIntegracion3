---
title: "Contexto General del Proyecto — Visión Macro y Micro (.NET 8 Enterprise)"
aliases:
  - Visión del Proyecto
  - Contexto General
  - Project Overview
tags:
  - proyecto/minimarket
  - tipo/vision
  - tipo/contexto
  - estado/referencia
  - stack/dotnet
created: 2026-08-17
updated: 2026-08-20
version: "2.0"
sprints_totales: 5
equipo: 6
capacidad_total: 600h
estado: Documento de Referencia
---

# Sistema de Gestión Global de Minimarkets (GlobalMart OS)
## Visión Macro y Micro — Entregable Final del Proyecto (.NET 8 & Microservicios)

> [!IMPORTANT] Propósito de este documento
> Este documento es la **fuente de verdad técnica y funcional** del proyecto. Define QUÉ se construye, POR QUÉ, CÓMO está organizado bajo una arquitectura de microservicios con **ASP.NET Core (.NET 8)** y QUÉ se espera al finalizar cada Sprint y el proyecto completo.

---

## Índice

- [[#La Visión del Producto]]
- [[#El Problema que Resuelve]]
- [[#Arquitectura General del Sistema]]
- [[#Los 7 Microservicios — Descripción Completa]]
- [[#Reglas de Oro Técnicas]]
- [[#Hoja de Ruta por Sprint — Visión Macro]]
- [[#Estado Esperado al Final de Cada Sprint]]
- [[#El Producto Final — Qué puede hacer un minimarket al terminar el proyecto]]
- [[#Métricas de Éxito del MVP]]
- [[#Stack Tecnológico Oficial]]
- [[#Equipo y Capacidad Total]]
- [[#Glosario]]

---

## La Visión del Producto

**GlobalMart OS** es un sistema de gestión operativa empresarial (Point of Sale + ERP) diseñado para minimarkets y cadenas de retail independientes a nivel global.

Construido como una **aplicación de escritorio de alto rendimiento (Electron + React)** respaldada por un ecosistema de **microservicios en ASP.NET Core (.NET 8)**, permite a un minimarket:

- Operar el punto de venta (POS) en el mostrador con integración fluida a balanzas físicas (USB / Serial / Red) y lectores de códigos de barra.
- Gestionar inventario inteligente con reglas **FEFO (First Expired, First Out)** y control volumétrico de refrigeradores físicos.
- Importar mercadería internacional calculando de forma exacta el **Costo Landed** (proveedor + flete + aranceles).
- Calcular y desglosar impuestos dinámicos y exenciones fiscales de múltiples países (ej. IVA en Chile, AFIP en Argentina, IRS en EE.UU.).
- Monitorear ventas y alertas proactivas en tiempo real a través de dashboards analíticos.
- Escalar horizontalmente gracias al aislamiento estricto de datos (**Database per Service**) y comunicación orientada a eventos con **Apache Kafka**.

> [!TIP] Propuesta de Valor Central
> Un minimarket en Santiago, Buenos Aires, Ciudad de México o Madrid instala la aplicación y esta se adapta de inmediato a su moneda, idioma, huso horario, formato de fecha, reglas impositivas y unidades de medida locales, sin tocar una sola línea de código fuente.

---

## El Problema que Resuelve

| Problema del mercado | Cómo lo resuelve GlobalMart OS |
|---|---|
| Los POS tradicionales son monolitos de una sola tienda sin gestión multi-sucursal | Arquitectura **Multi-Tenant nativa**: gestión centralizada de múltiples minimarkets y sucursales aisladas. |
| Sistemas fiscales rígidos atados a un solo país | **Tax & Compliance Service**: motor de reglas fiscales dinámicas, exenciones e impuestos compuestos. |
| Gestión manual o en planillas de inventario y mermas | **Warehouse & Inventory Service**: trazabilidad por lotes, control de mermas, regla FEFO y cubicaje de neveras. |
| Desconocimiento del costo real en compras internacionales | **Supply Chain & Import Service**: cálculo automatizado del Costo Landed. |
| Alertas estáticas que no consideran el ritmo de venta | **Analytics & Notification Service**: alertas dinámicas basadas en promedios móviles de demanda. |
| Caídas en cascada y acoplamiento severo de base de datos | **Aislamiento Database-per-Service + Apache Kafka** para consistencia eventual desacoplada. |

---

## Arquitectura General del Sistema

```
                    +---------------------------------------+
                    |       DESKTOP CLIENT (Electron)       |
                    |         React 18 + TypeScript         |
                    |     (POS, Admin, Inventario, UI)      |
                    +-------------------+-------------------+
                                        |  HTTP / REST
                                        v
                    +---------------------------------------+
                    |        API GATEWAY (Kong 3.6)         |
                    |    http://localhost:8000/api/v1/      |
                    |  - Enrutamiento unificado             |
                    |  - Validación JWT / Rate Limiting     |
                    |  - CORS & Security Headers            |
                    +---+---+---+---+---+---+---------------+
                        |   |   |   |   |   |
           +------------+   |   |   |   |   +------------+
           |                |   |   |   |                |
    +------v------+  +------v-+ | +-v------+  +----------v--+
    |  Tenant &   |  |Catalog | | |  POS & |  | Tax &       |
    |  Identity   |  |Pricing | | |  Cart  |  | Compliance  |
    |  Service    |  |Service | | |Service |  | Service     |
    |  (ASP.NET)  |  |(ASP.NET) | |(ASP.NET) | | (ASP.NET)   |
    |  :5001      |  | :5002  | | | :5003  |  |  :5004      |
    +------+------+  +------+-+ | +---+----+  +------+------+
           |                |   |     |               |
    +------v------+         |   |  +--v-----------+   |
    |  postgres   |  +------v+  |  |  postgres    |   |
    |  -tenant    |  |postgres|  |  |  -pos        |   |
    +-------------+  |-catalog|  |  +--------------+   |
                     +--------+  |                     |
                                 |   +--------------+  |
                        +--------v-+ | Warehouse &  |  |
                        |Supply    | | Inventory    |  |
                        |Chain &   | | Service      |  |
                        |Import    | | (ASP.NET)    |  |
                        |(ASP.NET) | | :5005        |  |
                        |:5006     | +-----------+--+  |
                        +-----+----+             |     |
                              |         +--------v--+  |
                    +---------v-------+ |  postgres  |  |
                    |  Analytics &    | |  -warehouse|  |
                    | Notification    | +------------+  |
                    | Service (ASP.NET)                 |
                    | :5007           +-----------------+
                    +-----------------+
                              |
    ==========================v=================================
                 BUS DE EVENTOS ASÍNCRONO (Apache Kafka)
        Topics: tenant.created, user.registered, sale.completed,
                stock.low, product.expiring_soon, etc.
    ============================================================
```

---

## Los 7 Microservicios — Descripción Completa

### S1 — Tenant & Identity Service (Puerto 5001)
**Propósito:** Núcleo de identidad y multi-tenancy. Gestiona el alta de inquilinos (minimarkets), sus sucursales, usuarios, roles operativos y configuración regional base.
- **Responsabilidades:**
  - Registro de Tenants y configuración de localización (moneda ISO 4217, símbolo monetario, idioma BCP-47, zona horaria IANA, formato de fecha).
  - Gestión de sucursales físicas (`Sucursal`) por Tenant.
  - Autenticación JWT stateless con Refresh Tokens en base de datos.
  - Control de accesos y roles: `SUPER_ADMIN`, `ADMINISTRADOR`, `CAJERO`, `REPONEDOR`.
- **Base de datos propia:** `postgres-tenant`
- **Tablas clave:** `Tenants`, `Sucursales`, `Users`, `RefreshTokens`, `TenantConfigs`
- **Eventos Kafka:** Emite `tenant.created`, `user.registered`, `tenant.updated`.

---

### S2 — Catalog & Pricing Service (Puerto 5002)
**Propósito:** Catálogo centralizado de productos, categorías, conversión de unidades de medida (UOM) y listas de precios dinámicas.
- **Responsabilidades:**
  - CRUD de productos con código de barras y generación automática de código QR descargable (PNG).
  - Categorías jerárquicas multinivel.
  - Motor de conversión de UOM (`kg ↔ lb`, `L ↔ oz`, unidades/docenas).
  - Precios dinámicos diferenciados por sucursal y país.
  - Soporte de productos de peso variable (pesaje en POS).
- **Base de datos propia:** `postgres-catalog`
- **Tablas clave:** `Productos`, `Categorias`, `ProductoPrecios`, `UnidadesMedida`
- **Eventos Kafka:** Emite `product.created`, `product.price_updated`.

---

### S3 — POS & Cart Service (Puerto 5003)
**Propósito:** Operación del Punto de Venta en el mostrador, control de cajas por turno y liquidación de transacciones.
- **Responsabilidades:**
  - Apertura y cierre de turnos de caja con cuadre de efectivo y reporte de turno.
  - Carrito de compras rápido en memoria/sesión con cálculo de subtotales e impuestos.
  - Integración directa con balanzas de pesaje externas (protocolo Serial RS-232 / USB / TCP-IP).
  - Procesamiento de pagos multimodales (Efectivo, Tarjeta, Mixto) y emisión de tickets/boletas.
  - Historial de ventas y anulaciones supervisadas.
- **Base de datos propia:** `postgres-pos`
- **Tablas clave:** `TurnosCaja`, `Ventas`, `VentaItems`, `Pagos`
- **Eventos Kafka:** Emite `sale.completed`, `shift.closed`.

---

### S4 — Warehouse & Inventory Service (Puerto 5005)
**Propósito:** Control físico del stock, gestión de almacenes/neveras y trazabilidad temporal de perecederos.
- **Responsabilidades:**
  - Control de entradas, salidas, mermas y transferencias de inventario entre sucursales.
  - Actualización automática de stock al consumir el evento `sale.completed` desde Kafka.
  - Modelado volumétrico/área de refrigeradores físicos y bloqueo preventivo de compras si se supera la capacidad máxima.
  - Aplicación estricta de la regla **FEFO** (Primero en Caducar, Primero en Salir) con trazabilidad de lotes.
- **Base de datos propia:** `postgres-warehouse`
- **Tablas clave:** `ProductosStock`, `Lotes`, `Refrigeradores`, `Transferencias`, `Mermas`
- **Eventos Kafka:** Escucha `sale.completed`, emite `stock.low`, `product.expiring_soon`.

---

### S5 — Tax & Compliance Service (Puerto 5004)
**Propósito:** Motor tributario internacional desacoplado del código fuente principal.
- **Responsabilidades:**
  - Cálculo dinámico de impuestos por país, estado/provincia y categoría de producto.
  - Gestión de impuestos compuestos y reglas de exención fiscal (ej. 0% IVA en vegetales, recargo en alcohol/tabaco).
  - Preparación de estructuras para integración con entes tributarios (SII en Chile, AFIP en Argentina, IRS en EE.UU.).
- **Base de datos propia:** `postgres-tax`
- **Tablas clave:** `ReglasFiscales`, `Exenciones`, `CalculosImpuesto`, `CategoriasTributarias`

---

### S6 — Supply Chain & Import Service (Puerto 5006)
**Propósito:** Gestión de compras internacionales y logística de abastecimiento.
- **Responsabilidades:**
  - Emisión y seguimiento de órdenes de compra a proveedores nacionales e internacionales.
  - Algoritmo de cálculo del **Costo Landed** (`Precio Proveedor + Flete Internacional + Aranceles Aduaneros + Seguros`).
  - Actualización de costos base para recalcular márgenes en el catálogo.
- **Base de datos propia:** `postgres-supply`
- **Tablas clave:** `OrdenesCompra`, `Proveedores`, `Envios`, `CostosLanded`
- **Eventos Kafka:** Emite `purchase_order.received`.

---

### S7 — Analytics & Notification Service (Puerto 5007)
**Propósito:** Inteligencia de negocio, analítica predictiva y despacho de notificaciones operativas.
- **Responsabilidades:**
  - Dashboards ejecutivos de ventas diarias, semanales y mensuales consolidados.
  - Alertas inteligentes de bajo stock basadas en el promedio móvil de demanda de la última semana (no umbrales fijos).
  - Alertas tempranas de vencimiento para lácteos y perecederos con sugerencias de liquidación.
  - Notificaciones en tiempo real vía WebSocket / Email / Push.
- **Base de datos propia:** `postgres-analytics`
- **Tablas clave:** `MetricasVentas`, `AlertasGeneradas`, `Notificaciones`, `ConfiguracionesReporte`
- **Eventos Kafka:** Escucha `sale.completed`, `stock.low`, `product.expiring_soon`, `shift.closed`.

---

## Reglas de Oro Técnicas

> [!CAUTION] Reglas Inquebrantables de Arquitectura
> Cualquier modificación a estas directrices requiere aprobación unánime y documentación mediante un ADR (Architecture Decision Record).

| Regla | Descripción | Consecuencia de violarla |
|---|---|---|
| **R1 — API Gateway Único** | Todo el tráfico proveniente de la app Electron o clientes externos ingresa exclusivamente por Kong 3.6 (`http://localhost:8000/api/v1/`). Ningún microservicio expone puertos de producción de forma directa. | Vulnerabilidad de seguridad, bypass de autenticación y falta de observabilidad centralizada. |
| **R2 — Database per Service** | Cada microservicio es dueño absoluto y exclusivo de su base de datos PostgreSQL. Queda terminantemente prohibido hacer JOINs directos o compartir conexiones entre servicios. | Alto acoplamiento, dependencias circulares y pérdida de autonomía en despliegues. |
| **R3 — Comunicación Asíncrona vía Kafka** | La sincronización inter-servicios ante mutaciones de estado se realiza únicamente mediante eventos en Apache Kafka. No se admiten llamadas REST sincrónicas de backend a backend para flujos transaccionales. | Acoplamiento temporal, degradación del rendimiento y fallos en cascada. |
| **R4 — Aislamiento Multi-Tenant Estricto** | Todas las consultas y mutaciones en Entity Framework Core deben filtrar por `TenantId` (mediante Global Query Filters o middleware de contexto). | Fuga crítica de datos y violación de privacidad entre clientes. |
| **R5 — Autenticación JWT Stateless** | Los tokens JWT emitidos por el servicio de identidad contienen claims enriquecidos (`tenantId`, `sucursalId`, `role`, `tenantConfig`). Los microservicios validan la firma de forma local e independiente sin consultar a la BD de identidad. | Cuellos de botella y dependencia de disponibilidad del servicio de identidad. |

---

## Hoja de Ruta por Sprint — Visión Macro

```
SPRINT 1             SPRINT 2             SPRINT 3             SPRINT 4             SPRINT 5
(Semanas 1-4)        (Semanas 5-8)        (Semanas 9-12)       (Semanas 13-16)      (Semanas 17-20)
-----------------    -----------------    -----------------    -----------------    -----------------
Infraestructura      Catálogo y POS       Inventario           Motor Fiscal         Logística,
Base, Identidad y    (MVP de Venta en     Avanzado y Bus       y Reglas             Importaciones
Multi-Tenant         Mostrador)           de Eventos           Internacionales      y Analítica

S1 — Tenant &        S2 — Catalog &       S4 — Warehouse &     S5 — Tax &           S6 — Supply Chain
Identity Service     Pricing Service      Inventory Service    Compliance Service   & Import Service
(ASP.NET Core 8)     (ASP.NET Core 8)     (ASP.NET Core 8)     (ASP.NET Core 8)     (ASP.NET Core 8)

API Gateway          S3 — POS & Cart      Bus de Mensajes      Precios dinámicos    S7 — Analytics &
(Kong 3.6 DB-less)   Service (ASP.NET)    (Apache Kafka)       por sucursal/país    Notification

Login, Sucursales,   Carrito, Cuadre de   FEFO, neveras,       Exenciones de IVA,   Costo Landed,
Roles, Localización  Caja, Balanza,       Alertas de stock y   SII / AFIP / IRS     Alertas dinámicas
y Monedas            Pagos y Tickets      vencimientos         preparados           por promedio móvil
```

---

## Estado Esperado al Final de Cada Sprint

### Al finalizar el Sprint 1
- **Capacidades:** Registro de Tenants con configuración de localización completa (moneda, símbolo, idioma, zona horaria, formato de fecha); administración de sucursales; gestión de roles (`ADMINISTRADOR`, `CAJERO`, `REPONEDOR`); autenticación JWT con refresh tokens; enrutamiento seguro a través de Kong 3.6; shell de escritorio Electron + React operativo con login y dashboard funcional.
- **Microservicios activos:** Tenant & Identity Service (ASP.NET Core 8) + Kong API Gateway + PostgreSQL (`postgres-tenant`).

### Al finalizar el Sprint 2
- **Capacidades:** Catálogo completo de productos con generación de códigos QR (PNG); motor de conversión de unidades de medida (UOM); precios dinámicos por sucursal; módulo POS con apertura/cierre de turnos de caja; captura automática de peso desde balanza externa; procesamiento de pagos (efectivo/tarjeta/mixto) y emisión de tickets.
- **Microservicios activos:** + Catalog & Pricing Service + POS & Cart Service.

### Al finalizar el Sprint 3
- **Capacidades:** Actualización automática de stock tras venta mediante eventos de Kafka (`sale.completed`); cubicaje y control de capacidad máxima de refrigeradores; trazabilidad de lotes con regla FEFO; emisión de alertas de bajo stock y vencimiento temprano.
- **Microservicios activos:** + Warehouse & Inventory Service + Apache Kafka Broker.

### Al finalizar el Sprint 4
- **Capacidades:** Motor fiscal internacional con cálculo dinámico; exenciones automáticas (0% IVA a verduras) e impuestos compuestos (nacionales + estatales); precios finales desglosados en ticket; preparación de contratos tributarios (SII, AFIP, IRS).
- **Microservicios activos:** + Tax & Compliance Service.

### Al finalizar el Sprint 5 (MVP Completo)
- **Capacidades:** Órdenes de compra a proveedores y cálculo automatizado del Costo Landed; tracking logístico de importaciones; dashboards analíticos consolidados; alertas dinámicas basadas en promedios móviles semanales; notificaciones proactivas en tiempo real.
- **Microservicios activos:** + Supply Chain & Import Service + Analytics & Notification Service (7 de 7 microservicios en producción).

---

## Stack Tecnológico Oficial

### Backend (.NET 8 Enterprise)
| Tecnología | Versión | Uso en el Proyecto |
|---|---|---|
| **C# / .NET** | 8.0 LTS | Lenguaje y plataforma base de todos los microservicios |
| **ASP.NET Core Web API** | 8.0 | Framework REST de alto rendimiento con Clean Architecture |
| **Entity Framework Core** | 8.0 | ORM oficial con `Npgsql.EntityFrameworkCore.PostgreSQL` |
| **dotnet ef migrations** | 8.0 | Herramienta de versionamiento y ejecución de migraciones de BD |
| **PostgreSQL** | 16 (Alpine) | Motor de base de datos relacional (una instancia por servicio) |
| **Apache Kafka** | 3.6 / Confluent | Bus de eventos asíncrono inter-servicios |
| **Confluent.Kafka** | 2.3+ | Cliente .NET de alto rendimiento para productores/consumidores |
| **JWT Bearer Authentication** | .NET 8 | Autenticación stateless (`Microsoft.AspNetCore.Authentication.JwtBearer`) |
| **BCrypt.Net-Next** | 4.0+ | Hashing seguro de contraseñas de usuarios |
| **FluentValidation** | 11.x | Validación declarativa de DTOs y reglas de negocio |
| **Swashbuckle / OpenAPI** | 6.5+ | Documentación y contratos Swagger generados automáticamente |
| **xUnit** | 2.6+ | Framework de pruebas unitarias y de integración |
| **FluentAssertions & Moq** | Últimas | Aserciones legibles y dobles de prueba para repositorios/servicios |
| **WebApplicationFactory** | .NET 8 | Testing de integración end-to-end con bases de datos de prueba |

### API Gateway & Red
| Tecnología | Versión | Uso en el Proyecto |
|---|---|---|
| **Kong Gateway** | 3.6 (Alpine) | API Gateway unificado en modo declarativo (DB-less) |
| **Kong Plugins** | 3.6 | `jwt`, `rate-limiting`, `cors`, `response-transformer` |

### Frontend (Desktop Client)
| Tecnología | Versión | Uso en el Proyecto |
|---|---|---|
| **Electron** | 29+ | Shell de aplicación de escritorio nativa |
| **React** | 18+ | Librería de componentes para la interfaz de usuario (Renderer) |
| **TypeScript** | 5.x | Tipado estático estricto en todo el cliente |
| **React Router** | 6.x (`HashRouter`) | Enrutamiento SPA compatible con el protocolo de archivos de Electron |
| **Zustand & electron-store** | Últimas | Manejo de estado global y persistencia segura en disco |
| **Axios** | 1.6+ | Cliente HTTP con interceptores para inyección de JWT y manejo de errores |
| **CSS Modules / PostCSS** | Últimas | Estilos modulares encapsulados con diseño responsive |
| **Playwright** | 1.42+ | Pruebas automatizadas E2E sobre el ejecutable de Electron |

### DevOps & Contenedores
| Tecnología | Uso en el Proyecto |
|---|---|
| **Docker & Docker Compose** | Orquestación local de PostgreSQL, Kafka, Zookeeper, Kong y microservicios |
| **Imágenes oficiales .NET** | `mcr.microsoft.com/dotnet/sdk:8.0` (build) y `mcr.microsoft.com/dotnet/aspnet:8.0` (runtime) |
| **GitHub Actions** | Pipelines CI/CD: `dotnet format`, `dotnet build`, `dotnet test`, `docker build` |
| **Coverlet** | Recolección de métricas de cobertura de código (mínimo 70%) |

---

## Equipo y Capacidad Total

| Integrante | Rol y Disciplinas Principales | Sprints Asignados |
|---|---|---|
| **Diego** | DevOps / Arquitectura .NET / API Gateway Kong | S1 - S5 |
| **Sebastián** | Backend Lead / DDD / Seguridad JWT / EF Core | S1 - S5 |
| **Martín** | Base de Datos / EF Core Migrations / Backend / UI | S1 - S5 |
| **Rodrigo** | Frontend Lead / Electron / React / UX | S1 - S5 |
| **Daniel** | QA Lead / Testing xUnit / Playwright E2E | S1 - S5 |
| **Nicolás** | Backend / Kafka Integration / CI-CD Pipelines | S1 - S5 |

| Indicador | Valor |
|---|---|
| Miembros del equipo | 6 desarrolladores full-stack |
| Dedicación individual semanal | 5 horas / semana |
| Capacidad semanal del equipo | 30 horas / semana |
| Duración de cada Sprint | 4 semanas |
| Cantidad total de Sprints | 5 Sprints |
| **Capacidad Total del Proyecto** | **600 horas** |

---

## Glosario Técnico y de Negocio

- **Tenant:** Entidad comercial (minimarket o empresa contratante) que opera de forma totalmente aislada.
- **Sucursal:** Establecimiento físico perteneciente a un Tenant con su propio stock, precios y cajas.
- **FEFO (First Expired, First Out):** Regla de inventario donde el lote más próximo a caducar se despacha primero.
- **UOM (Unit of Measure):** Unidad de medida para venta e inventario (`kg`, `lb`, `L`, `oz`, `unidad`).
- **Costo Landed:** Costo total puesto en tienda de una mercadería importada (`FOB + Flete + Seguro + Aduana`).
- **Database per Service:** Patrón arquitectónico donde cada microservicio es dueño de su propio esquema de datos.
- **Kong DB-less:** Modo de operación de Kong basado en un archivo de configuración declarativo (`kong.yml`) sin base de datos propia.
- **EF Core Migrations:** Herramienta de Entity Framework para versionar incrementalmente el esquema de PostgreSQL.
- **xUnit:** Framework moderno y estándar en el ecosistema .NET para ejecución de tests automatizados.
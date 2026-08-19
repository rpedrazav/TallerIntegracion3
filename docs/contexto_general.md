---
title: "Contexto General del Proyecto — Vision Macro y Micro"
aliases:
  - Vision del Proyecto
  - Contexto General
  - Project Overview
tags:
  - proyecto/minimarket
  - tipo/vision
  - tipo/contexto
  - estado/referencia
created: 2026-08-17
version: "1.0"
sprints_totales: 5
equipo: 6
capacidad_total: 600h
estado: Documento de Referencia
---

# Sistema de Gestion Global de Minimarkets
## Vision Macro y Micro — Entregable Final del Proyecto

> [!IMPORTANT] Proposito de este documento
> Este documento es la fuente de verdad del proyecto. Define QUE se construye, POR QUE, COMO esta organizado y QUE se espera al finalizar cada Sprint y el proyecto completo. Debe ser consultado antes de planificar cualquier Sprint, tomar decisiones de arquitectura o incorporar a un nuevo integrante.

---

## Indice

- [[#La Vision del Producto]]
- [[#El Problema que Resuelve]]
- [[#Arquitectura General del Sistema]]
- [[#Los 7 Microservicios — Descripcion Completa]]
- [[#Reglas de Oro Tecnicas]]
- [[#Hoja de Ruta por Sprint — Vision Macro]]
- [[#Estado Esperado al Final de Cada Sprint]]
- [[#El Producto Final — Que puede hacer un minimarket al terminar el proyecto]]
- [[#Metricas de Exito del MVP]]
- [[#Stack Tecnologico]]
- [[#Equipo y Capacidad Total]]
- [[#Glosario]]

---

## La Vision del Producto

**GlobalMart OS** es un sistema de gestion operativa todo-en-uno para minimarkets, disenado para operar en cualquier pais del mundo desde el primer dia.

No es solo una caja registradora. Es un ERP completo que permite a un dueno de minimarket:

- Vender productos en el mostrador con integracion de balanza fisica
- Gestionar su stock con alertas inteligentes de caducidad y bajo inventario
- Importar mercaderia internacional calculando el costo real (proveedor + flete + aranceles)
- Cumplir con las obligaciones fiscales de su pais automaticamente
- Tomar decisiones con reportes y dashboards en tiempo real
- Escalar de una sucursal a una cadena de tiendas sin cambiar de software

> [!TIP] La propuesta de valor central
> Un dueno de minimarket en Santiago, Buenos Aires o Ciudad de Mexico instala el mismo sistema y este se adapta automaticamente a su moneda, idioma, impuestos y regulaciones locales. Sin configuracion manual. Sin desarrolladores.

---

## El Problema que Resuelve

| Problema del mercado | Como lo resuelve GlobalMart OS |
|---|---|
| Los POS tradicionales son soluciones de una sola tienda, sin gestion centralizada | Arquitectura Multi-Tenant: un sistema para N sucursales bajo un mismo Tenant |
| Los sistemas fiscales son rigidos y no se adaptan a distintos paises | Tax & Compliance Service con motor fiscal dinamico por pais |
| El inventario se gestiona manualmente en planillas | Warehouse Service con FEFO automatico, alertas de vencimiento y control de capacidad de refrigeradores |
| Las importaciones no tienen calculo de costo real | Supply Chain Service con algoritmo de Costo Landed (proveedor + flete + aranceles) |
| Los reportes son estaticos y llegan tarde | Analytics Service con alertas dinamicas basadas en el promedio de ventas |
| Cada pais tiene distintos impuestos y exenciones | Motor fiscal con soporte de impuestos compuestos y exenciones automaticas (ej. 0% IVA en vegetales) |

---

## Arquitectura General del Sistema

```
+--------------------------------------------------+
|              ELECTRON DESKTOP APP                |
|  +-----------------+    +---------------------+ |
|  |  Main Process   |    |  Renderer Process   | |
|  |  (Node.js)      |    |  (React + TS)       | |
|  |  Spawns FastAPI |<-->|  axios → localhost   | |
|  |  Manages DB     |    |  Pages, Components  | |
|  +-----------------+    +---------------------+ |
+--------------------------------------------------+
          | spawns (child_process)
          ↓
+---------------------------+
|  FastAPI Services (local) |
|  tenant-identity  :3001   |
|  catalog-pricing  :3002   |
|  pos-cart         :3003   |
|  tax-compliance   :3004   |
|  warehouse        :3005   |
|  supply-chain     :3006   |
|  analytics        :3007   |
+---------------------------+
          |
    +-----+-----+
    |           |
+---v---+  +---v---+
|Postgres|  |Kafka  |
|(local) |  |(S3+)  |
+-------+  +-------+
```

---

## Los 7 Microservicios — Descripcion Completa

### S1 — Tenant & Identity Service (Puerto 3001)

**Proposito:** Es el "portero" del sistema. Gestiona quienes son los clientes del software (Tenants = minimarkets contratantes) y quienes pueden usar el sistema dentro de cada uno.

**Responsabilidades finales:**
- Registrar y configurar nuevos minimarkets (Tenants) con su localizacion completa
- Gestionar sucursales dentro de cada Tenant
- Autenticar usuarios (login JWT con refresh token)
- Controlar roles: SUPER_ADMIN, ADMINISTRADOR, CAJERO, REPONEDOR
- Mantener la configuracion de localizacion: timezone (IANA), idioma (BCP-47), formato de fecha, moneda (ISO 4217) y simbolo monetario

**Base de datos propia:** `postgres-tenant`

**Tablas clave:** tenants, sucursales, users, refresh_tokens

**Eventos que emite (Kafka):**
- `tenant.created` — cuando se registra un nuevo minimarket
- `user.registered` — cuando se crea un nuevo usuario

---

### S2 — Catalog & Pricing Service (Puerto 3002)

**Proposito:** El "catalogo universal" del minimarket. Centraliza todos los productos, sus precios y las reglas de como se venden.

**Responsabilidades finales:**
- CRUD completo de productos con codigo de barras y QR generado automaticamente
- Gestion de categorias jerarquicas (padre -> hijo)
- Motor de conversion de Unidades de Medida (UOM): kg/lb, L/oz, unidad/docena
- Precios dinamicos: un mismo producto puede tener distintos precios por sucursal o pais
- Soporte para productos de peso variable (se venden por kg, el precio se calcula en el POS con la balanza)

**Base de datos propia:** `postgres-catalog`

**Tablas clave:** productos, categorias, producto_precios, unidades_medida

**Eventos que emite (Kafka):**
- `product.created`
- `product.price_updated`

---

### S3 — POS & Cart Service (Puerto 3003)

**Proposito:** La "caja registradora inteligente". Es el corazon operativo del minimarket — donde ocurre la venta.

**Responsabilidades finales:**
- Gestion de turnos de caja: apertura (con monto inicial) y cierre (reporte de turno)
- Carrito de compras: agregar productos por codigo de barras, QR o busqueda manual
- Integracion con balanza fisica (serial RS-232, USB HID, red TCP/IP) para productos vendidos por peso
- Procesamiento de pagos: efectivo (calculo de vuelto), tarjeta, pago mixto
- Generacion de boleta/ticket imprimible
- Historial de ventas y anulaciones (solo ADMINISTRADOR)

**Base de datos propia:** `postgres-pos`

**Tablas clave:** turnos_caja, ventas, venta_items, pagos

**Eventos que emite (Kafka):**
- `sale.completed` — cuando se completa una venta (el Inventory Service lo escucha para descontar stock)
- `shift.closed` — cuando se cierra un turno de caja

---

### S4 — Warehouse & Inventory Service (Puerto 3005)

**Proposito:** El "bodeguero digital". Controla el stock, el espacio fisico y la vida util de los productos.

**Responsabilidades finales:**
- Entradas y salidas de stock (recepciones, mermas, transferencias entre sucursales)
- Actualizacion automatica de stock al escuchar el evento `sale.completed` de Kafka
- Registro de la capacidad maxima (volumen/area) de cada refrigerador fisico de la tienda
- Bloqueo automatico de ordenes de compra si los productos entrantes superan el volumen disponible en las neveras
- Regla FEFO (First Expired, First Out): seguimiento de lotes y fechas de caducidad
- Emision de alertas de bajo stock y productos proximos a vencer

**Base de datos propia:** `postgres-warehouse`

**Tablas clave:** productos_stock, lotes, refrigeradores, transferencias, mermas

**Eventos que escucha (Kafka):**
- `sale.completed` — descuenta el stock automaticamente

**Eventos que emite (Kafka):**
- `stock.low` — alerta de bajo stock
- `product.expiring_soon` — alerta de caducidad proxima

---

### S5 — Tax & Compliance Service (Puerto 3004)

**Proposito:** El "contador automatico". Separa la logica fiscal del codigo fuente principal para adaptarse a cualquier pais.

**Responsabilidades finales:**
- Motor de calculo fiscal dinamico: dado un producto y un pais, calcula el impuesto correcto
- Soporte de impuestos compuestos: aranceles nacionales + estatales sumados
- Exenciones automaticas: 0% IVA en vegetales, impuesto adicional en licores, etc.
- Precios dinamicos segun sucursal o pais de operacion
- Arquitectura preparada para conectar con entidades tributarias: SII (Chile), AFIP (Argentina), IRS (USA)

**Base de datos propia:** `postgres-tax`

**Tablas clave:** reglas_fiscales, exenciones, calculos_impuesto, categorias_tributarias

**Nota:** Este servicio es consumido por el POS Service en el momento de calcular el total de una venta.

---

### S6 — Supply Chain & Import Service (Puerto 3006)

**Proposito:** El "agente de importaciones". Gestiona las compras a proveedores, especialmente las internacionales.

**Responsabilidades finales:**
- Creacion de ordenes de compra a proveedores nacionales e internacionales
- Seguimiento del estado de los envios (en transito, en aduana, entregado)
- Algoritmo de calculo del "Costo Landed": precio del proveedor + flete + aranceles aduaneros
- El Costo Landed actualiza el costo base del producto en el Catalog Service

**Base de datos propia:** `postgres-supply`

**Tablas clave:** ordenes_compra, proveedores, envios, calculos_costo_landed

**Eventos que emite (Kafka):**
- `purchase_order.received` — cuando llega una orden (Inventory Service actualiza stock)

---

### S7 — Analytics & Notification Service (Puerto 3007)

**Proposito:** El "asistente inteligente" del dueno. Centraliza los reportes y genera alertas proactivas.

**Responsabilidades finales:**
- Dashboard de ventas: totales del dia, semana, mes; comparativas entre sucursales
- Alertas dinamicas de bajo stock basadas en el promedio de ventas de la ultima semana (no umbral fijo)
- Alertas tempranas de caducidad: notifica X dias antes de que venzan lacteos y vegetales
- Escucha los eventos de Kafka para actualizar los reportes en tiempo real
- Notificaciones por email/push cuando se dispara una alerta

**Base de datos propia:** `postgres-analytics`

**Tablas clave:** metricas_ventas, alertas, notificaciones, configuraciones_reporte

**Eventos que escucha (Kafka):**
- `sale.completed`
- `stock.low`
- `product.expiring_soon`
- `shift.closed`

---

## Reglas de Oro Tecnicas

> [!CAUTION] Estas reglas son INQUEBRANTABLES en todo el proyecto
> Violar cualquiera de estas reglas requiere aprobacion explicita del lider de proyecto y debe quedar documentada en un ADR.

| Regla | Descripcion | Consecuencia de violarla |
|---|---|---|
| **R1 — Controlador de acceso local** | El Electron Main Process es el unico punto de entrada al sistema. Todos los servicios FastAPI escuchan SOLO en 127.0.0.1 (localhost) y no estan expuestos a la red externa. | Brecha de seguridad, acceso no autorizado desde la red local |
| **R2 — Database per Service** | Cada microservicio es dueno exclusivo de su propia BD. Ningun servicio puede hacer queries a la BD de otro. | Acoplamiento de datos, imposibilidad de escalar servicios independientemente |
| **R3 — Comunicacion asincrona** | Los servicios se comunican entre si SOLO a traves del bus de eventos (Kafka). No hay llamadas HTTP directas de servicio a servicio. | Acoplamiento temporal, fallos en cascada |
| **R4 — Multi-Tenant isolation** | Toda query a la BD debe incluir `tenant_id` en el WHERE. Un usuario nunca puede ver datos de otro Tenant. | Fuga de datos critica entre clientes |
| **R5 — JWT stateless** | El token JWT incluye `tenantId`, `sucursalId`, `role` y `tenantConfig`. Los servicios no consultan la BD de Identity para validar cada request. | Latencia alta, dependencia de Identity en cada llamada |

---

## Hoja de Ruta por Sprint — Vision Macro

```
SPRINT 1           SPRINT 2           SPRINT 3           SPRINT 4           SPRINT 5
(Semanas 1-4)      (Semanas 5-8)      (Semanas 9-12)     (Semanas 13-16)    (Semanas 17-20)
-----------        -----------        -----------         -----------        -----------
Infraestructura    Catalogo y POS     Inventario          Motor Fiscal       Logistica y
Base, Identidad    basico (MVP de     Avanzado y          y Reglas           Analitica
y Multi-Tenant     venta)             Kafka               Internacionales
                                      
S1 — Tenant &      S2 — Catalog &     S4 — Warehouse &    S5 — Tax &         S6 — Supply
Identity           Pricing            Inventory           Compliance         Chain & Import
(completo)         (completo)         (completo)          (completo)         (completo)

API Gateway        POS & Cart         Bus de mensajes     Precios            S7 — Analytics
(base)             (completo)         (Kafka activo)      dinamicos          & Notification
                                                          por pais           (completo)

Login, roles,      Carrito, caja,     FEFO, neveras,      Exenciones         Costo Landed,
sucursales,        balanza, pagos,    alertas stock,      fiscales,          alertas
localizacion,      boleta             caducidad           SII/AFIP/IRS       inteligentes
moneda
```

---

## Estado Esperado al Final de Cada Sprint

### Al finalizar el Sprint 1

**El sistema puede:**
- Registrar un nuevo minimarket (Tenant) con su pais, moneda, idioma, zona horaria, formato de fecha y simbolo monetario
- Crear sucursales dentro del minimarket
- Registrar usuarios con roles Cajero, Administrador y Reponedor
- Autenticar usuarios con JWT (login, logout, refresh token)
- Enrutar todas las peticiones a traves del API Gateway (Kong)
- Garantizar que dos Tenants distintos estan completamente aislados entre si

**Entrega visible:** Un Administrador puede registrar su minimarket, crear su sucursal y dar acceso a sus empleados.

**Microservicios activos:** Tenant & Identity Service + API Gateway

---

### Al finalizar el Sprint 2

**El sistema puede:**
- Registrar productos con codigo de barras, QR generado automaticamente, categoria y precio
- Configurar unidades de medida y conversiones automaticas (kg/lb, L/oz)
- Definir precios distintos por sucursal para un mismo producto
- Abrir un turno de caja
- Procesar una venta completa: escanear productos, pesar con balanza fisica, cobrar en efectivo/tarjeta y emitir ticket
- Cerrar el turno con el reporte del dia

**Entrega visible:** Un Cajero puede abrir su caja, registrar una venta real con productos pesados en una balanza y cobrarla.

**Microservicios activos:** + Catalog & Pricing Service + POS & Cart Service

---

### Al finalizar el Sprint 3

**El sistema puede:**
- Descontar el stock automaticamente cada vez que se completa una venta (via Kafka)
- Registrar el volumen maximo de cada refrigerador fisico de la tienda
- Bloquear una orden de compra si el producto nuevo no cabe en el refrigerador
- Aplicar la regla FEFO: al vender un producto, el sistema indica que lote usar primero
- Emitir alertas de bajo stock basadas en el nivel actual vs. el umbral configurado
- Emitir alertas de caducidad proxima para lacteos y vegetales

**Entrega visible:** El Reponedor recibe una alerta diciendo "El Yogur Colun vence en 3 dias, hay 12 unidades en el refrigerador 2".

**Microservicios activos:** + Warehouse & Inventory Service + Apache Kafka (activo)

---

### Al finalizar el Sprint 4

**El sistema puede:**
- Calcular el impuesto correcto de cada producto segun el pais del Tenant automaticamente
- Aplicar exenciones fiscales: 0% IVA a vegetales, impuesto adicional a licores
- Soportar impuestos compuestos: IVA nacional + arancel estatal/municipal
- Mostrar el precio final en el POS con el desglose de impuestos en el ticket
- Aplicar precios distintos segun la sucursal o el pais de operacion desde el catalogo
- (Arquitectura lista para) conectar con SII (Chile), AFIP (Argentina) o IRS (USA)

**Entrega visible:** Al vender un kilo de tomates en Chile, el POS aplica 0% IVA automaticamente. Al vender cerveza, aplica el impuesto adicional correspondiente.

**Microservicios activos:** + Tax & Compliance Service

---

### Al finalizar el Sprint 5 (Producto Final del MVP)

**El sistema puede:**
- Crear una orden de compra a un proveedor internacional
- Calcular el Costo Landed de la mercaderia: precio proveedor + flete + aranceles aduaneros
- Hacer seguimiento del estado del envio hasta la entrega
- Mostrar dashboards de ventas por dia, semana, mes y sucursal
- Emitir alertas inteligentes de bajo stock basadas en el PROMEDIO de ventas de la ultima semana
- Emitir alertas tempranas de caducidad con enfasis en lacteos y vegetales
- Enviar notificaciones por email o push cuando se dispara una alerta critica

**Entrega visible:** El dueno del minimarket ve en su dashboard que "las ventas de bebidas bajaron 20% esta semana" y recibe una alerta "El stock de leche se agotara en 2 dias segun el ritmo de ventas actual".

**Microservicios activos:** + Supply Chain & Import Service + Analytics & Notification Service

---

## El Producto Final — Que puede hacer un minimarket al terminar el proyecto

### Un dia tipico con GlobalMart OS

**7:00 AM — Apertura**
El Cajero (Nicolas, en la sucursal de Santiago) abre el sistema, hace login con su cuenta y abre el turno de caja registrando $50.000 CLP de fondo inicial. El sistema sabe automaticamente que esta en Chile, que la moneda es CLP con simbolo `$`, formato de fecha `DD/MM/YYYY` y timezone `America/Santiago`.

**8:30 AM — Primera venta**
Una clienta lleva 1.3 kg de tomates cherry y 2 yogures. El Cajero escanea el codigo QR de los tomates -> el POS activa el modo balanza -> la balanza serial envia el peso 1.3 kg -> el precio se calcula: $890/kg x 1.3 = $1.157. El Tax & Compliance Service aplica 0% IVA a las verduras. Para los yogures aplica 19% IVA. El total con desglose se muestra en pantalla. La clienta paga en efectivo, el sistema calcula el vuelto. Se imprime el ticket.

**Al mismo tiempo, en segundo plano:**
El evento `sale.completed` viaja por Kafka -> el Inventory Service descuenta 1.3 kg de tomates y 2 yogures del stock -> el Analytics Service actualiza el dashboard de ventas del dia.

**10:00 AM — Alerta del sistema**
El Administrador (Diego, en la oficina central) recibe una notificacion push: "Queso Gouda — Lote 2026-08-20 vence en 3 dias. 8 unidades en nevera 1. Precio sugerido de descuento: $890 (20% off)".

**11:00 AM — Orden de compra**
El Administrador crea una orden de compra a un proveedor en Argentina: 50 kg de yerba mate. El Supply Chain Service calcula el Costo Landed: precio ARS 12.000 + flete USD 80 + arancel de importacion Chile 6% = costo real por kg. Este costo se usa para calcular el margen real del producto.

**6:00 PM — Cierre**
El Cajero cierra el turno. El sistema genera el reporte: ventas totales del dia, efectivo vs. tarjeta, diferencia con el fondo inicial, productos mas vendidos. El Administrador lo ve en su dashboard en tiempo real.

---

## Metricas de Exito del MVP

### Funcionales

| Metrica | Objetivo al finalizar Sprint 5 |
|---|---|
| Microservicios operativos | 7 de 7 corriendo en Docker |
| Cobertura de tests unitarios | >= 70% en cada servicio |
| Tests E2E pasando | Flujo completo: registro -> login -> venta -> stock descontado |
| Tiempo de respuesta del POS | < 500 ms para registrar un producto en el carrito |
| Calculo fiscal correcto | 100% de precision en casos de prueba definidos por pais |
| Alertas de stock | Disparadas en <= 30 segundos tras completar una venta |

### Tecnicos

| Metrica | Objetivo |
|---|---|
| Aislamiento de datos | 0 queries cross-tenant en produccion |
| Disponibilidad del Gateway | 99.9% uptime en entorno de staging |
| Pipeline CI verde | 100% de merges a `develop` pasan lint + tests + build |
| Tiempo de arranque del stack | docker compose up completo en < 2 minutos |

---

## Stack Tecnologico

### Backend
| Tecnologia | Uso |
|---|---|
| **Python 3.12** | Lenguaje principal para todos los microservicios |
| **FastAPI** | Framework web async de alto rendimiento |
| **SQLAlchemy 2.x (async)** | ORM async para PostgreSQL |
| **Alembic** | Migraciones de base de datos |
| **PostgreSQL 16** | Base de datos relacional (una instancia por servicio) |
| **Pydantic v2** | Validacion de datos y esquemas de request/response |
| **python-jose + passlib[bcrypt]** | Autenticacion JWT stateless + hash de passwords |
| **FastAPI OpenAPI (/docs)** | Documentacion automatica de API |
| **pydantic-settings** | Gestion de configuracion y variables de entorno |
| **Apache Kafka** | Bus de mensajes para comunicacion asincrona entre servicios (Sprint 3) |
| **uvicorn** | Servidor ASGI para correr FastAPI |
| **prometheus-client** | Exposicion de metricas para monitoreo |

### Frontend (Desktop)
| Tecnologia | Uso |
|---|---|
| **Electron** | Shell de aplicacion de escritorio (empaqueta y lanza la app) |
| **React 18 + TypeScript** | UI del renderer process: POS, Admin, Reportes |
| **React Router (HashRouter)** | Enrutamiento client-side compatible con protocolo `file://` |
| **electron-store + IPC** | State management y comunicacion Main ↔ Renderer |
| **Axios** | Cliente HTTP que llama a `http://127.0.0.1:PORT` (localhost) |
| **CSS Modules** | Estilos encapsulados por componente |
| **Playwright (_electron.launch)** | Tests E2E compatibles con Electron |
| **Electron Forge / electron-vite** | Build y empaquetado de la aplicacion de escritorio |

### DevOps e Infraestructura
| Tecnologia | Uso |
|---|---|
| **Docker + Docker Compose** | Orquestacion del entorno local y staging |
| **python:3.12-slim** | Imagen base de los contenedores de los servicios FastAPI |
| **GitHub Actions** | Pipeline CI/CD: lint, tests, build Docker, deploy staging |
| **prometheus-client** | Monitoreo de metricas operativas desde Python |
| **pytest + httpx + pytest-asyncio + pytest-cov** | Tests unitarios e integracion del backend async |
| **black + flake8 + isort** | Linting y formateo de codigo Python |
| **pre-commit** | Hooks de pre-commit para mantener calidad del codigo |
| **pip + pyproject.toml** | Gestion de dependencias y configuracion del proyecto |

---

## Equipo y Capacidad Total

| Integrante | Disciplinas dominantes | Sprints |
|---|---|---|
| **Diego** | DevOps / Arquitectura / Backend | S1-S5 |
| **Sebastian** | Backend / DDD / Seguridad | S1-S5 |
| **Martin** | Base de datos / Backend / Frontend | S1-S5 |
| **Rodrigo** | Frontend / UX / React | S1-S5 |
| **Daniel** | QA / Testing / Playwright | S1-S5 |
| **Nicolas** | DevOps / CI-CD / Backend | S1-S5 |

| Indicador | Valor |
|---|---|
| Personas en el equipo | 6 |
| Horas por persona por semana | 5 h |
| Semanas por Sprint | 4 |
| Total de Sprints | 5 |
| **Capacidad total del proyecto** | **600 horas** |

---

## Glosario

| Termino | Definicion |
|---|---|
| **Tenant** | Un minimarket contratante del sistema. Cada Tenant tiene sus propios datos aislados. |
| **Sucursal** | Una tienda fisica dentro de un Tenant. Un Tenant puede tener N sucursales. |
| **FEFO** | First Expired, First Out. Regla de inventario: vender primero lo que vence antes. |
| **UOM** | Unit of Measure. Unidad de medida de un producto (kg, lb, L, oz, unidad, docena). |
| **Costo Landed** | El costo real de un producto importado: precio proveedor + flete + aranceles aduaneros. |
| **Multi-Tenant** | Arquitectura donde un mismo sistema sirve a multiples clientes (Tenants) de forma aislada. |
| **API Gateway** | Puerta de entrada unica al sistema. Todo el trafico pasa por Kong antes de llegar a los servicios. |
| **DDD** | Domain-Driven Design. Los microservicios se dividen por contextos de negocio, no por tablas. |
| **Kafka** | Bus de mensajes asincrono. Los servicios se comunican publicando y escuchando eventos. |
| **JWT** | JSON Web Token. Token de autenticacion que contiene el perfil del usuario y su configuracion de Tenant. |
| **IANA Timezone** | Formato estandar para zonas horarias (ej. America/Santiago, America/Buenos_Aires). |
| **ISO 4217** | Estandar internacional para codigos de moneda (ej. CLP, ARS, USD, EUR). |
| **BCP-47** | Estandar para codigos de idioma y locale (ej. es-CL, es-AR, en-US, pt-BR). |
| **ADR** | Architecture Decision Record. Documento que justifica una decision de arquitectura tomada. |
| **Impuesto compuesto** | Impuesto formado por multiples capas (ej. IVA nacional 19% + arancel municipal 2%). |
| **Exencion fiscal** | Regla que aplica 0% de impuesto a ciertos productos (ej. vegetales frescos en Chile). |
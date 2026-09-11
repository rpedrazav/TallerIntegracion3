# GlobalMart OS — Documento de Contexto Maestro
### Briefing Completo para Transferencia a Cualquier IA

> **Version:** 1.0 Final · Septiembre 2026  
> **Proposito:** Este documento es el contexto completo y exhaustivo del proyecto GlobalMart OS.  
> Cualquier IA que lea este documento debe comprender el proyecto al 100% sin necesidad de informacion adicional.

---

## INDICE

1. [Descripcion General del Proyecto](#1-descripcion-general-del-proyecto)
2. [Situacion Problema y Solucion](#2-situacion-problema-y-solucion)
3. [Arquitectura del Sistema](#3-arquitectura-del-sistema)
4. [Stack Tecnologico Definitivo](#4-stack-tecnologico-definitivo)
5. [Actores del Sistema](#5-actores-del-sistema)
6. [Los 8 Microservicios](#6-los-8-microservicios)
7. [Reglas de Negocio (MoSCoW)](#7-reglas-de-negocio-moscow)
8. [Requerimientos Funcionales (MoSCoW)](#8-requerimientos-funcionales-moscow)
9. [Requerimientos No Funcionales (MoSCoW)](#9-requerimientos-no-funcionales-moscow)
10. [Casos de Uso por Microservicio (MoSCoW)](#10-casos-de-uso-por-microservicio-moscow)
11. [Arquitectura de Eventos Kafka](#11-arquitectura-de-eventos-kafka)
12. [Integraciones con Sistemas Externos y Hardware](#12-integraciones-con-sistemas-externos-y-hardware)
13. [Diseno RBAC y Gestion Multi-Rol](#13-diseno-rbac-y-gestion-multi-rol)
14. [Diseno Multi-Tenant](#14-diseno-multi-tenant)
15. [Diagrama UML de Casos de Uso — Resumen](#15-diagrama-uml-de-casos-de-uso--resumen)
16. [Matriz de Trazabilidad](#16-matriz-de-trazabilidad)
17. [Sprint 1 — Backlog y Planificacion](#17-sprint-1--backlog-y-planificacion)
18. [Roadmap de Sprints](#18-roadmap-de-sprints)
19. [Decisiones Arquitectonicas Clave](#19-decisiones-arquitectonicas-clave)

---

## 1. Descripcion General del Proyecto

**GlobalMart OS** es un sistema de gestion operativa integral que combina **POS (Point of Sale)** y **ERP (Enterprise Resource Planning)** diseñado especificamente para minimarkets. El sistema es escalable a nivel mundial gracias a su arquitectura Multi-Tenant, donde cada minimarket opera como un inquilino (tenant) completamente aislado e independiente dentro de la misma plataforma.

### Caracteristicas Fundamentales

- **Multi-Tenant:** Cada minimarket es un tenant independiente con sus propios datos, usuarios, configuracion fiscal, moneda, idioma y precios. Ningun tenant puede ver datos de otro.
- **Event-Driven:** La comunicacion entre microservicios es principalmente asincrona mediante Apache Kafka. Los eventos representan hechos del negocio (venta completada, stock actualizado, etc.).
- **Microservicios:** El backend se divide en 8 microservicios especializados, cada uno con su propia base de datos PostgreSQL.
- **Desktop-First:** El frontend es una aplicacion de escritorio nativa construida con Electron + React, diseñada para operar en el mostrador del minimarket.
- **Global-Ready:** Soporta configuracion de pais, moneda, idioma, zona horaria y reglas fiscales especificas por tenant (Chile-SII, Argentina-AFIP, USA-IRS, etc.).

### Alcance del Sistema

El sistema cubre las siguientes areas operativas de un minimarket:
1. Autenticacion y gestion de usuarios con RBAC multi-rol
2. Punto de Venta (carrito, cobro, documentos tributarios)
3. Catalogo de productos con precios dinamicos y promociones
4. Control de inventario con regla FEFO para perecederos
5. Cumplimiento fiscal automatico por pais
6. Cadena de suministro y ordenes de compra con Costo Landed
7. Programa de lealtad para clientes afiliados
8. Analytics, dashboards y notificaciones en tiempo real

---

## 2. Situacion Problema y Solucion

### El Problema (Sin Solucion)

Los minimarkets, especialmente en Latinoamerica y mercados emergentes, enfrentan estos problemas criticos:

| Area | Problema |
|---|---|
| **Punto de Venta** | Cajas sin integracion fiscal. Cobros manuales con errores. Sin pasarela de pago integrada. |
| **Inventario** | Control en papel o Excel. Sin alertas de stock minimo. Productos caducados se venden por falta de control FEFO. |
| **Cumplimiento Fiscal** | Boletas emitidas manualmente. Alto riesgo de errores tributarios. Sin adaptacion por pais. |
| **Multi-Sucursal** | Cada sucursal opera como una isla. Sin visibilidad centralizada. |
| **Compras** | Ordenes en papel/correo. Sin calculo de costo real (flete, aranceles, seguros). |
| **Fidelizacion** | Sin programa de lealtad. Sin conocimiento del cliente recurrente. |
| **Reportes** | Sin datos en tiempo real. Decisiones basadas en informacion desactualizada. |
| **Escalabilidad** | Al crecer a nuevas sucursales o paises, el sistema actual no escala. |

**Consecuencias directas:**
- Perdidas economicas por mermas no registradas y stock caducado no detectado.
- Multas fiscales por documentos tributarios incorrectos.
- Perdida de ventas por desabastecimiento no anticipado.
- Incapacidad de expansion a nuevos mercados sin reemplazar todo el sistema.

### La Solucion (GlobalMart OS)

GlobalMart OS resuelve cada problema de la siguiente manera:

| Area | Solucion |
|---|---|
| **POS** | App de escritorio (Electron) con escaneo, balanza, terminal POS y pasarela de pago integrada. |
| **Inventario** | Control en tiempo real, descuento automatico por venta (Kafka), FEFO obligatorio, alertas de caducidad. |
| **Fiscal** | Integracion con SII/AFIP/IRS. Calculo automatico de impuestos compuestos. DTE con folio electronico validado. |
| **Multi-Sucursal** | Dashboard centralizado con filtros por sucursal. Precios e inventarios independientes por sucursal. |
| **Compras** | Modulo Supply Chain con Costo Landed (flete + aranceles + seguros). Tracking con 3PL. |
| **Fidelizacion** | Programa de puntos, tiers de membresia, cupones y canje de productos. |
| **Reportes** | KPIs en tiempo real via Kafka. Exportacion PDF/Excel. Alertas automaticas multicanal. |
| **Escalabilidad** | Multi-Tenant: agregar una sucursal o pais es configurar un nuevo tenant, no reescribir el sistema. |

---

## 3. Arquitectura del Sistema

### Vista General

```
[Electron + React Frontend]
         |
         v
[Kong API Gateway - JWT Validation, Rate Limiting, Routing]
         |
    _____|___________________________________________
    |        |        |        |        |        |  |
   MS-1    MS-2    MS-3    MS-4    MS-5    MS-6  MS-7  MS-8
    |        |        |        |        |        |  |   |
   [PG]    [PG]    [PG]    [PG]    [PG]    [PG] [PG] [PG]
    |
    v
[Apache Kafka + Zookeeper]  <-- Bus de eventos asincronos
```

### Patron de Arquitectura

- **Backend:** Microservicios RESTful con ASP.NET Core 8
- **Comunicacion sincrona:** REST via Kong API Gateway (operaciones que requieren respuesta inmediata: login, consultas, cobros)
- **Comunicacion asincrona:** Apache Kafka (operaciones event-driven: descuento de stock, acumulacion de puntos, actualizacion de dashboards)
- **Base de datos:** Patron *Database-per-Service* — cada microservicio tiene su propia instancia PostgreSQL 16 completamente aislada
- **Frontend:** Electron 6+ con renderer React 18 + TypeScript 5+
- **API Gateway:** Kong 3.6 DB-less para enrutamiento, validacion JWT y rate-limiting

### Patron Multi-Tenant

El aislamiento entre tenants se implementa mediante:
1. **Discriminador `tenant_id`** en cada tabla de cada base de datos
2. **JWT con claim `tenant_id`** obligatorio en cada request
3. **Middleware ASP.NET Core** que extrae el `tenant_id` del JWT y lo inyecta en cada query de EF Core automaticamente
4. **Validacion en API Gateway (Kong):** rechaza tokens sin tenant valido

---

## 4. Stack Tecnologico Definitivo

### Frontend (Desktop)

| Tecnologia | Version | Rol |
|---|---|---|
| **Electron** | Latest | Aplicacion de escritorio multiplataforma (Windows, macOS, Linux) |
| **React** | 18+ | Libreria de UI dentro del renderer de Electron |
| **TypeScript** | 5+ | Tipado estatico en todo el frontend |
| **electron-store** | — | Persistencia local de tokens y configuracion de usuario |

**Justificacion Electron:** Es el estandar de industria para POS de escritorio con tecnologia web. Permite integracion con hardware local (balanza serial, impresora, cajeron) via Node.js APIs que no estan disponibles en browsers.

### Backend

| Tecnologia | Version | Rol |
|---|---|---|
| **ASP.NET Core** | 8 (LTS) | Framework para los 8 microservicios REST |
| **Entity Framework Core** | 8 | ORM + migraciones de base de datos |
| **C#** | 12 | Lenguaje de programacion del backend |

### Base de Datos

| Tecnologia | Version | Rol |
|---|---|---|
| **PostgreSQL** | 16 | Una instancia dedicada por cada microservicio (8 instancias total) |

### Mensajeria / Eventos

| Tecnologia | Version | Rol |
|---|---|---|
| **Apache Kafka** | Latest | Bus de eventos asincronos entre microservicios |
| **Zookeeper** | — | Coordinacion del cluster Kafka |

**Nota:** Se evaluo usar RabbitMQ + MassTransit por ser mas simple, pero se decidio mantener Kafka por su mayor robustez y capacidad de replay de eventos. Se mantiene la arquitectura original.

### API Gateway

| Tecnologia | Version | Rol |
|---|---|---|
| **Kong** | 3.6 (DB-less) | Enrutamiento, validacion JWT, rate-limiting |

### Testing

| Tecnologia | Version | Rol |
|---|---|---|
| **xUnit** | 2+ | Tests unitarios e integracion del backend |
| **FluentAssertions** | — | Assertions expresivas en xUnit |
| **Moq** | — | Mocking para tests unitarios |
| **Playwright** | — | Tests E2E en la app Electron |
| **WebApplicationFactory** | Built-in | Tests de integracion de controllers ASP.NET Core |

### Infraestructura / DevOps

| Tecnologia | Rol |
|---|---|
| **Docker + Docker Compose** | Contenedorizacion de todos los servicios (desarrollo y produccion) |
| **GitHub Actions** | Pipeline CI/CD: build → test → deploy |
| **Prometheus + Grafana** | Monitoreo, metricas y dashboards de infraestructura |

---

## 5. Actores del Sistema

El sistema tiene **13 actores** divididos en dos categorias:

### Actores Humanos (5)

| Actor | Tipo | Descripcion | Capacidades Clave |
|---|---|---|---|
| **Cajero** | Humano Primario | Opera el POS en mostrador | Abrir/cerrar turno, gestionar carrito, cobrar, emitir comprobantes |
| **Reponedor** | Humano Primario | Gestiona el inventario fisico | Recibir mercancia, aplicar FEFO, registrar mermas, conteo fisico |
| **Administrador** | Humano Primario | Gestiona el tenant completo | Configurar tenant, usuarios, precios, ordenes de compra, reportes |
| **Super_Admin** | Humano Secundario | Dueno del sistema global | Gestion tecnica macro: tenants, tokens globales, auditoria sistema |
| **Cliente Afiliado** | Humano Secundario | Cliente con membresia activa | Identificarse en POS, acumular/canjear puntos, ver historial |

### Actores Sistema / Hardware (8)

| Actor | Tipo | Descripcion | Protocolo |
|---|---|---|---|
| **Balanza Fisica** | Hardware | Dispositivo de pesaje para productos a granel | USB / Serial (COM port) |
| **Terminal POS** | Hardware | Cajon de dinero, impresora de tickets, display cliente, lector de tarjetas | USB / Bluetooth / LAN |
| **Entidad Fiscal** | Sistema Externo | SII (Chile) / AFIP (Argentina) / IRS (USA). Valida folios y DTE | REST / SOAP segun pais |
| **Pasarela de Pago** | Sistema Externo | Stripe, Transbank, Mercado Pago. Procesa tarjetas debito/credito | REST / HTTPS |
| **Sistema de Transporte** | Sistema Externo | 3PL / Courier. Cotizaciones de flete, tracking de envios | REST API |
| **Proveedor Mensajeria** | Sistema Externo | Twilio (SMS), SendGrid (Email), Firebase (Push) | REST API |
| **Servicio Tipos de Cambio** | Sistema Externo | Fixer.io / Open Exchange Rates. Tasas FX en tiempo real | REST API |
| **Bus de Eventos (Kafka)** | Sistema Interno | Mensajeria asincrona entre los 8 microservicios | Apache Kafka Protocol |

### Generalizacion de Roles (Actor Multi-Rol)

En minimarkets pequenos, una persona puede ocupar multiples roles simultaneamente. El sistema modela esto con **herencia de actores UML**:

```
Usuario GlobalMart (superactor)
   ├── Cajero
   ├── Reponedor  
   ├── Administrador
   │      └── Super_Admin (hereda de Administrador)
   └── Cliente Afiliado

Encargado de Tienda (actor compuesto, caso real)
   ├── hereda de Cajero
   ├── hereda de Reponedor
   └── hereda de Administrador
```

El sistema gestiona esto mediante:
- RBAC con array de roles por usuario: `"roles": ["CAJERO", "REPONEDOR", "ADMIN"]`
- Campo `active_role` en el JWT indicando el rol activo en la sesion actual
- UI con selector de rol (RoleSwitcher) cuando el usuario tiene mas de un rol
- Posibilidad de "escalar rol temporalmente" para una sola accion sin cambiar el rol activo

---

## 6. Los 8 Microservicios

### MS-1 · Tenant & Identity Service

**Responsabilidad:** Gestionar la autenticacion, usuarios, roles, configuracion del tenant y tipos de cambio FX.

**Endpoints principales:**
- `POST /auth/login` — Genera JWT con tenant_id y roles
- `POST /auth/refresh` — Renueva token
- `GET/POST/PUT /users` — CRUD de usuarios
- `GET/PUT /tenants/{id}/config` — Configuracion de localizacion
- `GET /tenants/{id}/sucursales` — Gestion de sucursales
- `GET /fx/rates` — Tasas de cambio actuales

**Datos que gestiona:** Usuarios, roles, permisos, configuracion de tenant (pais, moneda, idioma, zona horaria), historial de tasas FX, tokens de sesion, log de accesos.

**Integraciones:** Servicio de Tipos de Cambio (FX) via REST. Publica evento `fx.rate.updated` a Kafka.

---

### MS-2 · Tax & Compliance Service

**Responsabilidad:** Calcular impuestos, gestionar exenciones, solicitar y validar folios electronicos con la entidad fiscal del pais del tenant, y emitir documentos tributarios electronicos (DTE).

**Endpoints principales:**
- `POST /tax/calculate` — Calcula impuesto para un carrito dado el tenant
- `POST /dte/solicitar-folio` — Solicita folio a la Entidad Fiscal
- `POST /dte/emitir` — Emite boleta/factura electronica
- `GET /dte/{id}/estado` — Consulta estado de un DTE
- `GET /reportes/declaracion-fiscal` — Reporte fiscal del periodo

**Logica de impuestos:**
- Soporta IVA simple (Chile 19%, Argentina 21%), IVA compuesto en cascada, y exenciones por producto o cliente.
- El porcentaje y tipo de impuesto se configura por tenant en MS-1.
- Manejo de reintentos automaticos si la Entidad Fiscal falla al validar un folio.

**Integraciones:** Entidad Fiscal (SII/AFIP/IRS) via REST/SOAP segun pais. Consume eventos de MS-5 para registrar hechos imponibles.

---

### MS-3 · Catalog & Pricing Service

**Responsabilidad:** Gestionar el catalogo de productos, codigos de barras/QR, unidades de medida (UOM), precios base, precios dinamicos por sucursal, descuentos y promociones con vigencia por fechas.

**Endpoints principales:**
- `GET/POST/PUT /products` — CRUD de productos
- `GET /products/lookup?barcode={code}` — Busqueda por codigo de barras
- `GET /products/search?q={query}` — Busqueda por nombre
- `GET/POST /prices` — Gestion de precios por producto y sucursal
- `GET/POST /promotions` — Gestion de promociones con fechas
- `POST /uom/convert` — Conversion de unidades de medida

**Conversion de unidades de medida (UOM):**
El sistema soporta conversion entre sistemas de medida para facilitar importaciones internacionales: kg ↔ lb, L ↔ gal, m ↔ ft, etc. Cada producto tiene una UOM base y el sistema convierte automaticamente.

**Productos a granel:** Para productos vendidos por peso (frutas, carnes, granos), el precio se calcula multiplicando el precio por kg/unidad por el peso capturado desde la balanza fisica. El proceso es: escanear producto → balanza envia peso → sistema calcula precio = peso × precio_unitario.

**Integraciones:** Consume evento `fx.rate.updated` de Kafka para actualizar precios en moneda extranjera. Publica `catalog.updated` cuando hay cambios.

---

### MS-4 · Warehouse & Inventory Service

**Responsabilidad:** Gestionar el stock de productos por sucursal, recepcion de mercancia con regla FEFO, control de lotes y fechas de caducidad, capacidad de neveras, mermas y transferencias de stock entre sucursales.

**Endpoints principales:**
- `GET /stock/{productId}` — Consulta stock actual
- `POST /stock/ajuste` — Ajuste manual de stock
- `POST /recepciones` — Registrar recepcion de mercancia
- `GET /lotes` — Consulta lotes y fechas de caducidad
- `POST /mermas` — Registrar merma o perdida
- `POST /transferencias` — Transferir stock entre sucursales
- `POST /conteos` — Registrar conteo fisico de inventario

**Regla FEFO (First Expired, First Out):**
Al registrar una recepcion de mercancia perecedera, el sistema:
1. Registra el lote con su fecha de vencimiento
2. Asigna el lote a su ubicacion en nevera/estante
3. Calcula si la nevera tiene capacidad suficiente (volumen)
4. Al momento de vender, siempre despacha el lote con menor fecha de vencimiento primero
5. Genera alerta cuando un lote esta proximo a caducar (umbral configurable por tenant)

**Flujo asincrono critico:** Cuando MS-5 completa una venta, publica `sale.completed` a Kafka. MS-4 consume este evento y descuenta el stock. Esto garantiza que el POS no se bloquee esperando la actualizacion de inventario.

**Publica a Kafka:** `stock.alert` (stock bajo minimo), `expiry.alert` (proxima caducidad), `stock.updated` (cambio en stock).

---

### MS-5 · POS & Cart Service

**Responsabilidad:** Es el microservicio central del POS. Gestiona turnos de caja, el carrito de compras, el proceso de cobro (efectivo, tarjeta, mixto), la integracion con hardware local (balanza, terminal, impresora) y la emision de comprobantes.

**Endpoints principales:**
- `POST /turnos/abrir` — Abre turno con fondo inicial
- `POST /turnos/cerrar` — Cierra turno y calcula cuadre
- `POST /ventas/iniciar` — Inicia nueva venta (carrito vacio)
- `POST /ventas/{id}/items` — Agrega item al carrito
- `PUT /ventas/{id}/items/{itemId}` — Modifica cantidad
- `DELETE /ventas/{id}/items/{itemId}` — Elimina item
- `POST /ventas/{id}/cobrar` — Procesa cobro (efectivo, tarjeta o mixto)
- `POST /ventas/{id}/anular` — Anula venta y solicita reembolso
- `GET /ventas/{id}/comprobante` — Obtiene datos del comprobante

**Flujo de venta completo:**
1. Cajero abre turno (requiere autenticacion JWT)
2. Cajero inicia nueva venta → se crea carrito vacio
3. Por cada producto: escanea codigo OR busca por nombre OR captura peso desde balanza
4. Sistema calcula total + impuesto via MS-2
5. Sistema muestra total en display del Terminal POS
6. Cajero selecciona metodo de pago:
   - **Efectivo:** sistema calcula vuelto, abre cajon de dinero via Terminal POS
   - **Tarjeta:** Terminal POS lee la tarjeta (NFC/chip/mag), MS-5 envia solicitud a Pasarela de Pago, espera aprobacion/rechazo
   - **Mixto:** combina ambos
7. Al confirmar pago: MS-2 emite DTE, Terminal POS imprime recibo
8. MS-5 publica `sale.completed` a Kafka → MS-4 descuenta stock, MS-8 acumula puntos, MS-7 actualiza KPIs

**Integraciones hardware:**
- Balanza: recibe peso por puerto serial/USB, lo inyecta en el item del carrito
- Terminal POS: protocolo especifico del fabricante para lector de tarjetas, cajon, impresora y display
- Pasarela de Pago: REST HTTPS

---

### MS-6 · Supply Chain & Import Service

**Responsabilidad:** Gestionar el ciclo completo de compras e importaciones: proveedores, ordenes de compra, calculo de Costo Landed, tracking de envios con transportista 3PL, y recepcion de importaciones.

**Endpoints principales:**
- `GET/POST /proveedores` — CRUD de proveedores
- `POST /ordenes-compra` — Crear orden de compra
- `PUT /ordenes-compra/{id}/aprobar` — Aprobar OC
- `POST /ordenes-compra/{id}/landed-cost` — Calcular Costo Landed
- `POST /importaciones` — Registrar embarque
- `GET /importaciones/{id}/tracking` — Estado del envio
- `POST /importaciones/{id}/recepcion` — Confirmar recepcion

**Calculo de Costo Landed:**
```
Costo Landed = Costo Producto
             + Costo de Flete (convertido a moneda local via MS-1 FX)
             + Aranceles e Impuestos de Importacion
             + Seguros
             + Gastos Aduaneros
```
Este calculo permite conocer el costo REAL de cada producto importado para establecer precios con margen correcto.

**Regla de separacion de funciones:** Por configuracion del tenant, el usuario que crea una Orden de Compra no puede ser el mismo que la aprueba. Esto se verifica en backend mediante los claims del JWT.

**Integraciones:** Sistema de Transporte 3PL via API REST (cotizaciones y tracking). Publica `purchase.received` a Kafka cuando se confirma recepcion.

---

### MS-7 · Analytics & Notification Service

**Responsabilidad:** Consumir eventos de Kafka para actualizar dashboards en tiempo real, generar reportes, gestionar alertas y despachar notificaciones multicanal (SMS, Email, Push).

**Endpoints principales:**
- `GET /dashboards/ventas` — Dashboard de ventas con filtros
- `GET /dashboards/inventario` — Dashboard de inventario
- `GET /dashboards/financiero` — Dashboard financiero
- `GET /reportes/ventas` — Generar reporte de ventas
- `GET /reportes/mermas` — Reporte de mermas
- `GET /reportes/top-productos` — Productos mas vendidos
- `POST /alertas/configurar` — Configurar umbrales de alerta
- `GET /notificaciones/historial` — Historial de envios

**Eventos que consume de Kafka:**
- `sale.completed` → actualiza KPIs de ventas en tiempo real
- `sale.reversed` → corrige KPIs por anulacion
- `stock.updated` → actualiza dashboard de inventario
- `stock.alert` → despacha notificacion de stock bajo
- `expiry.alert` → despacha notificacion de caducidad proxima
- `points.updated` → actualiza metricas de programa de lealtad
- `fx.rate.updated` → genera alerta de variacion de tipo de cambio

**Canales de notificacion:**
- SMS via Twilio
- Email via SendGrid
- Push via Firebase Cloud Messaging
Las plantillas de mensaje son configurables por tenant. El sistema registra historial de envios y maneja reintentos si el proveedor falla.

---

### MS-8 · Loyalty & Customer Service

**Responsabilidad:** Gestionar el programa de lealtad de clientes: registro de clientes afiliados, acumulacion y canje de puntos, tiers de membresia, cupones de descuento y comunicacion de estado de cuenta.

**Endpoints principales:**
- `POST /clientes` — Registrar nuevo cliente afiliado
- `GET /clientes/{id}` — Perfil y saldo de puntos
- `GET /clientes/{id}/historial` — Historial de compras
- `POST /clientes/{id}/identificar` — Identificar en POS (por QR, DNI o app)
- `POST /clientes/{id}/canjear` — Canjear puntos por descuento o producto
- `GET/POST /programas` — Configurar programa de lealtad
- `POST /cupones/validar` — Validar cupon en POS

**Logica de acumulacion de puntos:**
- Al completar una venta con cliente afiliado identificado, MS-8 consume el evento `sale.completed` de Kafka
- Los puntos se calculan segun: `puntos = monto_venta * tasa_base * multiplicador_categoria`
- El multiplicador por categoria es configurable (ej: lacteos x2, electrodomesticos x5)
- Los puntos de lealtad expiran automaticamente si no hay actividad en 12 meses

**Tiers de membresia:**
- Cada tenant puede definir sus propios niveles (ej: Bronce, Plata, Oro, Platino)
- Cada nivel tiene beneficios configurables: porcentaje de descuento, acceso anticipado a promociones, etc.
- El nivel sube automaticamente al superar el umbral de puntos acumulados historicos

**Publica a Kafka:** `points.updated` cuando hay cambios en el saldo de un cliente.

---

## 7. Reglas de Negocio (MoSCoW)

### MUST — Obligatorias e Innegociables

| ID | Regla de Negocio |
|---|---|
| RN-01 | Cada tenant debe estar **completamente aislado**: datos, configuracion y usuarios son independientes. Ningun query puede retornar datos de otro tenant. |
| RN-02 | Todo pago con tarjeta debe pasar **obligatoriamente por la pasarela de pago** externa. Jamas se procesa localmente. |
| RN-03 | Toda venta completada debe generar un **documento tributario valido** (boleta o factura) segun la normativa del pais del tenant. |
| RN-04 | El stock de cada producto debe **descontarse automaticamente** al completar una venta exitosa. |
| RN-05 | Los productos perecederos se gestionan bajo la regla **FEFO**: el lote con menor fecha de vencimiento se despacha primero siempre. |
| RN-06 | **Solo se pueden procesar ventas si existe un turno de caja abierto** por el cajero en sesion activa. |
| RN-07 | Todo usuario debe estar **autenticado mediante JWT valido** para cualquier operacion. |
| RN-08 | El **JWT debe incluir**: `user_id`, `tenant_id`, `roles[]`, `active_role`, `sucursal_id`, `exp`. |

### SHOULD — Importantes

| ID | Regla de Negocio |
|---|---|
| RN-09 | El **Costo Landed** se calcula como: `Costo Producto + Flete (en moneda local) + Aranceles + Seguros + Gastos Aduaneros`. |
| RN-10 | Las **alertas de stock minimo** se emiten cuando el stock cae por debajo del umbral configurado, antes de llegar a cero. |
| RN-11 | Los **precios pueden variar por sucursal** dentro del mismo tenant. |
| RN-12 | Un usuario puede tener **multiples roles activos**. El sistema registra el rol activo al momento de cada accion critica. |
| RN-13 | La **separacion de funciones** en OC es configurable por tenant: creador ≠ aprobador de la misma Orden de Compra. |
| RN-14 | Las **devoluciones** requieren permiso explicito del rol Administrador o permiso especial `refund.process` asignado al cajero. |

### COULD — Deseables

| ID | Regla de Negocio |
|---|---|
| RN-15 | Los **puntos de lealtad expiran** automaticamente si no hay actividad del cliente en 12 meses. |
| RN-16 | El **tipo de cambio** se actualiza automaticamente desde Fixer.io cuando la variacion supera el umbral configurado por el tenant. |
| RN-17 | Los **multiplicadores de puntos** de lealtad se configuran por categoria de producto. |
| RN-18 | Los **descuentos manuales en carrito** requieren el permiso `discount.apply` asignado al rol del cajero. |

### WON'T — Fuera de Alcance v1.0

| ID | Regla de Negocio |
|---|---|
| RN-19 | No se gestiona inventario en consignacion. |
| RN-20 | No se soporta multi-moneda dentro de una misma transaccion de venta. |
| RN-21 | No hay integracion con marketplaces online. |
| RN-22 | No se incluye gestion de nomina ni recursos humanos. |

---

## 8. Requerimientos Funcionales (MoSCoW)

### MUST

| ID | Requerimiento |
|---|---|
| RF-01 | El sistema permite **inicio de sesion** con JWT que incluye tenant, roles, sucursal y expiracion. |
| RF-02 | El sistema permite **abrir turno de caja** con registro del monto de fondo inicial. |
| RF-03 | El sistema permite **cerrar turno** con declaracion de billetes/monedas para cuadre. |
| RF-04 | El sistema permite **escanear o buscar productos** para agregarlos al carrito. |
| RF-05 | El sistema **calcula el total** de la venta con impuestos segun reglas fiscales del tenant. |
| RF-06 | El sistema procesa **cobros en efectivo** con calculo automatico de vuelto. |
| RF-07 | El sistema procesa **cobros con tarjeta** enviando solicitud a pasarela y procesando aprobacion/rechazo. |
| RF-08 | El sistema **emite documento tributario** (boleta/factura electronica) al completar una venta. |
| RF-09 | El sistema **descuenta el stock** de cada producto vendido de forma asincrona via Kafka. |
| RF-10 | El sistema permite **registrar recepcion de mercancia** aplicando regla FEFO automaticamente. |
| RF-11 | El sistema permite **crear, editar y desactivar productos** con codigo de barras, precio, UOM y categoria. |
| RF-12 | El sistema permite **configurar el tenant** con pais, moneda, idioma, zona horaria y porcentaje de impuesto. |
| RF-13 | El sistema permite **gestionar usuarios** con creacion, edicion, desactivacion y asignacion de roles RBAC. |
| RF-14 | El sistema garantiza **aislamiento multi-tenant** completo a nivel de datos y logica de negocio. |

### SHOULD

| ID | Requerimiento |
|---|---|
| RF-15 | El sistema se **integra con balanza fisica** (USB/Serial) para capturar peso en productos a granel. |
| RF-16 | El sistema **genera alertas automaticas** de stock minimo y proxima caducidad publicando eventos Kafka. |
| RF-17 | El sistema ofrece **dashboards en tiempo real** de ventas, inventario y KPIs financieros. |
| RF-18 | El sistema permite **crear ordenes de compra** con calculo de Costo Landed. |
| RF-19 | El sistema permite **gestionar multiples sucursales** con inventarios independientes. |
| RF-20 | El sistema soporta **usuarios con multiples roles** simultaneos y cambio de rol activo en sesion. |
| RF-21 | El sistema permite **configurar precios por sucursal** y promociones con rango de fechas. |
| RF-22 | El sistema permite **anular ventas y procesar devoluciones** con reembolso a pasarela si aplica. |

### COULD

| ID | Requerimiento |
|---|---|
| RF-23 | El sistema implementa un **programa de lealtad** con puntos, tiers, cupones y canje. |
| RF-24 | El sistema se integra con **Servicio de Tipos de Cambio** (Fixer.io) para actualizar tasas FX automaticamente. |
| RF-25 | El sistema envia **notificaciones por SMS, email y push** via Twilio/SendGrid/Firebase. |
| RF-26 | El sistema permite **exportar reportes** a PDF y Excel. |
| RF-27 | El sistema permite **rastrear importaciones** en tiempo real con 3PL. |
| RF-28 | El sistema permite **emitir y validar cupones** de descuento en el POS. |

### WON'T

| ID | Requerimiento |
|---|---|
| RF-29 | No se desarrolla app movil para clientes. |
| RF-30 | No hay integracion con e-commerce ni marketplaces. |
| RF-31 | No se incluye gestion de nomina. |
| RF-32 | No se soporta inventario en consignacion v1.0. |

---

## 9. Requerimientos No Funcionales (MoSCoW)

### MUST

| ID | Requerimiento |
|---|---|
| RNF-01 | **Aislamiento de datos:** `tenant_id` como discriminador obligatorio en cada tabla. El middleware ASP.NET Core lo inyecta en cada query automaticamente. |
| RNF-02 | **Rendimiento del POS:** tiempo de respuesta desde escaneo hasta visualizacion en carrito < 2 segundos. |
| RNF-03 | **Disponibilidad:** >= 99.5% (maximo ~3.6 horas de downtime al mes). |
| RNF-04 | **Seguridad:** TLS 1.3 para todos los datos en transito. AES-256 para datos sensibles en reposo. Passwords hasheados con bcrypt. |

### SHOULD

| ID | Requerimiento |
|---|---|
| RNF-05 | **Auditoria:** log inmutable de todas las operaciones criticas (login, venta, anulacion, ajuste de stock) con `user_id`, `tenant_id`, `active_role` y timestamp. |
| RNF-06 | **Multiplataforma:** la app Electron funciona en Windows, macOS y Linux sin cambios de codigo. |
| RNF-07 | **Cobertura de tests:** >= 80% en logica de negocio critica (MS-1, MS-2, MS-5). |

### COULD

| ID | Requerimiento |
|---|---|
| RNF-08 | **i18n:** soporte para multiples idiomas configurables por tenant (ES, EN, PT como minimo). |
| RNF-09 | **Modo offline parcial:** el POS puede procesar ventas en efectivo sin internet, sincronizando al reconectar. |

---

## 10. Casos de Uso por Microservicio (MoSCoW)

### MS-1 · Tenant & Identity Service

| ID | Caso de Uso | Actores | Prioridad |
|---|---|---|---|
| TI-01 | Iniciar Sesion (Login) | Cajero, Reponedor, Admin, Super_Admin, Cliente Afiliado | Must |
| TI-02 | Cerrar Sesion (Logout) | Todos los humanos | Must |
| TI-03 | Autenticar con MFA | Sistema (extend si MFA activo) | Should |
| TI-04 | Gestionar Perfil de Usuario | Admin, Super_Admin | Must |
| TI-05 | Crear / Editar / Desactivar Usuario | Admin | Must |
| TI-06 | Asignar Roles y Permisos | Admin | Must |
| TI-07 | Configurar Tenant (Localizacion) | Admin, Super_Admin | Must |
| TI-08 | Definir Idioma del Sistema | Admin | Should |
| TI-09 | Configurar Moneda Base | Admin | Must |
| TI-10 | Configurar Pais y Zona Horaria | Admin | Must |
| TI-11 | Gestionar Sucursales | Admin, Super_Admin | Should |
| TI-14 | Auditar Log de Accesos | Super_Admin, Admin | Should |
| TI-15 | Obtener Tipo de Cambio en Tiempo Real | Servicio FX | Could |
| TI-16 | Actualizar Tabla de Tipos de Cambio | Sistema | Could |
| TI-17 | Convertir Monto entre Monedas | Sistema, Admin | Could |
| TI-18 | Configurar Umbral Actualizacion FX | Admin | Could |
| TI-19 | Registrar Historial de Tasas FX | Sistema | Could |
| TI-20 | Asignar Multiples Roles a Usuario | Admin, Super_Admin | Should |
| TI-21 | Cambiar Rol Activo en Sesion | Todos los usuarios | Should |
| TI-22 | Escalar Rol Temporalmente | Usuario Multi-Rol | Should |
| TI-23 | Configurar Separacion de Funciones | Super_Admin | Should |

### MS-2 · Tax & Compliance Service

| ID | Caso de Uso | Actores | Prioridad |
|---|---|---|---|
| TC-01 | Calcular Impuesto de Venta | Sistema (MS-5) | Must |
| TC-02 | Calcular IVA Compuesto (cascada) | Sistema | Must |
| TC-03 | Aplicar Exencion Fiscal (Producto) | Admin | Should |
| TC-04 | Aplicar Exencion Fiscal (Cliente) | Cajero | Should |
| TC-05 | Configurar Reglas Fiscales por Pais | Admin, Super_Admin | Must |
| TC-06 | Solicitar Folio Electronico | Sistema (MS-5) | Must |
| TC-07 | Validar Folio con Entidad Fiscal | Entidad Fiscal | Must |
| TC-08 | Emitir DTE | Sistema → Entidad Fiscal | Must |
| TC-09 | Generar Boleta / Factura Electronica | Cajero, Sistema | Must |
| TC-10 | Manejar Rechazo de Folio (Reintento) | Sistema | Should |
| TC-11 | Consultar Estado de DTE | Admin | Should |
| TC-12 | Reporte Declaracion Fiscal | Admin | Should |

### MS-3 · Catalog & Pricing Service

| ID | Caso de Uso | Actores | Prioridad |
|---|---|---|---|
| CP-01 | Crear Producto en Catalogo | Admin | Must |
| CP-02 | Editar Producto | Admin | Must |
| CP-03 | Asignar Codigo Barras / QR | Admin, Reponedor | Must |
| CP-04 | Escanear Codigo Barras / QR | Cajero, Reponedor | Must |
| CP-05 | Buscar Producto en Catalogo | Cajero, Admin | Must |
| CP-06 | Configurar UOM (Unidad de Medida) | Admin | Should |
| CP-07 | Convertir Unidades (kg-lb, L-gal) | Sistema | Should |
| CP-08 | Registrar Producto a Granel (peso var.) | Cajero | Should |
| CP-09 | Capturar Peso desde Balanza | Balanza Fisica | Should |
| CP-10 | Definir Precio Base | Admin | Must |
| CP-11 | Configurar Precio Dinamico | Admin | Should |
| CP-12 | Aplicar Descuento / Promocion | Cajero, Sistema | Should |
| CP-13 | Definir Rango de Fechas Promocion | Admin | Should |
| CP-14 | Configurar Precio por Sucursal | Admin | Should |
| CP-15 | Gestionar Categorias de Productos | Admin | Must |

### MS-4 · Warehouse & Inventory Service

| ID | Caso de Uso | Actores | Prioridad |
|---|---|---|---|
| WI-01 | Consultar Stock Actual | Cajero, Reponedor, Admin | Must |
| WI-02 | Ajustar Stock Manualmente | Admin, Reponedor | Should |
| WI-03 | Registrar Merma / Perdida | Reponedor | Should |
| WI-04 | Registrar Recepcion de Mercancia | Reponedor | Must |
| WI-05 | Aplicar Regla FEFO | Sistema | Must |
| WI-06 | Ingresar Fecha de Caducidad de Lote | Reponedor | Must |
| WI-07 | Asignar Lote a Ubicacion en Nevera | Reponedor | Should |
| WI-08 | Controlar Volumen de Nevera | Sistema | Should |
| WI-09 | Generar Alerta Stock Minimo | Sistema → Kafka | Should |
| WI-10 | Generar Alerta Proxima Caducidad | Sistema → Kafka | Should |
| WI-11 | Realizar Conteo Fisico de Inventario | Reponedor | Should |
| WI-12 | Conciliar Inventario Fisico vs Sistema | Admin, Reponedor | Should |
| WI-13 | Procesar Evento sale.completed | Kafka | Must |
| WI-14 | Descontar Stock por Venta (async) | Sistema | Must |
| WI-15 | Transferir Stock entre Sucursales | Admin | Should |

### MS-5 · POS & Cart Service

| ID | Caso de Uso | Actores | Prioridad |
|---|---|---|---|
| PC-01 | Abrir Turno de Caja | Cajero | Must |
| PC-02 | Registrar Fondo de Apertura | Cajero | Must |
| PC-03 | Cerrar Turno de Caja | Cajero | Must |
| PC-04 | Cuadre de Caja | Cajero, Admin | Must |
| PC-05 | Declarar Billetes y Monedas | Cajero | Must |
| PC-06 | Iniciar Nueva Venta (Carrito) | Cajero | Must |
| PC-07 | Agregar Producto (escaneo) | Cajero | Must |
| PC-08 | Agregar Producto (busqueda) | Cajero | Must |
| PC-09 | Agregar Producto a Granel | Cajero | Should |
| PC-10 | Integrar Peso de Balanza | Balanza Fisica | Should |
| PC-11 | Modificar Cantidad | Cajero | Must |
| PC-12 | Eliminar Item del Carrito | Cajero | Must |
| PC-13 | Descuento Manual | Cajero | Should |
| PC-14 | Calcular Total con Impuestos | Sistema | Must |
| PC-15 | Cobrar en Efectivo | Cajero | Must |
| PC-16 | Calcular Vuelto | Sistema | Must |
| PC-17 | Cobrar con Tarjeta | Cajero | Must |
| PC-18 | Pago Mixto (efectivo + tarjeta) | Cajero | Should |
| PC-19 | Emitir Recibo / Comprobante | Cajero | Must |
| PC-20 | Anular Venta (Devolucion) | Cajero, Admin | Should |
| PC-21 | Devolucion Parcial | Cajero | Should |
| PC-22 | Poner Venta en Espera (Hold) | Cajero | Could |
| PC-23 | Recuperar Venta de Espera | Cajero | Could |
| PC-24 | Publicar sale.completed | Kafka | Must |
| PC-25 | Publicar sale.reversed | Kafka | Should |
| PC-26 | Leer Tarjeta en Terminal (NFC/chip/mag) | Terminal POS | Must |
| PC-27 | Enviar Solicitud a Pasarela | Pasarela de Pago | Must |
| PC-28 | Recibir Respuesta de Pasarela | Pasarela de Pago | Must |
| PC-29 | Manejar Rechazo de Pago | Cajero | Must |
| PC-30 | Solicitar Reembolso a Pasarela | Pasarela de Pago | Should |
| PC-31 | Confirmar Reembolso | Pasarela de Pago | Should |
| PC-32 | Abrir Cajon de Dinero | Terminal POS | Must |
| PC-33 | Imprimir Recibo en Terminal | Terminal POS | Must |
| PC-34 | Mostrar Total en Display Cliente | Terminal POS | Must |
| PC-35 | Identificar Cliente Afiliado en POS | Cajero, Cliente Afiliado | Could |
| PC-36 | Aplicar Beneficios de Membresia | Sistema | Could |
| PC-37 | Acumular Puntos tras Venta | Sistema → MS-8 | Could |

### MS-6 · Supply Chain & Import Service

| ID | Caso de Uso | Actores | Prioridad |
|---|---|---|---|
| SC-01 | Crear Orden de Compra | Admin | Should |
| SC-02 | Seleccionar Proveedor | Admin | Should |
| SC-03 | Agregar Items a Orden | Admin | Should |
| SC-04 | Aprobar Orden de Compra | Admin | Should |
| SC-05 | Enviar Orden a Proveedor | Sistema | Should |
| SC-06 | Calcular Costo Landed | Sistema | Should |
| SC-07 | Ingresar Costos de Flete | Admin | Should |
| SC-08 | Ingresar Aranceles e Impuestos | Admin | Should |
| SC-09 | Ingresar Seguros y Aduanas | Admin | Should |
| SC-10 | Registrar Importacion | Admin | Should |
| SC-11 | Actualizar Estado Importacion | Admin, Sistema | Should |
| SC-12 | Confirmar Recepcion de Importacion | Reponedor, Admin | Should |
| SC-14 | Gestionar Catalogo de Proveedores | Admin | Should |
| SC-15 | Crear / Editar Proveedor | Admin | Should |
| SC-16 | Evaluar Historial de Proveedor | Admin | Could |
| SC-18 | Solicitar Cotizacion de Flete | Sistema de Transporte | Could |
| SC-19 | Comparar Ofertas de Flete | Admin | Could |
| SC-20 | Confirmar Transportista | Admin, Transporte | Could |
| SC-21 | Rastrear Envio (Tracking) | Admin, Transporte | Could |
| SC-22 | Confirmar Entrega en Destino | Reponedor, Transporte | Could |
| SC-23 | Gestionar Incidencia de Transporte | Admin, Transporte | Could |
| SC-24 | Convertir Costo Flete a Moneda Local | Sistema | Could |

### MS-7 · Analytics & Notification Service

| ID | Caso de Uso | Actores | Prioridad |
|---|---|---|---|
| AN-01 | Dashboard de Ventas | Admin | Should |
| AN-02 | Filtrar por Periodo | Admin | Should |
| AN-03 | Filtrar por Sucursal | Admin | Should |
| AN-04 | Dashboard de Inventario | Admin, Reponedor | Should |
| AN-05 | Dashboard Financiero | Admin | Should |
| AN-06 | Generar Reporte de Ventas | Admin | Should |
| AN-07 | Top Productos Mas Vendidos | Admin | Should |
| AN-08 | Reporte de Mermas | Admin | Should |
| AN-09 | Exportar PDF / Excel | Admin | Could |
| AN-10 | Consumir sale.completed | Kafka | Should |
| AN-11 | Consumir stock.updated | Kafka | Should |
| AN-12 | Alerta Stock Minimo | Reponedor, Admin | Should |
| AN-13 | Alerta Proxima Caducidad | Reponedor | Should |
| AN-14 | Configurar Umbrales de Alerta | Admin | Should |
| AN-15 | Despachar Notificacion (orquestador) | Sistema | Could |
| AN-16 | KPIs en Tiempo Real | Admin | Should |
| AN-17 | Auditar Log de Eventos del Sistema | Super_Admin | Should |
| AN-18 | Enviar SMS via Twilio | Proveedor Mensajeria | Could |
| AN-19 | Enviar Email via SendGrid | Proveedor Mensajeria | Could |
| AN-20 | Enviar Notif. Push via Firebase | Proveedor Mensajeria | Could |
| AN-21 | Configurar Plantillas de Mensaje | Admin | Could |
| AN-22 | Historial de Envios | Sistema | Could |
| AN-23 | Manejar Fallo de Entrega | Sistema | Could |
| AN-24 | Alerta Variacion Tipo de Cambio | Admin | Could |

### MS-8 · Loyalty & Customer Service

| ID | Caso de Uso | Actores | Prioridad |
|---|---|---|---|
| LC-01 | Registrar Cliente Afiliado | Admin, Cajero | Could |
| LC-02 | Verificar Identidad del Cliente | Sistema | Could |
| LC-03 | Identificar Cliente en POS | Cajero, Cliente Afiliado | Could |
| LC-04 | Consultar Perfil y Saldo de Puntos | Cajero, Cliente Afiliado | Could |
| LC-05 | Ver Historial de Compras | Cliente Afiliado, Admin | Could |
| LC-06 | Acumular Puntos por Compra | Sistema | Could |
| LC-07 | Canjear Puntos por Descuento | Cajero, Cliente Afiliado | Could |
| LC-08 | Canjear Puntos por Producto Gratis | Cajero, Cliente Afiliado | Could |
| LC-09 | Aplicar Tier / Nivel Membresia | Sistema | Could |
| LC-10 | Emitir Cupon de Descuento | Admin, Sistema | Could |
| LC-11 | Validar Cupon en POS | Cajero | Could |
| LC-12 | Configurar Programa de Lealtad | Admin | Could |
| LC-13 | Definir Multiplicadores de Puntos | Admin | Could |
| LC-14 | Definir Beneficios por Nivel | Admin | Could |
| LC-15 | Expirar Puntos Inactivos | Sistema | Could |
| LC-16 | Enviar Estado de Cuenta al Cliente | Sistema → Mensajeria | Could |
| LC-17 | Publicar points.updated | Kafka | Could |

---

## 11. Arquitectura de Eventos Kafka

El sistema utiliza **9 topics de Kafka**. La comunicacion asincrona garantiza que el POS no se bloquee esperando operaciones secundarias.

### Topics y Flujos

```
PRODUCERS → KAFKA → CONSUMERS

MS-5 POS
  sale.completed ──────► MS-4 Warehouse  : descuenta stock (WI-13, WI-14)
                    ────► MS-2 Tax        : registra hecho imponible
                    ────► MS-8 Loyalty    : acumula puntos (LC-06)
                    ────► MS-7 Analytics  : actualiza KPIs en tiempo real (AN-10)

  sale.reversed ────────► MS-4 Warehouse  : revierte descuento de stock
                    ────► MS-7 Analytics  : corrige KPIs (AN-10)
                    ────► MS-8 Loyalty    : revierte puntos acumulados

MS-4 Warehouse
  stock.alert ──────────► MS-7 Analytics  : notifica stock bajo (AN-12)
                              → MS-7 despacha via SMS/Email/Push

  expiry.alert ─────────► MS-7 Analytics  : notifica caducidad proxima (AN-13)
                              → MS-7 despacha via SMS/Email/Push

  stock.updated ────────► MS-7 Analytics  : actualiza dashboard inventario (AN-11)

MS-6 Supply Chain
  purchase.received ────► MS-4 Warehouse  : ingresa mercancia (WI-04)
                              → MS-4 aplica FEFO automaticamente

MS-1 Tenant/FX
  fx.rate.updated ──────► MS-3 Catalog    : actualiza precios en moneda extranjera
                    ────► MS-6 Supply     : actualiza conversion de costos de flete
                    ────► MS-7 Analytics  : genera alerta de variacion FX (AN-24)

MS-8 Loyalty
  points.updated ───────► MS-7 Analytics  : actualiza metricas de lealtad
```

### Justificacion de Kafka vs Sincrono

La comunicacion de `sale.completed` es asincrona por diseño porque:
1. El cajero no debe esperar a que el inventario se descuente para emitir el ticket (latencia 0 en POS).
2. Si MS-4 esta temporalmente caido, los eventos de Kafka se acumulan y se procesan cuando vuelve (durabilidad).
3. Multiples consumidores pueden procesar el mismo evento independientemente (MS-4, MS-7, MS-8) sin acoplamiento.

---

## 12. Integraciones con Sistemas Externos y Hardware

### Balanza Fisica

- **Protocolo:** Puerto serial (COM) o USB con driver especifico del fabricante
- **Flujo:** La balanza envia el peso en gramos cuando se estabiliza → Electron captura via Node.js SerialPort → envia al carrito → MS-5 calcula precio = peso × precio_por_kg
- **Manejo de errores:** Si la balanza no responde, el cajero puede ingresar el peso manualmente

### Terminal POS

Dispositivo que agrupa: lector de tarjetas (NFC/chip/mag), cajon de efectivo, impresora de tickets y display para el cliente.
- **Lector de tarjetas:** Enviar los datos de la tarjeta a MS-5 → MS-5 los reenvía a la Pasarela de Pago
- **Cajon:** Se abre via comando serial/USB al confirmar cobro en efectivo
- **Impresora:** Recibe el contenido del ticket formateado (ESC/POS protocol) desde Electron
- **Display:** Muestra el total calculado antes de que el cliente confirme el pago

### Entidad Fiscal (SII / AFIP / IRS)

- **Chile (SII):** API REST con autenticacion por certificado digital. Folios en rangos CAF. DTE en formato XML firmado digitalmente.
- **Argentina (AFIP):** WebService SOAP. Solicitud de CAE (Codigo de Autorizacion Electronico) por cada comprobante.
- **USA (IRS):** Reportes periodicos, no validacion en tiempo real por transaccion.
- **Manejo de fallos:** Si la Entidad Fiscal no responde, el sistema reintenta hasta 3 veces con backoff exponencial. Si falla, la venta queda en estado `pending_dte` y se reintenta en segundo plano.

### Pasarela de Pago (Stripe / Transbank / Mercado Pago)

- **Flujo:** Terminal POS lee tarjeta → datos encriptados van a MS-5 → MS-5 envia a Pasarela via HTTPS → Pasarela devuelve `approved` o `declined`
- **Reembolsos:** MS-5 envia solicitud de reembolso a la Pasarela con el ID de la transaccion original → Pasarela confirma el reembolso
- **Seguridad:** Los datos de tarjeta nunca se almacenan en GlobalMart OS (cumplimiento PCI DSS)

### Sistema de Transporte 3PL

- **Cotizacion de flete:** MS-6 envia origen, destino, peso y dimensiones → 3PL devuelve opciones de precio y tiempo
- **Tracking:** MS-6 consulta periodicamente el estado del envio con el numero de guia
- **Confirmacion de entrega:** 3PL notifica via webhook cuando el paquete es entregado

### Proveedor de Mensajeria

- **Twilio (SMS):** Para alertas criticas de stock y caducidad a telefonos moviles
- **SendGrid (Email):** Para reportes, estados de cuenta de lealtad y alertas detalladas
- **Firebase Cloud Messaging (Push):** Para notificaciones en la app Electron
- **Manejo de fallos:** Si el proveedor falla, el sistema registra el intento en `AN-22` y reintenta hasta 3 veces

### Servicio de Tipos de Cambio

- **Proveedor:** Fixer.io (u Open Exchange Rates como alternativa)
- **Frecuencia:** Se actualiza automaticamente cuando la variacion supera el umbral configurado por tenant (ej: si el USD/CLP varia mas del 1%)
- **Almacenamiento:** MS-1 guarda el historial de tasas para conversion de costos historicos

---

## 13. Diseno RBAC y Gestion Multi-Rol

### Roles del Sistema

| Rol | Permisos Clave |
|---|---|
| `CAJERO` | Abrir/cerrar turno, gestionar carrito, cobrar, emitir comprobante, anular con permiso |
| `REPONEDOR` | Recibir mercancia, registrar mermas, conteo fisico, consultar stock |
| `ADMIN` | Todo lo de CAJERO y REPONEDOR + configurar tenant, gestionar usuarios, ordenes de compra, reportes, aprobar OC |
| `SUPER_ADMIN` | Todo lo de ADMIN + crear tenants, revocar tokens globales, auditoria del sistema completo |
| `CLIENTE_AFILIADO` | Identificarse en POS, consultar puntos, canjear puntos, ver historial |

### Permisos Especiales (Granulares)

Ademas de los roles, el sistema soporta permisos granulares asignables individualmente:
- `discount.apply` — Permite aplicar descuentos manuales en carrito
- `refund.process` — Permite procesar devoluciones y anulaciones
- `stock.adjust` — Permite ajustar stock manualmente
- `oc.approve` — Permite aprobar ordenes de compra (usado con separacion de funciones)

### Estructura del JWT

```json
{
  "sub": "user_001",
  "tenant_id": "minimarket_santiago_001",
  "sucursal_id": "sucursal_providencia",
  "roles": ["CAJERO", "REPONEDOR"],
  "active_role": "CAJERO",
  "permissions": ["discount.apply"],
  "exp": 1725820800,
  "iat": 1725791200
}
```

### Gestion Multi-Rol en el Frontend

Cuando un usuario tiene mas de un rol, Electron muestra un selector de rol (RoleSwitcher) en la barra de navegacion. El usuario puede:
1. **Cambiar el rol activo** (TI-21): cambia el contexto de toda la sesion al rol seleccionado
2. **Escalar rol temporalmente** (TI-22): para una sola accion especifica sin cambiar el rol activo principal, el sistema pide confirmacion de contraseña

Cada accion critica registrada en el log de auditoria incluye el `active_role` al momento de la accion.

---

## 14. Diseno Multi-Tenant

### Aislamiento de Datos

**Cada tabla de cada microservicio tiene un campo `tenant_id`:**

```sql
-- Ejemplo tabla productos en MS-3
CREATE TABLE productos (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,  -- <-- discriminador obligatorio
    nombre VARCHAR(200) NOT NULL,
    precio DECIMAL(10,2) NOT NULL,
    codigo_barras VARCHAR(50),
    -- ...
    CONSTRAINT fk_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id)
);

CREATE INDEX idx_productos_tenant ON productos(tenant_id);
```

**Middleware ASP.NET Core que inyecta el tenant en cada query:**

```csharp
// Se ejecuta en CADA request antes de llegar al controller
public class TenantMiddleware
{
    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        var tenantId = context.User.FindFirst("tenant_id")?.Value;
        if (tenantId == null) { context.Response.StatusCode = 401; return; }
        db.CurrentTenantId = Guid.Parse(tenantId); // EF Core filtra automaticamente
        await _next(context);
    }
}
```

### Configuracion por Tenant

Cada tenant puede configurar independientemente:
- **Pais:** Determina que Entidad Fiscal usar, que reglas fiscales aplicar
- **Moneda base:** CLP, ARS, USD, EUR, etc.
- **Idioma:** ES, EN, PT (i18n en frontend)
- **Zona horaria:** Critica para cuadres de caja y reportes diarios
- **Porcentaje de impuesto:** IVA o equivalente segun pais
- **Umbral de stock minimo:** Porcentaje del stock objetivo que activa la alerta
- **Umbral de variacion FX:** Porcentaje de variacion de tipo de cambio que activa actualizacion
- **Reglas de separacion de funciones:** Si OC requiere aprobador diferente al creador
- **Programa de lealtad:** Configuracion de puntos, tiers y multiplicadores

---

## 15. Diagrama UML de Casos de Uso — Resumen

El proyecto tiene **9 diagramas PlantUML** que representan el sistema completo. Los diagramas estan codificados en archivos `.md` del proyecto.

### Estructura de los 9 Diagramas

| Diagrama | Nombre | Contenido | Actores Incluidos |
|---|---|---|---|
| **D-0** | Actores & Generalizacion de Roles | Jerarquia de herencia de todos los actores, relacion Encargado multi-rol | Todos los 13 actores |
| **D-1** | Identidad & Cumplimiento Fiscal | MS-1 completo (auth, FX, usuarios, tenant) + MS-2 (impuestos, DTE, folios) | Cajero, Admin, SuperAdmin, Entidad Fiscal, Servicio FX, Kafka |
| **D-2** | Catalogo & Inventario | MS-3 completo (productos, precios, UOM, granel) + MS-4 (stock, FEFO, alertas, conteo) | Cajero, Reponedor, Admin, Balanza, Kafka |
| **D-3a** | POS — Turno & Carrito | MS-5 parte 1: apertura turno, carrito completo, identificacion cliente afiliado | Cajero, Admin, Cliente Afiliado, Balanza |
| **D-3b** | POS — Cobro & Pagos | MS-5 parte 2: calculo total, cobro efectivo/tarjeta/mixto, terminal POS, pasarela, devoluciones, eventos Kafka | Cajero, Admin, Terminal POS, Pasarela, Kafka |
| **D-4** | Cadena de Suministro | MS-6 completo: OC, Landed Cost, importaciones, tracking, proveedores | Admin, Reponedor, Sistema Transporte, Kafka |
| **D-5a** | Analytics & Notificaciones | MS-7 completo: dashboards, reportes, alertas, canales de mensajeria (SMS/Email/Push) | Admin, Reponedor, SuperAdmin, Kafka, Proveedor Mensajeria |
| **D-5b** | Loyalty & Customer | MS-8 completo: registro, puntos, canje, tiers, cupones, comunicacion | Cajero, Admin, Cliente Afiliado, Kafka, Proveedor Mensajeria |
| **D-6** | Flujos Kafka | Vista de alto nivel de los 9 topics y todos sus productores y consumidores | Los 8 microservicios + Kafka |

### Relaciones UML Clave

**`<<include>>`** — El caso de uso base SIEMPRE ejecuta el incluido:
- PC-14 (Calcular Total) `<<include>>` TC-01 (Calcular Impuesto): siempre hay impuesto en el total
- TC-09 (Boleta/Factura) `<<include>>` TC-06 + TC-07: siempre necesita folio valido
- PC-27 (Cobro tarjeta) `<<include>>` Pasarela: siempre pasa por pasarela

**`<<extend>>`** — El caso de uso se activa SOLO bajo condicion especifica:
- TI-03 (MFA) `<<extend>>` TI-01 (Login): solo si el tenant tiene MFA habilitado
- TC-03 (Exencion) `<<extend>>` TC-01 (Impuesto): solo si el producto tiene exencion
- PC-29 (Manejar Rechazo) `<<extend>>` PC-28 (Respuesta Pasarela): solo si la pasarela rechaza
- LC-15 (Expirar Puntos) `<<extend>>` LC-06 (Acumular): solo si hay mas de 12 meses inactivo

---

## 16. Matriz de Trazabilidad

| Regla de Negocio | Requerimiento(s) | Caso(s) de Uso | Prioridad |
|---|---|---|---|
| RN-01 Aislamiento tenant | RF-14, RNF-01 | TI-07, TI-11 | Must |
| RN-02 Pago tarjeta via pasarela | RF-07 | PC-17, PC-27, PC-28 | Must |
| RN-03 Documento tributario | RF-08 | TC-09, TC-06, TC-07 | Must |
| RN-04 Descuento stock automatico | RF-09 | WI-13, WI-14 | Must |
| RN-05 Regla FEFO | RF-10 | WI-04, WI-05, WI-06 | Must |
| RN-06 Turno abierto para ventas | RF-02 | PC-01, PC-02, PC-06 | Must |
| RN-07 Usuario autenticado | RF-01 | TI-01, TI-02 | Must |
| RN-08 JWT con tenant y roles | RF-01, RF-13 | TI-01, TI-06, TI-20 | Must |
| RN-09 Calculo Costo Landed | RF-18 | SC-06, SC-07, SC-08, SC-09 | Should |
| RN-10 Alertas stock antes de cero | RF-16 | WI-09, AN-12 | Should |
| RN-11 Precios por sucursal | RF-21 | CP-14 | Should |
| RN-12 Multiples roles / rol activo | RF-20 | TI-20, TI-21, TI-22 | Should |
| RN-13 Separacion de funciones OC | RF-18, RF-13 | SC-04, TI-23 | Should |
| RN-14 Devolucion requiere permiso | RF-22 | PC-20, PC-21 | Should |
| RN-15 Expiracion puntos 12 meses | RF-23 | LC-15 | Could |
| RN-16 FX automatico con umbral | RF-24 | TI-15, TI-16, TI-18 | Could |
| RN-17 Multiplicadores puntos | RF-23 | LC-13 | Could |
| RN-18 Descuento manual con permiso | RF-21, RF-13 | PC-13 | Could |
| RN-19 Sin consignacion | RF-32 | — | Won't |
| RN-20 Sin multi-moneda en transaccion | RF-14 | — | Won't |

---

## 17. Sprint 1 — Backlog y Planificacion

### Objetivo del Sprint 1

> *"Un cajero autenticado puede abrir un turno, escanear productos, cobrar en efectivo y cerrar el turno con cuadre de caja. Un administrador puede crear el tenant, usuarios, roles y productos basicos."*

**Duracion:** 2 semanas | **Puntos:** 45 | **Equipo:** 2-3 desarrolladores

### User Stories (11)

| ID | Historia de Usuario | Puntos | Prioridad |
|---|---|---|---|
| US-01 | Como **admin**, quiero **configurar mi tenant** (pais, moneda, idioma) para operar segun mi region. | 5 | Must |
| US-02 | Como **admin**, quiero **crear usuarios y asignarles roles** para controlar accesos. | 5 | Must |
| US-03 | Como **cajero**, quiero **iniciar sesion** con usuario y contrasena para acceder al POS. | 3 | Must |
| US-04 | Como **cajero**, quiero **abrir un turno de caja** registrando el fondo inicial para comenzar a vender. | 3 | Must |
| US-05 | Como **admin**, quiero **crear productos en el catalogo** con nombre, precio y codigo de barras. | 5 | Must |
| US-06 | Como **cajero**, quiero **escanear el codigo de barras** de un producto para agregarlo al carrito. | 5 | Must |
| US-07 | Como **cajero**, quiero **buscar un producto por nombre** para agregarlo al carrito cuando no tengo lector. | 3 | Must |
| US-08 | Como **cajero**, quiero **ver el subtotal, impuesto y total** actualizados en tiempo real. | 3 | Must |
| US-09 | Como **cajero**, quiero **cobrar en efectivo** y que el sistema calcule el vuelto automaticamente. | 5 | Must |
| US-10 | Como **cajero**, quiero que al **completar una venta** se genere un comprobante imprimible. | 5 | Must |
| US-11 | Como **cajero**, quiero **cerrar el turno** declarando el efectivo para calcular el cuadre. | 3 | Must |

### Tareas Tecnicas (19)

| ID | Tarea | Estimacion |
|---|---|---|
| TT-01 | Scaffold 8 microservicios ASP.NET Core 8 con estructura base | 1 dia |
| TT-02 | Docker Compose: PostgreSQL x8, Kafka, Zookeeper, Kafka UI, Kong | 1 dia |
| TT-03 | MS-1: JWT generation, multi-tenant middleware, endpoint /auth/login | 2 dias |
| TT-04 | MS-1: CRUD de usuarios y roles | 1 dia |
| TT-05 | MS-1: Configuracion de tenant (pais, moneda, idioma) | 1 dia |
| TT-06 | MS-3: CRUD productos, busqueda por nombre y lookup por codigo de barras | 2 dias |
| TT-07 | MS-5: Apertura/cierre de turno, estado de turno por cajero | 1 dia |
| TT-08 | MS-5: Gestion de carrito (crear, agregar, modificar, eliminar, total) | 2 dias |
| TT-09 | MS-5: Cobro en efectivo, calculo de vuelto, publicacion sale.completed a Kafka | 2 dias |
| TT-10 | MS-5: Cuadre de caja al cierre de turno | 1 dia |
| TT-11 | MS-4: Consumo de sale.completed para descontar stock | 1 dia |
| TT-12 | MS-2: Calculo de impuesto basico por tenant (IVA configurable) | 1 dia |
| TT-13 | Electron App: Skeleton con React Router, IPC handlers, pantalla de Login | 2 dias |
| TT-14 | Electron App: Pantalla POS (carrito, escaneo, totales, boton cobrar) | 3 dias |
| TT-15 | Electron App: Pantalla de apertura/cierre de turno y cuadre | 1 dia |
| TT-16 | Electron App: Pantalla de administracion de productos (CRUD) | 2 dias |
| TT-17 | Configuracion de Kong con rutas hacia MS-1, MS-3, MS-5 | 1 dia |
| TT-18 | Tests unitarios: MS-1 (login, JWT), MS-5 (carrito, cobro), MS-2 (impuesto) | 2 dias |
| TT-19 | Tests de integracion: flujo completo login → turno → venta → cierre | 1 dia |

### Definition of Done — Sprint 1

- [ ] Todos los endpoints del sprint pasan tests de integracion
- [ ] Cobertura de tests unitarios >= 80% en MS-1, MS-2 y MS-5
- [ ] `docker compose up` levanta el entorno completo desde cero
- [ ] Flujo end-to-end funciona en la app Electron
- [ ] Evento `sale.completed` se publica en Kafka y MS-4 descuenta el stock
- [ ] Aislamiento multi-tenant validado: tenant A no ve datos de tenant B
- [ ] GitHub Actions ejecuta build + tests en cada push a `main`

---

## 18. Roadmap de Sprints

| Sprint | Objetivo | Microservicios / Epicas |
|---|---|---|
| **Sprint 1** | Autenticacion, POS basico, catalogo y cobro en efectivo | MS-1, MS-2, MS-3, MS-5 (parcial) |
| **Sprint 2** | Cobro con tarjeta, Terminal POS, balanza fisica y pago mixto | MS-5 (completo), hardware |
| **Sprint 3** | Inventario FEFO, alertas de stock y caducidad, recepcion de mercancia | MS-4 |
| **Sprint 4** | Cumplimiento fiscal completo (DTE, folios, Entidad Fiscal por pais) | MS-2 (completo) |
| **Sprint 5** | Dashboards, reportes, exportacion PDF/Excel, KPIs tiempo real | MS-7 |
| **Sprint 6** | Ordenes de compra, Landed Cost, gestion de proveedores, sucursales | MS-6, MS-1 (sucursales) |
| **Sprint 7** | Programa de lealtad, cliente afiliado, tiers, cupones, puntos | MS-8 |
| **Sprint 8** | Notificaciones multicanal, FX en tiempo real, tracking importaciones | MS-7 (notif.), MS-1 (FX), MS-6 (tracking) |

---

## 19. Decisiones Arquitectonicas Clave

### Decision 1: Mantener Apache Kafka (vs RabbitMQ)

Se evaluo migrar a RabbitMQ + MassTransit por ser mas simple para equipos pequenos. Se decidio **mantener Kafka** porque:
- Mayor robustez y durabilidad de eventos
- Capacidad de replay de eventos (util para sincronizar MS-4 si estuvo caido)
- El equipo acepta la curva de aprendizaje a cambio de la escalabilidad futura

### Decision 2: Mantener Kong API Gateway (vs YARP)

Se evaluo YARP (Microsoft nativo) y Ocelot por ser mas naturales en .NET. Se decidio **mantener Kong** porque:
- Mayor madurez como API Gateway de produccion
- Plugin JWT validation sin codigo adicional
- Rate limiting y balanceo de carga out-of-the-box

### Decision 3: EF Core 8 como ORM unico (sin Dapper)

Para mantener el stack simple en v1.0. Si en futuras versiones el rendimiento del POS es insuficiente, se incorporara Dapper para queries criticos de MS-5.

### Decision 4: Patron Database-per-Service

Cada microservicio tiene su propia instancia PostgreSQL. Esto garantiza:
- Aislamiento completo de datos entre servicios
- Cada servicio puede evolucionar su esquema independientemente
- Un fallo de base de datos en MS-7 (analytics) no afecta a MS-5 (POS)

Costo: 8 instancias PostgreSQL en Docker Compose (manejable en desarrollo, separable en produccion).

### Decision 5: Electron para el Frontend

POS de escritorio nativo porque:
- Acceso directo a puertos seriales (balanza, Terminal POS) via Node.js APIs
- Funciona sin browser, sin restricciones de CORS ni seguridad del browser
- Empaquetable como instalador .exe / .dmg / .AppImage
- React 18 para UI moderna y mantenible

### Decision 6: MoSCoW para Priorizar el Alcance

Se adopto MoSCoW para gestionar la complejidad del sistema:
- **Must:** 14 RF + 4 RNF + 8 RN + 12 CU del MS-1 y MS-5 core
- **Should:** 8 RF + 3 RNF + 6 RN + CUs de MS-4, MS-6, MS-7
- **Could:** 6 RF + 2 RNF + 4 RN + MS-8 completo
- **Won't:** 4 RF (app movil, e-commerce, nomina, consignacion)

El Sprint 1 cubre exclusivamente los Must de mayor impacto operativo.

---

*Este documento representa el estado completo del proyecto GlobalMart OS al 8 de Septiembre de 2026. Cualquier IA que haya leido este documento tiene suficiente contexto para contribuir al proyecto sin informacion adicional.*

---
id: ms7-analytics
tipo: microservicio
titulo: MS-7 · Analytics & Notification Service
estado: planificado
fuentes: [src/AnalyticsNotificationService/]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: [ms1-identity]
publica: []
consume: [sale.completed, sale.reversed, stock.updated, stock.alert, expiry.alert, points.updated, fx.rate.updated]
reglas: [RF-17, RF-25, RF-26, AN-01]
---
# MS-7 · Analytics & Notification Service

> Consume eventos Kafka para dashboards en tiempo real, reportes y notificaciones multicanal. **Solo tiene modelos y DbContext implementados. Sin controllers ni Kafka consumers.** Sprint 5 según roadmap.

## Estado actual

**PLANIFICADO** — Sprint 5.

Lo que existe: modelos (Alerta, HistorialEnvio, KPIVenta) + DbContext + migración inicial.
Lo que NO existe: controllers, endpoints, Kafka consumers, lógica de dashboards, integración Twilio/SendGrid/Firebase.

## Modelo de datos (implementado)

```
KPIVenta
  id         GUID PK
  tenant_id  GUID
  fecha      DateTime
  total      decimal
  num_ventas int

Alerta
  id         GUID PK
  tenant_id  GUID
  tipo       string ("stock_minimo"|"caducidad"|"fx_variacion")
  mensaje    string
  leida      bool
  creada_en  DateTime

HistorialEnvio
  id          GUID PK
  tenant_id   GUID
  canal       string ("sms"|"email"|"push")
  destinatario string
  mensaje     string
  estado      string ("enviado"|"fallido"|"reintentando")
  enviado_en  DateTime
```

## Endpoints diseñados (PLANIFICADOS)

- `GET /dashboards/ventas` — dashboard de ventas con filtros
- `GET /dashboards/inventario` — dashboard de inventario
- `GET /dashboards/financiero` — dashboard financiero
- `GET /reportes/ventas` — generar reporte
- `GET /reportes/mermas` — reporte de mermas
- `GET /reportes/top-productos` — productos más vendidos
- `POST /alertas/configurar` — configurar umbrales
- `GET /notificaciones/historial` — historial de envíos

## Kafka consumers diseñados (PLANIFICADOS)

| Topic | Acción esperada |
|-------|-----------------|
| `sale.completed` | Actualiza KPIs de ventas |
| `sale.reversed` | Corrige KPIs por anulación |
| `stock.updated` | Actualiza dashboard inventario |
| `stock.alert` | Despacha notificación stock bajo |
| `expiry.alert` | Despacha notificación caducidad |
| `points.updated` | Actualiza métricas de lealtad |
| `fx.rate.updated` | Genera alerta de variación FX |

## Casos de uso cubiertos (todos PLANIFICADOS)

| ID | Caso de Uso | Estado |
|----|-------------|--------|
| AN-01 | Dashboard de Ventas | [PLANIFICADO] |
| AN-02 | Filtrar por Período | [PLANIFICADO] |
| AN-03 | Filtrar por Sucursal | [PLANIFICADO] |
| AN-04 | Dashboard de Inventario | [PLANIFICADO] |
| AN-05 | Dashboard Financiero | [PLANIFICADO] |
| AN-06 | Generar Reporte de Ventas | [PLANIFICADO] |
| AN-07 | Top Productos Más Vendidos | [PLANIFICADO] |
| AN-08 | Reporte de Mermas | [PLANIFICADO] |
| AN-09 | Exportar PDF / Excel | [PLANIFICADO] |
| AN-10 | Consumir sale.completed | [PLANIFICADO] |
| AN-11 | Consumir stock.updated | [PLANIFICADO] |
| AN-12 | Alerta Stock Mínimo | [PLANIFICADO] |
| AN-13 | Alerta Próxima Caducidad | [PLANIFICADO] |
| AN-14 | Configurar Umbrales de Alerta | [PLANIFICADO] |
| AN-15 | Despachar Notificación (orquestador) | [PLANIFICADO] |
| AN-16 | KPIs en Tiempo Real | [PLANIFICADO] |
| AN-17 | Auditar Log de Eventos del Sistema | [PLANIFICADO] |
| AN-18 | Enviar SMS vía Twilio | [PLANIFICADO] |
| AN-19 | Enviar Email vía SendGrid | [PLANIFICADO] |
| AN-20 | Enviar Notif. Push vía Firebase | [PLANIFICADO] |
| AN-21 | Configurar Plantillas de Mensaje | [PLANIFICADO] |
| AN-22 | Historial de Envíos | [PLANIFICADO] |
| AN-23 | Manejar Fallo de Entrega | [PLANIFICADO] |
| AN-24 | Alerta Variación Tipo de Cambio | [PLANIFICADO] |

## Conexiones
- Consumiría: `sale.completed`, `stock.alert`, etc. ← [[ms4-inventory]], [[ms5-pos]]
- Integraría: [[mensajeria]] (Twilio/SendGrid/Firebase)
- Reglas: [[reglas-negocio]] (RF-17, RF-25)

## Fuentes
- `src/AnalyticsNotificationService/Models/KPIVenta.cs`
- `src/AnalyticsNotificationService/Program.cs`

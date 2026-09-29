---
id: mensajeria
tipo: integracion
titulo: Integración con Proveedores de Mensajería
estado: planificado
fuentes: [src/LoyaltyCustomerService/Program.cs, GlobalMart_ContextMaster.md#sec12]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: [ms7-analytics, ms8-loyalty]
publica: []
consume: []
reglas: [RF-25, AN-15]
---
# Integración con Proveedores de Mensajería

> Twilio (SMS), SendGrid (Email), Firebase (Push notifications). **Estado: PLANIFICADO** — LoyaltyCustomerService registra `HttpClient("ServiciosMensajeria")` en Program.cs pero sin implementación.

## Estado: [PLANIFICADO]

LoyaltyCustomerService tiene registrado:
```csharp
// src/LoyaltyCustomerService/Program.cs
builder.Services.AddHttpClient("ServiciosMensajeria", client => {
    // ... configuración pendiente
});
```
Pero sin handlers, DTOs ni lógica de envío.

## Canales diseñados

| Canal | Proveedor | Uso |
|-------|---------|-----|
| SMS | Twilio | Alertas urgentes, acumulación de puntos, códigos de verificación |
| Email | SendGrid | Facturas, resumen mensual de puntos, boletines |
| Push | Firebase Cloud Messaging | Notificaciones en tiempo real (si hay app móvil) |

## Flujo diseñado

```
MS-4 produce stock.alert → Kafka → MS-7 consume
MS-7 consulta config de alertas del tenant
MS-7 llama a MS-8 (o servicio de mensajería) para enviar notificación
MS-8/MS-7 llama a Twilio/SendGrid/Firebase API
MS-8/MS-7 registra en HistorialEnvio
```

## Casos de uso afectados

| ID | Caso de Uso | Estado |
|----|-------------|--------|
| AN-15 | Enviar Alerta SMS | [PLANIFICADO] |
| AN-16 | Enviar Email de Resumen | [PLANIFICADO] |
| AN-17 | Enviar Push Notification | [PLANIFICADO] |
| AN-18 | Reintentar Envío Fallido | [PLANIFICADO] |
| AN-19 | Registrar Historial de Envíos | [PLANIFICADO] |

## Conexiones
- MS-7 Analytics: [[ms7-analytics]]
- MS-8 Loyalty: [[ms8-loyalty]]
- Kafka: [[kafka-topics]] (stock.alert, expiry.alert)

## Fuentes
- `src/LoyaltyCustomerService/Program.cs`
- `GlobalMart_ContextMaster.md` §12

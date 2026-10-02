---
id: tipos-de-cambio
tipo: integracion
titulo: Integración con Servicio de Tipos de Cambio
estado: planificado
fuentes: [GlobalMart_ContextMaster.md#sec12, GlobalMart_ContextMaster.md#sec6-ms1]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: [ms1-identity, ms6-supply-chain]
publica: [fx.rate.updated]
consume: []
reglas: [RN-16, RF-24, TI-15]
---
# Integración con Servicio de Tipos de Cambio

> MS-1 obtiene tipos de cambio actualizados y los publica vía Kafka. **Estado: PLANIFICADO** — `GET /fx/rates` no existe en el código de MS-1.

## Estado: [PLANIFICADO]

## Proveedores contemplados

- **Fixer.io** — API REST, 170+ monedas
- **OpenExchangeRates** — alternativa

## Flujo diseñado

```
Trigger: cron job diario O cuando variación > umbral (RN-16)
1. MS-1 llama GET https://api.fixer.io/latest?base=USD&symbols=CLP,ARS,EUR,...
2. MS-1 compara con tasas anteriores
3. Si variación > umbral configurado por tenant:
   → MS-1 publica fx.rate.updated a Kafka
4. Consumidores:
   → MS-3 Catalog: actualiza precios en moneda extranjera
   → MS-6 Supply: recalcula costos de importación
   → MS-7 Analytics: genera alerta de variación FX

Endpoint diseñado (NO IMPLEMENTADO):
  GET /fx/rates                    → tasas actuales
  GET /fx/rates/{from}/{to}        → tasa específica
  POST /fx/convert                 → conversión de monto
```

## Endpoint NO encontrado en código MS-1

El ContextMaster documenta `GET /fx/rates` y `POST /fx/convert` como parte de MS-1, pero el código de `TenantIdentityService/Controllers/` solo tiene `AuthController`, `UsuarioController` y `TenantConfigController`. → **DISCREPANCIA**

## Casos de uso afectados

| ID | Caso de Uso | Estado |
|----|-------------|--------|
| TI-15 | Consultar Tasa de Cambio Vigente | [PLANIFICADO] |
| TI-16 | Convertir Monto entre Monedas | [PLANIFICADO] |
| TI-17 | Actualizar Tasas Automáticamente | [PLANIFICADO] |
| TI-18 | Publicar fx.rate.updated | [PLANIFICADO] |
| TI-19 | Alertar Variación de FX | [PLANIFICADO] |

## Conexiones
- MS-1 Identity: [[ms1-identity]]
- MS-6 Supply Chain: [[ms6-supply-chain]] (usa FX para Costo Landed)
- Kafka: [[kafka-topics]] (fx.rate.updated)

## Fuentes
- `GlobalMart_ContextMaster.md` §12, §6 MS-1

---
id: transporte-3pl
tipo: integracion
titulo: Integración con Transporte / 3PL
estado: planificado
fuentes: [src/SupplyChainService/, GlobalMart_ContextMaster.md#sec12]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: [ms6-supply-chain]
publica: []
consume: []
reglas: [RF-27, SC-18, SC-19]
---
# Integración con Transporte / 3PL

> Proveedores logísticos de terceros para tracking de importaciones. **Estado: PLANIFICADO** — MS-6 (Supply Chain) no tiene controllers implementados.

## Estado: [PLANIFICADO]

## Proveedores contemplados (diseño)

- DHL Express
- FedEx Trade Networks
- Maersk (marítimo)
- Latam Cargo (aéreo regional)

## Flujo diseñado

```
MS-6 registra Envio { numeroGuia, transportista, estado }
MS-6 consulta API REST del transportista con numeroGuia
Transportista responde: { estado, ubicacion, etaEstimada }
MS-6 actualiza Envio.Estado
MS-6 publica evento a Kafka (si estado cambia)
MS-7 Analytics muestra tracking en dashboard
```

## Casos de uso afectados

| ID | Caso de Uso | Estado |
|----|-------------|--------|
| SC-18 | Registrar Envío con Transportista | [PLANIFICADO] |
| SC-19 | Rastrear Estado del Envío | [PLANIFICADO] |
| SC-20 | Alertar sobre Retrasos | [PLANIFICADO] |

## Conexiones
- MS-6 Supply Chain: [[ms6-supply-chain]]

## Fuentes
- `src/SupplyChainService/Models/Envio.cs`
- `GlobalMart_ContextMaster.md` §12

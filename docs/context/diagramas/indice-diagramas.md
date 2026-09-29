---
id: indice-diagramas
tipo: diagrama
titulo: Índice de Diagramas — GlobalMart OS
estado: implementado
fuentes: [diagramas-casos-uso/nuevo/, diagramas/, GlobalMart_ContextMaster.md#sec15]
verificado_contra_codigo: false
ultima_revision: 2026-09-28
depende_de: [actores]
publica: []
consume: []
reglas: []
---
# Índice de Diagramas — GlobalMart OS

> Mapa de todos los diagramas del proyecto y cómo encontrarlos.

## Diagramas de Casos de Uso (Mermaid .mmd)

En `diagramas-casos-uso/nuevo/`:

| Archivo | Descripción |
|---------|-------------|
| `D0_Actores_Generalizacion.mmd` | Jerarquía de actores con generalización UML |
| `D1_CasosUso_MS1_TenantIdentity.mmd` | Casos de uso MS-1 (auth, usuarios, config) |
| `D2_CasosUso_MS2_TaxCompliance.mmd` | Casos de uso MS-2 (impuestos, DTE) |
| `D3_CasosUso_MS3_CatalogPricing.mmd` | Casos de uso MS-3 (catálogo, precios) |
| `D4_CasosUso_MS4_WarehouseInventory.mmd` | Casos de uso MS-4 (inventario, FEFO) |
| `D5_CasosUso_MS5_POSCart.mmd` | Casos de uso MS-5 (POS, ventas, cobro) |
| `D6_CasosUso_MS6_SupplyChain.mmd` | Casos de uso MS-6 (supply chain) |
| `D7_CasosUso_MS7_Analytics.mmd` | Casos de uso MS-7 (analytics) |
| `D8_CasosUso_MS8_Loyalty.mmd` | Casos de uso MS-8 (loyalty) |

**Nota:** Los diagramas `.mmd` de Mermaid pueden renderizarse en VS Code con la extensión "Mermaid Preview" o en GitHub directamente.

## Diagramas ERD / Modelo de datos

En `diagramas/martin/`:

| Archivo | Descripción |
|---------|-------------|
| `M1_ERD_MS1_Identity.drawio` | Diagrama entidad-relación MS-1 |
| `M2_ERD_MS2_Tax.drawio` | Diagrama entidad-relación MS-2 |
| `M3_ERD_MS3_Catalog.drawio` | Diagrama entidad-relación MS-3 |
| `M4_ERD_MS4_Warehouse.drawio` | Diagrama entidad-relación MS-4 |
| `M5_ERD_MS5_POS.drawio` | Diagrama entidad-relación MS-5 |

**Nota:** Archivos `.drawio` abrir con `diagrams.net` (draw.io) o VS Code extensión.

## Diagramas de Secuencia

En `diagramas/nicolas/`:

| Archivo | Descripción |
|---------|-------------|
| `N1_Secuencia_Login.drawio` | Flujo completo de login |
| `N2_Secuencia_VentaCompleta.drawio` | Secuencia de venta de inicio a fin |
| `N3_Secuencia_Kafka_SaleCompleted.drawio` | Flujo sale.completed vía Kafka |

## Diagramas de UI/Frontend

En `diagramas/rodrigo/`:

| Archivo | Descripción |
|---------|-------------|
| `R1_screen_flow_electron.drawio.png` | Flujo de pantallas en Electron |
| `R2_wireframes.drawio.png` | Wireframes de la UI del POS |
| `R3_component_tree.drawio.png` | Árbol de componentes React |
| `R4_state_management.drawio.png` | Gestión de estado de la app |

**Nota:** Los archivos PNG son exportaciones de los diagramas `.drawio` para visualización rápida.

## Diagramas eliminados (historia)

Según commit `f21fa54` (2026-09-16): `"docs: eliminar diagramas antiguos"`. Los diagramas en `diagramas-casos-uso/` (raíz, sin subcarpeta) fueron eliminados y reemplazados por los de `diagramas-casos-uso/nuevo/`.

## Conexiones
- Actores: [[actores]]
- Casos de uso por servicio: [[ms1-identity]]..[[ms8-loyalty]]

## Fuentes
- `diagramas-casos-uso/nuevo/`
- `diagramas/martin/`
- `diagramas/nicolas/`
- `diagramas/rodrigo/`
- `GlobalMart_ContextMaster.md` §15

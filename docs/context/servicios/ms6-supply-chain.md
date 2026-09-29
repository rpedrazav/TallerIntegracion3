---
id: ms6-supply-chain
tipo: microservicio
titulo: MS-6 · Supply Chain & Import Service
estado: planificado
fuentes: [src/SupplyChainService/]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: [ms1-identity]
publica: [purchase.received]
consume: [fx.rate.updated]
reglas: [RN-09, RN-13, RF-18, SC-01]
---
# MS-6 · Supply Chain & Import Service

> Gestiona el ciclo completo de compras e importaciones: proveedores, órdenes de compra, Costo Landed y tracking con 3PL. **Solo tiene modelos de datos y DbContext implementados. Sin endpoints ni lógica de negocio.**

## Estado actual

**PLANIFICADO** — Sprint 6 según roadmap.

Lo que existe:
- Modelos de datos (OrdenCompra, Proveedor, Envio, CostoLandedHistorico)
- DbContext + migración inicial (2026-09-12)
- Program.cs con auth/swagger configurado

Lo que NO existe:
- Controllers / endpoints
- Servicios de dominio
- Lógica de Costo Landed
- Integración 3PL
- Kafka producer

## Modelo de datos (implementado)

```
Proveedor
  id        GUID PK
  tenant_id GUID
  nombre    string
  pais      string
  contacto  string

OrdenCompra
  id           GUID PK
  tenant_id    GUID
  proveedor_id GUID FK
  creado_por   GUID (userId)
  aprobado_por GUID? (userId ≠ creado_por — RN-13)
  estado       enum
  total        decimal
  creada_en    DateTime

OrdenCompraItem
  id              GUID PK
  orden_compra_id GUID FK
  producto_id     GUID
  cantidad        decimal
  precio_unitario decimal

Envio
  id              GUID PK
  tenant_id       GUID
  orden_compra_id GUID FK
  numero_guia     string
  estado          string
  transportista   string

CostoLandedHistorico
  id              GUID PK
  tenant_id       GUID
  orden_compra_id GUID FK
  costo_producto  decimal
  flete           decimal
  aranceles       decimal
  seguros         decimal
  gastos_aduaneros decimal
  total_landed    decimal
  calculado_en    DateTime
```

## Endpoints diseñados (PLANIFICADOS)

- `GET/POST /proveedores` — CRUD proveedores
- `POST /ordenes-compra` — crear OC
- `PUT /ordenes-compra/{id}/aprobar` — aprobar OC (aprobador ≠ creador)
- `POST /ordenes-compra/{id}/landed-cost` — calcular Costo Landed
- `POST /importaciones` — registrar embarque
- `GET /importaciones/{id}/tracking` — estado del envío
- `POST /importaciones/{id}/recepcion` — confirmar recepción

## Regla de separación de funciones (RN-13)

El usuario que crea una OC no puede ser el mismo que la aprueba. Se verificaría en backend comparando `creado_por` vs JWT `sub`.

## Costo Landed (diseño)

```
Costo Landed = Costo Producto
             + Flete (convertido a moneda local via MS-1 FX)
             + Aranceles
             + Seguros
             + Gastos Aduaneros
```

## Casos de uso (todos PLANIFICADOS)

SC-01..24: Gestión de OC, proveedores, Landed Cost, importaciones, tracking 3PL.

## Conexiones
- Depende de: [[ms1-identity]] (JWT + FX rates)
- Publicaría: `purchase.received` → [[ms4-inventory]] (para activar recepción FEFO)
- Consumiría: `fx.rate.updated` ← [[ms1-identity]] (para conversión de flete)
- Reglas: [[costo-landed]] (RN-09), [[reglas-negocio]] (RN-13)
- Integración: [[transporte-3pl]]

## Fuentes
- `src/SupplyChainService/Models/OrdenCompra.cs`
- `src/SupplyChainService/Models/CostoLandedHistorico.cs`
- `src/SupplyChainService/Program.cs`

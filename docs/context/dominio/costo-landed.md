---
id: costo-landed
tipo: dominio
titulo: Costo Landed — Cálculo de Costo Real de Importación
estado: planificado
fuentes: [src/SupplyChainService/Models/CostoLandedHistorico.cs, GlobalMart_ContextMaster.md#sec6-ms6]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: [ms6-supply-chain, ms1-identity]
publica: []
consume: []
reglas: [RN-09, RF-18, SC-06]
---
# Costo Landed — Cálculo de Costo Real de Importación

> El Costo Landed es el costo real total de un producto importado, incluyendo todos los gastos hasta llegar al destino.

## Estado actual: [PLANIFICADO]

El modelo `CostoLandedHistorico` existe en MS-6, pero sin lógica de cálculo ni endpoints.

## Fórmula

```
Costo Landed = Costo Producto
             + Flete (convertido a moneda local via tasas FX de MS-1)
             + Aranceles e Impuestos de Importación
             + Seguros
             + Gastos Aduaneros
```

## Modelo de datos (implementado en MS-6)

```csharp
public class CostoLandedHistorico
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid OrdenCompraId { get; set; }
    public decimal CostoProducto { get; set; }
    public decimal Flete { get; set; }          // convertido a moneda local
    public decimal Aranceles { get; set; }
    public decimal Seguros { get; set; }
    public decimal GastosAduaneros { get; set; }
    public decimal TotalLanded { get; set; }    // suma de todos los componentes
    public DateTime CalculadoEn { get; set; }
}
```

## Propósito de negocio

Permite conocer el **costo real** de cada producto importado para:
- Establecer precio de venta con margen correcto
- Comparar proveedores considerando todos los costos
- Registrar historial para análisis de rentabilidad

## Dependencia con tipos de cambio

El costo de flete generalmente viene en la moneda del proveedor (ej: USD). MS-6 debe consultar la tasa FX a MS-1 para convertir a la moneda base del tenant antes de sumar al Costo Landed.

## Conexiones
- Implementado en: [[ms6-supply-chain]]
- Depende de FX: [[tipos-de-cambio]], [[ms1-identity]]
- Reglas: [[reglas-negocio]] (RN-09)

## Fuentes
- `src/SupplyChainService/Models/CostoLandedHistorico.cs`
- `GlobalMart_ContextMaster.md` §6 MS-6

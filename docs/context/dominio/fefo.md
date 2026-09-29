---
id: fefo
tipo: dominio
titulo: Regla FEFO — First Expired, First Out
estado: planificado
fuentes: [src/WarehouseInventoryService/Models/Lote.cs, GlobalMart_ContextMaster.md#sec6-ms4]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: [ms4-inventory]
publica: []
consume: []
reglas: [RN-05, RF-10, WI-04, WI-05]
---
# Regla FEFO — First Expired, First Out

> Política de despacho de inventario que garantiza que el lote con menor fecha de vencimiento se consume primero. Crítica para productos perecederos en minimarkets.

## Estado actual

**PLANIFICADO** — El modelo `Lote` existe con `FechaVencimiento`, pero no hay lógica FEFO implementada. El propio KafkaConsumerService documenta: `"No implementa selección de lote por FEFO (ver ticket separado)"`.

## Por qué FEFO y no FIFO

FIFO (First In, First Out) despacha el más antiguo en fecha de ingreso, pero si un proveedor entregó lotes con vencimiento posterior, pueden quedar productos por vencer antes. FEFO garantiza despachar siempre el que expira primero, independientemente de cuándo llegó.

## Flujo diseñado (cuando se implemente)

```
1. POST /recepciones  { productoId, cantidad, fechaVencimiento, sucursalId }
   → Crear Lote { productoId, sucursalId, cantidad, fechaVencimiento, fechaRecepcion }
   → Validar capacidad de nevera (si aplica)
   → Actualizar Stock.Cantidad total

2. Al descontar stock (desde sale.completed):
   → Consultar lotes ORDENADOS por fechaVencimiento ASC (más próximos primero)
   → Descontar de los lotes con menor fecha hasta cubrir la cantidad vendida
   → Si un lote se agota → pasar al siguiente

3. Alertas:
   → Si FechaVencimiento < NOW() + umbralDias → publicar expiry.alert a Kafka
   → MS-7 despacha notificación al Reponedor
```

## Modelo de datos (implementado)

```csharp
// src/WarehouseInventoryService/Models/Lote.cs
public class Lote
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ProductoId { get; set; }
    public Guid SucursalId { get; set; }
    public decimal Cantidad { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public DateTime FechaRecepcion { get; set; }
}
```

## Restricciones de diseño

- Solo aplica a productos con `EsPesoVariable = false` o con `FechaVencimiento` no nula
- El umbral de alerta de caducidad es configurable por tenant
- Un producto sin fecha de vencimiento (ej: sal, aceite industrial) no requiere FEFO

## Casos de uso

| ID | Caso de Uso | Estado |
|----|-------------|--------|
| WI-04 | Registrar Recepción de Mercancía | [PLANIFICADO] |
| WI-05 | Aplicar Regla FEFO | [PLANIFICADO] |
| WI-06 | Ingresar Fecha Caducidad de Lote | [PLANIFICADO] |
| WI-07 | Asignar Lote a Nevera | [PLANIFICADO] |
| WI-08 | Controlar Volumen Nevera | [PLANIFICADO] |
| WI-10 | Generar Alerta Caducidad | [PLANIFICADO] |

## Conexiones
- Implementación: [[ms4-inventory]]
- Alertas: [[ms7-analytics]]
- Reglas: [[reglas-negocio]] (RN-05)

## Fuentes
- `src/WarehouseInventoryService/Models/Lote.cs`
- `src/WarehouseInventoryService/Messaging/KafkaConsumerService.cs` (comentario "No implementa FEFO")
- `GlobalMart_ContextMaster.md` §6 MS-4

---
id: peso-variable
tipo: dominio
titulo: Contrato de ítem pesable — S2-061
estado: contrato_definido
fuentes: [src/POSCartService/Controllers/VentasController.cs, src/POSCartService/DTOs/AgregarItemDTOs.cs, src/POSCartService/DTOs/CrearVentaRequest.cs, src/CatalogPricingService/Models/Producto.cs]
verificado_contra_codigo: false
ultima_revision: 2026-10-24
depende_de: [ms3-catalog, ms5-pos, hardware]
publica: []
consume: []
reglas: [RN-01, RN-06, RN-07]
---
# Contrato de ítem pesable — S2-061

**Entrega: CONTRATO DEFINIDO, versión 1. Aplicación integral en ejecución: PARCIAL (S2-062: validación al agregar; S2-063: cálculo y precisión del precio; S2-064: edición por tipo; S2-067: simulador).** Este documento especifica las reglas que deberán compartir catálogo, simulador, POS y MS-5. No certifica pruebas de integración ni una balanza física implementada.

El mínimo de 10 g y la precisión de 1 g son decisiones de diseño de esta tarea para el simulador, no especificaciones verificadas de hardware. Capacidad máxima y resolución de una balanza física: [NO VERIFICADO].

## 1. Unidad, precisión y mínimo

- Unidad canónica de un producto con `EsPesoVariable = true`: **kilogramo (`kg`)**. En JSON se llama `peso_kg`; en C#, `PesoKg`.
- Peso mínimo por ítem: **0.010 kg (10 g)**, inclusive.
- Incremento admitido: **0.001 kg (1 g)**; como máximo tres decimales significativos después del separador decimal. `1.2340` equivale a `1.234` y es válido.
- Se rechazan cero, negativos, valores inferiores al mínimo y fracciones de gramo. No se redondea el peso para hacerlo válido.
- El JSON usa números decimales con punto, sin texto de unidad: `0.750`, no `"750 g"` ni `"0,750"`. Cálculos de negocio con aritmética decimal.

## 2. Relación entre PesoKg y Cantidad

`EsPesoVariable` se obtiene del catálogo del tenant; el cliente no decide el tipo del producto.

| Tipo de producto | Entrada | Representación normalizada en MS-5 |
|---|---|---|
| Pesable | `peso_kg` obligatorio y válido; `cantidad` puede omitirse | `PesoKg = Cantidad = peso_kg` |
| Pesable con ambos campos | `cantidad` debe ser exactamente igual a `peso_kg` | Ambos representan kg; no se multiplican entre sí |
| No pesable | `peso_kg` ausente o `null`; `cantidad > 0` | `PesoKg = null`; cantidad en la unidad del producto |

Una cantidad contradictoria se rechaza: no se ignora ni se sustituye silenciosamente. Para pesables, omitir peso no permite usar `cantidad = 1` como sustituto. Para productos no pesables, este contrato no introduce una regla general de cantidades enteras: depende de su unidad de venta.

Estas reglas aplican a crear venta (`POST /ventas`), agregar ítem (`POST /ventas/{id}/items`) y modificar ítem (`PUT /ventas/{id}/items/{itemId}`), incluidos sus alias `/api`. Modificar un pesable requiere enviar el nuevo peso, aun si se omite cantidad. El ítem normalizado debe conservar la igualdad al persistirlo y devolverlo.

## 3. Conversión explícita desde la balanza o simulador

La lectura de entrada al adaptador debe identificar valor y unidad. Es un contrato conceptual del adaptador, **no un endpoint existente**:

```json
{"valor": 750, "unidad": "g"}
```

1. Si la unidad es `g`, calcular `peso_kg = valor / 1000` exactamente una vez.
2. Si es `kg`, conservar el valor.
3. Rechazar unidad ausente o diferente de `g`/`kg`; no adivinarla por magnitud.
4. Validar el peso convertido contra mínimo y precisión antes de enviarlo a MS-5. MS-5 debe repetir las validaciones del peso canónico.
5. Enviar solamente el peso convertido en `peso_kg`. La captura manual también debe indicar su unidad antes de convertir.

Ejemplo: `750 g → 0.750 kg`. Una lectura de `750 g` nunca debe enviarse como `peso_kg: 750`. El nombre `peso_kg` declara kg: el backend no puede descubrir por sí solo que un cliente etiquetó mal un valor. Por eso la aceptación exige comprobar también el adaptador, no solo validar el número en MS-5.

Una venta puede contener una lectura de 250 g y otra de 1 kg si se representan como `0.250` y `1.000` kg, respectivamente. También puede combinar productos pesables y productos por unidad. Lo prohibido es usar gramos como si fueran kilogramos sin conversión explícita.

## 4. Precio y subtotal

- Para pesables, `PrecioBase` de MS-3 debe representar el precio **por kilogramo**, en la moneda de la venta. Un precio por gramo no es compatible sin convertirlo explícitamente a precio por kg antes de usarlo.
- `UomBaseId` debe corresponder a kg para el producto pesable. La correspondencia concreta de UUID a kg está [NO VERIFICADO]; este contrato no inventa un identificador. Una unidad de catálogo desconocida o incompatible debe impedir agregar el producto hasta corregir su configuración.
- Nombre, tipo y precio se resuelven en el servidor. El precio enviado por el cliente nunca es autoridad; el renderer puede mostrar una estimación.
- Se conserva el precio de catálogo como snapshot con sus hasta cuatro decimales, sin redondearlo antes de multiplicar. Al modificar peso se usa ese mismo snapshot.
- `Subtotal = redondear(PrecioUnitario × PesoKg, 2, ToEven)`: dos decimales y empate al par, como el cálculo actual de MS-5. MS-2 calcula el IVA de cada línea sobre su subtotal redondeado, redondea el IVA a dos decimales con `ToEven` y suma los subtotales e IVA de las líneas para la venta (S2-063). No cambia las tasas ni la selección de exenciones.
- Precio negativo o no representable: rechazar. Precio cero es admisible en el ítem; no modifica las restricciones existentes del cobro.

Ejemplo: precio por kg `2000`, peso `0.750` → subtotal `1500.00`, antes de impuestos. No calcular `precio × cantidad × peso`.

**Persistencia — S2-063:** catálogo declara `decimal(12,4)` y `ItemVenta.PrecioUnitario` ahora usa `(20,4)`. La migración `20261024142752_PreserveItemPricePrecision` conserva cuatro decimales y los 16 dígitos enteros admitidos anteriormente. Los precios históricos ya redondeados no pueden recuperar decimales perdidos. El rollback a `(18,2)` puede redondear precios nuevos.

## 5. Ejemplos válidos y rechazados

En esta tabla el producto es pesable salvo indicación contraria. Resultados esperados del contrato, no resultados de pruebas ejecutadas.

| Entrada | Resultado esperado | Motivo / normalización |
|---|---|---|
| Lectura `750 g` | Válido | Convertir a `peso_kg = 0.750` |
| Lectura `1.234 kg` | Válido | Conservar `1.234` |
| `peso_kg: 0.010`, cantidad omitida | Válido | Mínimo; cantidad normalizada `0.010` |
| `peso_kg: 0.750, cantidad: 0.750` | Válido | Coinciden |
| `peso_kg: 1.2340` | Válido | Equivale a `1.234` |
| `peso_kg: 0.009` | Rechazado | Inferior a 10 g |
| `peso_kg: 0` o `-0.500` | Rechazado | Peso no positivo |
| `peso_kg: 0.7505` | Rechazado | Fracción de gramo; no redondear |
| `peso_kg: null` o ausente, `cantidad: 1` | Rechazado | Falta peso de producto pesable |
| `peso_kg: 0.750, cantidad: 1` | Rechazado | Cantidad contradictoria |
| Lectura `750` sin unidad | Rechazado en adaptador | Unidad ambigua |
| Lectura `2 lb` | Rechazado en adaptador | Unidad no admitida |
| Lectura `9 g` | Rechazado tras convertir | `0.009 kg` menor al mínimo |
| Lectura `750.5 g` | Rechazado tras convertir | `0.7505 kg` excede precisión |
| No pesable: `cantidad: 2`, peso ausente | Válido | Venta en su unidad habitual |
| No pesable: `cantidad: 2, peso_kg: 0.750` | Rechazado | Peso incompatible con tipo |
| Pesable con precio por gramo o unidad de catálogo desconocida | Rechazado | Configuración incompatible |

Fragmento de ítem válido para crear o agregar (UUID ilustrativo; debe existir y ser pesable en el tenant):

```json
{
  "producto_id": "11111111-1111-4111-8111-111111111111",
  "peso_kg": 0.750
}
```

Para modificar ese ítem: `{"peso_kg": 0.500}`. La respuesta/persistencia esperada debe contener `cantidad = 0.500` y `peso_kg = 0.500`.

## 6. Rechazo y aceptación verificable futura

Una entrada inválida debe producir HTTP 400 identificando el campo y la causa, sin agregar o modificar ítems ni totales. Al crear una venta con varios ítems, uno inválido impide crear la venta completa. Un error de configuración de unidad/precio también debe impedir la mutación, indicando la causa. No se alteran los controles JWT, tenant, turno abierto ni estado de venta existentes.

Para acreditar implementación posteriormente:

- Ejecutar los ejemplos válidos y rechazados en creación, agregado y modificación; verificar ausencia de mutación ante rechazo.
- Verificar conversión del simulador `250 g → 0.250 kg` junto con `1 kg → 1.000 kg` en la misma venta; subtotales a precio por kg.
- Verificar que un producto no pesable no acepte peso y que un pesable no acepte cantidad contradictoria.
- Verificar precio autoritativo y snapshot: `12.3456 × 0.750 → 9.26`; después de persistir y modificar a `0.500`, `12.3456 × 0.500 → 6.17`.
- Verificar empates del subtotal: `1 × 0.125 → 0.12` y `1 × 0.135 → 0.14` (ToEven).

## 7. Estado real y trabajo de implementación pendiente

Inspección de código, 2026-10-24:

- [IMPLEMENTADO] Los DTOs admiten `peso_kg`; crear/agregar copia un peso positivo a cantidad cuando catálogo indica `EsPesoVariable`. MS-5 obtiene precio de catálogo y calcula subtotal.
- [IMPLEMENTADO — S2-062] Agregar ítem consulta `EsPesoVariable` en catálogo y rechaza peso ausente, inferior a 0.010 kg, fracciones de gramo, cantidad contradictoria y peso en productos no pesables antes de calcular impuestos o persistir. S2-064 aplica las mismas restricciones al modificar, consultando el tipo en MS-3. Crear venta todavía no aplica estas reglas completas.
- [PARCIAL — S2-062] `AgregarItemRequest.Cantidad` es nullable: para pesables se deriva del peso cuando se omite o es null; si tiene valor debe coincidir. Para no pesables, omisión/null conserva default 1. `ModificarCantidadItemRequest.Cantidad` también es nullable desde S2-064, pero para no pesables debe enviarse una cantidad positiva al editar. El DTO de crear aún necesita adaptación.
- [PENDIENTE] Exponer/resolver unidad del catálogo: `ProductoCatalogDto` no incluye `UomBaseId`. Definir la correspondencia real y validar precio por kg.
- [IMPLEMENTADO — S2-063] Se conserva la precisión del precio persistido y MS-2 redondea subtotal e IVA por línea antes de sumar la venta.
- [PENDIENTE] Aplicar las validaciones completas de peso en crear venta y probarlas mediante HTTP/persistencia.
- [IMPLEMENTADO — S2-067] El simulador recibe unidad explícita `g`/`kg`, convierte una sola vez a kg y rechaza valores menores a 10 g o con fracciones inferiores a 1 g. La prueba dirigida verifica `750 g → 0.750 kg`; la aceptación E2E mediante IPC/UI sigue pendiente.

Validación S2-062 del 2026-10-24: **20/20 casos nuevos aprobados** más 12 de regresión, limitados a agregar ítems. [Reporte y evidencia](../../qa/S2-062/reporte.md).

## Fuentes de código y conexiones

- [Controlador de ventas](../../../src/POSCartService/Controllers/VentasController.cs)
- [DTOs de agregar/modificar y catálogo](../../../src/POSCartService/DTOs/AgregarItemDTOs.cs)
- [DTO de crear venta](../../../src/POSCartService/DTOs/CrearVentaRequest.cs)
- [Modelo de catálogo](../../../src/CatalogPricingService/Models/Producto.cs)
- [Persistencia MS-5](../../../src/POSCartService/Data/PosCartDbContext.cs)
- [Adaptador simulado](../../../globalmart-frontend/src/hardware/simulated-scale-adapter.ts)
- [MS-5](../servicios/ms5-pos.md), [MS-3](../servicios/ms3-catalog.md), [hardware](../integraciones/hardware.md)

Validación S2-063 del 2026-10-24: **42/42 pruebas Release aprobadas** (10 nuevas, 32 de regresión), con PostgreSQL temporal, HTTP MS-5 y calculador productivo MS-2 a través de handler HTTP. Regresión manual TC-01 aprobada. [Evidencia y límites](../../qa/S2-063/reporte.md).

S2-064: editar conserva el precio snapshot y consulta el tipo actual del catálogo. Un catálogo indisponible impide editar; no existe aún snapshot persistido de `EsPesoVariable`. [Reglas y evidencia de edición](../../qa/S2-064/reporte.md).

Validación S2-064 del 2026-10-24: **75/75 pruebas Release aprobadas**, incluidas 23 nuevas de edición y 10 de aislamiento de ventas entre tenants, con HTTP/JWT y PostgreSQL temporal. [Evidencia](../../qa/S2-064/reporte.md).

Verificación final S2-064: recompilación Release y **33/33 casos S2-064/S2-063 aprobados**, sin fallos ni omisiones; evidencia adicional en el reporte S2-064.

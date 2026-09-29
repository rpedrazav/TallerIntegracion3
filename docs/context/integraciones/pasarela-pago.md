---
id: pasarela-pago
tipo: integracion
titulo: Integración con Pasarela de Pago
estado: planificado
fuentes: [GlobalMart_ContextMaster.md#sec12, src/POSCartService/]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: [ms5-pos]
publica: []
consume: []
reglas: [RN-02, RF-07, PC-17, PC-27, PC-28]
---
# Integración con Pasarela de Pago

> Toda transacción con tarjeta DEBE ir por una pasarela externa (RN-02). **Estado: PLANIFICADO** — MS-5 no tiene integración de pago implementada.

## Estado: [PLANIFICADO]

`VentaService.CompletarAsync` solo cambia el estado a COMPLETADA sin procesar pago real.

## Pasarelas contempladas por región

| Región | Pasarela | Protocolo |
|--------|---------|-----------|
| Chile | Transbank Webpay | REST API oficial |
| Global | Stripe | REST API + webhooks |
| Latinoamérica | MercadoPago | REST API |

## Regla crítica (RN-02)

```
Nunca almacenar datos de tarjeta en GlobalMart OS.
Toda transacción pasa obligatoriamente por la pasarela.
El terminal POS (hardware) captura los datos y los envía directamente a la pasarela.
GlobalMart OS solo recibe la referencia de la transacción (RRN/código de autorización).
```

## Flujo diseñado

```
1. Cajero selecciona "Cobrar con tarjeta"
2. Terminal POS muestra monto al cliente
3. Cliente inserta/acerca la tarjeta
4. Terminal captura datos y los envía a la pasarela (PCI DSS scope)
5. MS-5 POST a pasarela: { monto, moneda, referencia_interna }
6. Pasarela responde: { autorizado: true/false, codigo_autorizacion, rrn }
7. Si autorizado:
   → VentaService.CompletarAsync
   → Kafka sale.completed (PLANIFICADO)
   → Imprimir comprobante (PLANIFICADO)
8. Si rechazado:
   → Mostrar error al cajero
   → Venta sigue en PENDIENTE
```

## Reembolsos (anulaciones)

Al anular una venta cobrada con tarjeta:
- MS-5 debe llamar a la pasarela con el código de autorización para revertir el cargo
- Actualmente `VentaService.AnularAsync` no llama a ninguna pasarela [DISCREPANCIA]

## Casos de uso afectados

| ID | Caso de Uso | Estado |
|----|-------------|--------|
| PC-17 | Cobrar con Tarjeta (débito/crédito) | [PLANIFICADO] |
| PC-18 | Cobrar Mixto (efectivo + tarjeta) | [PLANIFICADO] |
| PC-27 | Procesar Pago en Pasarela | [PLANIFICADO] |
| PC-28 | Manejar Respuesta de Pasarela | [PLANIFICADO] |

## Conexiones
- POS: [[ms5-pos]]
- Reglas: [[reglas-negocio]] (RN-02)
- Hardware: [[hardware]]

## Fuentes
- `GlobalMart_ContextMaster.md` §12
- `src/POSCartService/Services/VentaService.cs` (AnularAsync sin reembolso)

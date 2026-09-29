---
id: ms5-pos
tipo: microservicio
titulo: MS-5 · POS & Cart Service
estado: parcial
fuentes: [src/POSCartService/]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: [ms1-identity, ms2-tax, ms3-catalog]
publica: []
consume: []
reglas: [RN-02, RN-03, RN-06, RF-02, RF-03, RF-04, RF-05, RF-06, RF-07]
---
# MS-5 · POS & Cart Service

> Servicio central del punto de venta. Gestiona turnos de caja y el ciclo completo de una venta (carrito, ítems, cobro, anulación). Integrado con MS-2 (IVA) y MS-3 (productos). **FALTA:** publicar `sale.completed` a Kafka, integración con pasarela de pago y hardware.

## Estructura del proyecto

```
src/POSCartService/
├── Controllers/
│   ├── TurnosController.cs   POST abrir|activo|cerrar
│   └── VentasController.cs   CRUD ventas + gestión de ítems (cobro y anulación aún no expuestos)
├── Data/  PosCartDbContext.cs + Migrations/
├── Models/  Venta · ItemVenta · Turno · Pago · Anulacion
├── Repositories/  IVentaRepository · VentaRepository · ITurnoRepository · TurnoRepository · IItemVentaRepository · ItemVentaRepository
├── Services/
│   ├── VentaService.cs       ← lógica de negocio central
│   ├── TurnoService.cs
│   ├── CatalogClient.cs      ← llama MS-3
│   └── TaxClient.cs          ← llama MS-2
├── Validators/  (4 validators FluentValidation)
├── Exceptions/  TurnoYaAbiertoException · ExternalServiceException
└── tests/
    ├── POSCartService.AgregarItemTest/
    └── POSCartService.ManualTest/
```

## Endpoints reales

| Método | Ruta | Auth | Estado |
|--------|------|------|--------|
| POST | `/turnos/abrir` | JWT | [IMPLEMENTADO] |
| GET | `/turnos/activo` | JWT | [IMPLEMENTADO] |
| POST | `/turnos/cerrar` | JWT | [IMPLEMENTADO] |
| POST | `/ventas` | JWT | [IMPLEMENTADO] |
| POST | `/ventas/{id}/items` | JWT | [IMPLEMENTADO] |
| PUT | `/ventas/{id}/items/{itemId}` | JWT | [IMPLEMENTADO] |
| DELETE | `/ventas/{id}/items/{itemId}` | JWT | [IMPLEMENTADO] |
| GET | `/ventas/{id}` | JWT | [IMPLEMENTADO] |
| GET | `/ventas/turno/{turnoId}` | JWT | [IMPLEMENTADO] |
| POST | `/ventas/{id}/cobrar` | JWT | [PLANIFICADO en controller — implementado en VentaService.CompletarAsync] |
| POST | `/ventas/{id}/anular` | JWT | [PLANIFICADO en controller — implementado en VentaService.AnularAsync] |
| GET | `/health` | Público | [IMPLEMENTADO] |

## Flujo de venta (implementado)

```
1. POST /turnos/abrir  { sucursalId, montoFondoInicial }
   → Verifica que el cajero no tenga turno activo (TurnoYaAbiertoException → 409)
   → Crea Turno { cajeroId, tenantId, sucursalId, fondoInicial, estado=ABIERTO }

2. POST /ventas  { turnoId, items[], metodoPago }
   → Verifica turno abierto (RN-06)
   → Calcula subtotal, IVA via TaxClient→MS-2, total
   → Estado: PENDIENTE

3. POST /ventas/{id}/items  { productoId, cantidad, pesoKg? }
   → Llama CatalogClient→MS-3 para obtener nombre y precio del producto
   → Llama TaxClient→MS-2 para recalcular IVA con todos los ítems
   → Actualiza Venta.Subtotal / .Impuestos / .Total

4. POST /ventas/{id}/cobrar  (PARCIAL)
   → VentaService.CompletarAsync: cambia estado a COMPLETADA
   → ❌ NO publica sale.completed a Kafka
   → ❌ NO integra pasarela de pago

5. POST /ventas/{id}/anular  { motivo }
   → VentaService.AnularAsync: estado → ANULADA
   → Crea Anulacion { ventaId, autorizadoPor, motivo }
   → ❌ NO solicita reembolso a pasarela
```

## Modelo de datos

```
Turno
  id               GUID PK
  tenant_id        GUID
  cajero_id        GUID
  sucursal_id      GUID
  fondo_inicial    decimal
  estado           enum (ABIERTO|CERRADO)
  abierto_en       DateTime
  cerrado_en       DateTime?

Venta
  id          GUID PK
  tenant_id   GUID
  turno_id    GUID FK
  cajero_id   GUID
  sucursal_id GUID
  subtotal    decimal
  impuestos   decimal
  total       decimal
  metodo_pago enum (EFECTIVO|TARJETA|MIXTO)
  estado      enum (PENDIENTE|COMPLETADA|ANULADA|CANCELADA)
  creada_en   DateTime

ItemVenta
  id              GUID PK
  venta_id        GUID FK
  producto_id     GUID
  nombre_producto string (snapshot del nombre al momento de la venta)
  precio_unitario decimal
  cantidad        decimal
  peso_kg         decimal?  (para productos a granel)
  subtotal        decimal

Anulacion
  id             GUID PK
  venta_id       GUID FK
  autorizado_por GUID (cajeroId)
  motivo         string
  creada_en      DateTime

Pago
  id             GUID PK
  venta_id       GUID FK
  metodo         string
  monto          decimal
  referencia     string? (código de pasarela)
```

## Multi-tenant

JWT claims requeridos:
- `cajero_id` (o `sub` o `NameIdentifier`) — identifica el cajero
- `tenant_id` — aislamiento de datos

TenantMiddleware inyecta `CurrentTenantId` en `PosCartDbContext`.

## Brecha crítica: sale.completed sin publicar

```csharp
// VentaService.CompletarAsync — FALTA agregar:
// var producer = new ProducerBuilder<string,string>(config).Build();
// producer.Produce("sale.completed", new Message<string,string> {
//   Key = venta.TenantId.ToString(),
//   Value = JsonSerializer.Serialize(new SaleCompletedEvent { ... })
// });

public async Task<Venta> CompletarAsync(Guid ventaId)
{
    // ...
    venta.Estado = EstadoVenta.COMPLETADA;
    return await _ventaRepository.ActualizarAsync(venta);
    // ← sale.completed NO se publica aquí
}
```

## Casos de uso cubiertos

| ID | Caso de Uso | Estado |
|----|-------------|--------|
| PC-01 | Abrir Turno de Caja | [IMPLEMENTADO] |
| PC-02 | Registrar Fondo de Apertura | [IMPLEMENTADO] |
| PC-03 | Cerrar Turno de Caja | [IMPLEMENTADO] |
| PC-04 | Cuadre de Caja | [PARCIAL — sin declaración de billetes] |
| PC-05 | Declarar Billetes y Monedas | [PLANIFICADO] |
| PC-06 | Iniciar Nueva Venta (Carrito) | [IMPLEMENTADO] |
| PC-07 | Agregar Producto (escaneo) | [IMPLEMENTADO] |
| PC-08 | Agregar Producto (búsqueda) | [IMPLEMENTADO] |
| PC-09 | Agregar Producto a Granel | [PARCIAL — modelo soporta PesoKg] |
| PC-10 | Integrar Peso de Balanza | [PLANIFICADO] |
| PC-11 | Modificar Cantidad | [IMPLEMENTADO] |
| PC-12 | Eliminar Ítem del Carrito | [IMPLEMENTADO] |
| PC-13 | Descuento Manual | [PLANIFICADO] |
| PC-14 | Calcular Total con Impuestos | [IMPLEMENTADO] |
| PC-15 | Cobrar en Efectivo | [PARCIAL — sin vuelto automático] |
| PC-16 | Calcular Vuelto | [PLANIFICADO] |
| PC-17 | Cobrar con Tarjeta | [PLANIFICADO] |
| PC-18 | Pago Mixto (efectivo + tarjeta) | [PLANIFICADO] |
| PC-19 | Emitir Recibo / Comprobante | [PLANIFICADO] |
| PC-20 | Anular Venta (Devolución) | [PARCIAL — implementado en VentaService.AnularAsync] |
| PC-21 | Devolución Parcial | [PLANIFICADO] |
| PC-22 | Poner Venta en Espera (Hold) | [PLANIFICADO] |
| PC-23 | Recuperar Venta de Espera | [PLANIFICADO] |
| PC-24 | Publicar sale.completed | [PLANIFICADO] |
| PC-25 | Publicar sale.reversed | [PLANIFICADO] |
| PC-26 | Leer Tarjeta en Terminal (NFC/chip/mag) | [PLANIFICADO] |
| PC-27 | Enviar Solicitud a Pasarela | [PLANIFICADO] |
| PC-28 | Recibir Respuesta de Pasarela | [PLANIFICADO] |
| PC-29 | Manejar Rechazo de Pago | [PLANIFICADO] |
| PC-30 | Solicitar Reembolso a Pasarela | [PLANIFICADO] |
| PC-31 | Confirmar Reembolso | [PLANIFICADO] |
| PC-32 | Abrir Cajón de Dinero | [PLANIFICADO] |
| PC-33 | Imprimir Recibo en Terminal | [PLANIFICADO] |
| PC-34 | Mostrar Total en Display Cliente | [PLANIFICADO] |
| PC-35 | Identificar Cliente Afiliado en POS | [PLANIFICADO] |
| PC-36 | Aplicar Beneficios de Membresía | [PLANIFICADO] |
| PC-37 | Acumular Puntos tras Venta | [PLANIFICADO] |

## Conexiones
- Depende de: [[ms2-tax]] (TaxClient), [[ms3-catalog]] (CatalogClient), [[ms1-identity]] (JWT)
- Debería publicar: `sale.completed` → [[ms4-inventory]], [[ms7-analytics]], [[ms8-loyalty]]
- Reglas: [[reglas-negocio]] (RN-02, RN-06), [[multi-tenant]]
- Kafka: [[kafka-topics]]

## Fuentes
- `src/POSCartService/Controllers/VentasController.cs`
- `src/POSCartService/Controllers/TurnosController.cs`
- `src/POSCartService/Services/VentaService.cs`
- `src/POSCartService/Services/TurnoService.cs`

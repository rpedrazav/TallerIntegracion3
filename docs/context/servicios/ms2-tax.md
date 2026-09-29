---
id: ms2-tax
tipo: microservicio
titulo: MS-2 · Tax & Compliance Service
estado: parcial
fuentes: [src/TaxComplianceService/, src/POSCartService/Services/TaxClient.cs]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: [ms1-identity]
publica: []
consume: []
reglas: [RN-03, RF-05, RF-08, TC-01]
---
# MS-2 · Tax & Compliance Service

> Calcula impuestos (IVA) para ventas del POS. Diseñado para emitir DTEs electrónicos (boletas/facturas) con entidades fiscales por país. Solo el cálculo de IVA está implementado; todo lo fiscal (DTE, folios, SII/AFIP) es PLANIFICADO.

## Estructura del proyecto

```
src/TaxComplianceService/
├── Controllers/  TaxController.cs
├── Data/  TaxDbContext.cs + Migrations/
├── Models/  ConfiguracionFiscal · DocumentoTributario · TaxCalculateRequest · TaxCalculateRequestValidator
├── Services/
│   ├── ITaxCalculatorService.cs / TaxCalculatorService.cs
│   ├── ITenantConfigClient.cs / TenantConfigClient.cs   ← llama MS-1
│   ├── TaxBreakdown.cs  (DTO de respuesta)
│   └── TaxItem.cs       (DTO de ítem)
└── tests/  TaxComplianceService.ManualTest/
```

## Endpoints reales

| Método | Ruta | Auth | Estado |
|--------|------|------|--------|
| POST | `/tax/calculate` | JWT | [IMPLEMENTADO] |
| GET | `/health` | Público | [IMPLEMENTADO] |

### Endpoints diseñados pero NO implementados
- `POST /dte/solicitar-folio` — solicitar folio a entidad fiscal [PLANIFICADO]
- `POST /dte/emitir` — emitir boleta/factura electrónica [PLANIFICADO]
- `GET /dte/{id}/estado` — consultar estado de DTE [PLANIFICADO]
- `GET /reportes/declaracion-fiscal` — reporte fiscal del período [PLANIFICADO]

## Lógica de cálculo (implementada)

```
POST /tax/calculate
Body: { "items": [{ "nombre": "Coca Cola", "precio": 2100, "cantidad": 1 }], "porcentajeIva": null }

Flujo:
1. Extrae tenant_id del JWT
2. Llama GET /tenants/{id}/config en MS-1 para obtener PorcentajeIva
3. Calcula: subtotal = Σ(precio × cantidad)
             IVA = subtotal × porcentaje / 100
             total = subtotal + IVA
4. Retorna TaxBreakdown con desglose por ítem

Respuesta:
{
  "subtotal": 2100,
  "porcentajeIva": 19,
  "iva": 399,
  "total": 2499,
  "items": [{ "nombre": "Coca Cola", "subtotal": 2100, "iva": 399, "total": 2499 }]
}
```

**Override:** Si el request envía `porcentajeIva`, se usa ese valor en lugar del configurado en el tenant.

## Dependencia con MS-1

`TenantConfigClient` hace un `GET /tenants/{id}/config` a MS-1 pasando el mismo Authorization header. Si MS-1 no responde, MS-2 retorna 404. Sin MS-1 activo, MS-2 no puede calcular impuestos.

## Modelo de datos

```
ConfiguracionFiscal
  id          GUID PK
  tenant_id   GUID (discriminador multi-tenant)
  pais        string
  tipo_iva    string ("simple"|"compuesto")
  porcentaje  decimal

DocumentoTributario
  id          GUID PK
  tenant_id   GUID
  tipo        string ("boleta"|"factura")
  folio       string
  estado      string ("pendiente"|"emitido"|"rechazado")
  emitido_en  DateTime
```

## Cómo es consumido

MS-5 (POSCartService) llama a MS-2 directamente (no vía Kong) en cada operación de carrito:
- Al agregar ítem → recalcula IVA
- Al modificar cantidad → recalcula IVA  
- Al eliminar ítem → recalcula IVA
- Al completar venta → (pendiente de implementar)

`src/POSCartService/Services/TaxClient.cs` — cliente HTTP hacia MS-2.

## Brechas respecto al diseño

| Brecha | Regla | Impacto |
|--------|-------|---------|
| Sin emisión de DTE | RN-03, RF-08 | Toda venta carece de documento tributario |
| Sin integración SII/AFIP/IRS | TC-06..09 | Cumplimiento fiscal no automatizable |
| Solo IVA simple | TC-02 | IVA compuesto en cascada no implementado |
| Sin exenciones fiscales | TC-03/04 | No se puede eximir productos o clientes |
| Sin reintentos si Entidad Fiscal falla | TC-10 | Fragilidad ante caídas del SII |

## Casos de uso cubiertos

| ID | Caso de Uso | Estado |
|----|-------------|--------|
| TC-01 | Calcular Impuesto de Venta | [IMPLEMENTADO] |
| TC-02 | Calcular IVA Compuesto (cascada) | [PLANIFICADO] |
| TC-03 | Aplicar Exención Fiscal (Producto) | [PLANIFICADO] |
| TC-04 | Aplicar Exención Fiscal (Cliente) | [PLANIFICADO] |
| TC-05 | Configurar Reglas Fiscales por País | [PARCIAL — solo IVA% en MS-1] |
| TC-06 | Solicitar Folio Electrónico | [PLANIFICADO / SIMULADO] |
| TC-07 | Validar Folio con Entidad Fiscal | [PLANIFICADO / SIMULADO] |
| TC-08 | Emitir DTE | [PLANIFICADO / SIMULADO] |
| TC-09 | Generar Boleta / Factura Electrónica | [PLANIFICADO / SIMULADO] |
| TC-10 | Manejar Rechazo de Folio (Reintento) | [PLANIFICADO / SIMULADO] |
| TC-11 | Consultar Estado de DTE | [PLANIFICADO] |
| TC-12 | Reporte Declaración Fiscal | [PLANIFICADO] |

## Conexiones
- Depende de: [[ms1-identity]] (para obtener PorcentajeIva del tenant)
- Es consumido por: [[ms5-pos]] (TaxClient)
- Reglas: [[fiscal-por-pais]], [[reglas-negocio]] (RN-03)

## Fuentes
- `src/TaxComplianceService/Controllers/TaxController.cs`
- `src/TaxComplianceService/Services/TaxCalculatorService.cs`
- `src/TaxComplianceService/Services/TenantConfigClient.cs`
- `src/POSCartService/Services/TaxClient.cs`

---
id: fiscal-por-pais
tipo: dominio
titulo: Cumplimiento Fiscal por País — GlobalMart OS
estado: planificado
fuentes: [src/TaxComplianceService/Models/ConfiguracionFiscal.cs, GlobalMart_ContextMaster.md#sec12]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: [ms2-tax, ms1-identity]
publica: []
consume: []
reglas: [RN-03, RF-08, TC-05]
---
# Cumplimiento Fiscal por País — GlobalMart OS

> Cada tenant opera según las reglas fiscales de su país. MS-2 maneja la lógica; MS-1 almacena la configuración fiscal del tenant.

## Estado actual: [PLANIFICADO]

Solo el cálculo de IVA simple está implementado. La integración con entidades fiscales reales (SII, AFIP, IRS) es PLANIFICADA.

## Países y entidades fiscales diseñadas

| País | Entidad | Protocolo | Documento |
|------|---------|-----------|-----------|
| Chile | SII (Servicio de Impuestos Internos) | REST con certificado digital | DTE (XML firmado) + CAF |
| Argentina | AFIP | WebService SOAP | CAE por comprobante |
| USA | IRS | Reportes periódicos (no por transacción) | — |

## IVA por país (diseñado)

| País | IVA estándar | Tipo |
|------|-------------|------|
| Chile | 19% | Simple |
| Argentina | 21% | Simple (con exenciones por categoría) |
| USA | Variable por estado | No aplica IVA federal por transacción |

## Flujo diseñado de DTE (Chile/SII)

```
1. MS-5 completa venta
2. MS-5 llama MS-2 POST /dte/solicitar-folio
3. MS-2 consulta SII con certificado digital → recibe CAF (rango de folios)
4. MS-2 asigna folio al DTE
5. MS-2 genera XML del DTE firmado digitalmente
6. MS-2 envía DTE al SII → recibe confirmación
7. MS-2 retorna DTE al MS-5 para imprimir
8. Si SII falla → reintenta 3 veces con backoff exponencial
9. Si sigue fallando → venta en estado pending_dte, reintento en background
```

## Configuración fiscal en MS-1

```json
// GET /tenants/{id}/config
{
  "pais": "CL",
  "moneda": "CLP",
  "idioma": "es",
  "zonaHoraria": "America/Santiago",
  "porcentajeIva": 19
}
```

## Lo que MS-2 tiene implementado actualmente

- ✅ Cálculo de IVA simple con porcentaje configurable
- ✅ Consulta de configuración fiscal del tenant a MS-1
- ❌ Sin solicitud de folios al SII/AFIP
- ❌ Sin generación de XML DTE
- ❌ Sin firma digital de documentos
- ❌ Sin integración SOAP para AFIP

## Conexiones
- Servicio fiscal: [[ms2-tax]]
- Configuración tenant: [[ms1-identity]]
- Reglas: [[reglas-negocio]] (RN-03)

## Fuentes
- `src/TaxComplianceService/Models/ConfiguracionFiscal.cs`
- `GlobalMart_ContextMaster.md` §12

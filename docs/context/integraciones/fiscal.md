---
id: fiscal
tipo: integracion
titulo: Integración con Entidades Fiscales (SII/AFIP/IRS)
estado: planificado
fuentes: [GlobalMart_ContextMaster.md#sec12, src/TaxComplianceService/]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: [ms2-tax, fiscal-por-pais]
publica: []
consume: []
reglas: [RN-03, RF-08, TC-06, TC-07, TC-08]
---
# Integración con Entidades Fiscales

> Conexión entre MS-2 y las entidades fiscales de cada país para emitir DTEs electrónicos. **Completamente PLANIFICADO** — sin ninguna implementación en el código actual.

## Estado: [PLANIFICADO]

MS-2 tiene solo el cálculo de IVA implementado. Toda la integración con SII/AFIP/IRS es futura.

## Chile — SII

```
Protocolo: REST + certificado digital (`.pfx` o `.pem`)
Flujo:
  1. Solicitar CAF (Código de Autorización de Folios) → rango de folios
  2. Por cada venta: asignar folio del CAF
  3. Generar XML del DTE (formato SII)
  4. Firmar XML con certificado digital del emisor
  5. Enviar al SII → recibir respuesta de aceptación/rechazo

Si SII no responde: reintento exponencial (hasta 3 veces)
Si sigue fallando: venta queda en estado `pending_dte`
```

## Argentina — AFIP

```
Protocolo: WebService SOAP
Flujo:
  1. Por cada comprobante: solicitar CAE (Código de Autorización Electrónico)
  2. AFIP valida y devuelve CAE + fecha vencimiento
  3. CAE se imprime en el comprobante
```

## USA — IRS

```
Sin validación por transacción individual.
Reportes fiscales periódicos (mensual/trimestral).
MS-2 acumula datos; MS-7 genera reportes.
```

## Seguridad

- Certificados digitales: almacenados como variables de entorno (NUNCA en el repositorio)
- Datos de tarjeta: NUNCA almacenados en GlobalMart OS (PCI DSS)

## Casos de uso afectados

| ID | Caso de Uso | Estado |
|----|-------------|--------|
| TC-06 | Solicitar Folio Electrónico | [PLANIFICADO] |
| TC-07 | Validar Folio con Entidad Fiscal | [PLANIFICADO] |
| TC-08 | Emitir DTE | [PLANIFICADO] |
| TC-09 | Generar Boleta/Factura Electrónica | [PLANIFICADO] |
| TC-10 | Manejar Rechazo de Folio | [PLANIFICADO] |

## Conexiones
- Servicio: [[ms2-tax]]
- Dominio fiscal: [[fiscal-por-pais]]
- Reglas: [[reglas-negocio]] (RN-03)

## Fuentes
- `GlobalMart_ContextMaster.md` §12
- `src/TaxComplianceService/Models/DocumentoTributario.cs`

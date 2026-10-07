---
id: ms2-tax
tipo: microservicio
titulo: MS-2 · Tax & Compliance Service
estado: parcial
fuentes: [src/TaxComplianceService/, src/POSCartService/Services/TaxClient.cs]
verificado_contra_codigo: true
ultima_revision: 2026-10-06
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
├── Controllers/  TaxController.cs · ComprobantesController.cs
├── Data/  TaxDbContext.cs + Migrations/
├── Exceptions/  TenantConfigNotFoundException.cs
├── Middleware/  TenantMiddleware.cs   ← inyecta CurrentTenantId (RN-01)
├── Models/  ConfiguracionFiscal · DocumentoTributario · Comprobante · TaxCalculateRequest · CrearComprobanteRequest · TaxCalculateRequestValidator
├── Services/
│   ├── ITaxCalculatorService.cs / TaxCalculatorService.cs
│   ├── IComprobanteService.cs / ComprobanteService.cs
│   ├── ITenantConfigClient.cs / TenantConfigClient.cs   ← llama MS-1
│   ├── TaxBreakdown.cs  (DTO de respuesta)
│   └── TaxItem.cs       (DTO de ítem)
└── tests/  TaxComplianceService.ManualTest/
```

## Endpoints reales

| Método | Ruta | Auth | Estado |
|--------|------|------|--------|
| POST | `/tax/calculate` | JWT | [IMPLEMENTADO] |
| POST | `/comprobantes` | JWT | [IMPLEMENTADO] |
| GET | `/comprobantes/{id}` | JWT | [IMPLEMENTADO] |
| GET | `/health` | Público | [IMPLEMENTADO] |

## Emisión de comprobantes (implementada)

```
POST /comprobantes
Body: { "venta_id": "<uuid>", "items": [{ "nombre": "Coca Cola", "precio": 2100, "cantidad": 1 }] }

Flujo:
1. Extrae tenant_id del JWT y cajero_id del claim sub
2. Llama GET /tenants/{id}/config en MS-1 para obtener PorcentajeIva
3. Calcula subtotal, IVA y total en el servidor con TaxCalculatorService
   (los importes NO se aceptan desde el cliente)
4. Obtiene el correlativo con obtener_correlativo_comprobante(uuid),
   que hace nextval sobre la secuencia PostgreSQL del tenant
5. Persiste el Comprobante y retorna 201 con el registro creado

Respuesta (201):
{
  "id": "<uuid>",
  "tenantId": "<uuid>",
  "numeroCorrelativo": 1,
  "ventaId": "<uuid>",
  "cajeroId": "<uuid>",
  "items": "[...]",
  "subtotal": 2100,
  "iva": 399,
  "total": 2499,
  "fechaEmision": "2026-10-01T19:00:00Z"
}
```

**Correlativo:** es por tenant y se reinicia en 1 para cada tenant. La unicidad bajo concurrencia
la garantiza `nextval` de PostgreSQL, que es atómico. Se apoya en el índice único
`(TenantId, NumeroCorrelativo)` de la tabla `Comprobantes`.

**Funciones PostgreSQL** (migraciones `AddComprobanteSequenceFunction`, `AddComprobanteCorrelativeFunction`
y `FixComprobanteCorrelativeSequenceRace`):
- `crear_secuencia_comprobante(uuid)` — crea la secuencia del tenant si no existe (RETURNS void)
- `obtener_correlativo_comprobante(uuid)` — crea la secuencia si no existe y retorna el siguiente valor (RETURNS bigint)

> [!WARNING]
> La creación de la secuencia dentro de `obtener_correlativo_comprobante` estuvoraces un defecto
> real, corregido en `FixComprobanteCorrelativeSequenceRace`. `CREATE SEQUENCE IF NOT EXISTS` **no
> es atómico**: evalúa la existencia y luego crea, así que varias transacciones concurrentes
> pasaban el chequeo y competían por el índice `pg_class_relname_nsp_index`. La perdedora recibía
> `23505 duplicate key value` y `POST /comprobantes` devolvía **500**.
>
> Solo se manifestaba en el **primer** comprobante de un tenant nuevo, porque con la secuencia ya
> creada el `CREATE` nunca se ejecutaba. Reproducido con 5 POST simultáneos contra un tenant sin
> secuencia: 1× 201 y 4× 500. La corrección envuelve el `CREATE` en un bloque `EXCEPTION` que
> absorbe `unique_violation` y `duplicate_table`, porque esa excepción significa que otra
> transacción ya creó la secuencia y se puede continuar con `nextval`.
>
> `nextval` nunca estuvo en duda: es atómico desde el inicio.

### Verificación de concurrencia

Probado con `curl --parallel` (conexiones simultáneas reales, no procesos en background que
pueden serializarse), corroborando el solapamiento por aritmética: si la suma de los tiempos
individuales supera el tiempo de pared, los requests se ejecutaron unos dentro de otros.

| Escenario | Resultado |
|---|---|
| 5 simultáneos, tenant con secuencia existente | 5× 201, correlativos consecutivos sin huecos |
| 5 simultáneos, tenant **nuevo** (sin secuencia) | 5× 201 con correlativos `1..5` (pre-fix: 1× 201, 4× 500) |
| 20 simultáneos, tenant nuevo | 20× 201, correlativos `1..20` |
| 10 simultáneos repartidos entre 2 tenants | Cada tenant con su serie independiente, sin colisión |
| Integridad en base | 0 duplicados en `(TenantId, NumeroCorrelativo)`, sin huecos en ningún tenant |

**Alcance:** el comprobante se registra localmente en MS-2. No emite DTE ni solicita folio a la entidad
fiscal, y no se persiste `sucursal_id` (queda pendiente Despite que MS-1 ya expone
`GET`/`POST /sucursales`, porque el modelo de `Comprobantes` no tiene esa columna).

### Consulta para reimpresión

```
GET /comprobantes/{id}
Auth: Bearer <JWT con tenant_id>

200 → comprobante completo (misma entidad que devuelve POST)
401 → sin JWT, JWT inválido o sin claim tenant_id
404 → no existe o pertenece a otro tenant (indistinguishable a propósito)
```

La lectura se aísla por el `HasQueryFilter` global de `TaxDbContext`, alimentado por
`Middleware/TenantMiddleware.cs` (TI3-460), que corre después de `UseAuthorization()` y antes de
`MapControllers()`. `TenantMiddleware` salta `/health` y `/swagger` para que sigan siendo públicos.
Soporta claims `tenant_id` y `TenantId`, loguea advertencias estructuradas cuando falta el claim y retorna 401 Unauthorized `{ error = "Token inválido: falta tenant_id" }`.
Validado con pruebas unitarias en `Ms2TenantMiddlewareTests.cs` (8 pruebas).

**Diferencia con POST:** `POST` no dependía de `TenantMiddleware` porque los `HasQueryFilter` solo
afectan lecturas; el tenant se asignaba explícitamente desde el claim. `GET` sí lo requiere: sin
`CurrentTenantId` el filtro se traduce a `WHERE "TenantId" = NULL` y no matchea ninguna fila.
Por eso el middleware es indispensable para cualquier endpoint de lectura de MS-2.

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

Comprobante
  id                GUID PK
  tenant_id         GUID (discriminador multi-tenant)
  numero_correlativo bigint (único junto a tenant_id, reinicia en 1 por tenant)
  venta_id          GUID (referencia a MS-5, sin navigation property)
  cajero_id         GUID (referencia a MS-1, sin navigation property)
  items             jsonb (desglose de TaxItemBreakdown serializado)
  subtotal          numeric(12,2)
  iva               numeric(12,2)
  total             numeric(12,2)
  fecha_emision     timestamptz
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
| Comprobante no es DTE | RN-03, RF-08 | Se registra el comprobante localmente pero sin folio/CAE ante la entidad fiscal |
| Sin emisión de DTE | RN-03, RF-08 | Toda venta carece de documento tributario |
| Sin integración SII/AFIP/IRS | TC-06..09 | Cumplimiento fiscal no automatizable |
| Solo IVA simple | TC-02 | IVA compuesto en cascada no implementado |
| Sin exenciones fiscales | TC-03/04 | No se puede eximir productos o clientes |
| Sin reintentos si Entidad Fiscal falla | TC-10 | Fragilidad ante caídas del SII |
| Sin `sucursal_id` en el comprobante | RN-01 | MS-1 ya expone `GET`/`POST /sucursales`, pero `Comprobantes` no tiene la columna ni se pide en el request |

## Casos de uso cubiertos

| ID | Caso de Uso | Estado |
|----|-------------|--------|
| TC-01 | Calcular Impuesto de Venta | [IMPLEMENTADO] |
| US-10 | Generar comprobante al completar una venta | [PARCIAL — registra comprobante local, sin DTE] |
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
- Conexión a [[multi-tenant]] y [[ms4-inventory]] (mismo patrón de TenantMiddleware)
- Reglas: [[fiscal-por-pais]], [[reglas-negocio]] (RN-03)

## Fuentes
- `src/TaxComplianceService/Controllers/TaxController.cs`
- `src/TaxComplianceService/Controllers/ComprobantesController.cs`
- `src/TaxComplianceService/Middleware/TenantMiddleware.cs`
- `src/TaxComplianceService/Services/TaxCalculatorService.cs`
- `src/TaxComplianceService/Services/ComprobanteService.cs`
- `src/TaxComplianceService/Services/TenantConfigClient.cs`
- `src/TaxComplianceService/Data/Migrations/20261001190000_AddComprobanteCorrelativeFunction.cs`
- `src/POSCartService/Services/TaxClient.cs`

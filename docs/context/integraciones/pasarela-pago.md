---
id: pasarela-pago
tipo: integracion
titulo: Integración con Pasarela de Pago
estado: parcial
fuentes: [GlobalMart_ContextMaster.md#sec12, src/POSCartService/Services/IPaymentGatewayClient.cs, src/POSCartService/Services/PaymentGatewayClient.cs, src/POSCartService/DTOs/PaymentGatewayDtos.cs, src/POSCartService/Program.cs, tests/GlobalMart.IntegrationTests/PaymentGatewayClientTests.cs, tareas/sprint2_youtrack_resultado.json, Docker/env.example, .env.example, src/POSCartService/appsettings.json]
verificado_contra_codigo: true
ultima_revision: 2026-10-16
depende_de: [ms5-pos]
publica: []
consume: []
reglas: [RN-02, RF-07, PC-17, PC-27, PC-28]
---
# Integración con Pasarela de Pago

> Toda transacción con tarjeta DEBE ir por una pasarela externa (RN-02). **Estado: PARCIAL** — MS-5 dispone del adaptador HTTP, pero todavía no orquesta el cobro con tarjeta ni existe el endpoint del simulador.

## Estado: [PARCIAL]

S2-036 / TI3-667 define el contrato reproducible del simulador. S2-037 / TI3-668 implementa `IPaymentGatewayClient` y su adaptador HTTP. El endpoint del simulador y el cobro con tarjeta todavía no están implementados; `POST /ventas/{id}/cobrar` solo admite efectivo y rechaza ventas `TARJETA` o `MIXTO`.

## S2-036 — Escenarios controlables del simulador [CONTRATO DEFINIDO]

El simulador debe exponer `POST /simulator/payments` únicamente en entornos de desarrollo y pruebas. El resultado se selecciona mediante `scenario_id`, nunca mediante un número de tarjeta. Los tres identificadores son públicos, no sensibles y estables.

| `scenario_id` | Conducta reproducible | Resultado para MS-5 | Estado de la venta |
|---|---|---|---|
| `sim-approved-v1` | Responde inmediatamente con aprobación y referencias simuladas deterministas. | HTTP 200, `status = APPROVED`. | Puede completarse solo después de persistir el pago autorizado. |
| `sim-declined-v1` | Responde inmediatamente con rechazo de negocio. | HTTP 200, `status = DECLINED`, `decline_code = DO_NOT_HONOR`. | Permanece `PENDIENTE`; no se crea pago confirmado ni `sale.completed`. |
| `sim-timeout-v1` | Mantiene la solicitud abierta durante 35 segundos. El cliente vigente corta a los 30 segundos. | `TaskCanceledException`/timeout; no existe payload de respuesta utilizable. | Permanece `PENDIENTE`; el resultado se trata como incierto y queda trazado. |

El sufijo `v1` versiona el comportamiento de prueba. Cambiar demora, códigos o semántica requiere otro identificador para no volver inestables las pruebas existentes. Un `scenario_id` desconocido devuelve HTTP 400 y no crea una transacción simulada.

### Solicitud común

```http
POST /simulator/payments
Content-Type: application/json
Idempotency-Key: 11111111-1111-4111-8111-111111111111
```

```json
{
  "request_id": "11111111-1111-4111-8111-111111111111",
  "tenant_id": "22222222-2222-4222-8222-222222222222",
  "sale_id": "33333333-3333-4333-8333-333333333333",
  "amount": 11900,
  "currency": "CLP",
  "scenario_id": "sim-approved-v1"
}
```

| Campo | Regla |
|---|---|
| `request_id` | UUID no vacío generado por MS-5 para un intento lógico. Coincide con `Idempotency-Key`. |
| `tenant_id` | UUID del tenant autenticado; se usa para aislamiento y auditoría. |
| `sale_id` | UUID de la venta `PENDIENTE` del mismo tenant. |
| `amount` | Decimal positivo en la unidad principal de `currency`; debe coincidir con el total autoritativo de la venta. |
| `currency` | Código ISO 4217 en mayúsculas. Para el escenario inicial: `CLP`. |
| `scenario_id` | Uno de los tres identificadores de la tabla. Solo controla el simulador. |

No se aceptan ni almacenan PAN, CVV, fecha de vencimiento, nombre del titular, track magnético, PIN o token de una tarjeta real. El simulador no imita la captura de tarjeta: representa exclusivamente la respuesta de una pasarela externa.

### Payload aprobado

Para `sim-approved-v1`:

```json
{
  "request_id": "11111111-1111-4111-8111-111111111111",
  "transaction_id": "SIM-APP-11111111111141118111111111111111",
  "status": "APPROVED",
  "authorization_code": "SIM-APPROVED",
  "gateway_reference": "SIM-RRN-11111111111141118111111111111111",
  "processed_at": "2026-10-16T12:00:00Z"
}
```

`transaction_id` y `gateway_reference` se derivan de `request_id`; no representan una transacción bancaria. `processed_at` corresponde al primer procesamiento y se conserva en repeticiones idempotentes.

### Payload rechazado

Para `sim-declined-v1`:

```json
{
  "request_id": "11111111-1111-4111-8111-111111111111",
  "transaction_id": "SIM-DEC-11111111111141118111111111111111",
  "status": "DECLINED",
  "decline_code": "DO_NOT_HONOR",
  "message": "Pago rechazado por escenario de prueba.",
  "processed_at": "2026-10-16T12:00:00Z"
}
```

El rechazo usa HTTP 200 porque el simulador procesó correctamente la solicitud y devolvió un resultado de negocio. No incluye `authorization_code` ni `gateway_reference` de aprobación.

### Escenario timeout

Para `sim-timeout-v1`, el cuerpo de solicitud es el mismo y solo cambia `scenario_id`. El simulador demora 35 segundos; con `PasarelaPago:TimeoutSeconds = 30`, MS-5 cancela la espera antes de recibir una respuesta. Por ello no existe un payload de respuesta válido para persistir como aprobado o rechazado. El simulador puede terminar internamente la solicitud, pero MS-5 debe registrar el resultado como incierto y no completar la venta.

### Idempotencia y reproducibilidad

- La clave lógica es `(tenant_id, request_id)`.
- Repetir exactamente la misma solicitud devuelve el mismo estado, identificadores y `processed_at`; no crea otra transacción simulada.
- Reutilizar `request_id` con distinto `sale_id`, monto, moneda o escenario devuelve HTTP 409.
- Cada prueba usa UUID propios y uno de los tres `scenario_id`; no requiere tarjeta, token bancario ni acceso a internet.
- El simulador debe rechazarse o no registrarse fuera de ambientes de desarrollo/pruebas. La selección de escenarios nunca debe quedar expuesta como opción de un cajero en producción.

### Trazas auditables

Cada solicitud debe producir una traza estructurada con `request_id`, `tenant_id`, `sale_id`, `scenario_id`, `status`, `elapsed_ms`, `transaction_id` cuando exista y fecha UTC. En timeout, `status = TIMED_OUT`; en conflicto idempotente, `status = IDEMPOTENCY_CONFLICT`.

No se registra el cuerpo completo, encabezados de autorización, JWT, datos de tarjeta ni secretos. MS-5 y el simulador deben usar el mismo `request_id` como correlación. Estas trazas se definen en S2-036; su persistencia o transporte se implementará en tareas posteriores.

## S2-037 — Adaptador HTTP de pasarela [IMPLEMENTADO]

`PaymentGatewayClient` implementa `IPaymentGatewayClient` mediante el `HttpClient` nombrado `PasarelaPago`. `Program.cs` obtiene `PasarelaPago:BaseUrl` y `PasarelaPago:TimeoutSeconds`, exige un timeout positivo y registra el adaptador con alcance scoped.

El adaptador envía `POST simulator/payments`, serializa exclusivamente `request_id`, `tenant_id`, `sale_id`, `amount`, `currency` y `scenario_id`, y propaga el mismo UUID en `Idempotency-Key`. El contrato C# no dispone de propiedades para PAN, CVV, vencimiento, titular, PIN o track de tarjeta. Tampoco propaga JWT ni encabezados del request del cajero.

Las trazas estructuradas contienen únicamente identificadores de correlación, escenario, estado, referencia simulada y tiempo transcurrido. No se registra el cuerpo de solicitud o respuesta. Un timeout del `HttpClient` se traduce a `TimeoutException`; la cancelación solicitada por el llamador conserva `OperationCanceledException`; fallos HTTP o payloads inconsistentes se exponen como `ExternalServiceException` sin incluir el cuerpo remoto.

[PaymentGatewayClientTests](../../../tests/GlobalMart.IntegrationTests/PaymentGatewayClientTests.cs) aprobó **4/4 casos** el 2026-10-16: URL configurada, propagación de idempotencia, conjunto exacto de campos seguros, respuesta rechazada, timeout explícito y rechazo local de `request_id` vacío sin invocar la red. [Evidencia TRX](../../qa/S2-037/S2-037.trx). El simulador y la integración con el endpoint de cobro permanecen [PLANIFICADO].

## S2-056 / TI3-687 — Configuración segura del simulador [IMPLEMENTADO]

Para garantizar entornos reproducibles y proteger credenciales de producción (RN-02 / PCI DSS), la configuración de la pasarela y simulador separa estrictamente endpoints, llaves y timeouts entre desarrollo y producción.

### Variables de entorno y configuración

| Parámetro | Variable de Entorno | `appsettings.json` (`PasarelaPago`) | Propósito / Valor Desarrollo | Valor Producción |
|---|---|---|---|---|
| URL Base | `PASARELA_BASE_URL` o `PasarelaPago__BaseUrl` | `BaseUrl` | `https://api.sandbox-pasarela.example.com` | Endpoint HTTPS oficial de la pasarela |
| Timeout | `PASARELA_TIMEOUT_SECONDS` o `PasarelaPago__TimeoutSeconds` | `TimeoutSeconds` | `30` (segundos; permite validar timeout frente a demoras de 35s en `sim-timeout-v1`) | `30` a `60` segundos según SLA |
| Clave de API | `PASARELA_API_KEY` o `PasarelaPago__ApiKey` | `ApiKey` | `sim_key_dummy_test_only` (clave pública no sensible) | Secreto inyectado desde Key Vault / Secrets Manager |
| Ambiente | `PASARELA_ENVIRONMENT` o `PasarelaPago__Environment` | `Environment` | `sandbox` | `production` |
| Escenario por defecto | `PASARELA_DEFAULT_SCENARIO` o `PasarelaPago__DefaultScenario` | `DefaultScenario` | `sim-approved-v1` | N/A (en producción no se envían escenarios) |

### Reglas de seguridad aplicadas
1. **Sin secretos reales en repositorios**: `Docker/env.example` y la plantilla raíz `.env.example` definen únicamente valores ficticios y de sandbox.
2. **Sin datos de tarjeta (RN-02)**: La pasarela/simulador solo recibe `{ request_id, tenant_id, sale_id, amount, currency, scenario_id }`. No existen variables ni campos para PAN, CVV, fecha de expiración ni PIN.
3. **Idempotencia obligatoria**: Se propaga `request_id` como `Idempotency-Key` en los encabezados HTTP.
4. **Instalación limpia**: Cualquier máquina o contenedor conoce la totalidad de variables requeridas a través de `Docker/env.example` y `.env.example`.

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

## Flujo futuro diseñado

```
1. Cajero selecciona "Cobrar con tarjeta"
2. Terminal POS muestra monto al cliente
3. Cliente inserta/acerca la tarjeta
4. Terminal captura datos y los envía a la pasarela (PCI DSS scope)
5. MS-5 envía a la pasarela: { request_id, tenant_id, sale_id, amount, currency }
6. La pasarela responde con APPROVED o DECLINED, o la llamada vence por timeout
7. Si autorizado:
   → VentaService.CompletarAsync
   → Kafka sale.completed mediante outbox (IMPLEMENTADO)
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
| PC-17 | Cobrar con Tarjeta (débito/crédito) | [PARCIAL — adaptador disponible, sin orquestación] |
| PC-18 | Cobrar Mixto (efectivo + tarjeta) | [PLANIFICADO] |
| PC-27 | Procesar Pago en Pasarela | [PARCIAL — cliente HTTP implementado, aún no invocado] |
| PC-28 | Manejar Respuesta de Pasarela | [PARCIAL — DTO y errores implementados, sin flujo de cobro] |

## Conexiones
- POS: [[ms5-pos]]
- Reglas: [[reglas-negocio]] (RN-02)
- Hardware: [[hardware]]

## Fuentes
- `GlobalMart_ContextMaster.md` §12
- `tareas/sprint2_youtrack_resultado.json` (`S2-036` → `TI3-667`)
- `tareas/sprint2_youtrack_resultado.json` (`S2-037` → `TI3-668`)
- `src/POSCartService/Program.cs` (HttpClient `PasarelaPago` y timeout vigente)
- `src/POSCartService/Services/IPaymentGatewayClient.cs`
- `src/POSCartService/Services/PaymentGatewayClient.cs`
- `src/POSCartService/DTOs/PaymentGatewayDtos.cs`
- `tests/GlobalMart.IntegrationTests/PaymentGatewayClientTests.cs`
- `src/POSCartService/Controllers/VentasController.cs` (cobro vigente solo en efectivo)
- `src/POSCartService/Services/VentaService.cs` (AnularAsync sin reembolso)
- `Docker/env.example`
- `.env.example`
- `src/POSCartService/appsettings.json`

---
id: pasarela-pago
tipo: integracion
titulo: Integración con Pasarela de Pago
estado: parcial
fuentes: [GlobalMart_ContextMaster.md#sec12, src/POSCartService/Services/IPaymentGatewayClient.cs, src/POSCartService/Services/PaymentGatewayClient.cs, src/POSCartService/Services/VentaService.cs, src/POSCartService/Repositories/VentaRepository.cs, src/POSCartService/Controllers/VentasController.cs, src/POSCartService/Models/Pago.cs, src/POSCartService/Exceptions/PaymentAuthorizationUncertainException.cs, src/POSCartService/DTOs/PaymentGatewayDtos.cs, src/POSCartService/DTOs/CobrarTarjetaDTOs.cs, src/POSCartService/Program.cs, Docker/simulator/server.js, tests/GlobalMart.IntegrationTests/PaymentGatewayClientTests.cs, tests/GlobalMart.IntegrationTests/PaymentSimulatorIntegrationTests.cs, tests/GlobalMart.IntegrationTests/PaymentRetrySafetyTests.cs, tests/GlobalMart.IntegrationTests/CobrarTarjetaIntegrationTests.cs, tests/GlobalMart.IntegrationTests/PaymentEventSequenceContractTests.cs, tareas/sprint2_youtrack_resultado.json, Docker/env.example, .env.example, src/POSCartService/appsettings.json]
verificado_contra_codigo: true
ultima_revision: 2026-10-18
depende_de: [ms5-pos]
publica: []
consume: []
reglas: [RN-02, RF-07, PC-17, PC-27, PC-28]
---
# Integración con Pasarela de Pago

> Toda transacción con tarjeta DEBE ir por una pasarela externa (RN-02). **Estado: PARCIAL** — MS-5 dispone del endpoint separado `POST /ventas/{id}/cobrar-tarjeta`, adaptador HTTP, simulador y persistencia atómica; la validación de integración S2-040 requiere PostgreSQL disponible.

## Estado: [PARCIAL]

### S2-051 — Matriz RN-02 de pagos [CONTRATO DEFINIDO]

La [matriz S2-051](../../../diagramas/matriz/S2-051-matriz-rn02-pagos.md) define 18 casos de aceptación por método, resultado, idempotencia y datos permitidos en logs. Prohíbe explícitamente captura local, almacenamiento y transmisión de PAN/CVV, incluso de prueba. Incluye aprobación, rechazo, incertidumbre, concurrencia, recuperación, aislamiento y controles de efectivo/MIXTO. El backend de tarjeta ya está conectado; la ejecución integral de la matriz y la sanitización completa de errores/logs quedan [NO VERIFICADAS].

S2-031 / TI3-662 define el contrato de pasarela simulada (`IPaymentGatewayClient` y DTOs seguros). S2-033 / TI3-664 y S2-034 / TI3-665 conectan el cobro con tarjeta y persisten sus resultados. S2-036 / TI3-667 define los escenarios reproducibles. S2-037 / TI3-668 implementa el adaptador HTTP, S2-038 / TI3-669 el simulador y S2-039 / TI3-670 la consulta segura tras timeout. S2-040 / TI3-671 añade la referencia opaca al `Pago` y verifica la secuencia pago-evento.

## S2-031 / TI3-662 — Contrato de pasarela simulada [IMPLEMENTADO]

### S2-049 — Recuperación de intento incierto

MS-5 registra el intento `PENDIENTE` antes de llamar a la pasarela. El índice único `(TenantId, IdempotencyKey)` y una transacción serializable impiden reutilizar la clave o abrir dos intentos sin resultado definitivo para la misma venta. Tras timeout, el intento continúa pendiente. `GET /ventas/{id}/intento-tarjeta` consulta `GetPaymentStatusAsync` con el `request_id` original: `APPROVED` completa venta, pago y outbox; `DECLINED` permite reintento manual; ausencia de estado o fallo de consulta conserva `UNCERTAIN`. Nunca se reenvía automáticamente el POST. La integración HTTP con PostgreSQL queda [NO VERIFICADA] por falta de base local el 2026-10-18.

Define la interfaz de comunicación y los objetos de transferencia de datos en MS-5 para interactuar con pasarelas de pago externas o su simulador de pruebas, asegurando estricto cumplimiento de **RN-02** y normativas PCI DSS:

- **`IPaymentGatewayClient`** (`src/POSCartService/Services/IPaymentGatewayClient.cs`):
  Expone el método asíncrono `ProcessPaymentAsync(PaymentGatewayRequest request, CancellationToken cancellationToken = default)`.
- **`PaymentGatewayRequest`** (`src/POSCartService/DTOs/PaymentGatewayDtos.cs`):
  Transporta exclusivamente `request_id` (UUID idempotente), `tenant_id` (aislamiento multi-tenant RN-01), `sale_id`, `amount` (monto decimal), `currency` (código ISO 4217) y `scenario_id` (escenario de prueba).
- **`PaymentGatewayResponse`** (`src/POSCartService/DTOs/PaymentGatewayDtos.cs`):
  Transporta el resultado del procesamiento conteniendo únicamente referencias opacas: `request_id`, `transaction_id`, `status` (`APPROVED`, `DECLINED`), `authorization_code`, `gateway_reference` (RRN), `decline_code`, `message` y `processed_at` (UTC).
- **Aceptación y Seguridad**:
  El contrato **no transporta, almacena ni serializa PAN, CVV, fecha de caducidad, PIN ni datos de tarjeta**. Se valida mediante pruebas de reflexión y serialización en `PaymentGatewayClientTests` (trait `S2-031`).

## S2-032 / TI3-663 — Persistencia de intentos de pago e idempotencia [IMPLEMENTADO]

Para cumplir el criterio P03 de la matriz de aceptación (evitar doble cargo por doble clic, reintentos de red o recarga de página), MS-5 persiste cada intento de cobro con tarjeta en la tabla `IntentosPago` (`IntentoPago.cs`):

- **Campos almacenados**: `Id`, `TenantId`, `VentaId`, `IdempotencyKey` (string max 100), `Estado` (`EstadoIntentoPago`: `PENDIENTE`, `APROBADO`, `RECHAZADO`, `FALLIDO`), `Monto` (18,2), `Moneda` (3), `Referencia` opaca (100), `AuthorizationCode` (100), `DeclineCode` (100), `Mensaje` (500), `CreatedAt` y `UpdatedAt`.
- **Garantía de idempotencia (Criterio de Aceptación)**: Se impone mediante el índice único compuesto `IX_IntentosPago_TenantId_IdempotencyKey`. Si un cliente reenvía la misma clave para el mismo tenant, la base de datos rechaza la inserción duplicada evitando la creación de un segundo cobro o cargo.
- **Aislamiento Multi-Tenant (RN-01)**: Filtro global `CurrentTenantId` en `PosCartDbContext`. Claves de idempotencia iguales pertenecientes a distintos tenants no colisionan.
- **Migración**: `20261017163020_AddIntentoPago.cs`. Pruebas en `IntentoPagoPersistenceTests` (trait `S2-032`).

## S2-033 / TI3-664 — Endpoint cobrar-tarjeta [IMPLEMENTADO]

Expone `POST /ventas/{id}/cobrar-tarjeta` en `VentasController` (MS-5). El endpoint implementa validación estricta de precondiciones antes de invocar `IPaymentGatewayClient`:

1. **Autenticación (RN-07):** Exige JWT válido con claims `tenant_id` y `cajero_id` / `sub` (HTTP 401 si falta o es inválido).
2. **Turno Activo (RN-06):** Verifica que el cajero en sesión disponga de un turno `ABIERTO` en el tenant autenticado (HTTP 409 si no tiene turno).
3. **Existencia y Aislamiento (RN-01):** Busca la venta filtrada por `tenant_id`. Si no existe o pertenece a otro tenant, responde HTTP 404.
4. **Estado PENDIENTE:** La venta debe estar en estado `PENDIENTE` (HTTP 409 si ya está `COMPLETADA`, `ANULADA` o `CANCELADA`).
5. **Método de Pago:** Rechaza ventas creadas con método `EFECTIVO` (HTTP 400).
6. **Monto Válido:** Valida que `Total > 0` y que coincida con `monto` del request si fue provisto (HTTP 400).
7. **Regla de Aceptación:** Una venta ajena o completada (o sin turno / JWT) **nunca contacta a la pasarela**. Pruebas de integración en `CobrarTarjetaIntegrationTests` (6/6 aprobadas).

## S2-034 / TI3-665 — Persistir aprobación y rechazo [IMPLEMENTADO]

En `POST /ventas/{id}/cobrar-tarjeta`, la respuesta de la pasarela persiste de manera atómica el resultado mediante `IntentoPago` y gestiona el ciclo de vida de la venta:
- **Aprobado (`status == APPROVED`):**
  - Venta transiciona a `COMPLETADA`.
  - Se genera registro `Pago` con `Metodo = TARJETA`, `Monto = Total`, `Vuelto = 0`.
  - Se registra `IntentoPago` con `Estado = APROBADO`, `AuthorizationCode`, `Referencia` y `Mensaje`.
  - Se publica el evento `sale.completed` mediante el `OutboxMessage` en la misma transacción atómica de EF Core (`CompletarTarjetaConOutboxAsync`).
  - Retorna **HTTP 200 OK**.
- **Rechazado (`status == DECLINED`):**
  - La venta permanece en estado `PENDIENTE`.
  - Se registra `IntentoPago` con `Estado = RECHAZADO`, `DeclineCode` y `Mensaje`.
  - **No** se genera registro `Pago` ni evento outbox `sale.completed`.
  - Retorna **HTTP 422 Unprocessable Entity**.
- **Timeout o error de red:**
  - La venta permanece en `PENDIENTE`.
  - El `IntentoPago` permanece `PENDIENTE` hasta consultar el estado; un resultado desconocido no habilita otra autorización.
  - Retorna **HTTP 504 Gateway Timeout** (en timeout) o **HTTP 502 Bad Gateway** (en fallo de transporte/500).
- **Pruebas:** las 10 pruebas previas de `CobrarTarjetaIntegrationTests` se actualizaron para el nuevo flujo; su ejecución actual requiere PostgreSQL y está [NO VERIFICADA] en este entorno.

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
  "transaction_id": "SIM-TXN-3306D887135306CE2B1A4AC9",
  "status": "APPROVED",
  "authorization_code": "SIM-APPROVED",
  "gateway_reference": "SIM-REF-3306D887135306CE2B1A4AC9",
  "processed_at": "2026-10-16T12:00:00Z"
}
```

`transaction_id` y `gateway_reference` se derivan de un resumen SHA-256 de `tenant_id` y `request_id`; son opacos y no representan una transacción bancaria. `processed_at` corresponde al primer procesamiento y se conserva en repeticiones idempotentes durante la vida del proceso.

### Payload rechazado

Para `sim-declined-v1`:

```json
{
  "request_id": "11111111-1111-4111-8111-111111111111",
  "transaction_id": "SIM-TXN-3306D887135306CE2B1A4AC9",
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

No se registra el cuerpo completo, encabezados de autorización, JWT, datos de tarjeta ni secretos. MS-5 y el simulador usan el mismo `request_id` como correlación. El simulador emite estas trazas como JSON estructurado por salida estándar; su persistencia o transporte se implementará en tareas posteriores.

## S2-037 — Adaptador HTTP de pasarela [IMPLEMENTADO]

`PaymentGatewayClient` implementa `IPaymentGatewayClient` mediante el `HttpClient` nombrado `PasarelaPago`. `Program.cs` obtiene `PasarelaPago:BaseUrl` y `PasarelaPago:TimeoutSeconds`, exige un timeout positivo y registra el adaptador con alcance scoped.

El adaptador envía `POST simulator/payments`, serializa exclusivamente `request_id`, `tenant_id`, `sale_id`, `amount`, `currency` y `scenario_id`, y propaga el mismo UUID en `Idempotency-Key`. El contrato C# no dispone de propiedades para PAN, CVV, vencimiento, titular, PIN o track de tarjeta. Tampoco propaga JWT ni encabezados del request del cajero.

Las trazas estructuradas contienen únicamente identificadores de correlación, escenario, estado, referencia simulada y tiempo transcurrido. No se registra el cuerpo de solicitud o respuesta. La cancelación solicitada por el llamador conserva `OperationCanceledException`; fallos HTTP o payloads inconsistentes se exponen como `ExternalServiceException` sin incluir el cuerpo remoto. Desde S2-039, un timeout inicia la consulta de estado y, si no puede resolverse, produce `PaymentAuthorizationUncertainException`, derivada de `TimeoutException`.

[PaymentGatewayClientTests](../../../tests/GlobalMart.IntegrationTests/PaymentGatewayClientTests.cs) aprobó **4/4 casos** el 2026-10-16: URL configurada, propagación de idempotencia, conjunto exacto de campos seguros, respuesta rechazada, timeout explícito y rechazo local de `request_id` vacío sin invocar la red. [Evidencia TRX](../../qa/S2-037/S2-037.trx). La integración con el endpoint de cobro está [IMPLEMENTADA]; su validación HTTP actual con PostgreSQL está [NO VERIFICADA].

## S2-038 / TI3-669 — Servicio simulador de autorizaciones [IMPLEMENTADO]

El proceso Node existente en `Docker/simulator/server.js` expone `POST /simulator/payments` sin conexión bancaria. Valida UUID, monto y moneda; exige que `Idempotency-Key` sea un UUID igual a `request_id`; y usa `(tenant_id, request_id)` para conservar la primera respuesta. Repetir la misma solicitud devuelve exactamente los mismos identificadores y `processed_at`; reutilizar la clave con otros datos devuelve HTTP 409.

`sim-approved-v1` y `sim-declined-v1` responden HTTP 200 con resultados de negocio deterministas. Las referencias `SIM-TXN-*` y `SIM-REF-*` usan un resumen SHA-256 estable y no exponen el UUID original. `sim-timeout-v1` demora 35 segundos y devuelve HTTP 504 si el cliente no cancela antes. Los payloads con campos de tarjeta se rechazan con HTTP 400 antes de autorizar.

Las trazas son JSON estructurado y contienen solo fecha UTC, identificadores de correlación, escenario, estado, duración y referencia simulada. [PaymentSimulatorIntegrationTests](../../../tests/GlobalMart.IntegrationTests/PaymentSimulatorIntegrationTests.cs), ejecutada con el proceso Node real y `PaymentGatewayClient`, aprobó **7/7 casos** el 2026-10-17. [Evidencia TRX](../../qa/S2-038/S2-038.trx).

## S2-039 / TI3-670 — Reintento seguro de cobro [IMPLEMENTADO]

`PaymentGatewayClient` distingue el rechazo de negocio del resultado incierto. Una respuesta `DECLINED` se devuelve como resultado definitivo y no dispara una consulta ni un nuevo intento. Si el `POST /simulator/payments` vence por timeout, el adaptador ejecuta una sola consulta `GET /simulator/payments/{request_id}?tenant_id={tenant_id}`, enviando el mismo `request_id` como `Idempotency-Key`; nunca repite automáticamente el `POST` de autorización.

La consulta devuelve la respuesta ya almacenada cuando la pasarela alcanzó a procesar la autorización. Si responde 404 o la consulta falla, MS-5 lanza `PaymentAuthorizationUncertainException` con `tenant_id` y `request_id`. Ese estado exige reconciliar la misma clave y evita iniciar otro cargo con un identificador nuevo. El almacenamiento del simulador es en memoria: tras reiniciar el proceso, un estado no encontrado continúa siendo incierto y no habilita un reintento automático.

[PaymentRetrySafetyTests](../../../tests/GlobalMart.IntegrationTests/PaymentRetrySafetyTests.cs) y la prueba contra el proceso Node real validaron **4/4 casos** el 2026-10-17: timeout desconocido, timeout con aprobación recuperable, rechazo sin consulta y recuperación del payload almacenado. En ambos casos de timeout hubo exactamente un `POST` y una consulta `GET`. [Evidencia TRX](../../qa/S2-039/S2-039.trx).

## S2-040 / TI3-671 — Secuencia de pago y evento [PARCIAL]

La migración `20261018040621_AddPagoGatewayReference` agrega `Pago.Referencia` nullable de 100 caracteres. En una aprobación, `CompletarTarjetaAsync` copia allí la misma referencia opaca que guarda `IntentoPago.Referencia` y que `CobrarTarjetaResponse.gateway_reference` devuelve al cliente; venta, `Pago TARJETA`, intento aprobado y `sale.completed` pendiente se conservan en la transacción existente.

La suite con trait `S2-040` cubre contrato seguro, propagación servicio-pago-outbox, aprobación, rechazo y timeout. Las pruebas sin infraestructura aprobaron **2/2** el 2026-10-18 y EF confirmó que no existen cambios de modelo pendientes después de la migración. Las tres pruebas PostgreSQL están implementadas y compiladas, pero su ejecución quedó [NO VERIFICADA] porque `localhost:5436` no estaba disponible y el usuario del entorno no pudo acceder al socket Docker. [Reporte y reproducción](../../qa/S2-040/reporte.md).

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

## S2-057 / TI3-688 — Incorporación del simulador al entorno local [IMPLEMENTADO]

Se implementó el servicio de pasarela de pruebas en `Docker/simulator/server.js` y su contenedor en `Docker/docker-compose.yml` (`payment-simulator:5050`).
- Expone `GET /health` respondiendo HTTP 200 `{"status":"Healthy"}` para Docker healthchecks y monitoreo.
- Implementa los escenarios controlables `sim-approved-v1`, `sim-declined-v1` y `sim-timeout-v1` según el contrato S2-036.
- Almacena idempotencia en memoria por `(tenant_id, request_id)` y detecta conflictos (HTTP 409).
- Rechaza de forma estricta e inmediata cualquier campo sensible de tarjeta (`pan`, `card`, `cvv`, etc.) con HTTP 400 (`CARD_DATA_FORBIDDEN`), garantizando cumplimiento total de RN-02 y el alcance PCI DSS.
- Pruebas de integración en C# [PaymentSimulatorIntegrationTests](../../../tests/GlobalMart.IntegrationTests/PaymentSimulatorIntegrationTests.cs): **7/7 aprobadas** el 2026-10-17, incluidos los controles S2-038 de referencia opaca y coincidencia de `Idempotency-Key`.

## S2-058 / TI3-689 — Exposición de ruta de pagos en gateway [IMPLEMENTADO]

Se configuró el enrutamiento en Kong API Gateway (`Docker/config/kong.yaml`) para exponer el simulador al entorno local y microservicios:
- Servicio: `payment-simulator-service` (`http://payment-simulator:5050`).
- Rutas:
  - `simulator-payments-route`: `POST` de autorización y `GET` de estado en `/simulator/payments` y `/api/simulator/payments` (`strip_path: false`).
  - `simulator-health-route`: `GET /simulator/health` y `GET /api/simulator/health` (`strip_path: false`).
- Permite que MS-5 (`PaymentGatewayClient`) alcance el simulador directamente en `http://localhost:5050` o a través del Gateway en `http://localhost:8000/simulator/payments`.
- Se validó el contrato de red impidiendo que el frontend Electron transmita datos de tarjeta bancarios hacia la pasarela o el backend.

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
| PC-17 | Cobrar con Tarjeta (débito/crédito) | [PARCIAL — backend orquestado; integración S2-040 pendiente de ejecutar con PostgreSQL] |
| PC-18 | Cobrar Mixto (efectivo + tarjeta) | [PLANIFICADO] |
| PC-27 | Procesar Pago en Pasarela | [IMPLEMENTADO en endpoint de tarjeta] |
| PC-28 | Manejar Respuesta de Pasarela | [PARCIAL — flujo implementado; integración S2-040 pendiente de PostgreSQL] |

## Conexiones
- POS: [[ms5-pos]]
- Reglas: [[reglas-negocio]] (RN-02)
- Hardware: [[hardware]]

## Fuentes
- `GlobalMart_ContextMaster.md` §12
- `tareas/sprint2_youtrack_resultado.json` (`S2-036` → `TI3-667`)
- `tareas/sprint2_youtrack_resultado.json` (`S2-037` → `TI3-668`)
- `tareas/sprint2_youtrack_resultado.json` (`S2-038` → `TI3-669`)
- `tareas/sprint2_youtrack_resultado.json` (`S2-039` → `TI3-670`)
- `tareas/sprint2_youtrack_resultado.json` (`S2-040` → `TI3-671`)
- `src/POSCartService/Program.cs` (HttpClient `PasarelaPago` y timeout vigente)
- `src/POSCartService/Services/IPaymentGatewayClient.cs`
- `src/POSCartService/Services/PaymentGatewayClient.cs`
- `src/POSCartService/Exceptions/PaymentAuthorizationUncertainException.cs`
- `src/POSCartService/DTOs/PaymentGatewayDtos.cs`
- `tests/GlobalMart.IntegrationTests/PaymentGatewayClientTests.cs`
- `Docker/simulator/server.js`
- `tests/GlobalMart.IntegrationTests/PaymentSimulatorIntegrationTests.cs`
- `docs/qa/S2-038/S2-038.trx`
- `tests/GlobalMart.IntegrationTests/PaymentRetrySafetyTests.cs`
- `docs/qa/S2-039/S2-039.trx`
- `src/POSCartService/Controllers/VentasController.cs`
- `src/POSCartService/Models/Pago.cs`
- `tests/GlobalMart.IntegrationTests/PaymentEventSequenceContractTests.cs`
- `tests/GlobalMart.IntegrationTests/CobrarTarjetaIntegrationTests.cs`
- `docs/qa/S2-040/reporte.md`
- `src/POSCartService/Services/VentaService.cs` (AnularAsync sin reembolso)
- `Docker/env.example`
- `.env.example`
- `src/POSCartService/appsettings.json`

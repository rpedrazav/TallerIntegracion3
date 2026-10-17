/**
 * ==============================================================================
 * GlobalMart OS — Simulador de Pasarela de Pagos (Mock Gateway)
 * Tarea: TI3-688 (Plan ID: S2-057) y TI3-689 (Plan ID: S2-058)
 *
 * Simula el comportamiento determinista y auditable de una pasarela externa
 * (Transbank / Stripe) sin requerir cuentas bancarias ni hardware POS físico.
 *
 * Reglas Críticas:
 *   - RN-02 / PCI DSS: Prohíbe estrictamente datos de tarjeta (PAN, CVV, PIN, etc.).
 *   - S2-036: Escenarios 'sim-approved-v1', 'sim-declined-v1', 'sim-timeout-v1'.
 *   - S2-037: Idempotencia por (tenant_id, request_id) e Idempotency-Key.
 * ==============================================================================
 */

const http = require('http');
const crypto = require('crypto');

const PORT = parseInt(process.env.PORT || '5050', 10);
const HOST = process.env.HOST || '0.0.0.0';

// Lista de palabras clave sensibles asociadas a tarjetas de crédito/débito
const SENSITIVE_CARD_PATTERNS = [
  'pan', 'card', 'card_number', 'cardnumber', 'card_num',
  'cvv', 'cvc', 'pin', 'track', 'exp', 'expiration', 'expiry',
  'holder', 'cardholder', 'tarjeta', 'numero_tarjeta'
];

// Almacén en memoria de idempotencia: key = `${tenant_id}:${request_id}`
const idempotencyStore = new Map();
const UUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
const SCENARIO_PATTERN = /^[a-z0-9-]{1,64}$/;

function safeUuid(value) {
  return typeof value === 'string' && UUID_PATTERN.test(value) ? value : null;
}

function safeScenario(value) {
  return typeof value === 'string' && SCENARIO_PATTERN.test(value) ? value : null;
}

function elapsedMilliseconds(startedAt) {
  return Number(process.hrtime.bigint() - startedAt) / 1e6;
}

function logAudit(body, status, startedAt, transactionId = null) {
  console.log(JSON.stringify({
    event: 'payment_simulator.authorization',
    timestamp: new Date().toISOString(),
    request_id: safeUuid(body.request_id),
    tenant_id: safeUuid(body.tenant_id),
    sale_id: safeUuid(body.sale_id),
    scenario_id: safeScenario(body.scenario_id),
    status,
    elapsed_ms: elapsedMilliseconds(startedAt),
    transaction_id: transactionId
  }));
}

function createOpaqueId(tenantId, requestId) {
  return crypto
    .createHash('sha256')
    .update(`${tenantId.toLowerCase()}:${requestId.toLowerCase()}`, 'utf8')
    .digest('hex')
    .slice(0, 24)
    .toUpperCase();
}

/**
 * Verifica si un objeto contiene algún campo prohibido con datos de tarjeta.
 */
function containsCardData(obj) {
  if (!obj || typeof obj !== 'object') return false;
  for (const key of Object.keys(obj)) {
    const lowerKey = key.toLowerCase().replace(/[^a-z0-9]/g, '');
    for (const pattern of SENSITIVE_CARD_PATTERNS) {
      if (lowerKey.includes(pattern)) {
        return true;
      }
    }
    if (typeof obj[key] === 'object' && containsCardData(obj[key])) {
      return true;
    }
  }
  return false;
}

/**
 * Envía una respuesta JSON con encabezados CORS.
 */
function sendJson(res, statusCode, data) {
  const payload = JSON.stringify(data);
  res.writeHead(statusCode, {
    'Content-Type': 'application/json',
    'Content-Length': Buffer.byteLength(payload),
    'Access-Control-Allow-Origin': '*',
    'Access-Control-Allow-Methods': 'GET, POST, OPTIONS',
    'Access-Control-Allow-Headers': 'Content-Type, Authorization, Idempotency-Key, X-Requested-With'
  });
  res.end(payload);
}

/**
 * Manejador principal de peticiones HTTP.
 */
const server = http.createServer((req, res) => {
  const urlPath = (req.url || '').split('?')[0];

  // Manejo de preflight CORS (OPTIONS)
  if (req.method === 'OPTIONS') {
    res.writeHead(204, {
      'Access-Control-Allow-Origin': '*',
      'Access-Control-Allow-Methods': 'GET, POST, OPTIONS',
      'Access-Control-Allow-Headers': 'Content-Type, Authorization, Idempotency-Key, X-Requested-With'
    });
    return res.end();
  }

  // 1. Health checks (para Docker Compose healthcheck, Kong y smoke test)
  if (req.method === 'GET' && (urlPath === '/health' || urlPath === '/simulator/health' || urlPath === '/api/simulator/health' || urlPath === '/')) {
    return sendJson(res, 200, {
      status: 'Healthy',
      service: 'payment-simulator',
      timestamp: new Date().toISOString()
    });
  }

  // 2. Procesamiento de Pagos Simulados
  if (req.method === 'POST' && (urlPath === '/simulator/payments' || urlPath === '/api/simulator/payments')) {
    const startedAt = process.hrtime.bigint();
    let rawBody = '';
    req.on('data', chunk => {
      rawBody += chunk;
      // Prevenir payloads excesivos (> 1MB)
      if (rawBody.length > 1e6) {
        req.destroy();
      }
    });

    req.on('end', () => {
      let body;
      try {
        body = JSON.parse(rawBody || '{}');
      } catch (err) {
        return sendJson(res, 400, {
          error: 'INVALID_JSON',
          message: 'El cuerpo de la solicitud no es un JSON válido.'
        });
      }

      // Regla Crítica (RN-02 / PCI DSS): Rechazar cualquier intento de transmitir datos de tarjeta
      if (containsCardData(body)) {
        logAudit(body, 'CARD_DATA_FORBIDDEN', startedAt);
        return sendJson(res, 400, {
          error: 'CARD_DATA_FORBIDDEN',
          message: 'No se permiten datos de tarjeta. Toda transacción con tarjeta debe procesarse externamente sin transmitir PAN ni CVV (cumplimiento RN-02 / PCI DSS).'
        });
      }

      const { request_id, tenant_id, sale_id, amount, currency, scenario_id } = body;

      // Validación de campos obligatorios
      if (!UUID_PATTERN.test(request_id)
        || !UUID_PATTERN.test(tenant_id)
        || !UUID_PATTERN.test(sale_id)
        || typeof amount !== 'number'
        || amount <= 0
        || typeof currency !== 'string'
        || !/^[A-Z]{3}$/.test(currency)
        || typeof scenario_id !== 'string'
        || !scenario_id) {
        logAudit(body, 'VALIDATION_ERROR', startedAt);
        return sendJson(res, 400, {
          error: 'VALIDATION_ERROR',
          message: 'request_id, tenant_id y sale_id deben ser UUID válidos; amount debe ser positivo; currency debe ser ISO 4217 y scenario_id es obligatorio.'
        });
      }

      const idempotencyHeader = req.headers['idempotency-key'];
      if (typeof idempotencyHeader !== 'string' || !UUID_PATTERN.test(idempotencyHeader)) {
        logAudit(body, 'INVALID_IDEMPOTENCY_KEY', startedAt);
        return sendJson(res, 400, {
          error: 'INVALID_IDEMPOTENCY_KEY',
          message: 'Idempotency-Key debe contener un UUID válido.'
        });
      }

      if (idempotencyHeader.toLowerCase() !== request_id.toLowerCase()) {
        logAudit(body, 'IDEMPOTENCY_CONFLICT', startedAt);
        return sendJson(res, 409, {
          error: 'IDEMPOTENCY_CONFLICT',
          message: 'Idempotency-Key debe coincidir con request_id.'
        });
      }

      // Validación de Idempotencia por (tenant_id, request_id)
      const idempotencyKey = `${tenant_id.toLowerCase()}:${request_id.toLowerCase()}`;
      const existing = idempotencyStore.get(idempotencyKey);

      if (existing) {
        // Si los parámetros coinciden exactamente, retornar el resultado previo almacenado
        if (existing.sale_id === sale_id && existing.amount === amount && existing.currency === currency && existing.scenario_id === scenario_id) {
          logAudit(body, existing.response.status, startedAt, existing.response.transaction_id);
          return sendJson(res, 200, existing.response);
        } else {
          // Reutilización de request_id con diferentes parámetros -> HTTP 409 Conflict
          logAudit(body, 'IDEMPOTENCY_CONFLICT', startedAt);
          return sendJson(res, 409, {
            error: 'IDEMPOTENCY_CONFLICT',
            message: 'El request_id ya fue utilizado previamente con parámetros diferentes.'
          });
        }
      }

      const opaqueId = createOpaqueId(tenant_id, request_id);

      // Ejecución de Escenarios Controlables (S2-036)
      switch (scenario_id) {
        case 'sim-approved-v1': {
          const responsePayload = {
            request_id,
            transaction_id: `SIM-TXN-${opaqueId}`,
            status: 'APPROVED',
            authorization_code: 'SIM-APPROVED',
            gateway_reference: `SIM-REF-${opaqueId}`,
            processed_at: new Date().toISOString()
          };

          idempotencyStore.set(idempotencyKey, {
            sale_id,
            amount,
            currency,
            scenario_id,
            response: responsePayload
          });

          logAudit(body, responsePayload.status, startedAt, responsePayload.transaction_id);
          return sendJson(res, 200, responsePayload);
        }

        case 'sim-declined-v1': {
          const responsePayload = {
            request_id,
            transaction_id: `SIM-TXN-${opaqueId}`,
            status: 'DECLINED',
            decline_code: 'DO_NOT_HONOR',
            message: 'Pago rechazado por escenario de prueba.',
            processed_at: new Date().toISOString()
          };

          idempotencyStore.set(idempotencyKey, {
            sale_id,
            amount,
            currency,
            scenario_id,
            response: responsePayload
          });

          logAudit(body, responsePayload.status, startedAt, responsePayload.transaction_id);
          return sendJson(res, 200, responsePayload);
        }

        case 'sim-timeout-v1': {
          // Demora 35 segundos para inducir el timeout configurado en MS-5 (30s)
          setTimeout(() => {
            if (!res.writableEnded) {
              logAudit(body, 'TIMED_OUT', startedAt);
              sendJson(res, 504, {
                error: 'GATEWAY_TIMEOUT',
                message: 'El simulador excedió el tiempo de respuesta esperado.'
              });
            }
          }, 35000);
          return;
        }

        default: {
          logAudit(body, 'UNKNOWN_SCENARIO', startedAt);
          return sendJson(res, 400, {
            error: 'UNKNOWN_SCENARIO',
            message: `Escenario '${scenario_id}' desconocido. Escenarios soportados: sim-approved-v1, sim-declined-v1, sim-timeout-v1.`
          });
        }
      }
    });

    return;
  }

  // Ruta no encontrada
  return sendJson(res, 404, {
    error: 'NOT_FOUND',
    message: `Ruta ${req.method} ${urlPath} no encontrada en el simulador.`
  });
});

server.listen(PORT, HOST, () => {
  console.log(`[SIMULATOR] Payment Gateway Simulator activo en http://${HOST}:${PORT}`);
  console.log(`[SIMULATOR] Endpoints: GET /health | POST /simulator/payments`);
});

// Manejo de cierre limpio
function shutdown() {
  console.log('[SIMULATOR] Apagando servidor...');
  server.close(() => {
    process.exit(0);
  });
}
process.on('SIGTERM', shutdown);
process.on('SIGINT', shutdown);

// Estado en memoria por proceso, igual que las autorizaciones del simulador.
const crypto = require('crypto');
const reversals = new Map();
const byPayment = new Map();
const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const scenarios = new Set(['sim-reversal-approved-v1', 'sim-reversal-declined-v1', 'sim-reversal-timeout-v1']);

module.exports = function handleReversal(req, res, url, payments, sendJson, containsCardData) {
  const route = url.pathname.match(/^\/(?:api\/)?simulator\/reversals(?:\/([^/]+))?$/);
  if (!route) return false;
  const fail = (code, error) => sendJson(res, code, { error });
  const identityValid = (tenant, id) => typeof tenant === 'string' && uuid.test(tenant)
    && typeof id === 'string' && uuid.test(id) && typeof req.headers['idempotency-key'] === 'string'
    && req.headers['idempotency-key'].toLowerCase() === id.toLowerCase();
  if (!['GET', 'POST'].includes(req.method) || (req.method === 'GET') !== Boolean(route[1])) {
    fail(405, 'METHOD_NOT_ALLOWED'); return true;
  }
  if (req.method === 'GET') {
    const tenant = url.searchParams.get('tenant_id'), id = route[1];
    if (!identityValid(tenant, id)) { fail(400, 'INVALID_IDENTITY'); return true; }
    const existing = reversals.get(`${tenant.toLowerCase()}:${id.toLowerCase()}`);
    if (existing) sendJson(res, 200, existing.response);
    else fail(404, 'REVERSAL_NOT_FOUND');
    return true;
  }
  let raw = '';
  req.on('data', chunk => { raw += chunk; if (raw.length > 1e6) req.destroy(); });
  req.on('end', () => {
    let body;
    try { body = JSON.parse(raw); } catch { return fail(400, 'INVALID_JSON'); }
    if (!body || Array.isArray(body) || typeof body !== 'object') return fail(400, 'INVALID_BODY');
    if (containsCardData(body)) return fail(400, 'CARD_DATA_FORBIDDEN');
    const allowed = ['request_id', 'tenant_id', 'sale_id', 'gateway_reference', 'amount', 'currency', 'scenario_id'];
    if (Object.keys(body).some(k => !allowed.includes(k))) return fail(400, 'UNKNOWN_FIELD');
    const { request_id, tenant_id, sale_id, gateway_reference, amount, currency, scenario_id } = body;
    if (!identityValid(tenant_id, request_id) || typeof sale_id !== 'string' || !uuid.test(sale_id)
      || typeof gateway_reference !== 'string' || !gateway_reference || gateway_reference.length > 200
      || typeof amount !== 'number' || !Number.isFinite(amount) || amount <= 0
      || typeof currency !== 'string' || !/^[A-Z]{3}$/.test(currency) || !scenarios.has(scenario_id))
      return fail(400, 'VALIDATION_ERROR');
    const key = `${tenant_id.toLowerCase()}:${request_id.toLowerCase()}`;
    const fingerprint = JSON.stringify([sale_id.toLowerCase(), gateway_reference, amount, currency, scenario_id]);
    const existing = reversals.get(key);
    if (existing) {
      if (existing.fingerprint !== fingerprint) return fail(409, 'IDEMPOTENCY_CONFLICT');
      return sendJson(res, 200, existing.response);
    }
    const original = [...payments.entries()].find(([k, p]) => k.startsWith(`${tenant_id.toLowerCase()}:`)
      && p.response.gateway_reference === gateway_reference && p.response.status === 'APPROVED')?.[1];
    if (!original) return fail(404, 'PAYMENT_NOT_FOUND');
    if (original.sale_id.toLowerCase() !== sale_id.toLowerCase() || original.amount !== amount || original.currency !== currency)
      return fail(409, 'PAYMENT_MISMATCH');
    const paymentKey = `${tenant_id.toLowerCase()}:${gateway_reference}`;
    if (byPayment.has(paymentKey)) return fail(409, 'REVERSAL_ALREADY_EXISTS');
    const declined = scenario_id === 'sim-reversal-declined-v1';
    const opaque = crypto.createHash('sha256').update(key).digest('hex').slice(0, 24).toUpperCase();
    const response = { request_id, tenant_id, sale_id, gateway_reference, amount, currency,
      status: declined ? 'DECLINED' : 'REVERSED', reversal_reference: declined ? null : `SIM-REV-${opaque}`,
      decline_code: declined ? 'REVERSAL_NOT_ALLOWED' : null };
    reversals.set(key, { fingerprint, response });
    byPayment.set(paymentKey, key);
    console.log(JSON.stringify({ event: 'payment_simulator.reversal', tenant_id, sale_id, request_id, status: response.status }));
    if (scenario_id === 'sim-reversal-timeout-v1') {
      // El efecto queda confirmado, pero se retrasa la respuesta; GET permite recuperarlo.
      const timer = setTimeout(() => { if (!res.destroyed && !res.writableEnded) sendJson(res, 200, response); }, 35000);
      timer.unref();
      return;
    }
    return sendJson(res, 200, response);
  });
  return true;
};

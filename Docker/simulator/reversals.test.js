const { test, before, after } = require('node:test');
const assert = require('node:assert/strict');
const { spawn } = require('node:child_process');
const { randomUUID } = require('node:crypto');
const net = require('node:net');
let child, base;
before(async () => {
  const listener = net.createServer(); await new Promise(resolve => listener.listen(0, '127.0.0.1', resolve));
  const port = listener.address().port; await new Promise(resolve => listener.close(resolve));
  base = `http://127.0.0.1:${port}`;
  child = spawn(process.execPath, [require.resolve('./server')], { env: { ...process.env, PORT: String(port), HOST: '127.0.0.1' }, stdio: 'ignore' });
  for (let i = 0; i < 100; i++) { try { await fetch(base); return; } catch { await new Promise(resolve => setTimeout(resolve, 30)); } }
  throw new Error('Simulator not ready');
});
after(() => child?.kill());
async function post(path, body) {
  return fetch(base + path, { method: 'POST', headers: { 'Content-Type': 'application/json', 'Idempotency-Key': body.request_id }, body: JSON.stringify(body) });
}
async function payment() {
  const body = { request_id: randomUUID(), tenant_id: randomUUID(), sale_id: randomUUID(), amount: 119, currency: 'CLP', scenario_id: 'sim-approved-v1' };
  const result = await post('/simulator/payments', body); assert.equal(result.status, 200);
  const { gateway_reference } = await result.json();
  return { ...body, request_id: randomUUID(), gateway_reference, scenario_id: 'sim-reversal-approved-v1' };
}
test('aprobación idempotente, nueva clave y parámetros diferentes rechazados', async () => {
  const body = await payment();
  const first = await post('/simulator/reversals', body); assert.equal(first.status, 200);
  const response = await first.json(); assert.equal(response.status, 'REVERSED'); assert.match(response.reversal_reference, /^SIM-REV-/);
  assert.deepEqual(await (await post('/simulator/reversals', body)).json(), response);
  assert.equal((await post('/simulator/reversals', { ...body, amount: 120 })).status, 409);
  assert.equal((await post('/simulator/reversals', { ...body, request_id: randomUUID() })).status, 409);
  const query = await fetch(`${base}/simulator/reversals/${body.request_id}?tenant_id=${body.tenant_id}`, { headers: { 'Idempotency-Key': body.request_id } });
  assert.deepEqual(await query.json(), response);
});
test('tenant, referencia, monto y venta corresponden al cobro original', async () => {
  const body = await payment();
  for (const change of [{ tenant_id: randomUUID() }, { gateway_reference: 'inexistente' }])
    assert.equal((await post('/simulator/reversals', { ...body, ...change })).status, 404);
  for (const change of [{ sale_id: randomUUID() }, { amount: 120 }, { currency: 'USD' }])
    assert.equal((await post('/simulator/reversals', { ...body, ...change })).status, 409);
  const first = await post('/simulator/reversals', body); assert.equal(first.status, 200);
  const foreign = await fetch(`${base}/simulator/reversals/${body.request_id}?tenant_id=${randomUUID()}`, { headers: { 'Idempotency-Key': body.request_id } });
  assert.equal(foreign.status, 404);
});
test('rechazo explícito no produce referencia de devolución', async () => {
  const body = { ...await payment(), scenario_id: 'sim-reversal-declined-v1' };
  const result = await (await post('/simulator/reversals', body)).json();
  assert.equal(result.status, 'DECLINED'); assert.equal(result.reversal_reference, null); assert.equal(result.decline_code, 'REVERSAL_NOT_ALLOWED');
});
test('no acepta datos de tarjeta ni campos fuera del contrato', async () => {
  const body = await payment();
  for (const extra of [{ pan: '4111111111111111' }, { cvv: '123' }, { usuario: 'cliente' }])
    assert.equal((await post('/simulator/reversals', { ...body, ...extra })).status, 400);
});

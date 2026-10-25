'use strict';

// S2-082: real TypeScript modules transpiled in memory; no generated frontend files.
// The POS callback is extracted structurally, so the boundary assertions execute
// its production guard. API spies measure calls, not actual HTTP/MS5 behavior.
// Text/comma/unknown-unit inputs deliberately corrupt the closed TypeScript
// numeric contract at runtime. They do not represent a serial/text parser.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const [root, scenario] = process.argv.slice(2);
if (!root || !scenario) throw new Error('Usage: node s2082-scale-tests.cjs <repository> <scenario>');
const frontend = path.join(root, 'globalmart-frontend');
const ts = require(path.join(frontend, 'node_modules', 'typescript'));
const compilerOptions = { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 };
const cache = new Map();
function load(relative) {
  const filename = path.join(frontend, 'src', relative);
  if (cache.has(filename)) return cache.get(filename);
  const source = fs.readFileSync(filename, 'utf8');
  const compiled = ts.transpileModule(source, { compilerOptions, fileName: filename });
  const module = { exports: {} };
  const context = { module, exports: module.exports, Date, setTimeout, clearTimeout,
    require: (request) => {
      const next = path.relative(path.join(frontend, 'src'), path.resolve(path.dirname(filename), request + '.ts'));
      return load(next);
    } };
  vm.runInNewContext(compiled.outputText, context, { filename });
  cache.set(filename, module.exports);
  return module.exports;
}
const { SimulatedScaleAdapter } = load('hardware/simulated-scale-adapter.ts');
const { readStableWeightForCart } = load('hardware/scale-cart.ts');
const { MockScaleAdapter } = load('devices/adapters/MockScaleAdapter.ts');
const cases = {
  kg: [1.234, 'kg', 1.234], g: [1234, 'g', 1.234],
  'min-kg': [0.01, 'kg', 0.01], 'min-g': [10, 'g', 0.01],
  'floating-noise': [0.1 + 0.2, 'kg', 0.3],
  nan: [NaN, 'kg'], infinity: [Infinity, 'kg'], 'negative-infinity': [-Infinity, 'kg'],
  negative: [-0.01, 'kg'], zero: [0, 'kg'], 'below-min': [0.009, 'kg'],
  'subgram-kg': [0.0105, 'kg'], 'subgram-g': [10.5, 'g'],
  subgram: [0.0105, 'kg'], text: ['0.750', 'kg'], comma: ['0,750', 'kg'], unit: [0.75, 'lb'],
};
function adapter(reading) {
  return new SimulatedScaleAdapter({ readings: [reading], now: () => new Date('2026-10-25T12:00:00Z') });
}
async function simulator() {
  const [value, unit, expected] = cases[scenario.slice(4)];
  const scale = adapter({ outcome: 'stable', value, unit });
  assert.equal((await scale.connect()).ok, true);
  const result = await scale.readStableWeight();
  if (expected === undefined) {
    assert.equal(result.ok, false, 'Invalid simulator input must be rejected');
    assert.equal(result.error.code, 'invalid_reading');
    assert.equal(result.state.readingStatus, 'error');
  } else {
    assert.equal(result.ok, true);
    assert.equal(result.value.weightKg, expected, 'Weight must be canonical to 0.001 kg');
    assert.equal(result.value.unit, 'kg');
    assert.equal(result.value.stable, true);
    assert.equal(result.value.capturedAt, '2026-10-25T12:00:00.000Z');
    assert.equal(result.state.readingStatus, 'stable');
  }
}
async function states() {
  const suffix = scenario.slice(6);
  const reading = suffix === 'disconnect-during-read' ? { outcome: 'disconnected' }
    : suffix === 'unstable' ? { outcome: 'error', code: 'unstable_reading' }
    : { outcome: 'stable', value: 0.75, unit: 'kg', delayMs: 20 };
  const scale = adapter(reading);
  if (suffix !== 'disconnected') await scale.connect();
  if (suffix === 'waiting') {
    const pending = scale.readStableWeight();
    assert.equal((await scale.getState()).readingStatus, 'waiting_for_stability');
    assert.equal((await pending).ok, true);
    assert.equal((await scale.getState()).readingStatus, 'stable');
    return;
  }
  const result = await scale.readStableWeight({ timeoutMs: suffix === 'timeout' ? 1 : 100 });
  assert.equal(result.ok, false);
  assert.equal(result.error.code, { disconnected: 'not_connected', timeout: 'read_timeout',
    unstable: 'unstable_reading', 'disconnect-during-read': 'device_unavailable' }[suffix]);
  assert.equal(result.state.readingStatus, 'error');
  if (suffix === 'disconnect-during-read') assert.equal(result.state.connectionStatus, 'disconnected');
}
async function mock() {
  const scale = new MockScaleAdapter();
  const suffix = scenario.slice(5);
  if (['nan', 'infinity', 'negative'].includes(suffix)) {
    await assert.rejects(() => scale.simularPeso({ nan: NaN, infinity: Infinity, negative: -0.01 }[suffix]),
      'Mock must reject invalid or nonfinite gross weight');
    assert.equal((await scale.obtenerPeso()).pesoKg, 0, 'Rejected input must not mutate prior state');
    return;
  }
  if (suffix.startsWith('round-')) {
    const result = await scale.simularPeso(suffix === 'round-down' ? 1.2344 : 1.2346);
    assert.equal(result.pesoKg, suffix === 'round-down' ? 1.234 : 1.235);
    assert.equal(result.estable, true);
    assert.equal(result.unidad, 'kg');
    return;
  }
  await scale.simularPeso(0.2);
  assert.equal((await scale.tarar()).exito, true);
  assert.equal((await scale.obtenerPeso()).pesoKg, 0);
  const net = await scale.simularPeso(suffix === 'tare-zero' ? 0.1 : 1.4346);
  assert.equal(net.pesoKg, suffix === 'tare-zero' ? 0 : 1.235);
}
function productiveCallback(context) {
  const filename = path.join(frontend, 'src', 'pages', 'Pos.tsx');
  const source = fs.readFileSync(filename, 'utf8');
  const ast = ts.createSourceFile(filename, source, ts.ScriptTarget.Latest, true, ts.ScriptKind.TSX);
  const matches = [];
  function visit(node) {
    if (ts.isVariableDeclaration(node) && ts.isIdentifier(node.name) && node.name.text === 'addProductToCart')
      matches.push(node);
    ts.forEachChild(node, visit);
  }
  visit(ast);
  assert.equal(matches.length, 1, 'Expected a unique production addProductToCart declaration');
  const initializer = matches[0].initializer;
  assert.ok(ts.isCallExpression(initializer), 'Expected useCallback initializer');
  assert.equal(initializer.expression.getText(ast), 'useCallback');
  const callback = initializer.arguments[0];
  assert.ok(ts.isArrowFunction(callback), 'Expected the production async callback');
  const compiled = ts.transpileModule(`globalThis.run = (${callback.getText(ast)});`, { compilerOptions });
  vm.runInNewContext(compiled.outputText, context, { filename: 'production-addProductToCart.js' });
  return context.run;
}
async function pos() {
  const suffix = scenario.slice(4);
  const valid = suffix.startsWith('valid-');
  const [weightKg, unit] = valid ? [0.75, 'kg'] : cases[suffix] || [0.75, 'kg'];
  const connectedState = { connectionStatus: 'connected', readingStatus: 'stable' };
  const scale = {
    getState: async () => connectedState,
    readStableWeight: async () => ['timeout', 'disconnected'].includes(suffix)
      ? { ok: false, error: { code: suffix === 'timeout' ? 'read_timeout' : 'device_unavailable', message: suffix }, state: connectedState }
      : { ok: true, value: { weightKg, unit, stable: suffix !== 'unstable' }, state: connectedState },
  };
  const calls = [];
  const mutations = [];
  const sale = { id: 'sale-1', items: suffix === 'valid-update'
    ? [{ id: 'item-1', producto_id: 'product-1', cantidad: 0.25 }] : [] };
  let error = '';
  const context = {
    isStartingSale: false, saleNeedsRefresh: false, paymentState: 'idle',
    scaleReadInFlight: { current: false }, venta: sale, window: { api: { scale } },
    readStableWeightForCart, setIsReadingScale: () => {}, setVentaError: value => { error = value; },
    queueSaleMutation: callback => { const pending = callback(sale); mutations.push(pending); return pending; },
    posApi: {
      addSaleItem: async (...args) => { calls.push({ method: 'add', args }); },
      updateSaleItem: async (...args) => { calls.push({ method: 'update', args }); },
    },
    setSearchResults: () => {}, setClearSearchSignal: () => {},
    setDraftItems: () => { throw new Error('Existing-sale test must not use the draft path'); },
  };
  const callback = productiveCallback(context);
  const accepted = await callback({ id: 'product-1', nombre: 'Producto por peso', esPesoVariable: true, precioBase: 1000 });
  await Promise.all(mutations);
  assert.equal(context.scaleReadInFlight.current, false, 'Scale lock must always be released');
  assert.equal(accepted, valid);
  assert.equal(calls.length, valid ? 1 : 0, 'Invalid weights must never call MS5 mutations');
  assert.equal(mutations.length, valid ? 1 : 0, 'Invalid weights must not enter the mutation queue');
  if (valid) {
    assert.equal(error, '');
    assert.equal(calls[0].method, suffix === 'valid-update' ? 'update' : 'add');
    assert.deepEqual(calls[0].args, suffix === 'valid-update'
      ? ['sale-1', 'item-1', 1, 1] : ['sale-1', 'product-1', 0.75, 0.75]);
  } else assert.ok(error.length > 0, 'Rejected readings must explain the error');
  console.log(`SPIES ${scenario}: addSaleItem=${calls.filter(call => call.method === 'add').length}, updateSaleItem=${calls.filter(call => call.method === 'update').length}`);
}
(async () => {
  if (scenario.startsWith('sim-')) await simulator();
  else if (scenario.startsWith('state-')) await states();
  else if (scenario.startsWith('mock-')) await mock();
  else if (scenario.startsWith('pos-')) await pos();
  else throw new Error(`Unknown scenario: ${scenario}`);
  console.log(`PASS ${scenario}`);
})().catch(error => { console.error(error.stack || error); process.exitCode = 1; });

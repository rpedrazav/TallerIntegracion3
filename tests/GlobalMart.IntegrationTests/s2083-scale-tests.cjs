'use strict';
// Runs the real production callback and simulator; API/state spies are local.
// No mounted React UI, IPC, HTTP, database, authorization service or physical scale.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const [root, scenario] = process.argv.slice(2);
const frontend = path.join(root, 'globalmart-frontend');
const ts = require(path.join(frontend, 'node_modules/typescript'));
const compilerOptions = { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 };
function load(relative) {
  const filename = path.join(frontend, 'src', relative);
  const module = { exports: {} };
  vm.runInNewContext(ts.transpileModule(fs.readFileSync(filename, 'utf8'), { compilerOptions }).outputText,
    { module, exports: module.exports, Date, setTimeout, clearTimeout }, { filename });
  return module.exports;
}
const { SimulatedScaleAdapter } = load('hardware/simulated-scale-adapter.ts');
const { readStableWeightForCart } = load('hardware/scale-cart.ts');
const filename = path.join(frontend, 'src/pages/Pos.tsx');
const source = fs.readFileSync(filename, 'utf8');
const ast = ts.createSourceFile(filename, source, ts.ScriptTarget.Latest, true, ts.ScriptKind.TSX);
const callbacks = [];
function visit(node) {
  if (ts.isVariableDeclaration(node) && ts.isIdentifier(node.name) && node.name.text === 'addProductToCart')
    callbacks.push(node);
  ts.forEachChild(node, visit);
}
visit(ast);
assert.equal(callbacks.length, 1);
const initializer = callbacks[0].initializer;
assert.ok(ts.isCallExpression(initializer));
assert.equal(initializer.expression.getText(ast), 'useCallback');
const callback = initializer.arguments[0];
assert.ok(ts.isArrowFunction(callback));
const clone = value => JSON.parse(JSON.stringify(value));

function capabilityGuard() {
  // Detect the current terminal rejection structurally. Absence of this pattern
  // would require a real authorized manual-flow test before acceptance can pass.
  let terminalReadFailure = false;
  function inspect(node) {
    if (ts.isIfStatement(node) && ts.isPrefixUnaryExpression(node.expression)
      && node.expression.operator === ts.SyntaxKind.ExclamationToken
      && ts.isPropertyAccessExpression(node.expression.operand)
      && node.expression.operand.getText(ast) === 'reading.ok'
      && ts.isBlock(node.thenStatement)) {
      terminalReadFailure = node.thenStatement.statements.some(statement =>
        ts.isReturnStatement(statement) && statement.expression?.kind === ts.SyntaxKind.FalseKeyword);
    }
    ts.forEachChild(node, inspect);
  }
  inspect(callback);
  console.log(`CAPABILITY terminalReadFailure=${terminalReadFailure}; no manual authorization handler is supplied by this test`);
  assert.equal(terminalReadFailure, false,
    'S2-083 ACCEPTANCE INCOMPLETE (capability guard, NOT E2E): real addProductToCart returns false on failed scale reading; authorized manual continuation has not been exercised or implemented. Weighted CartItem UI currently renders a span.');
}

async function sequence() {
  const existing = scenario.endsWith('existing');
  const saleMode = scenario.startsWith('sale');
  const scale = new SimulatedScaleAdapter({ repeatLastReading: false, readings: [
    { outcome: 'stable', value: 0.5, unit: 'kg' },
    { outcome: 'disconnected', delayMs: 20 },
    { outcome: 'stable', value: 0.25, unit: 'kg' },
  ] });
  const sale = { id: 'sale-1', items: [{ id: 'unrelated', producto_id: 'other', cantidad: 2 }] };
  let draft = [{ id: 'other', quantity: 2, name: 'Existing unrelated item' }];
  if (existing) {
    sale.items.push({ id: 'weighted', producto_id: 'product-1', cantidad: 0.25 });
    draft.push({ id: 'product-1', quantity: 0.25, weightKg: 0.25, isWeightVariable: true });
  }
  const calls = [], mutations = [], busy = [];
  let error = '';
  const context = {
    isStartingSale: false, saleNeedsRefresh: false, paymentState: 'idle',
    scaleReadInFlight: { current: false }, venta: saleMode ? sale : null,
    window: { api: { scale } }, readStableWeightForCart,
    setIsReadingScale: value => busy.push(value), setVentaError: value => { error = value; },
    setSearchResults: () => {}, setClearSearchSignal: () => {},
    setDraftItems: updater => { draft = updater(draft); },
    queueSaleMutation: fn => { const pending = fn(sale); mutations.push(pending); return pending; },
    posApi: {
      addSaleItem: async (saleId, productId, quantity, weight) => {
        calls.push(['add', saleId, productId, quantity, weight]);
        sale.items.push({ id: 'weighted', producto_id: productId, cantidad: quantity });
      },
      updateSaleItem: async (saleId, itemId, quantity, weight) => {
        calls.push(['update', saleId, itemId, quantity, weight]);
        sale.items.find(item => item.id === itemId).cantidad = quantity;
      },
    },
  };
  vm.runInNewContext(ts.transpileModule(`globalThis.run = (${callback.getText(ast)});`, { compilerOptions }).outputText, context);
  const product = { id: 'product-1', nombre: 'Weighted product', esPesoVariable: true, precioBase: 1000 };
  const cart = () => clone(saleMode ? sale.items : draft);
  const quantity = () => saleMode ? sale.items.find(i => i.producto_id === product.id).cantidad
    : draft.find(i => i.id === product.id).quantity;
  assert.equal(await context.run(product), true);
  await Promise.all(mutations);
  assert.equal(quantity(), existing ? 0.75 : 0.5);
  const before = cart();
  const callsBefore = calls.length, queueBefore = mutations.length;
  const failedRead = context.run(product);
  assert.equal(context.scaleReadInFlight.current, true);
  assert.equal(await context.run(product), false, 'Concurrent click must be blocked during scale reading');
  assert.equal(await failedRead, false);
  await Promise.all(mutations);
  assert.deepEqual(cart(), before, 'Disconnect must preserve every pre-existing item and weight');
  assert.equal(calls.length, callsBefore, 'Disconnect and concurrent click must not call MS5 spies');
  assert.equal(mutations.length, queueBefore, 'Disconnect must not enqueue mutations');
  assert.equal(context.scaleReadInFlight.current, false, 'Failure must release lock');
  assert.equal(busy.at(-1), false);
  assert.ok(error.includes('No se agregó'));
  assert.equal((await scale.getState()).connectionStatus, 'disconnected');
  assert.equal((await scale.getState()).lastStableReading.weightKg, 0.5, 'Old stable reading remains in device state but must not be reused');
  await scale.connect();
  await Promise.resolve();
  assert.deepEqual(cart(), before, 'Reconnect alone must not replay or duplicate the failed addition');
  assert.equal(calls.length, callsBefore);
  assert.equal(await context.run(product), true, 'A new explicit retry can consume the next stable weight');
  await Promise.all(mutations);
  assert.equal(quantity(), existing ? 1 : 0.75, 'Retry must add 0.25 exactly once');
  assert.equal(cart().length, 2, 'One weighted line and the unrelated line must survive');
  assert.deepEqual(cart()[0], before[0]);
  assert.equal(calls.length, saleMode ? callsBefore + 1 : 0);
  assert.equal(mutations.length, saleMode ? queueBefore + 1 : 0);
  if (saleMode) assert.deepEqual(calls.at(-1), ['update', 'sale-1', 'weighted', existing ? 1 : 0.75, existing ? 1 : 0.75]);
  assert.equal(context.scaleReadInFlight.current, false);
  assert.equal(error, '');
  console.log(`SPIES ${scenario}: calls=${calls.length}, queued=${mutations.length}; stable/disconnected/stable; no HTTP/DB or manual fallback`);
}
(async () => {
  if (scenario === 'manual-capability') capabilityGuard();
  else if (['sale-existing', 'sale-new', 'draft-existing', 'draft-new'].includes(scenario)) await sequence();
  else throw new Error(`Unknown scenario: ${scenario}`);
  console.log(`PASS ${scenario}`);
})().catch(error => { console.error(error.stack || error); process.exitCode = 1; });

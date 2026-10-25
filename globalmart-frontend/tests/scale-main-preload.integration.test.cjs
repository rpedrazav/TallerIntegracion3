const { readFileSync } = require('node:fs');
const { test } = require('node:test');
const assert = require('node:assert/strict');
const path = require('node:path');
const ts = require('typescript');

const sourceRoot = path.resolve(__dirname, '../src');

function createTypeScriptLoader(externalModules = {}) {
  const moduleCache = new Map();

  return function loadTypeScriptModule(filePath) {
    const resolvedPath = path.resolve(filePath);
    if (moduleCache.has(resolvedPath)) return moduleCache.get(resolvedPath).exports;

    const source = readFileSync(resolvedPath, 'utf8');
    const javascript = ts.transpileModule(source, {
      compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 },
    }).outputText;
    const moduleUnderTest = { exports: {} };
    moduleCache.set(resolvedPath, moduleUnderTest);

    const localRequire = (request) => {
      if (Object.hasOwn(externalModules, request)) return externalModules[request];
      if (!request.startsWith('.')) return require(request);
      return loadTypeScriptModule(path.resolve(path.dirname(resolvedPath), `${request}.ts`));
    };

    Function('module', 'exports', 'require', javascript)(
      moduleUnderTest,
      moduleUnderTest.exports,
      localRequire,
    );
    return moduleUnderTest.exports;
  };
}

test('main y preload aceptan solo pesos estables y reconectan el simulador', async () => {
  const loadMainModule = createTypeScriptLoader();
  const { SimulatedScaleAdapter } = loadMainModule(
    path.join(sourceRoot, 'hardware/simulated-scale-adapter.ts'),
  );
  const { registerScaleIpcHandlers } = loadMainModule(
    path.join(sourceRoot, 'hardware/scale-ipc.ts'),
  );
  const adapter = new SimulatedScaleAdapter({
    readings: [
      { outcome: 'stable', value: 750, unit: 'g' },
      { outcome: 'stable', value: Number.NaN, unit: 'kg' },
      { outcome: 'disconnected', message: 'Desconexión simulada.' },
      { outcome: 'stable', value: 1.25, unit: 'kg' },
    ],
    repeatLastReading: false,
    now: () => new Date('2026-10-25T12:00:00.000Z'),
  });
  const originalConnect = adapter.connect.bind(adapter);
  let connectionAttempts = 0;
  adapter.connect = async () => {
    connectionAttempts += 1;
    return originalConnect();
  };

  const handlers = new Map();
  registerScaleIpcHandlers(
    { handle: (channel, handler) => handlers.set(channel, handler) },
    adapter,
  );

  const invocations = [];
  let exposedApi;
  const electronMock = {
    contextBridge: {
      exposeInMainWorld: (_name, api) => { exposedApi = api; },
    },
    ipcRenderer: {
      invoke: async (channel, ...args) => {
        invocations.push(channel);
        const handler = handlers.get(channel);
        assert.ok(handler, `Canal IPC no registrado: ${channel}`);
        return handler({}, ...args);
      },
    },
  };
  const loadRendererModule = createTypeScriptLoader({ electron: electronMock });
  loadRendererModule(path.join(sourceRoot, 'preload.ts'));
  const { readStableWeightForCart } = loadRendererModule(
    path.join(sourceRoot, 'hardware/scale-cart.ts'),
  );

  const cartWeights = [];
  const outcomes = [];
  for (let attempt = 0; attempt < 4; attempt += 1) {
    const result = await readStableWeightForCart(exposedApi.scale);
    outcomes.push(result);
    if (result.ok) cartWeights.push(result.weightKg);
  }

  assert.deepEqual(cartWeights, [0.75, 1.25]);
  assert.equal(outcomes[1].ok, false);
  assert.equal(outcomes[1].code, 'invalid_reading');
  assert.equal(outcomes[2].ok, false);
  assert.equal(outcomes[2].code, 'device_unavailable');
  assert.equal(outcomes[3].ok, true);
  assert.equal(connectionAttempts, 2);
  assert.deepEqual(invocations, [
    'scale:get-state', 'scale:connect', 'scale:read-stable',
    'scale:get-state', 'scale:read-stable',
    'scale:get-state', 'scale:read-stable',
    'scale:get-state', 'scale:connect', 'scale:read-stable',
  ]);
});

test('guard del carrito rechaza éxito aparente con peso no positivo o inestable', async () => {
  const loadModule = createTypeScriptLoader();
  const { readStableWeightForCart } = loadModule(
    path.join(sourceRoot, 'hardware/scale-cart.ts'),
  );
  const connectedState = {
    connectionStatus: 'connected',
    readingStatus: 'stable',
  };
  const createScaleApi = (reading) => ({
    getState: async () => connectedState,
    connect: async () => ({ ok: true, value: connectedState, state: connectedState }),
    disconnect: async () => ({ ok: true, value: connectedState, state: connectedState }),
    readStableWeight: async () => ({ ok: true, value: reading, state: connectedState }),
  });

  const negative = await readStableWeightForCart(createScaleApi({
    weightKg: -1,
    unit: 'kg',
    stable: true,
    capturedAt: '2026-10-25T12:00:00.000Z',
  }));
  const unstable = await readStableWeightForCart(createScaleApi({
    weightKg: 1,
    unit: 'kg',
    stable: false,
    capturedAt: '2026-10-25T12:00:00.000Z',
  }));

  assert.equal(negative.ok, false);
  assert.equal(negative.code, 'invalid_reading');
  assert.equal(unstable.ok, false);
  assert.equal(unstable.code, 'invalid_reading');
});

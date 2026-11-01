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

const loadTypeScriptModule = createTypeScriptLoader();
const {
  SCALE_IPC_CHANNELS,
  SCALE_IPC_CHANNEL_WHITELIST,
  parseStableWeightReadOptions,
  registerScaleIpcHandlers,
} = loadTypeScriptModule(path.join(sourceRoot, 'hardware/scale-ipc.ts'));

test('registra únicamente la whitelist de operaciones de balanza', async () => {
  const handlers = new Map();
  const calls = [];
  const ipcMain = {
    handle: (channel, handler) => handlers.set(channel, handler),
  };
  const adapter = {
    connect: async () => calls.push(['connect']),
    disconnect: async () => calls.push(['disconnect']),
    readStableWeight: async (options) => calls.push(['readStableWeight', options]),
    getState: async () => calls.push(['getState']),
  };

  registerScaleIpcHandlers(ipcMain, adapter);

  assert.deepEqual([...handlers.keys()], SCALE_IPC_CHANNEL_WHITELIST);
  assert.equal(Object.isFrozen(SCALE_IPC_CHANNELS), true);
  assert.equal(Object.isFrozen(SCALE_IPC_CHANNEL_WHITELIST), true);
  assert.deepEqual(SCALE_IPC_CHANNEL_WHITELIST, [
    'scale:connect',
    'scale:disconnect',
    'scale:read-stable',
    'scale:get-state',
  ]);

  await handlers.get(SCALE_IPC_CHANNELS.connect)({});
  await handlers.get(SCALE_IPC_CHANNELS.disconnect)({});
  await handlers.get(SCALE_IPC_CHANNELS.readStableWeight)({}, {
    timeoutMs: 5_000,
    stabilityWindowMs: 250,
  });
  await handlers.get(SCALE_IPC_CHANNELS.getState)({});

  assert.deepEqual(calls, [
    ['connect'],
    ['disconnect'],
    ['readStableWeight', { timeoutMs: 5_000, stabilityWindowMs: 250 }],
    ['getState'],
  ]);
});

test('rechaza propiedades seriales, desconocidas y tiempos inválidos', () => {
  assert.deepEqual(parseStableWeightReadOptions(undefined), {});
  assert.throws(
    () => parseStableWeightReadOptions({ portPath: '/dev/ttyUSB0' }),
    /no está permitida/,
  );
  assert.throws(
    () => parseStableWeightReadOptions({ command: 'READ' }),
    /no está permitida/,
  );
  assert.throws(
    () => parseStableWeightReadOptions({ timeoutMs: -1 }),
    /entero entre 0 y 60000/,
  );
  assert.throws(
    () => parseStableWeightReadOptions({ stabilityWindowMs: 60_001 }),
    /entero entre 0 y 60000/,
  );
});

test('preload expone API tipada sin invoke genérico ni comandos seriales', async () => {
  const invocations = [];
  let exposedApi;
  const electronMock = {
    contextBridge: {
      exposeInMainWorld: (name, api) => {
        assert.equal(name, 'api');
        exposedApi = api;
      },
    },
    ipcRenderer: {
      invoke: async (...args) => {
        invocations.push(args);
        return { ok: true };
      },
    },
  };
  const loadPreloadModule = createTypeScriptLoader({ electron: electronMock });

  loadPreloadModule(path.join(sourceRoot, 'preload.ts'));

  assert.ok(exposedApi?.scale);
  assert.equal(exposedApi.invoke, undefined);
  assert.equal(exposedApi.scale.invoke, undefined);
  assert.equal(exposedApi.scale.openPort, undefined);
  assert.equal(exposedApi.scale.sendSerialCommand, undefined);

  await exposedApi.scale.connect();
  await exposedApi.scale.disconnect();
  await exposedApi.scale.readStableWeight({ timeoutMs: 1_000 });
  await exposedApi.scale.getState();

  assert.deepEqual(invocations, [
    ['scale:connect'],
    ['scale:disconnect'],
    ['scale:read-stable', { timeoutMs: 1_000 }],
    ['scale:get-state'],
  ]);
});

test('renderer conserva aislamiento y no integra Node.js', () => {
  const mainSource = readFileSync(path.join(sourceRoot, 'index.ts'), 'utf8');

  assert.match(mainSource, /contextIsolation:\s*true/);
  assert.match(mainSource, /nodeIntegration:\s*false/);
  assert.match(mainSource, /sandbox:\s*true/);
});

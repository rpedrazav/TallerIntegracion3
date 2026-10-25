const { readFileSync } = require('node:fs');
const { test } = require('node:test');
const assert = require('node:assert/strict');
const path = require('node:path');
const ts = require('typescript');

const sourceRoot = path.resolve(__dirname, '../src');
const moduleCache = new Map();

function loadTypeScriptModule(filePath) {
  const resolvedPath = path.resolve(filePath);
  if (moduleCache.has(resolvedPath)) return moduleCache.get(resolvedPath).exports;

  const source = readFileSync(resolvedPath, 'utf8');
  const javascript = ts.transpileModule(source, {
    compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 },
  }).outputText;
  const moduleUnderTest = { exports: {} };
  moduleCache.set(resolvedPath, moduleUnderTest);

  const localRequire = (request) => {
    if (!request.startsWith('.')) return require(request);
    return loadTypeScriptModule(path.resolve(path.dirname(resolvedPath), `${request}.ts`));
  };

  Function('module', 'exports', 'require', javascript)(
    moduleUnderTest,
    moduleUnderTest.exports,
    localRequire,
  );
  return moduleUnderTest.exports;
}

const {
  SCALE_ADAPTER_ENVIRONMENT_KEY,
  createScaleAdapter,
  readScaleAdapterConfiguration,
} = loadTypeScriptModule(path.join(sourceRoot, 'hardware/scale-adapter-factory.ts'));

test('selecciona el simulador por configuración sin requerir hardware', async () => {
  const defaultConfig = readScaleAdapterConfiguration({});
  const explicitConfig = readScaleAdapterConfiguration({
    [SCALE_ADAPTER_ENVIRONMENT_KEY]: 'SIMULATOR',
  });

  assert.deepEqual(defaultConfig, { driver: 'simulator' });
  assert.deepEqual(explicitConfig, { driver: 'simulator' });
  assert.throws(
    () => readScaleAdapterConfiguration({ [SCALE_ADAPTER_ENVIRONMENT_KEY]: 'unknown' }),
    /debe ser "simulator" o "serial"/,
  );

  const adapter = createScaleAdapter({
    driver: 'simulator',
    simulator: {
      readings: [{ outcome: 'stable', value: 750, unit: 'g' }],
      now: () => new Date('2026-10-24T12:00:00.000Z'),
    },
  });

  const disconnectedRead = await adapter.readStableWeight();
  assert.equal(disconnectedRead.ok, false);
  assert.equal(disconnectedRead.error.code, 'not_connected');

  await adapter.connect();
  const result = await adapter.readStableWeight();

  assert.equal(result.ok, true);
  assert.deepEqual(result.value, {
    weightKg: 0.75,
    unit: 'kg',
    stable: true,
    capturedAt: '2026-10-24T12:00:00.000Z',
  });
  assert.equal(result.state.connectionStatus, 'connected');
  assert.equal(result.state.readingStatus, 'stable');
});

test('entrega desconexión controlada y exige reconectar', async () => {
  const adapter = createScaleAdapter({
    driver: 'simulator',
    simulator: {
      readings: [{ outcome: 'disconnected', message: 'Cable simulado desconectado.' }],
    },
  });

  await adapter.connect();
  const disconnected = await adapter.readStableWeight();

  assert.equal(disconnected.ok, false);
  assert.equal(disconnected.error.code, 'device_unavailable');
  assert.equal(disconnected.error.message, 'Cable simulado desconectado.');
  assert.equal(disconnected.state.connectionStatus, 'disconnected');
  assert.equal(disconnected.state.readingStatus, 'error');

  const readWithoutReconnect = await adapter.readStableWeight();
  assert.equal(readWithoutReconnect.ok, false);
  assert.equal(readWithoutReconnect.error.code, 'not_connected');
});

test('entrega error controlado y rechaza lecturas de peso inválidas', async () => {
  const adapter = createScaleAdapter({
    driver: 'simulator',
    simulator: {
      readings: [
        {
          outcome: 'error',
          code: 'device_error',
          message: 'Fallo simulado del sensor.',
          retryable: false,
        },
        { outcome: 'stable', value: 9, unit: 'g' },
      ],
      repeatLastReading: false,
    },
  });

  await adapter.connect();
  const controlledError = await adapter.readStableWeight();
  const invalidWeight = await adapter.readStableWeight();

  assert.equal(controlledError.ok, false);
  assert.deepEqual(controlledError.error, {
    code: 'device_error',
    message: 'Fallo simulado del sensor.',
    retryable: false,
  });
  assert.equal(controlledError.state.connectionStatus, 'connected');
  assert.equal(invalidWeight.ok, false);
  assert.equal(invalidWeight.error.code, 'invalid_reading');
});

test('permite inyectar el adaptador serial futuro sin cambiar al consumidor', () => {
  const serialAdapter = { marker: 'serial-placeholder' };
  const selected = createScaleAdapter(
    { driver: 'serial' },
    { createSerialAdapter: () => serialAdapter },
  );

  assert.equal(selected, serialAdapter);
  assert.throws(
    () => createScaleAdapter({ driver: 'serial' }),
    /todavía no está disponible/,
  );
});

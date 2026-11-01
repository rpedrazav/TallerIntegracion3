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
  SerialScaleAdapter,
  SerialScaleTransportError,
} = loadTypeScriptModule(path.join(sourceRoot, 'hardware/serial-scale-adapter.ts'));
const {
  SCALE_ADAPTER_ENVIRONMENT_KEY,
  SERIAL_SCALE_ENVIRONMENT_KEYS,
  createScaleAdapter,
  readScaleAdapterConfiguration,
} = loadTypeScriptModule(path.join(sourceRoot, 'hardware/scale-adapter-factory.ts'));

test('adapta transporte y parser inyectados al contrato común', async () => {
  const calls = [];
  const transport = {
    open: async (configuration) => calls.push(['open', configuration]),
    close: async () => calls.push(['close']),
    readFrame: async (options) => {
      calls.push(['readFrame', options]);
      return 'FRAME_MODEL_SPECIFIC';
    },
  };
  const parser = {
    id: 'test-parser',
    parse: (frame) => {
      calls.push(['parse', frame]);
      return { value: 750, unit: 'g', stable: true };
    },
  };
  const adapter = new SerialScaleAdapter(
    { portPath: '/dev/test-scale', baudRate: 9_600, parserId: 'test-parser' },
    {
      transport,
      parser,
      now: () => new Date('2026-10-25T12:00:00.000Z'),
    },
  );

  const connected = await adapter.connect();
  const reading = await adapter.readStableWeight({ timeoutMs: 2_000 });
  const disconnected = await adapter.disconnect();

  assert.equal(connected.ok, true);
  assert.equal(reading.ok, true);
  assert.deepEqual(reading.value, {
    weightKg: 0.75,
    unit: 'kg',
    stable: true,
    capturedAt: '2026-10-25T12:00:00.000Z',
  });
  assert.equal(disconnected.ok, true);
  assert.equal(disconnected.state.connectionStatus, 'disconnected');
  assert.deepEqual(calls, [
    ['open', { portPath: '/dev/test-scale', baudRate: 9_600 }],
    ['readFrame', { timeoutMs: 2_000 }],
    ['parse', 'FRAME_MODEL_SPECIFIC'],
    ['close'],
  ]);
});

test('traduce timeout, desconexión y trama inestable sin filtrar datos seriales', async () => {
  const frames = [
    () => { throw new SerialScaleTransportError('timeout', 'detalle privado'); },
    () => 'UNSTABLE',
    () => { throw new SerialScaleTransportError('unavailable', 'detalle privado'); },
  ];
  const adapter = new SerialScaleAdapter(
    { portPath: 'COM-TEST', baudRate: 115_200, parserId: 'test-parser' },
    {
      transport: {
        open: async () => undefined,
        close: async () => undefined,
        readFrame: async () => frames.shift()(),
      },
      parser: {
        id: 'test-parser',
        parse: () => ({ value: 1, unit: 'kg', stable: false }),
      },
    },
  );

  await adapter.connect();
  const timeout = await adapter.readStableWeight();
  const unstable = await adapter.readStableWeight();
  const unavailable = await adapter.readStableWeight();

  assert.equal(timeout.ok, false);
  assert.equal(timeout.error.code, 'read_timeout');
  assert.doesNotMatch(timeout.error.message, /detalle privado/);
  assert.equal(unstable.ok, false);
  assert.equal(unstable.error.code, 'unstable_reading');
  assert.equal(unavailable.ok, false);
  assert.equal(unavailable.error.code, 'device_unavailable');
  assert.equal(unavailable.state.connectionStatus, 'disconnected');
});

test('rechaza unidades que el contrato no reconoce', async () => {
  const adapter = new SerialScaleAdapter(
    { portPath: 'TEST-PORT', baudRate: 9_600, parserId: 'test-parser' },
    {
      transport: {
        open: async () => undefined,
        close: async () => undefined,
        readFrame: async () => 'UNKNOWN_UNIT',
      },
      parser: {
        id: 'test-parser',
        parse: () => ({ value: 2, unit: 'lb', stable: true }),
      },
    },
  );

  await adapter.connect();
  const result = await adapter.readStableWeight();

  assert.equal(result.ok, false);
  assert.equal(result.error.code, 'invalid_reading');
});

test('mantiene serial deshabilitado por defecto y valida configuración privada', () => {
  assert.deepEqual(readScaleAdapterConfiguration({}), { driver: 'simulator' });

  const environment = {
    [SCALE_ADAPTER_ENVIRONMENT_KEY]: 'serial',
    [SERIAL_SCALE_ENVIRONMENT_KEYS.portPath]: '/dev/test-scale',
    [SERIAL_SCALE_ENVIRONMENT_KEYS.baudRate]: '9600',
    [SERIAL_SCALE_ENVIRONMENT_KEYS.parserId]: 'vendor-model-parser',
  };
  const serialConfig = readScaleAdapterConfiguration(environment);
  assert.deepEqual(serialConfig, {
    driver: 'serial',
    serial: {
      portPath: '/dev/test-scale',
      baudRate: 9_600,
      parserId: 'vendor-model-parser',
    },
  });

  let receivedConfiguration;
  const placeholder = { marker: 'serial-adapter' };
  const selected = createScaleAdapter(serialConfig, {
    createSerialAdapter: (configuration) => {
      receivedConfiguration = configuration;
      return placeholder;
    },
  });

  assert.equal(selected, placeholder);
  assert.deepEqual(receivedConfiguration, serialConfig.serial);
  assert.throws(
    () => createScaleAdapter(serialConfig),
    /proveedor está deshabilitado/,
  );
  assert.throws(
    () => readScaleAdapterConfiguration({ [SCALE_ADAPTER_ENVIRONMENT_KEY]: 'serial' }),
    /puerto serial.*obligatorio/,
  );
});

test('no incorpora librería ni protocolo de un modelo desconocido', () => {
  const adapterSource = readFileSync(
    path.join(sourceRoot, 'hardware/serial-scale-adapter.ts'),
    'utf8',
  );
  const packageSource = readFileSync(path.resolve(__dirname, '../package.json'), 'utf8');

  assert.doesNotMatch(adapterSource, /from\s+['"]serialport['"]/i);
  assert.doesNotMatch(packageSource, /"serialport"/i);
  assert.match(adapterSource, /interface SerialScaleTransport/);
  assert.match(adapterSource, /interface SerialScaleParser/);
});

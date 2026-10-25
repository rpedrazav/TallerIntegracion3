const { readFileSync, readdirSync, statSync } = require('node:fs');
const { test } = require('node:test');
const assert = require('node:assert/strict');
const path = require('node:path');
const ts = require('typescript');

const frontendRoot = path.resolve(__dirname, '..');
const contractPath = path.join(frontendRoot, 'src/hardware/scale-contract.ts');

function transpileContract() {
  const source = readFileSync(contractPath, 'utf8');
  const javascript = ts.transpileModule(source, {
    compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 },
  }).outputText;
  const moduleUnderTest = { exports: {} };
  Function('module', 'exports', 'require', javascript)(
    moduleUnderTest,
    moduleUnderTest.exports,
    require,
  );
  return { source, contract: moduleUnderTest.exports };
}

function sourceFiles(directory) {
  return readdirSync(directory, { withFileTypes: true }).flatMap((entry) => {
    const entryPath = path.join(directory, entry.name);
    if (entry.isDirectory()) return sourceFiles(entryPath);
    return /\.(ts|tsx)$/.test(entry.name) ? [entryPath] : [];
  });
}

test('contrato define conexión, lectura estable y errores cerrados', () => {
  const { contract } = transpileContract();

  assert.deepEqual(contract.SCALE_CONNECTION_STATUSES, [
    'disconnected',
    'connecting',
    'connected',
    'disconnecting',
    'error',
  ]);
  assert.deepEqual(contract.SCALE_READING_STATUSES, [
    'idle',
    'waiting_for_stability',
    'stable',
    'error',
  ]);
  assert.deepEqual(contract.SCALE_ERROR_CODES, [
    'device_unavailable',
    'connection_failed',
    'not_connected',
    'read_timeout',
    'unstable_reading',
    'invalid_reading',
    'device_error',
  ]);
});

test('renderer y contrato no acceden al puerto serial', () => {
  const { source: contractSource } = transpileContract();
  const rendererRoots = [
    'api',
    'components',
    'hooks',
    'pages',
    'store',
    'types',
    'utils',
  ].map((directory) => path.join(frontendRoot, 'src', directory));
  const rendererFiles = [
    path.join(frontendRoot, 'src/App.tsx'),
    path.join(frontendRoot, 'src/renderer.tsx'),
    ...rendererRoots.flatMap((directory) => statSync(directory).isDirectory()
      ? sourceFiles(directory)
      : []),
  ];
  const rendererSource = rendererFiles
    .map((file) => readFileSync(file, 'utf8'))
    .join('\n');

  for (const source of [contractSource, rendererSource]) {
    assert.doesNotMatch(source, /from\s+['"]serialport['"]/i);
    assert.doesNotMatch(source, /navigator\.serial/i);
    assert.doesNotMatch(source, /SerialPort\s*\(/);
  }

  assert.doesNotMatch(contractSource, /portPath|baudRate|com\d+/i);
});

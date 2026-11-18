const { readFileSync } = require('node:fs');
const { test } = require('node:test');
const assert = require('node:assert/strict');
const path = require('node:path');
const ts = require('typescript');

function loadApiConfig(mode) {
  const sourcePath = path.resolve(__dirname, '../src/config/api.ts');
  const source = readFileSync(sourcePath, 'utf8');
  const javascript = ts.transpileModule(source, {
    compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 },
  }).outputText;

  const prev = process.env.API_MODE;
  process.env.API_MODE = mode;
  try {
    const moduleUnderTest = { exports: {} };
    Function('require', 'module', 'exports', javascript)(
      require,
      moduleUnderTest,
      moduleUnderTest.exports,
    );
    return moduleUnderTest.exports;
  } finally {
    if (prev === undefined) delete process.env.API_MODE;
    else process.env.API_MODE = prev;
  }
}

test('S3-028: todas las rutas de login y POS en modo local calzan con Docker/config/kong.yaml sin /api duplicado', () => {
  const kongYaml = readFileSync(path.resolve(__dirname, '../../Docker/config/kong.yaml'), 'utf8');
  const localApi = loadApiConfig('local');

  const expectedFlowPaths = [
    { service: 'identity', path: '/auth/login', kongPrefix: '/api/auth/login' },
    { service: 'pos', path: '/turnos/activo', kongPrefix: '/api/turnos' },
    { service: 'pos', path: '/turnos/abrir', kongPrefix: '/api/turnos' },
    { service: 'pos', path: '/turnos/cuadre', kongPrefix: '/api/turnos' },
    { service: 'pos', path: '/turnos/cerrar', kongPrefix: '/api/turnos' },
    { service: 'catalog', path: '/products', kongPrefix: '/api/products' },
    { service: 'pos', path: '/ventas', kongPrefix: '/api/ventas' },
    { service: 'pos', path: '/ventas/11111111-1111-1111-1111-111111111111/cobrar', kongPrefix: '/api/ventas' },
  ];

  for (const entry of expectedFlowPaths) {
    const fullUrl = localApi.buildServiceUrl(entry.service, entry.path);
    const parsed = new URL(fullUrl);
    assert.equal(parsed.origin, 'http://localhost:8000');
    assert.equal(parsed.pathname.includes('/api/api/'), false, `${fullUrl} duplica /api`);
    assert.equal(
      parsed.pathname.startsWith(entry.kongPrefix),
      true,
      `${parsed.pathname} debe comenzar con ${entry.kongPrefix}`,
    );
    assert.match(
      kongYaml,
      new RegExp(`-\\s+${entry.kongPrefix.replace(/\//g, '\\/')}\\b`),
      `kong.yaml debe declarar la ruta ${entry.kongPrefix}`,
    );
  }
});

test('S3-028: todas las rutas de login y POS en modo remote apuntan al Ingress del clúster sin /api duplicado', () => {
  const remoteApi = loadApiConfig('remote');
  const checks = [
    { service: 'identity', path: '/auth/login', expected: 'https://auth-rpedraza.dev.censei.cl/api/auth/login' },
    { service: 'pos', path: '/turnos/activo', expected: 'https://pos-rpedraza.dev.censei.cl/api/turnos/activo' },
    { service: 'pos', path: '/turnos/abrir', expected: 'https://pos-rpedraza.dev.censei.cl/api/turnos/abrir' },
    { service: 'catalog', path: '/products', expected: 'https://catalog-rpedraza.dev.censei.cl/api/products' },
    { service: 'pos', path: '/ventas', expected: 'https://pos-rpedraza.dev.censei.cl/api/ventas' },
    { service: 'pos', path: '/ventas/123/cobrar', expected: 'https://pos-rpedraza.dev.censei.cl/api/ventas/123/cobrar' },
  ];

  for (const check of checks) {
    const actual = remoteApi.buildServiceUrl(check.service, check.path);
    assert.equal(actual, check.expected);
    assert.equal(actual.includes('/api/api/'), false);
  }
});

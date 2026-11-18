const { existsSync, readFileSync } = require('node:fs');
const { test } = require('node:test');
const assert = require('node:assert/strict');
const path = require('node:path');
const ts = require('typescript');

function loadApiConfig(envApiMode) {
  const sourcePath = path.resolve(__dirname, '../src/config/api.ts');
  const source = readFileSync(sourcePath, 'utf8');
  const javascript = ts.transpileModule(source, {
    compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 },
  }).outputText;

  const prev = process.env.API_MODE;
  if (envApiMode === undefined) delete process.env.API_MODE;
  else process.env.API_MODE = envApiMode;

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

test('S3-026: modo por defecto es remote apuntando a los ingress del clúster', () => {
  const api = loadApiConfig(undefined);

  assert.equal(api.API_MODE, 'remote');
  assert.deepEqual(api.API_URLS, {
    identity: 'https://auth-rpedraza.dev.censei.cl/api',
    catalog: 'https://catalog-rpedraza.dev.censei.cl/api',
    pos: 'https://pos-rpedraza.dev.censei.cl/api',
    inventory: 'https://inventory-rpedraza.dev.censei.cl/api',
  });
  assert.equal(api.buildServiceUrl('identity', '/auth/login'), 'https://auth-rpedraza.dev.censei.cl/api/auth/login');
  assert.equal(api.buildServiceUrl('pos', 'turnos/activo'), 'https://pos-rpedraza.dev.censei.cl/api/turnos/activo');
});

test('S3-026: API_MODE=local apunta todos los servicios a Docker Compose vía Kong', () => {
  const api = loadApiConfig('local');

  assert.equal(api.API_MODE, 'local');
  assert.deepEqual(api.API_URLS, {
    identity: 'http://localhost:8000/api',
    catalog: 'http://localhost:8000/api',
    pos: 'http://localhost:8000/api',
    inventory: 'http://localhost:8000/api',
  });
  assert.equal(api.buildServiceUrl('identity', '/auth/login'), 'http://localhost:8000/api/auth/login');
  assert.equal(api.buildServiceUrl('pos', '/ventas'), 'http://localhost:8000/api/ventas');
});

test('S3-026: valores desconocidos en API_MODE caen de forma segura en remote', () => {
  const api = loadApiConfig('gateway');
  assert.equal(api.API_MODE, 'remote');
});

test('S3-026: config/api.ts elimina modo gateway, window.env, REACT_APP_* y overrides individuales', () => {
  const source = readFileSync(path.resolve(__dirname, '../src/config/api.ts'), 'utf8');

  assert.doesNotMatch(source, /'gateway'/, 'No debe existir el modo gateway');
  assert.doesNotMatch(source, /window\.env/, 'No debe leer window.env');
  assert.doesNotMatch(source, /REACT_APP_/, 'No debe leer variables REACT_APP_*');
  assert.doesNotMatch(
    source,
    /GATEWAY_URL|IDENTITY_API_URL|CATALOG_API_URL|POS_API_URL|INVENTORY_API_URL/,
    'No deben existir overrides individuales de URL',
  );
});

test('S3-027: src/config.ts no existe y api/client.ts no tiene interceptor ni rutas con /api duplicado', () => {
  assert.equal(
    existsSync(path.resolve(__dirname, '../src/config.ts')),
    false,
    'No debe existir el archivo paralelo src/config.ts',
  );

  const clientSource = readFileSync(path.resolve(__dirname, '../src/api/client.ts'), 'utf8');
  assert.doesNotMatch(
    clientSource,
    /endsWith\('\/api'\)|startsWith\('\/api\/'\)/,
    'api/client.ts no debe conservar el interceptor de normalización de /api',
  );
  assert.doesNotMatch(
    clientSource,
    /['"`]\/api\//,
    'Las rutas en api/client.ts no deben prefijarse con /api/ porque baseURL ya termina en /api',
  );

  for (const mode of ['remote', 'local']) {
    const api = loadApiConfig(mode);
    for (const [service, base] of Object.entries(api.API_URLS)) {
      assert.equal(base.endsWith('/api'), true, `${mode}.${service} debe terminar en /api`);
      const sampleUrl = api.buildServiceUrl(service, '/turnos/activo');
      assert.equal(sampleUrl.includes('/api/api/'), false, `${sampleUrl} no debe duplicar /api`);
    }
  }
});

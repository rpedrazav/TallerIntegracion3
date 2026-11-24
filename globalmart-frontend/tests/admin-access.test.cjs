const { readFileSync } = require('node:fs');
const { test } = require('node:test');
const assert = require('node:assert/strict');
const path = require('node:path');
const ts = require('typescript');
require('ts-node/register/transpile-only');

const storage = new Map();
global.localStorage = {
  getItem: (key) => storage.get(key) ?? null,
  removeItem: (key) => storage.delete(key),
  setItem: (key, value) => storage.set(key, String(value)),
};

const { useTenantStore } = require('../src/store/useTenantStore.ts');

function crearToken(payload) {
  const codificar = (value) => Buffer.from(JSON.stringify(value), 'utf8').toString('base64url');
  return `${codificar({ alg: 'none', typ: 'JWT' })}.${codificar(payload)}.`;
}

test('useTenantStore deriva esAdmin exclusivamente de active_role', async (t) => {
  const casos = [
    { nombre: 'ADMIN activo permite', roles: ['ADMIN'], activeRole: 'ADMIN', esperado: true },
    { nombre: 'CAJERO activo deniega', roles: ['CAJERO'], activeRole: 'CAJERO', esperado: false },
    { nombre: 'ADMIN y CAJERO asignados con CAJERO activo deniega', roles: ['ADMIN', 'CAJERO'], activeRole: 'CAJERO', esperado: false },
    { nombre: 'ADMIN y CAJERO asignados con ADMIN activo permite', roles: ['ADMIN', 'CAJERO'], activeRole: 'ADMIN', esperado: true },
    { nombre: 'rol activo ausente deniega aunque ADMIN esté asignado', roles: ['ADMIN', 'CAJERO'], esperado: false },
    { nombre: 'rol activo desconocido deniega aunque ADMIN esté asignado', roles: ['ADMIN'], activeRole: 'DESCONOCIDO', esperado: false },
    { nombre: 'SUPER_ADMIN activo permite según el contrato RBAC', roles: ['SUPER_ADMIN'], activeRole: 'SUPER_ADMIN', esperado: true },
  ];

  for (const caso of casos) {
    await t.test(caso.nombre, () => {
      useTenantStore.getState().clear();
      useTenantStore.getState().setToken(crearToken({
        tenant_id: 'tenant-a',
        roles: caso.roles,
        ...(caso.activeRole ? { active_role: caso.activeRole } : {}),
      }));

      assert.equal(useTenantStore.getState().esAdmin, caso.esperado);
    });
  }
});

test('ADMIN y SUPER_ADMIN acceden a Administración y CAJERO no', () => {
  const helperPath = path.resolve(__dirname, '../src/utils/adminAccess.ts');
  const source = readFileSync(helperPath, 'utf8');
  const javascript = ts.transpileModule(source, {
    compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 },
  }).outputText;
  const moduleUnderTest = { exports: {} };

  Function('module', 'exports', javascript)(moduleUnderTest, moduleUnderTest.exports);
  const { canAccessAdmin } = moduleUnderTest.exports;

  const permissions = {
    ADMIN: canAccessAdmin(true),
    SUPER_ADMIN: canAccessAdmin(true),
    CAJERO: canAccessAdmin(false),
  };

  assert.deepEqual(permissions, {
    ADMIN: true,
    SUPER_ADMIN: true,
    CAJERO: false,
  });
});

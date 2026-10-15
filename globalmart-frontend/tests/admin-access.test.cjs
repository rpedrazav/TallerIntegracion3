const { readFileSync } = require('node:fs');
const { test } = require('node:test');
const assert = require('node:assert/strict');
const path = require('node:path');
const ts = require('typescript');

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

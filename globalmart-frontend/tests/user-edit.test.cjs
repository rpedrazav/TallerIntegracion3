const { readFileSync } = require('node:fs');
const { test } = require('node:test');
const assert = require('node:assert/strict');
const path = require('node:path');
const ts = require('typescript');

test('edición precarga y valida email y construye PUT con el mismo usuario', () => {
  const helperPath = path.resolve(__dirname, '../src/components/admin/editarUsuario.ts');
  const source = readFileSync(helperPath, 'utf8');
  const javascript = ts.transpileModule(source, {
    compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 },
  }).outputText;
  const moduleUnderTest = { exports: {} };

  Function('module', 'exports', javascript)(moduleUnderTest, moduleUnderTest.exports);
  const {
    crearSolicitudEdicionUsuario,
    obtenerEmailInicial,
    validarEmailEdicion,
  } = moduleUnderTest.exports;

  const usuarioSeleccionado = {
    id: '11111111-1111-1111-1111-111111111111',
    email: 'actual@globalmart.cl',
  };

  assert.equal(obtenerEmailInicial(usuarioSeleccionado), 'actual@globalmart.cl');
  assert.equal(validarEmailEdicion(''), 'El correo es obligatorio.');
  assert.equal(validarEmailEdicion('correo-invalido'), 'Ingresa un correo válido.');
  assert.equal(
    validarEmailEdicion(`${'a'.repeat(196)}@x.cl`),
    'El correo no puede superar los 200 caracteres.',
  );
  assert.equal(validarEmailEdicion('nuevo@globalmart.cl'), undefined);

  const solicitud = crearSolicitudEdicionUsuario(usuarioSeleccionado, ' NUEVO@GlobalMart.cl ');

  assert.equal(solicitud.method, 'put');
  assert.equal(solicitud.path, `/users/${usuarioSeleccionado.id}`);
  assert.deepEqual(solicitud.body, { email: 'nuevo@globalmart.cl' });
  assert.equal('nombre' in solicitud.body, false);
  assert.equal('password' in solicitud.body, false);
  assert.equal('roles' in solicitud.body, false);
});

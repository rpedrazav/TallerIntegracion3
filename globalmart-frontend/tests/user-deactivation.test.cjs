const { readFileSync } = require('node:fs');
const { test } = require('node:test');
const assert = require('node:assert/strict');
const path = require('node:path');
const ts = require('typescript');

test('desactivación confirma, usa DELETE y conserva al usuario como Inactivo', () => {
  const helperPath = path.resolve(__dirname, '../src/components/admin/desactivarUsuario.ts');
  const source = readFileSync(helperPath, 'utf8');
  const javascript = ts.transpileModule(source, {
    compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 },
  }).outputText;
  const moduleUnderTest = { exports: {} };

  Function('module', 'exports', javascript)(moduleUnderTest, moduleUnderTest.exports);
  const {
    confirmarDesactivacionUsuario,
    crearSolicitudDesactivacionUsuario,
    marcarUsuarioInactivo,
  } = moduleUnderTest.exports;

  const usuarioSeleccionado = {
    id: '11111111-1111-1111-1111-111111111111',
    nombre: 'Usuario Activo',
    activo: true,
  };
  const usuarios = [
    usuarioSeleccionado,
    { id: '22222222-2222-2222-2222-222222222222', nombre: 'Otro Usuario', activo: true },
  ];
  let mensajeConfirmacion = '';

  const confirmado = confirmarDesactivacionUsuario(usuarioSeleccionado, mensaje => {
    mensajeConfirmacion = mensaje;
    return true;
  });
  const solicitud = crearSolicitudDesactivacionUsuario(usuarioSeleccionado);
  const actualizados = marcarUsuarioInactivo(usuarios, usuarioSeleccionado.id);

  assert.equal(confirmado, true);
  assert.match(mensajeConfirmacion, /desactivar a Usuario Activo/);
  assert.equal(solicitud.method, 'delete');
  assert.equal(solicitud.path, `/users/${usuarioSeleccionado.id}`);
  assert.equal(actualizados.length, usuarios.length);
  assert.equal(actualizados.some(usuario => usuario.id === usuarioSeleccionado.id), true);
  assert.equal(
    actualizados.find(usuario => usuario.id === usuarioSeleccionado.id)?.activo,
    false,
  );
});

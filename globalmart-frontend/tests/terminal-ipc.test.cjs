const { readFileSync } = require('node:fs');
const { test } = require('node:test');
const assert = require('node:assert/strict');
const path = require('node:path');
const ts = require('typescript');

function loadTerminalIpc() {
  const sourcePath = path.resolve(__dirname, '../src/payments/terminal-ipc.ts');
  const source = readFileSync(sourcePath, 'utf8');
  const javascript = ts.transpileModule(source, {
    compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 },
  }).outputText;
  const moduleUnderTest = { exports: {} };
  Function('require', 'module', 'exports', javascript)(
    require,
    moduleUnderTest,
    moduleUnderTest.exports,
  );
  return moduleUnderTest.exports;
}

const ventaId = '11111111-1111-1111-1111-111111111111';
const claveIdempotencia = '22222222-2222-2222-2222-222222222222';

test('contrato IPC limita canales y acepta DTO válidos', () => {
  const {
    TERMINAL_IPC_CHANNELS,
    parseConsultarEstadoTerminal,
    parseIniciarCobroTerminal,
  } = loadTerminalIpc();

  assert.deepEqual(Object.values(TERMINAL_IPC_CHANNELS), [
    'devices:terminal:iniciarCobro',
    'devices:terminal:consultarEstado',
  ]);
  assert.deepEqual(parseIniciarCobroTerminal({
    ventaId,
    monto: 15990,
    claveIdempotencia,
  }), {
    ventaId,
    monto: 15990,
    claveIdempotencia,
  });
  assert.deepEqual(parseConsultarEstadoTerminal({
    ventaId,
    monto: 15990,
    referenciaIntento: claveIdempotencia,
  }), {
    ventaId,
    monto: 15990,
    referenciaIntento: claveIdempotencia,
  });
});

test('validadores IPC rechazan UUID, monto y referencias inválidas', () => {
  const {
    parseConsultarEstadoTerminal,
    parseIniciarCobroTerminal,
  } = loadTerminalIpc();

  assert.throws(
    () => parseIniciarCobroTerminal({ ventaId: 'no-uuid', monto: 15990, claveIdempotencia }),
    /ventaId debe ser un UUID válido/,
  );
  for (const monto of [0, -1, Number.NaN]) {
    assert.throws(
      () => parseIniciarCobroTerminal({ ventaId, monto, claveIdempotencia }),
      /monto debe ser un número finito mayor que cero/,
    );
  }
  for (const clave of ['', 'clave con espacios', 'x'.repeat(129)]) {
    assert.throws(
      () => parseIniciarCobroTerminal({ ventaId, monto: 15990, claveIdempotencia: clave }),
      /claveIdempotencia debe ser una referencia segura/,
    );
  }
  assert.throws(
    () => parseConsultarEstadoTerminal({ ventaId, monto: 15990, referenciaIntento: '' }),
    /referenciaIntento debe ser una referencia segura/,
  );
});

test('validadores IPC rechazan propiedades adicionales incluidos PAN y CVV', () => {
  const {
    parseConsultarEstadoTerminal,
    parseIniciarCobroTerminal,
  } = loadTerminalIpc();

  assert.throws(
    () => parseIniciarCobroTerminal({
      ventaId,
      monto: 15990,
      claveIdempotencia,
      descripcion: 'extra',
    }),
    /propiedades no permitidas/,
  );
  assert.throws(
    () => parseIniciarCobroTerminal({
      ventaId,
      monto: 15990,
      claveIdempotencia,
      pan: '4111111111111111',
      cvv: '123',
    }),
    /propiedades no permitidas/,
  );
  assert.throws(
    () => parseConsultarEstadoTerminal({
      ventaId,
      monto: 15990,
      referenciaIntento: claveIdempotencia,
      vencimiento: '12/30',
    }),
    /propiedades no permitidas/,
  );
});

test('handlers validan antes del puerto y devuelven solo campos seguros', async () => {
  const { registerTerminalIpcHandlers, TERMINAL_IPC_CHANNELS } = loadTerminalIpc();
  const handlers = new Map();
  const ipcMain = {
    handle: (channel, handler) => handlers.set(channel, handler),
  };
  const solicitudes = [];
  const terminalPort = {
    iniciarCobro: async solicitud => {
      solicitudes.push(solicitud);
      return {
        ventaId: solicitud.ventaId,
        monto: solicitud.monto,
        referenciaIntento: solicitud.claveIdempotencia,
        estado: 'processing',
        referenciaOpaca: null,
        pan: 'no-debe-cruzar-ipc',
      };
    },
    consultarEstado: async solicitud => ({
      ventaId: solicitud.ventaId,
      monto: solicitud.monto,
      referenciaIntento: solicitud.referenciaIntento ?? null,
      estado: 'uncertain',
      referenciaOpaca: null,
      cvv: 'no-debe-cruzar-ipc',
    }),
  };

  registerTerminalIpcHandlers(ipcMain, terminalPort);
  assert.equal(handlers.size, 2);

  const respuesta = await handlers.get(TERMINAL_IPC_CHANNELS.iniciarCobro)(null, {
    ventaId,
    monto: 15990,
    claveIdempotencia,
  });

  assert.deepEqual(solicitudes, [{ ventaId, monto: 15990, claveIdempotencia }]);
  assert.deepEqual(respuesta, {
    ventaId,
    monto: 15990,
    referenciaIntento: claveIdempotencia,
    estado: 'processing',
    referenciaOpaca: null,
  });
});

test('modo real en main falla explícitamente sin fallback a mock', async () => {
  const { registerTerminalIpcHandlers, TERMINAL_IPC_CHANNELS } = loadTerminalIpc();
  const handlers = new Map();
  registerTerminalIpcHandlers({
    handle: (channel, handler) => handlers.set(channel, handler),
  }, undefined);

  await assert.rejects(
    handlers.get(TERMINAL_IPC_CHANNELS.iniciarCobro)(null, {
      ventaId,
      monto: 15990,
      claveIdempotencia,
    }),
    /transporte real\/MS-5 desde main no está soportado; pendiente TI3-705/,
  );
});

test('preload usa canales fijos y no expone invoke genérico en terminalApi', () => {
  const preloadPath = path.resolve(__dirname, '../src/preload.ts');
  const source = readFileSync(preloadPath, 'utf8');
  const start = source.indexOf('const terminalApi: TerminalRendererApi');
  const end = source.indexOf('\n};', start);
  const terminalApiSource = source.slice(start, end);

  assert.ok(start >= 0 && end > start);
  assert.match(terminalApiSource, /TERMINAL_IPC_CHANNELS\.iniciarCobro/);
  assert.match(terminalApiSource, /TERMINAL_IPC_CHANNELS\.consultarEstado/);
  assert.doesNotMatch(terminalApiSource, /\b(?:invoke|send|hardware|cancelar)\s*:/);
});

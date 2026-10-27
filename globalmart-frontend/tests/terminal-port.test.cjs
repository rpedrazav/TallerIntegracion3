const { readFileSync } = require('node:fs');
const { test } = require('node:test');
const assert = require('node:assert/strict');
const path = require('node:path');
const ts = require('typescript');

function loadTypeScript(relativePath, mocks = {}) {
  const sourcePath = path.resolve(__dirname, relativePath);
  const source = readFileSync(sourcePath, 'utf8');
  const javascript = ts.transpileModule(source, {
    compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 },
  }).outputText;
  const moduleUnderTest = { exports: {} };
  const localRequire = request => (
    Object.prototype.hasOwnProperty.call(mocks, request) ? mocks[request] : require(request)
  );

  Function('require', 'module', 'exports', javascript)(
    localRequire,
    moduleUnderTest,
    moduleUnderTest.exports,
  );
  return moduleUnderTest.exports;
}

function loadSimulatedTerminalAdapter() {
  const stateMachine = loadTypeScript('../src/payments/terminalStateMachine.ts');
  return loadTypeScript(
    '../src/payments/SimulatedTerminalAdapter.ts',
    { './terminalStateMachine': stateMachine },
  ).SimulatedTerminalAdapter;
}

const simulatedRequest = {
  ventaId: '11111111-1111-1111-1111-111111111111',
  monto: 15990,
  claveIdempotencia: '22222222-2222-2222-2222-222222222222',
};

test('máquina del terminal permite transiciones válidas incluido cancelled', () => {
  const {
    canTransitionTerminal,
    transitionTerminalState,
  } = loadTypeScript('../src/payments/terminalStateMachine.ts');

  assert.equal(transitionTerminalState('available', 'START'), 'processing');
  assert.equal(transitionTerminalState('processing', 'APPROVE'), 'approved');
  assert.equal(transitionTerminalState('processing', 'DECLINE'), 'declined');
  assert.equal(transitionTerminalState('processing', 'MARK_UNCERTAIN'), 'uncertain');
  assert.equal(transitionTerminalState('uncertain', 'VERIFY'), 'processing');
  assert.equal(transitionTerminalState('declined', 'RETRY'), 'available');
  assert.equal(transitionTerminalState('processing', 'CANCEL'), 'cancelled');
  assert.equal(transitionTerminalState('uncertain', 'CANCEL'), 'cancelled');
  assert.equal(canTransitionTerminal('approved', 'CANCEL'), false);
});

test('máquina del terminal rechaza transiciones inválidas', () => {
  const { transitionTerminalState } = loadTypeScript('../src/payments/terminalStateMachine.ts');

  assert.throws(
    () => transitionTerminalState('available', 'APPROVE'),
    error => error.name === 'InvalidTerminalTransitionError'
      && error.state === 'available'
      && error.event === 'APPROVE',
  );
  assert.throws(
    () => transitionTerminalState('cancelled', 'START'),
    error => error.name === 'InvalidTerminalTransitionError',
  );
});

test('TerminalPort inicia autorización con venta, monto y clave sin datos sensibles', async () => {
  const contractSource = readFileSync(
    path.resolve(__dirname, '../src/payments/TerminalPort.ts'),
    'utf8',
  );
  assert.doesNotMatch(contractSource, /\bpan\b|\bcvv\b|card.?number|numero.?tarjeta|fecha.?expiracion/i);

  const llamadas = [];
  const posApi = {
    chargeCard: async (ventaId, claveIdempotencia) => {
      llamadas.push({ operacion: 'inicio', ventaId, claveIdempotencia });
      return {
        venta: { id: ventaId, estado: 'COMPLETADA' },
        status: 'APPROVED',
        gateway_reference: 'gw-opaca-123',
      };
    },
    cardAttemptStatus: async ventaId => {
      llamadas.push({ operacion: 'consulta', ventaId });
      return {
        venta: { id: ventaId, estado: 'PENDIENTE' },
        status: 'UNCERTAIN',
        gateway_reference: null,
      };
    },
  };
  const { Ms5TerminalAdapter } = loadTypeScript(
    '../src/payments/Ms5TerminalAdapter.ts',
    { '../api/client': { posApi } },
  );
  const terminal = new Ms5TerminalAdapter(posApi);
  const solicitud = {
    ventaId: '11111111-1111-1111-1111-111111111111',
    monto: 15990,
    claveIdempotencia: '22222222-2222-2222-2222-222222222222',
  };

  assert.deepEqual(Object.keys(solicitud).sort(), ['claveIdempotencia', 'monto', 'ventaId']);
  assert.doesNotMatch(JSON.stringify(solicitud), /pan|cvv|card.?number|numero.?tarjeta|fecha.?expiracion/i);

  const inicio = await terminal.iniciarCobro(solicitud);
  assert.deepEqual(llamadas[0], {
    operacion: 'inicio',
    ventaId: solicitud.ventaId,
    claveIdempotencia: solicitud.claveIdempotencia,
  });
  assert.deepEqual(inicio, {
    ventaId: solicitud.ventaId,
    monto: solicitud.monto,
    referenciaIntento: solicitud.claveIdempotencia,
    estado: 'approved',
    referenciaOpaca: 'gw-opaca-123',
  });

  const consulta = await terminal.consultarEstado({
    ventaId: solicitud.ventaId,
    monto: solicitud.monto,
    referenciaIntento: solicitud.claveIdempotencia,
  });
  assert.equal(consulta.referenciaIntento, solicitud.claveIdempotencia);
  assert.equal(consulta.estado, 'uncertain');
});

test('adaptador MS-5 reporta cancelación no soportada sin llamada HTTP', async () => {
  let llamadasHttp = 0;
  const posApi = {
    chargeCard: async () => { llamadasHttp += 1; },
    cardAttemptStatus: async () => { llamadasHttp += 1; },
  };
  const { Ms5TerminalAdapter } = loadTypeScript(
    '../src/payments/Ms5TerminalAdapter.ts',
    { '../api/client': { posApi } },
  );
  const terminal = new Ms5TerminalAdapter(posApi);

  const resultado = await terminal.cancelarIntento({
    ventaId: '11111111-1111-1111-1111-111111111111',
    monto: 15990,
    referenciaIntento: '22222222-2222-2222-2222-222222222222',
  });

  assert.deepEqual(resultado, {
    soportada: false,
    codigo: 'CANCELACION_NO_SOPORTADA',
    ventaId: '11111111-1111-1111-1111-111111111111',
    monto: 15990,
    referenciaIntento: '22222222-2222-2222-2222-222222222222',
  });
  assert.equal(llamadasHttp, 0);
});

test('simulador inicia lectura del intento y conserva sus datos', async () => {
  const SimulatedTerminalAdapter = loadSimulatedTerminalAdapter();
  const terminal = new SimulatedTerminalAdapter({ scenario: 'waiting', delayMs: 0 });

  const lectura = await terminal.iniciarCobro(simulatedRequest);

  assert.deepEqual(lectura, {
    ventaId: simulatedRequest.ventaId,
    monto: simulatedRequest.monto,
    referenciaIntento: simulatedRequest.claveIdempotencia,
    estado: 'processing',
    referenciaOpaca: null,
  });
  assert.doesNotMatch(
    JSON.stringify({ solicitud: simulatedRequest, resultado: lectura }),
    /\bpan\b|\bcvv\b|card.?number|numero.?tarjeta|fecha.?expiracion/i,
  );
});

test('simulador waiting permanece processing', async () => {
  const SimulatedTerminalAdapter = loadSimulatedTerminalAdapter();
  const terminal = new SimulatedTerminalAdapter({ scenario: 'waiting', delayMs: 0 });

  const inicio = await terminal.iniciarCobro(simulatedRequest);
  const consulta = await terminal.consultarEstado({
    ventaId: inicio.ventaId,
    monto: inicio.monto,
    referenciaIntento: inicio.referenciaIntento,
  });

  assert.equal(inicio.estado, 'processing');
  assert.equal(consulta.estado, 'processing');
  assert.equal(consulta.referenciaIntento, simulatedRequest.claveIdempotencia);
});

test('simulador approved entrega resultado y referencia opaca deterministas', async () => {
  const SimulatedTerminalAdapter = loadSimulatedTerminalAdapter();

  async function execute() {
    const terminal = new SimulatedTerminalAdapter({ scenario: 'approved', delayMs: 0 });
    const inicio = await terminal.iniciarCobro(simulatedRequest);
    return terminal.consultarEstado({
      ventaId: inicio.ventaId,
      monto: inicio.monto,
      referenciaIntento: inicio.referenciaIntento,
    });
  }

  const first = await execute();
  const second = await execute();
  assert.equal(first.estado, 'approved');
  assert.match(first.referenciaOpaca, /^SIM-[0-9A-F]{8}$/);
  assert.deepEqual(second, first);
});

test('simulador declined termina rechazado sin referencia de aprobación', async () => {
  const SimulatedTerminalAdapter = loadSimulatedTerminalAdapter();
  const terminal = new SimulatedTerminalAdapter({ scenario: 'declined', delayMs: 0 });
  const inicio = await terminal.iniciarCobro(simulatedRequest);
  const resultado = await terminal.consultarEstado({
    ventaId: inicio.ventaId,
    monto: inicio.monto,
    referenciaIntento: inicio.referenciaIntento,
  });

  assert.equal(resultado.estado, 'declined');
  assert.equal(resultado.referenciaOpaca, null);
});

test('simulador cancelled soporta cancelación y termina cancelled', async () => {
  const SimulatedTerminalAdapter = loadSimulatedTerminalAdapter();
  const terminal = new SimulatedTerminalAdapter({ scenario: 'cancelled', delayMs: 0 });
  const inicio = await terminal.iniciarCobro(simulatedRequest);
  const resultado = await terminal.cancelarIntento({
    ventaId: inicio.ventaId,
    monto: inicio.monto,
    referenciaIntento: inicio.referenciaIntento,
  });

  assert.equal(resultado.soportada, true);
  assert.equal(resultado.intento.estado, 'cancelled');
  assert.equal(resultado.intento.referenciaOpaca, null);
});

test('simulador disconnected conserva el intento como uncertain', async () => {
  const SimulatedTerminalAdapter = loadSimulatedTerminalAdapter();
  const terminal = new SimulatedTerminalAdapter({ scenario: 'disconnected', delayMs: 0 });
  const inicio = await terminal.iniciarCobro(simulatedRequest);
  const resultado = await terminal.consultarEstado({
    ventaId: inicio.ventaId,
    monto: inicio.monto,
    referenciaIntento: inicio.referenciaIntento,
  });

  assert.equal(resultado.estado, 'uncertain');
  assert.equal(resultado.referenciaIntento, simulatedRequest.claveIdempotencia);
  assert.equal(resultado.referenciaOpaca, null);
});

test('simulador timeout termina uncertain y conserva la referencia', async () => {
  const SimulatedTerminalAdapter = loadSimulatedTerminalAdapter();
  const terminal = new SimulatedTerminalAdapter({ scenario: 'timeout', delayMs: 5 });

  const inicio = await terminal.iniciarCobro(simulatedRequest);
  assert.deepEqual(inicio, {
    ventaId: simulatedRequest.ventaId,
    monto: simulatedRequest.monto,
    referenciaIntento: simulatedRequest.claveIdempotencia,
    estado: 'processing',
    referenciaOpaca: null,
  });

  const resultado = await terminal.consultarEstado({
    ventaId: inicio.ventaId,
    monto: inicio.monto,
    referenciaIntento: inicio.referenciaIntento,
  });
  const segundaConsulta = await terminal.consultarEstado({
    ventaId: inicio.ventaId,
    monto: inicio.monto,
    referenciaIntento: inicio.referenciaIntento,
  });

  assert.equal(resultado.estado, 'uncertain');
  assert.notEqual(resultado.estado, 'declined');
  assert.equal(resultado.referenciaIntento, simulatedRequest.claveIdempotencia);
  assert.deepEqual(segundaConsulta, resultado);
  assert.doesNotMatch(
    JSON.stringify({ solicitud: simulatedRequest, inicio, resultado, segundaConsulta }),
    /\bpan\b|\bcvv\b|card.?number|numero.?tarjeta|fecha.?expiracion/i,
  );
});

test('fábrica selecciona simulador para mock y MS-5 para real', () => {
  const SimulatedTerminalAdapter = loadSimulatedTerminalAdapter();
  const posApi = {
    chargeCard: async () => undefined,
    cardAttemptStatus: async () => undefined,
  };
  const { Ms5TerminalAdapter } = loadTypeScript(
    '../src/payments/Ms5TerminalAdapter.ts',
    { '../api/client': { posApi } },
  );
  const defaultDevicesConfig = {
    terminalMode: 'mock',
    scaleMode: 'mock',
    mockTerminalDelayMs: 0,
    autoApproveMock: false,
    mockTerminalScenario: 'waiting',
  };
  const { createTerminalPort } = loadTypeScript(
    '../src/payments/createTerminalPort.ts',
    {
      '../config/devices.config': { defaultDevicesConfig },
      './Ms5TerminalAdapter': { Ms5TerminalAdapter },
      './SimulatedTerminalAdapter': { SimulatedTerminalAdapter },
    },
  );

  const mock = createTerminalPort(defaultDevicesConfig);
  const real = createTerminalPort({ ...defaultDevicesConfig, terminalMode: 'real' });

  assert.equal(mock instanceof SimulatedTerminalAdapter, true);
  assert.equal(real instanceof Ms5TerminalAdapter, true);

  const simulatorSource = readFileSync(
    path.resolve(__dirname, '../src/payments/SimulatedTerminalAdapter.ts'),
    'utf8',
  );
  assert.doesNotMatch(simulatorSource, /\bpan\b|\bcvv\b|card.?number|numero.?tarjeta|fecha.?expiracion/i);
  assert.doesNotMatch(simulatorSource, /Math\.random|Date\.now/);
});

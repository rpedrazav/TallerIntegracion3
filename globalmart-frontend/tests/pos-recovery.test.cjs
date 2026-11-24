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
  const localRequire = (request) => (
    Object.prototype.hasOwnProperty.call(mocks, request) ? mocks[request] : require(request)
  );
  Function('require', 'module', 'exports', javascript)(
    localRequire,
    moduleUnderTest,
    moduleUnderTest.exports,
  );
  return moduleUnderTest.exports;
}

function loadRecoveryModule() {
  const syncModule = loadTypeScript('../src/payments/synchronizeCardPayment.ts');
  return loadTypeScript('../src/payments/posRecovery.ts', {
    './synchronizeCardPayment': syncModule,
  });
}

function createMemoryStorage() {
  const map = new Map();
  return {
    getItem: (key) => (map.has(key) ? map.get(key) : null),
    setItem: (key, value) => { map.set(key, String(value)); },
    removeItem: (key) => { map.delete(key); },
    dump: () => Object.fromEntries(map.entries()),
  };
}

const turnoId = '22222222-2222-2222-2222-222222222222';
const ventaId = '11111111-1111-1111-1111-111111111111';
const claveIdempotencia = '33333333-3333-3333-3333-333333333333';

const pendingCardSale = {
  id: ventaId,
  turno_id: turnoId,
  subtotal: 1000,
  impuestos: 190,
  total: 1190,
  metodo_pago: 'TARJETA',
  estado: 'PENDIENTE',
  created_at: '2026-11-17T20:30:00.000Z',
  items: [],
};

test('S2-109: persistencia conserva ventaId y claveIdempotencia sin almacenar datos sensibles', () => {
  const {
    saveRecoveryAttempt,
    loadRecoveryAttempt,
    clearRecoveryAttempt,
  } = loadRecoveryModule();

  const storage = createMemoryStorage();
  saveRecoveryAttempt(storage, {
    ventaId,
    turnoId,
    metodoPago: 'TARJETA',
    claveIdempotencia,
    estado: 'uncertain',
    updatedAt: '2026-11-17T20:31:00.000Z',
  });

  const rawDump = JSON.stringify(storage.dump());
  assert.doesNotMatch(rawDump, /\bpan\b|\bcvv\b|card.?number|numero.?tarjeta|fecha.?expiracion/i);

  const loaded = loadRecoveryAttempt(storage, turnoId);
  assert.deepEqual(loaded, {
    ventaId,
    turnoId,
    metodoPago: 'TARJETA',
    claveIdempotencia,
    estado: 'uncertain',
    updatedAt: '2026-11-17T20:31:00.000Z',
  });

  clearRecoveryAttempt(storage, turnoId);
  assert.equal(loadRecoveryAttempt(storage, turnoId), null);
});

test('S2-109: recarga tras timeout recupera venta COMPLETADA sin perder identificador ni duplicar cargo', async () => {
  const { recoverPosStateOnReload } = loadRecoveryModule();
  const completedSale = { ...pendingCardSale, estado: 'COMPLETADA' };
  let chargeCalls = 0;

  const ms5 = {
    getSale: async (id) => {
      assert.equal(id, ventaId);
      return completedSale;
    },
    cardAttemptStatus: async (id) => {
      assert.equal(id, ventaId);
      return {
        status: 'APPROVED',
        request_id: claveIdempotencia,
        gateway_reference: 'gw-rec-109',
        venta: completedSale,
      };
    },
    chargeCard: async () => {
      chargeCalls += 1;
      throw new Error('Nunca debe reautorizar durante la recarga');
    },
  };

  // salesByShift ya no trae la venta como PENDIENTE porque se completó en MS-5 durante la recarga
  const result = await recoverPosStateOnReload({
    turnoId,
    ventas: [completedSale],
    persistedAttempt: {
      ventaId,
      turnoId,
      metodoPago: 'TARJETA',
      claveIdempotencia,
      estado: 'uncertain',
      updatedAt: '2026-11-17T20:32:00.000Z',
    },
    ms5,
  });

  assert.equal(chargeCalls, 0);
  assert.equal(result.venta?.id, ventaId);
  assert.equal(result.venta?.estado, 'COMPLETADA');
  assert.equal(result.paymentState, 'approved');
  assert.equal(result.gatewayReference, 'gw-rec-109');
  assert.equal(result.attemptReference, claveIdempotencia);
  assert.equal(result.openModal, true);
  assert.match(result.recoveryNotice, /COMPLETADA/);
});

test('S2-109: recarga con intento incierto conserva ventaId y claveIdempotencia sin reautorizar', async () => {
  const { recoverPosStateOnReload } = loadRecoveryModule();

  const ms5 = {
    getSale: async () => pendingCardSale,
    cardAttemptStatus: async () => ({
      status: 'UNCERTAIN',
      request_id: claveIdempotencia,
      gateway_reference: null,
      venta: pendingCardSale,
    }),
  };

  const result = await recoverPosStateOnReload({
    turnoId,
    ventas: [pendingCardSale],
    persistedAttempt: {
      ventaId,
      turnoId,
      metodoPago: 'TARJETA',
      claveIdempotencia,
      estado: 'uncertain',
      updatedAt: '2026-11-17T20:33:00.000Z',
    },
    ms5,
  });

  assert.equal(result.venta?.id, ventaId);
  assert.equal(result.paymentState, 'uncertain');
  assert.equal(result.attemptReference, claveIdempotencia);
  assert.equal(result.openModal, true);
});

test('S2-109: cancelPaymentAttemptSafely bloquea cancelación a ciegas si MS-5 está UNCERTAIN y recupera si ya fue APPROVED', async () => {
  const { cancelPaymentAttemptSafely } = loadRecoveryModule();

  // Caso 1: MS-5 sigue UNCERTAIN -> NO cancelar a ciegas
  const uncertainCancel = await cancelPaymentAttemptSafely({
    ventaId,
    monto: 1190,
    referenciaIntento: claveIdempotencia,
    ms5: {
      getSale: async () => pendingCardSale,
      cardAttemptStatus: async () => ({
        status: 'UNCERTAIN',
        request_id: claveIdempotencia,
        venta: pendingCardSale,
      }),
    },
  });
  assert.equal(uncertainCancel.outcome, 'cannot_cancel_uncertain');

  // Caso 2: MS-5 ya confirmó APPROVED -> recuperar en vez de cancelar
  const completedSale = { ...pendingCardSale, estado: 'COMPLETADA' };
  const approvedCancel = await cancelPaymentAttemptSafely({
    ventaId,
    monto: 1190,
    referenciaIntento: claveIdempotencia,
    ms5: {
      getSale: async () => completedSale,
      cardAttemptStatus: async () => ({
        status: 'APPROVED',
        request_id: claveIdempotencia,
        gateway_reference: 'gw-already-approved',
        venta: completedSale,
      }),
    },
  });
  assert.equal(approvedCancel.outcome, 'already_approved');
  assert.equal(approvedCancel.gatewayReference, 'gw-already-approved');
  assert.equal(approvedCancel.venta?.estado, 'COMPLETADA');

  // Caso 3: MS-5 confirma NONE y el terminal cancela -> cancelación segura habilitada
  let cancelCalled = false;
  const safeCancel = await cancelPaymentAttemptSafely({
    ventaId,
    monto: 1190,
    referenciaIntento: claveIdempotencia,
    ms5: {
      getSale: async () => pendingCardSale,
      cardAttemptStatus: async () => ({
        status: 'NONE',
        venta: pendingCardSale,
      }),
    },
    terminal: {
      cancelarIntento: async (req) => {
        cancelCalled = true;
        return {
          soportada: true,
          intento: {
            ventaId: req.ventaId,
            monto: req.monto,
            referenciaIntento: req.referenciaIntento,
            estado: 'cancelled',
            referenciaOpaca: null,
          },
        };
      },
      consultarEstado: async () => {
        throw new Error('No debería consultar estado si cancelarIntento fue soportado');
      },
    },
  });
  assert.equal(cancelCalled, true);
  assert.equal(safeCancel.outcome, 'cancelled');
  assert.equal(safeCancel.attemptReference, null);
});

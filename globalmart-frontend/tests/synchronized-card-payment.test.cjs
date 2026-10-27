const { readFileSync } = require('node:fs');
const { test } = require('node:test');
const assert = require('node:assert/strict');
const path = require('node:path');
const ts = require('typescript');

function loadCoordinator() {
  const sourcePath = path.resolve(__dirname, '../src/payments/synchronizeCardPayment.ts');
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

function sale(estado) {
  return { id: ventaId, estado };
}

function terminalScenario(finalState, calls) {
  return {
    iniciarCobro: async solicitud => {
      calls.push({ operation: 'terminal:start', solicitud });
      return {
        ventaId,
        monto: solicitud.monto,
        referenciaIntento: solicitud.claveIdempotencia,
        estado: 'processing',
        referenciaOpaca: null,
      };
    },
    consultarEstado: async solicitud => {
      calls.push({ operation: 'terminal:status', solicitud });
      return {
        ventaId,
        monto: solicitud.monto,
        referenciaIntento: solicitud.referenciaIntento,
        estado: finalState,
        referenciaOpaca: finalState === 'approved' ? 'terminal-local-reference' : null,
      };
    },
  };
}

test('terminal approved y MS-5 confirmado permiten approved con la misma clave', async () => {
  const { synchronizeCardPayment } = loadCoordinator();
  const calls = [];
  const terminal = terminalScenario('approved', calls);
  const ms5 = {
    chargeCard: async (saleId, idempotencyKey) => {
      calls.push({ operation: 'ms5:charge', saleId, idempotencyKey });
      return {
        venta: sale('COMPLETADA'),
        status: 'APPROVED',
        gateway_reference: 'gateway-confirmed-reference',
        message: null,
      };
    },
    cardAttemptStatus: async () => {
      throw new Error('No debe reconciliar una confirmación definitiva.');
    },
  };

  const result = await synchronizeCardPayment({
    terminal,
    ms5,
    ventaId,
    monto: 15990,
    claveIdempotencia,
  });

  assert.equal(result.estado, 'approved');
  assert.equal(result.venta.estado, 'COMPLETADA');
  assert.equal(result.gatewayReference, 'gateway-confirmed-reference');
  assert.equal(calls[0].solicitud.claveIdempotencia, claveIdempotencia);
  assert.equal(calls[1].solicitud.referenciaIntento, claveIdempotencia);
  assert.deepEqual(calls[2], {
    operation: 'ms5:charge',
    saleId: ventaId,
    idempotencyKey: claveIdempotencia,
  });
});

test('terminal approved sin confirmación definitiva de MS-5 no se considera cobrado', async () => {
  const { synchronizeCardPayment } = loadCoordinator();
  let statusQueries = 0;
  const result = await synchronizeCardPayment({
    terminal: terminalScenario('approved', []),
    ms5: {
      chargeCard: async () => ({
        venta: sale('PENDIENTE'),
        status: 'APPROVED',
        gateway_reference: 'gateway-no-confirmada',
        message: null,
      }),
      cardAttemptStatus: async () => {
        statusQueries += 1;
        return {
          venta: sale('PENDIENTE'),
          status: 'UNCERTAIN',
          request_id: claveIdempotencia,
        };
      },
    },
    ventaId,
    monto: 15990,
    claveIdempotencia,
  });

  assert.equal(statusQueries, 1);
  assert.equal(result.estado, 'uncertain');
  assert.notEqual(result.estado, 'approved');
  assert.equal(result.gatewayReference, null);
});

test('error ambiguo de MS-5 consulta el intento y acepta solo confirmación correlacionada', async () => {
  const { synchronizeCardPayment } = loadCoordinator();
  let statusQueries = 0;
  const result = await synchronizeCardPayment({
    terminal: terminalScenario('approved', []),
    ms5: {
      chargeCard: async () => {
        throw new Error('respuesta perdida');
      },
      cardAttemptStatus: async () => {
        statusQueries += 1;
        return {
          venta: sale('COMPLETADA'),
          status: 'APPROVED',
          request_id: claveIdempotencia,
          gateway_reference: 'gateway-reconciliada',
        };
      },
    },
    ventaId,
    monto: 15990,
    claveIdempotencia,
  });

  assert.equal(statusQueries, 1);
  assert.equal(result.estado, 'approved');
  assert.equal(result.referenciaIntento, claveIdempotencia);
  assert.equal(result.gatewayReference, 'gateway-reconciliada');
});

test('reconciliación con request_id diferente nunca aprueba el intento', async () => {
  const { reconcileCardPayment } = loadCoordinator();
  const result = await reconcileCardPayment({
    ms5: {
      chargeCard: async () => {
        throw new Error('No corresponde iniciar otro cobro.');
      },
      cardAttemptStatus: async () => ({
        venta: sale('COMPLETADA'),
        status: 'APPROVED',
        request_id: '33333333-3333-3333-3333-333333333333',
        gateway_reference: 'gateway-ajena',
      }),
    },
    ventaId,
    referenciaEsperada: claveIdempotencia,
  });

  assert.equal(result.estado, 'uncertain');
  assert.equal(result.gatewayReference, null);
});

test('terminal declined no ejecuta autorización MS-5', async () => {
  const { synchronizeCardPayment } = loadCoordinator();
  let ms5Calls = 0;
  const ms5 = {
    chargeCard: async () => {
      ms5Calls += 1;
      throw new Error('No debe llamarse.');
    },
    cardAttemptStatus: async () => {
      ms5Calls += 1;
      throw new Error('No debe llamarse.');
    },
  };

  const result = await synchronizeCardPayment({
    terminal: terminalScenario('declined', []),
    ms5,
    ventaId,
    monto: 15990,
    claveIdempotencia,
  });

  assert.equal(result.estado, 'declined');
  assert.equal(ms5Calls, 0);
});

test('terminal uncertain no muestra cobrado ni ejecuta autorización MS-5', async () => {
  const { synchronizeCardPayment } = loadCoordinator();
  let ms5Calls = 0;
  const ms5 = {
    chargeCard: async () => {
      ms5Calls += 1;
      throw new Error('No debe llamarse.');
    },
    cardAttemptStatus: async () => {
      ms5Calls += 1;
      throw new Error('No debe llamarse.');
    },
  };

  const result = await synchronizeCardPayment({
    terminal: terminalScenario('uncertain', []),
    ms5,
    ventaId,
    monto: 15990,
    claveIdempotencia,
  });

  assert.equal(result.estado, 'uncertain');
  assert.notEqual(result.estado, 'approved');
  assert.equal(ms5Calls, 0);
});

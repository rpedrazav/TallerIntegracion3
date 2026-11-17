const { readFileSync } = require('node:fs');
const { test } = require('node:test');
const assert = require('node:assert/strict');
const path = require('node:path');
const ts = require('typescript');

function loadTypeScript(relativePath) {
  const sourcePath = path.resolve(__dirname, relativePath);
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

test('S2-108: buildSafeReceiptData toma montos, método y referencia opaca de VentaResponse sin datos sensibles ni folio DTE inventado', () => {
  const {
    buildSafeReceiptData,
    SIMULATED_RECEIPT_LABEL,
    NON_DTE_NOTICE,
    NO_DTE_FOLIO_TEXT,
  } = loadTypeScript('../src/components/pos/receiptData.ts');

  const ventaGetResponse = {
    id: '11111111-2222-3333-4444-555555555555',
    turno_id: 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee',
    subtotal: 10000,
    impuestos: 1900,
    total: 11900,
    metodo_pago: 'TARJETA',
    estado: 'COMPLETADA',
    created_at: '2026-11-17T20:00:00.000Z',
    items: [
      {
        id: 'item-1',
        producto_id: 'prod-1',
        nombre_producto: 'Queso Mantecoso',
        cantidad: 1,
        peso_kg: 0.5,
        precio_unitario: 20000,
        subtotal: 10000,
      },
    ],
  };

  const receipt = buildSafeReceiptData(ventaGetResponse, {
    reference: 'SIM-9F3C1A2B',
    change: 500,
    cashReceived: 12400,
  });

  // Coincidencia exacta con GET /ventas/{id}
  assert.equal(receipt.ventaId, ventaGetResponse.id);
  assert.equal(receipt.estado, 'COMPLETADA');
  assert.equal(receipt.subtotal, ventaGetResponse.subtotal);
  assert.equal(receipt.impuestos, ventaGetResponse.impuestos);
  assert.equal(receipt.total, ventaGetResponse.total);
  assert.equal(receipt.metodoPago, 'TARJETA');
  assert.equal(receipt.esTarjeta, true);
  assert.equal(receipt.referenciaOpaca, 'SIM-9F3C1A2B');
  assert.equal(receipt.montoRecibido, null);
  assert.equal(receipt.vuelto, null);
  assert.equal(receipt.items.length, 1);
  assert.equal(receipt.items[0].subtotal, 10000);
  assert.equal(receipt.items[0].pesoKg, 0.5);

  // Rótulo de simulado y sin folio DTE inventado
  assert.equal(receipt.esSimulado, true);
  assert.equal(receipt.rotuloSimulado, SIMULATED_RECEIPT_LABEL);
  assert.equal(receipt.avisoNoTributario, NON_DTE_NOTICE);
  assert.equal(receipt.folioDte, NO_DTE_FOLIO_TEXT);

  // Sin datos sensibles
  assert.doesNotMatch(
    JSON.stringify(receipt),
    /\bpan\b|\bcvv\b|card.?number|numero.?tarjeta|fecha.?expiracion/i,
  );
});

test('S2-108: comprobante en efectivo conserva monto recibido y vuelto MS-5 sin referencia de tarjeta', () => {
  const { buildSafeReceiptData } = loadTypeScript('../src/components/pos/receiptData.ts');

  const ventaEfectivo = {
    id: '99999999-2222-3333-4444-555555555555',
    turno_id: 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee',
    subtotal: 100,
    impuestos: 19,
    total: 119,
    metodo_pago: 'EFECTIVO',
    estado: 'COMPLETADA',
    created_at: '2026-11-17T20:05:00.000Z',
    items: [
      {
        id: 'item-cash-1',
        producto_id: 'prod-cash-1',
        nombre_producto: 'Producto A',
        cantidad: 1,
        precio_unitario: 100,
        subtotal: 100,
      },
    ],
  };

  const receipt = buildSafeReceiptData(ventaEfectivo, {
    reference: 'ignorado-en-efectivo',
    cashReceived: 200,
    change: 81,
  });

  assert.equal(receipt.esTarjeta, false);
  assert.equal(receipt.metodoPago, 'EFECTIVO');
  assert.equal(receipt.total, 119);
  assert.equal(receipt.montoRecibido, 200);
  assert.equal(receipt.vuelto, 81);
  assert.equal(receipt.referenciaOpaca, null);
  assert.equal(receipt.folioDte, 'No emitido (comprobante simulado)');
});

test('componente ComprobantePago y estilos cumplen S2-108 sin exponer datos sensibles', () => {
  const componentSource = readFileSync(
    path.resolve(__dirname, '../src/components/pos/ComprobantePago.tsx'),
    'utf8',
  );
  const receiptDataSource = readFileSync(
    path.resolve(__dirname, '../src/components/pos/receiptData.ts'),
    'utf8',
  );
  const cssSource = readFileSync(
    path.resolve(__dirname, '../src/components/pos/ComprobantePago.css'),
    'utf8',
  );

  assert.match(receiptDataSource, /\*\*\* COMPROBANTE DE PAGO SIMULADO \*\*\*/);
  assert.match(receiptDataSource, /DOCUMENTO NO TRIBUTARIO · NO VÁLIDO COMO DTE/);
  assert.match(receiptDataSource, /No emitido \(comprobante simulado\)/);

  assert.match(componentSource, /buildSafeReceiptData/);
  assert.match(componentSource, /Folio DTE:/);
  assert.match(componentSource, /Referencia opaca:/);

  for (const source of [componentSource, receiptDataSource]) {
    assert.doesNotMatch(
      source,
      /\bpan\b|\bcvv\b|card.?number|numero.?tarjeta|fecha.?expiracion/i,
      'No debe contener referencias a PAN, CVV, número de tarjeta ni fecha de expiración',
    );
  }

  assert.match(cssSource, /\.comprobante-ticket/);
  assert.match(cssSource, /\.comprobante-ticket__simulated-badge/);
  assert.match(cssSource, /@media print/);
});

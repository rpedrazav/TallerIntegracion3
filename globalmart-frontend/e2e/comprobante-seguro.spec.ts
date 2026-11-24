import { expect, test } from '@playwright/test';

const shiftId = '22222222-2222-2222-2222-222222222222';
const saleId = '11111111-1111-1111-1111-111111111111';

test('S2-108: comprobante de pago con tarjeta coincide con GET /ventas/{id}, rotulado como simulado y sin datos sensibles', async ({ page }) => {
  const cardSale = {
    id: saleId,
    turno_id: shiftId,
    subtotal: 2500,
    impuestos: 475,
    total: 2975,
    metodo_pago: 'TARJETA',
    estado: 'PENDIENTE',
    created_at: '2026-11-17T20:10:00.000Z',
    items: [
      {
        id: '33333333-3333-3333-3333-333333333333',
        producto_id: '44444444-4444-4444-4444-444444444444',
        nombre_producto: 'Café Grano',
        cantidad: 1,
        precio_unitario: 2500,
        subtotal: 2500,
      },
    ],
  };

  await page.addInitScript(() => {
    window.api = {
      ping: async () => 'pong',
      getToken: async () => 'test-token',
      setToken: async () => undefined,
      logout: async () => undefined,
      terminal: {
        iniciarCobro: async (request) => ({
          ventaId: request.ventaId,
          monto: request.monto,
          referenciaIntento: request.claveIdempotencia,
          estado: 'approved',
          referenciaOpaca: 'SIM-AABBCC11',
        }),
        consultarEstado: async (request) => ({
          ventaId: request.ventaId,
          monto: request.monto,
          referenciaIntento: request.referenciaIntento ?? null,
          estado: 'approved',
          referenciaOpaca: 'SIM-AABBCC11',
        }),
        cancelarIntento: async (request) => ({
          soportada: false,
          codigo: 'CANCELACION_NO_SOPORTADA',
          ventaId: request.ventaId,
          monto: request.monto,
          referenciaIntento: request.referenciaIntento,
        }),
      },
    };
  });

  await page.route('**/api/turnos/activo', (route) => route.fulfill({ json: { id: shiftId } }));
  await page.route('**/ventas/turno/**', (route) => route.fulfill({ json: [cardSale] }));
  await page.route(`**/ventas/${saleId}`, (route) => route.fulfill({ json: cardSale }));
  await page.route(`**/ventas/${saleId}/intento-tarjeta`, (route) =>
    route.fulfill({ json: { status: 'NONE', venta: cardSale } }),
  );
  await page.route(`**/ventas/${saleId}/cobrar-tarjeta`, (route) =>
    route.fulfill({
      json: {
        venta: { ...cardSale, estado: 'COMPLETADA' },
        status: 'APPROVED',
        gateway_reference: 'gw-opaque-s2108',
        message: 'Aprobado',
      },
    }),
  );

  await page.goto('/');
  await page.getByRole('button', { name: 'COBRAR' }).click();
  await page.getByRole('button', { name: 'Confirmar tarjeta' }).click();

  const receipt = page.getByLabel('Comprobante de pago seguro');
  await expect(receipt).toBeVisible();
  await expect(receipt).toContainText('*** COMPROBANTE DE PAGO SIMULADO ***');
  await expect(receipt).toContainText('DOCUMENTO NO TRIBUTARIO · NO VÁLIDO COMO DTE');
  await expect(receipt).toContainText('Subtotal (MS-5):');
  await expect(receipt).toContainText('$ 2.500');
  await expect(receipt).toContainText('Impuestos (MS-5):');
  await expect(receipt).toContainText('$ 475');
  await expect(receipt).toContainText('$ 2.975');
  await expect(receipt).toContainText('TARJETA');
  await expect(receipt).toContainText('gw-opaque-s2108');
  await expect(receipt).toContainText('Folio DTE:');
  await expect(receipt).toContainText('No emitido (comprobante simulado)');

  const text = (await receipt.textContent()) ?? '';
  expect(text).not.toMatch(/\b\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}\b/);
  expect(text).not.toMatch(/\bpan\b|\bcvv\b/i);
});

test('S2-108: comprobante de pago en efectivo muestra montos de MS-5, monto recibido y vuelto', async ({ page }) => {
  const cashSale = {
    id: saleId,
    turno_id: shiftId,
    subtotal: 1000,
    impuestos: 190,
    total: 1190,
    metodo_pago: 'EFECTIVO',
    estado: 'PENDIENTE',
    created_at: '2026-11-17T20:15:00.000Z',
    items: [
      {
        id: '33333333-3333-3333-3333-333333333333',
        producto_id: '44444444-4444-4444-4444-444444444444',
        nombre_producto: 'Pan Integral',
        cantidad: 1,
        precio_unitario: 1000,
        subtotal: 1000,
      },
    ],
  };

  await page.addInitScript(() => {
    window.api = {
      ping: async () => 'pong',
      getToken: async () => 'test-token',
      setToken: async () => undefined,
      logout: async () => undefined,
    };
  });

  await page.route('**/api/turnos/activo', (route) => route.fulfill({ json: { id: shiftId } }));
  await page.route('**/ventas/turno/**', (route) => route.fulfill({ json: [cashSale] }));
  await page.route(`**/ventas/${saleId}`, (route) => route.fulfill({ json: cashSale }));
  await page.route(`**/ventas/${saleId}/cobrar`, (route) =>
    route.fulfill({
      json: {
        venta: { ...cashSale, estado: 'COMPLETADA' },
        montoRecibido: 2000,
        vuelto: 810,
      },
    }),
  );

  await page.goto('/');
  await page.getByRole('button', { name: 'COBRAR' }).click();
  await page.getByLabel('Monto recibido').fill('2000');
  await page.getByRole('button', { name: 'Confirmar efectivo' }).click();

  const receipt = page.getByLabel('Comprobante de pago seguro');
  await expect(receipt).toBeVisible();
  await expect(receipt).toContainText('*** COMPROBANTE DE PAGO SIMULADO ***');
  await expect(receipt).toContainText('$ 1.000');
  await expect(receipt).toContainText('$ 190');
  await expect(receipt).toContainText('$ 1.190');
  await expect(receipt).toContainText('EFECTIVO');
  await expect(receipt).toContainText('$ 2.000');
  await expect(receipt).toContainText('$ 810');
  await expect(receipt).toContainText('No emitido (comprobante simulado)');
});

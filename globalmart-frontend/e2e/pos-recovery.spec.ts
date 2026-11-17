import { expect, test } from '@playwright/test';

const shiftId = '22222222-2222-2222-2222-222222222222';
const saleId = '11111111-1111-1111-1111-111111111111';
const baseSale = {
  id: saleId,
  turno_id: shiftId,
  subtotal: 1000,
  impuestos: 190,
  total: 1190,
  metodo_pago: 'TARJETA',
  estado: 'PENDIENTE',
  created_at: '2026-11-17T20:30:00.000Z',
  items: [
    {
      id: '33333333-3333-3333-3333-333333333333',
      producto_id: '44444444-4444-4444-4444-444444444444',
      nombre_producto: 'Aceite Vegetal',
      cantidad: 1,
      precio_unitario: 1000,
      subtotal: 1000,
    },
  ],
};

test('S2-109: recarga tras timeout recupera venta completada en MS-5 sin duplicar cargo ni perder el ID', async ({ page }) => {
  let chargePosts = 0;
  let capturedKey = '';
  let completedOnServer = false;

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
          referenciaOpaca: 'SIM-REC10901',
        }),
        consultarEstado: async (request) => ({
          ventaId: request.ventaId,
          monto: request.monto,
          referenciaIntento: request.referenciaIntento ?? null,
          estado: 'approved',
          referenciaOpaca: 'SIM-REC10901',
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
  await page.route('**/ventas/turno/**', (route) =>
    route.fulfill({
      json: [{ ...baseSale, estado: completedOnServer ? 'COMPLETADA' : 'PENDIENTE' }],
    }),
  );
  await page.route(`**/ventas/${saleId}`, (route) =>
    route.fulfill({
      json: { ...baseSale, estado: completedOnServer ? 'COMPLETADA' : 'PENDIENTE' },
    }),
  );
  await page.route(`**/ventas/${saleId}/intento-tarjeta`, (route) => {
    if (completedOnServer) {
      return route.fulfill({
        json: {
          status: 'APPROVED',
          request_id: capturedKey,
          gateway_reference: 'gw-recovered-109',
          venta: { ...baseSale, estado: 'COMPLETADA' },
        },
      });
    }
    return route.fulfill({
      json: {
        status: capturedKey ? 'UNCERTAIN' : 'NONE',
        request_id: capturedKey || null,
        venta: baseSale,
      },
    });
  });
  await page.route(`**/ventas/${saleId}/cobrar-tarjeta`, async (route) => {
    chargePosts += 1;
    capturedKey = route.request().postDataJSON().idempotency_key;
    await route.fulfill({ status: 504, json: { error: 'Gateway timeout' } });
  });

  await page.goto('/');
  await page.getByRole('button', { name: 'COBRAR' }).click();
  await page.getByRole('button', { name: 'Confirmar tarjeta' }).click();

  await expect(page.getByRole('dialog')).toContainText('Resultado incierto');
  expect(chargePosts).toBe(1);

  // Simular que MS-5 completó la transacción justo antes/durante la recarga del POS
  completedOnServer = true;
  await page.reload();

  const dialog = page.getByRole('dialog');
  await expect(dialog).toBeVisible();
  await expect(dialog).toContainText('Cobro aprobado');
  await expect(dialog).toContainText('Referencia opaca: gw-recovered-109');
  await expect(dialog).toContainText('#11111111');
  await expect(dialog).toContainText('*** COMPROBANTE DE PAGO SIMULADO ***');
  expect(chargePosts).toBe(1);
});

test('S2-109: timeout en terminal permite cancelar con seguridad y continuar el cobro conservando el ID de venta', async ({ page }) => {
  let chargePosts = 0;

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
          estado: 'uncertain',
          referenciaOpaca: null,
        }),
        consultarEstado: async (request) => ({
          ventaId: request.ventaId,
          monto: request.monto,
          referenciaIntento: request.referenciaIntento ?? null,
          estado: 'uncertain',
          referenciaOpaca: null,
        }),
        cancelarIntento: async (request) => ({
          soportada: true,
          intento: {
            ventaId: request.ventaId,
            monto: request.monto,
            referenciaIntento: request.referenciaIntento,
            estado: 'cancelled',
            referenciaOpaca: null,
          },
        }),
      },
    };
  });

  await page.route('**/api/turnos/activo', (route) => route.fulfill({ json: { id: shiftId } }));
  await page.route('**/ventas/turno/**', (route) => route.fulfill({ json: [baseSale] }));
  await page.route(`**/ventas/${saleId}`, (route) => route.fulfill({ json: baseSale }));
  await page.route(`**/ventas/${saleId}/intento-tarjeta`, (route) =>
    route.fulfill({ json: { status: 'NONE', venta: baseSale } }),
  );
  await page.route(`**/ventas/${saleId}/cobrar-tarjeta`, async (route) => {
    chargePosts += 1;
    await route.fulfill({ status: 500, json: { error: 'No debería haberse invocado' } });
  });

  await page.goto('/');
  await page.getByRole('button', { name: 'COBRAR' }).click();
  await page.getByRole('button', { name: 'Confirmar tarjeta' }).click();

  await expect(page.getByRole('dialog')).toContainText('Resultado incierto');
  expect(chargePosts).toBe(0);

  // Recargar: el POS conserva el intento incierto y el identificador de venta sin reautorizar
  await page.reload();
  const dialog = page.getByRole('dialog');
  await expect(dialog).toBeVisible();
  await expect(dialog).toContainText('Resultado incierto');
  await expect(dialog).toContainText('#11111111');
  expect(chargePosts).toBe(0);

  // Cancelar de forma segura (MS-5 confirma NONE y el terminal soporta cancelación)
  await page.getByRole('button', { name: 'Cancelar intento' }).click();
  await expect(dialog).toContainText('Intento cancelado');

  // Continuar el cobro sin perder la venta pendiente
  await page.getByRole('button', { name: 'Continuar cobro' }).click();
  await expect(dialog).toContainText('Confirmar cobro');
  await expect(page.getByText(/Venta pendiente #11111111/)).toBeVisible();
});

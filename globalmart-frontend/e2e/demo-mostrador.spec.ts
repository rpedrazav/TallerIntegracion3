import { expect, test } from '@playwright/test';
import { mkdirSync } from 'node:fs';
import path from 'node:path';

const shiftId = '22222222-2222-2222-2222-222222222222';
const cashSaleId = '11111111-1111-1111-1111-111111111111';
const cardSaleId = '66666666-6666-6666-6666-666666666666';
const unitProduct = {
  id: '44444444-4444-4444-4444-444444444444',
  nombre: 'Leche Entera 1L',
  codigoBarras: '7801234001',
  precioBase: 1000,
  esPesoVariable: false,
};
const weightedProduct = {
  id: '55555555-5555-5555-5555-555555555555',
  nombre: 'Manzana a granel',
  codigoBarras: '7809999002',
  precioBase: 2000,
  esPesoVariable: true,
};

const screenshotsDir = path.resolve(__dirname, '../../docs/qa/S2-110/capturas');
mkdirSync(screenshotsDir, { recursive: true });

test('S2-110: demo completa de mostrador (efectivo, pesable por balanza simulada, tarjeta por terminal simulado, comprobante y recuperación sin alertas placeholder)', async ({ page }) => {
  const placeholderDialogs: string[] = [];
  page.on('dialog', async (dialog) => {
    placeholderDialogs.push(`${dialog.type()}: ${dialog.message()}`);
    await dialog.dismiss();
  });

  let activeSale: any = null;
  const terminalKeys: string[] = [];
  const ms5CardKeys: string[] = [];
  let cardChargeCalls = 0;
  let simulateTimeoutOnce = false;
  let completedCardOnServer = false;

  await page.addInitScript(() => {
    const scope = window as any;
    scope.__demoTerminalKeys = [];
    scope.api = {
      ping: async () => 'pong',
      getToken: async () => 'test-token',
      setToken: async () => undefined,
      logout: async () => undefined,
      scale: {
        getState: async () => ({ connectionStatus: 'connected', readingStatus: 'idle' }),
        connect: async () => ({ ok: true, state: { connectionStatus: 'connected', readingStatus: 'idle' } }),
        readStableWeight: async () => ({
          ok: true,
          value: { weightKg: 0.75, unit: 'kg', stable: true, capturedAt: new Date().toISOString() },
          state: { connectionStatus: 'connected', readingStatus: 'stable' },
        }),
      },
      terminal: {
        iniciarCobro: async (request: { ventaId: string; monto: number; claveIdempotencia: string }) => {
          scope.__demoTerminalKeys.push(request.claveIdempotencia);
          return {
            ventaId: request.ventaId,
            monto: request.monto,
            referenciaIntento: request.claveIdempotencia,
            estado: 'approved',
            referenciaOpaca: 'SIM-DEMO110A',
          };
        },
        consultarEstado: async (request: { ventaId: string; monto: number; referenciaIntento?: string | null }) => ({
          ventaId: request.ventaId,
          monto: request.monto,
          referenciaIntento: request.referenciaIntento ?? null,
          estado: 'approved',
          referenciaOpaca: 'SIM-DEMO110A',
        }),
      },
    };
  });

  await page.route('**/api/turnos/activo', (route) =>
    route.fulfill({ json: { id: shiftId, estado: 0 } }),
  );

  await page.route('**/products/search**', (route) => {
    const q = new URL(route.request().url()).searchParams.get('q')?.toLowerCase() ?? '';
    const data = q.includes('manzana') ? [weightedProduct] : [unitProduct];
    return route.fulfill({ json: { data, totalCount: data.length, page: 1, pageSize: 15 } });
  });

  await page.route('**/ventas/turno/**', (route) => {
    const list = activeSale ? [activeSale] : [];
    return route.fulfill({ json: list });
  });

  await page.route('**/ventas', async (route) => {
    if (route.request().method() !== 'POST') return route.continue();
    const body = route.request().postDataJSON();
    if (body.metodo_pago === 'EFECTIVO') {
      activeSale = {
        id: cashSaleId,
        turno_id: shiftId,
        subtotal: 1000,
        impuestos: 190,
        total: 1190,
        metodo_pago: 'EFECTIVO',
        estado: 'PENDIENTE',
        created_at: '2026-11-17T21:00:00.000Z',
        items: [
          {
            id: 'item-cash-1',
            producto_id: unitProduct.id,
            nombre_producto: unitProduct.nombre,
            cantidad: 1,
            precio_unitario: 1000,
            subtotal: 1000,
          },
        ],
      };
      return route.fulfill({ status: 201, json: activeSale });
    }

    const weight = body.items[0].peso_kg ?? 0.75;
    const subtotal = Math.round(weight * weightedProduct.precioBase);
    const impuestos = Math.round(subtotal * 0.19);
    activeSale = {
      id: cardSaleId,
      turno_id: shiftId,
      subtotal,
      impuestos,
      total: subtotal + impuestos,
      metodo_pago: 'TARJETA',
      estado: 'PENDIENTE',
      created_at: '2026-11-17T21:05:00.000Z',
      items: [
        {
          id: 'item-card-weight-1',
          producto_id: weightedProduct.id,
          nombre_producto: weightedProduct.nombre,
          cantidad: weight,
          peso_kg: weight,
          precio_unitario: weightedProduct.precioBase,
          subtotal,
        },
      ],
    };
    return route.fulfill({ status: 201, json: activeSale });
  });

  await page.route(`**/ventas/${cashSaleId}`, (route) => route.fulfill({ json: activeSale }));
  await page.route(`**/ventas/${cashSaleId}/cobrar`, (route) => {
    const { monto_recibido } = route.request().postDataJSON();
    activeSale = { ...activeSale, estado: 'COMPLETADA' };
    return route.fulfill({
      json: {
        venta: activeSale,
        montoRecibido: monto_recibido,
        vuelto: monto_recibido - activeSale.total,
      },
    });
  });

  await page.route(`**/ventas/${cardSaleId}`, (route) => route.fulfill({ json: activeSale }));
  await page.route(`**/ventas/${cardSaleId}/intento-tarjeta`, (route) => {
    if (completedCardOnServer) {
      return route.fulfill({
        json: {
          status: 'APPROVED',
          request_id: ms5CardKeys[ms5CardKeys.length - 1] ?? null,
          gateway_reference: 'gw-demo-s2110',
          venta: { ...activeSale, estado: 'COMPLETADA' },
        },
      });
    }
    if (ms5CardKeys.length > 0 && simulateTimeoutOnce) {
      return route.fulfill({
        json: {
          status: 'UNCERTAIN',
          request_id: ms5CardKeys[ms5CardKeys.length - 1],
          venta: activeSale,
        },
      });
    }
    return route.fulfill({ json: { status: 'NONE', venta: activeSale } });
  });

  await page.route(`**/ventas/${cardSaleId}/cobrar-tarjeta`, (route) => {
    cardChargeCalls += 1;
    const key = route.request().postDataJSON().idempotency_key;
    ms5CardKeys.push(key);
    if (simulateTimeoutOnce) {
      return route.fulfill({ status: 504, json: { error: 'Timeout de pasarela simulada' } });
    }
    activeSale = { ...activeSale, estado: 'COMPLETADA' };
    completedCardOnServer = true;
    return route.fulfill({
      json: {
        venta: activeSale,
        status: 'APPROVED',
        gateway_reference: 'gw-demo-s2110',
        message: 'Autorización aprobada',
      },
    });
  });

  // =========================================================================
  // ESCENA 1 (Meta G2 + Comprobante): Venta unitaria cobrada en EFECTIVO
  // =========================================================================
  await page.goto('/');
  await page.getByPlaceholder('Escanea o busca un producto...').fill('Leche');
  await page.getByText('Leche Entera 1L', { exact: true }).click();
  await page.getByRole('button', { name: 'INICIAR VENTA' }).click();
  await expect(page.getByText(/Venta pendiente #11111111/)).toBeVisible();

  await page.getByRole('button', { name: 'COBRAR' }).click();
  await page.getByLabel('Monto recibido').fill('2000');
  await page.getByRole('button', { name: 'Confirmar efectivo' }).click();

  const cashDialog = page.getByRole('dialog');
  await expect(cashDialog).toContainText('Cobro aprobado');
  await expect(cashDialog).toContainText('Vuelto calculado por MS-5: $ 810');
  await expect(cashDialog).toContainText('*** COMPROBANTE DE PAGO SIMULADO ***');
  await expect(cashDialog).toContainText('No emitido (comprobante simulado)');
  await page.screenshot({ path: path.join(screenshotsDir, '01-demo-efectivo-comprobante.png'), fullPage: true });

  await page.getByRole('button', { name: 'Cerrar cobro' }).click();
  await expect(page.getByText('Carrito vacío')).toBeVisible();
  activeSale = null;

  // =========================================================================
  // ESCENA 2 (Meta G4): Producto pesable mediante balanza simulada
  // =========================================================================
  await page.locator('select').selectOption('TARJETA');
  await page.getByPlaceholder('Escanea o busca un producto...').fill('Manzana');
  await page.getByText('Manzana a granel', { exact: true }).click();
  await expect(page.getByText('Peso estable: 0.750 kg')).toBeVisible();
  await page.getByRole('button', { name: 'Confirmar peso' }).click();

  await page.getByRole('button', { name: 'INICIAR VENTA' }).click();
  await expect(page.getByText(/Venta pendiente #66666666/)).toBeVisible();
  await expect(page.locator('tbody tr')).toContainText('0.750 kg');
  await expect(page.getByText('$ 1.785', { exact: true })).toBeVisible();
  await page.screenshot({ path: path.join(screenshotsDir, '02-demo-producto-pesable.png'), fullPage: true });

  // =========================================================================
  // ESCENA 3 (Metas G3 + G5): Terminal POS simulado + Timeout + Recarga + Comprobante seguro
  // =========================================================================
  simulateTimeoutOnce = true;
  await page.getByRole('button', { name: 'COBRAR' }).click();
  const cardDialog = page.getByRole('dialog');
  await expect(cardDialog).toContainText('Terminal POS (Simulador)');
  await expect(cardDialog).toContainText('Estado de conexión: Conectado');
  await expect(cardDialog).toContainText('no se solicita número de tarjeta');
  await page.screenshot({ path: path.join(screenshotsDir, '03-demo-terminal-pos.png'), fullPage: true });

  await page.getByRole('button', { name: 'Confirmar tarjeta' }).click();
  await expect(cardDialog).toContainText('Resultado incierto');
  expect(cardChargeCalls).toBe(1);

  const observedTerminalKeys: string[] = await page.evaluate(() => (window as any).__demoTerminalKeys);
  terminalKeys.push(...observedTerminalKeys);
  expect(terminalKeys.length).toBe(1);
  expect(ms5CardKeys[0]).toBe(terminalKeys[0]);

  await page.screenshot({ path: path.join(screenshotsDir, '04-demo-recuperacion-timeout.png'), fullPage: true });

  // Simular que MS-5 confirmó la venta durante la recarga -> reload recupera sin duplicar cargo
  simulateTimeoutOnce = false;
  completedCardOnServer = true;
  activeSale = { ...activeSale, estado: 'COMPLETADA' };
  await page.reload();

  const recoveredDialog = page.getByRole('dialog');
  await expect(recoveredDialog).toBeVisible();
  await expect(recoveredDialog).toContainText('Cobro aprobado');
  await expect(recoveredDialog).toContainText('Referencia opaca: gw-demo-s2110');
  await expect(recoveredDialog).toContainText('*** COMPROBANTE DE PAGO SIMULADO ***');
  await expect(recoveredDialog).toContainText('DOCUMENTO NO TRIBUTARIO · NO VÁLIDO COMO DTE');
  await expect(recoveredDialog).toContainText('No emitido (comprobante simulado)');
  await expect(recoveredDialog).toContainText('$ 1.500');
  await expect(recoveredDialog).toContainText('$ 285');
  await expect(recoveredDialog).toContainText('$ 1.785');
  expect(cardChargeCalls).toBe(1); // Recarga NO duplicó el cargo

  await page.screenshot({ path: path.join(screenshotsDir, '05-demo-tarjeta-comprobante.png'), fullPage: true });

  // Aceptación S2-110: Cero alertas placeholder durante toda la demo
  expect(placeholderDialogs).toEqual([]);
});

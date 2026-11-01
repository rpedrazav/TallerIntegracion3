import { expect, test, Page } from '@playwright/test';

const product = {
  id: '11111111-1111-1111-1111-111111111111',
  nombre: 'Manzana a granel', codigoBarras: '780001', precioBase: 1000, esPesoVariable: true,
};
const unitProduct = { ...product, id: '44444444-4444-4444-4444-444444444444', nombre: 'Lata unitaria', esPesoVariable: false };
const saleId = '22222222-2222-2222-2222-222222222222';
const itemId = '33333333-3333-3333-3333-333333333333';

async function prepare(page: Page, disconnected = false) {
  let sale: any = null;
  const writes: Array<{ method: string; body: any }> = [];
  await page.addInitScript((startDisconnected) => {
    const scope = window as any;
    scope.__scaleWeight = 0.75;
    scope.__scaleDisconnected = startDisconnected;
    scope.api = {
      getToken: async () => 'test-token', logout: async () => undefined,
      scale: {
        getState: async () => ({ connectionStatus: scope.__scaleDisconnected ? 'disconnected' : 'connected', readingStatus: 'idle' }),
        connect: async () => scope.__scaleDisconnected
          ? { ok: false, error: { code: 'device_unavailable', message: 'Balanza desconectada.', retryable: true }, state: { connectionStatus: 'disconnected', readingStatus: 'error' } }
          : { ok: true, state: { connectionStatus: 'connected', readingStatus: 'idle' } },
        readStableWeight: async () => ({ ok: true, value: { weightKg: scope.__scaleWeight, unit: 'kg', stable: true, capturedAt: new Date().toISOString() }, state: { connectionStatus: 'connected', readingStatus: 'stable' } }),
      },
    };
  }, disconnected);
  const json = (value: unknown) => ({ status: 200, contentType: 'application/json', headers: { 'access-control-allow-origin': '*' }, body: JSON.stringify(value) });
  await page.route('**/api/turnos/activo', route => route.fulfill(json({ id: 'shift-1', estado: 'ABIERTO' })));
  await page.route('**/products/search**', route => {
    const item = new URL(route.request().url()).searchParams.get('q')?.toLowerCase().includes('lata') ? unitProduct : product;
    return route.fulfill(json({ data: [item], totalCount: 1, page: 1, pageSize: 15 }));
  });
  await page.route('**/ventas**', async route => {
    const url = new URL(route.request().url());
    const method = route.request().method();
    if (url.pathname.endsWith('/ventas/turno/shift-1')) return route.fulfill(json(sale ? [{ id: saleId, estado: 'PENDIENTE' }] : []));
    if (url.pathname.endsWith(`/ventas/${saleId}`) && method === 'GET') return route.fulfill(json(sale));
    if (url.pathname.endsWith('/ventas') && method === 'POST') {
      const body = route.request().postDataJSON();
      writes.push({ method, body });
      const weight = body.items[0].peso_kg;
      sale = makeSale(weight);
      return route.fulfill(json(sale));
    }
    if (url.pathname.endsWith(`/ventas/${saleId}/items/${itemId}`) && method === 'PUT') {
      const body = route.request().postDataJSON();
      writes.push({ method, body });
      sale = makeSale(body.peso_kg);
      return route.fulfill(json(sale));
    }
    return route.fulfill({ status: 404, headers: { 'access-control-allow-origin': '*' } });
  });
  await page.goto('/');
  await expect(page.getByText('Buscar Producto')).toBeVisible();
  return { writes };
}

function makeSale(weight: number) {
  const subtotal = weight * 1000;
  const impuestos = subtotal * 0.2; // tasa de prueba distinta a 19 %
  return {
    id: saleId, turno_id: 'shift-1', estado: 'PENDIENTE', metodo_pago: 'EFECTIVO',
    subtotal, impuestos, total: subtotal + impuestos, created_at: new Date().toISOString(),
    items: [{ id: itemId, producto_id: product.id, nombre_producto: product.nombre,
      cantidad: weight, peso_kg: weight, precio_unitario: 1000, subtotal }],
  };
}

async function selectProduct(page: Page) {
  await page.getByPlaceholder('Escanea o busca un producto...').fill('Manzana');
  await page.getByText('Requiere peso estable').locator('..').getByText(product.nombre, { exact: true }).click();
}

test('lectura estable, confirmación, modificación y recuperación mantienen una línea con importes de MS-5', async ({ page }) => {
  const { writes } = await prepare(page);
  await expect(page.getByText('⚖️ Balanza Simulada', { exact: true })).toHaveCount(0);
  await selectProduct(page);
  await expect(page.getByText('⚖️ Balanza Simulada', { exact: true })).toBeVisible();
  await expect(page.getByText('Peso estable: 0.750 kg')).toBeVisible();
  await expect(page.locator('tbody tr')).toHaveCount(0);
  await page.getByRole('button', { name: 'Confirmar peso' }).click();
  await expect(page.locator('tbody tr')).toHaveCount(1);
  await expect(page.locator('tbody tr')).toContainText('0.750 kg');
  await expect(page.getByText('Pendiente de MS-5')).toBeVisible();
  await page.getByRole('button', { name: 'INICIAR VENTA' }).click();
  await expect.poll(() => writes.length).toBe(1);
  expect(writes[0].body.items[0]).toMatchObject({ producto_id: product.id, cantidad: 0.75, peso_kg: 0.75 });
  await expect(page.getByText('$ 900', { exact: true })).toBeVisible();

  await page.evaluate(() => { (window as any).__scaleWeight = 1.25; });
  await selectProduct(page);
  await page.getByRole('button', { name: 'Confirmar peso' }).click();
  await expect.poll(() => writes.length).toBe(2);
  expect(writes[1].body).toMatchObject({ cantidad: 2, peso_kg: 2 });
  await expect(page.locator('tbody tr')).toHaveCount(1);
  await expect(page.locator('tbody tr')).toContainText('2.000 kg');
  await expect(page.getByText('$ 2.400', { exact: true })).toBeVisible();
  await page.reload();
  await expect(page.locator('tbody tr')).toHaveCount(1);
  await expect(page.locator('tbody tr')).toContainText('2.000 kg');
});

test('producto unitario no muestra controles de balanza', async ({ page }) => {
  await prepare(page);
  await page.getByPlaceholder('Escanea o busca un producto...').fill('Lata');
  await page.getByText(unitProduct.nombre, { exact: true }).click();
  await expect(page.locator('tbody tr')).toHaveCount(1);
  await expect(page.getByText('⚖️ Balanza Simulada', { exact: true })).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Confirmar peso' })).toHaveCount(0);
});

test('desconexión ofrece respaldo manual y rechaza pesos inválidos sin enviar ítems', async ({ page }) => {
  const { writes } = await prepare(page, true);
  await selectProduct(page);
  await expect(page.getByText('Balanza desconectada.')).toBeVisible();
  await page.getByRole('button', { name: 'Ingresar peso manualmente' }).click();
  const input = page.getByRole('textbox', { name: 'Peso manual en kg' });
  for (const value of ['0', '-1', 'abc', '0,001']) {
    await input.fill(value);
    await expect(page.getByRole('button', { name: 'Confirmar peso' })).toHaveCount(0);
    await expect(page.locator('tbody tr')).toHaveCount(0);
    expect(writes).toHaveLength(0);
  }
  await input.fill('0,750');
  await page.getByRole('button', { name: 'Confirmar peso' }).click();
  await expect(page.locator('tbody tr')).toHaveCount(1);
  await expect(page.locator('tbody tr')).toContainText('0.750 kg');
  expect(writes).toHaveLength(0); // sigue siendo borrador hasta iniciar venta
});

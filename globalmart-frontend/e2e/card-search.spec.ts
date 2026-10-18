import { expect, test } from '@playwright/test';

const shiftId = '22222222-2222-2222-2222-222222222222';
const saleId = '11111111-1111-1111-1111-111111111111';
const product = { id: '44444444-4444-4444-4444-444444444444', nombre: 'Leche Entera', codigoBarras: '7801234', precioBase: 100 };
const sale = {
  id: saleId, turno_id: shiftId, subtotal: 100, impuestos: 19, total: 119,
  metodo_pago: 'TARJETA', estado: 'PENDIENTE',
  items: [{ id: '33333333-3333-3333-3333-333333333333', producto_id: product.id, nombre_producto: product.nombre, cantidad: 1, precio_unitario: 100, subtotal: 100 }],
};

test.beforeEach(async ({ page }) => {
  await page.addInitScript(() => {
    window.api = { ping: async () => 'pong', getToken: async () => 'test-token', setToken: async () => undefined, logout: async () => undefined };
  });
  await page.route('**/api/turnos/activo', route => route.fulfill({ json: { id: shiftId } }));
  await page.route('**/ventas/turno/**', route => route.fulfill({ json: [sale] }));
  await page.route(`**/ventas/${saleId}`, route => route.fulfill({ json: sale }));
});

test('busca por nombre en MS-3 y agrega al carrito del POS', async ({ page }) => {
  await page.route('**/products/search?**', route => route.fulfill({ json: { data: [product], totalCount: 1, page: 1, pageSize: 15 } }));
  await page.route(`**/ventas/${saleId}/items`, route => route.fulfill({ json: {} }));
  await page.goto('/');
  await page.getByPlaceholder('Escanea o busca un producto...').fill('leche');
  await expect(page.getByText('7801234', { exact: true })).toBeVisible();
  await page.getByText('7801234', { exact: true }).click();
  await expect(page.getByPlaceholder('Escanea o busca un producto...')).toHaveValue('');
});

test('escanea código exacto y agrega producto sin abrir administración', async ({ page }) => {
  let added = false;
  await page.route('**/products/lookup?**', route => route.fulfill({ json: product }));
  await page.route(`**/ventas/${saleId}/items/${sale.items[0].id}`, route => {
    added = route.request().postDataJSON().cantidad === 2;
    return route.fulfill({ json: {} });
  });
  await page.goto('/');
  await page.getByPlaceholder('Escanea o busca un producto...').fill(product.codigoBarras);
  await page.getByPlaceholder('Escanea o busca un producto...').press('Enter');
  await expect(page.getByPlaceholder('Escanea o busca un producto...')).toHaveValue('');
  await expect.poll(() => added).toBe(true);
});

test('aprueba tarjeta y cierra el carrito con referencia opaca', async ({ page }) => {
  let key = '';
  await page.route(`**/ventas/${saleId}/cobrar-tarjeta`, route => {
    key = route.request().postDataJSON().idempotency_key;
    return route.fulfill({ json: { venta: { ...sale, estado: 'COMPLETADA' }, status: 'APPROVED', gateway_reference: 'ref-opaque-1', message: 'Aprobado' } });
  });
  await page.goto('/');
  await page.getByRole('button', { name: 'COBRAR' }).click();
  await page.getByRole('button', { name: 'Confirmar tarjeta' }).click();
  await expect(page.getByRole('dialog')).toContainText('Referencia opaca: ref-opaque-1');
  expect(key).toMatch(/^[0-9a-f-]{36}$/i);
  await page.getByRole('button', { name: 'Cerrar cobro' }).click();
  await expect(page.getByText('Carrito vacío')).toBeVisible();
});

test('rechazo de tarjeta permite cambiar a efectivo', async ({ page }) => {
  await page.route(`**/ventas/${saleId}/cobrar-tarjeta`, route => route.fulfill({ status: 422, json: { error: 'La pasarela rechazó el pago.', mensaje: 'Rechazado' } }));
  await page.route(`**/ventas/${saleId}/metodo-pago`, route => route.fulfill({ json: { ...sale, metodo_pago: 'EFECTIVO' } }));
  await page.goto('/');
  await page.getByRole('button', { name: 'COBRAR' }).click();
  await page.getByRole('button', { name: 'Confirmar tarjeta' }).click();
  await expect(page.getByRole('dialog')).toContainText('Cobro rechazado');
  await page.getByRole('group', { name: 'Cambiar método tras rechazo' }).getByRole('button', { name: 'Efectivo' }).click();
  await page.getByRole('button', { name: 'Cambiar a efectivo' }).click();
  await expect(page.getByRole('dialog')).toContainText('Confirmar cobro');
  await expect(page.getByLabel('Monto recibido')).toBeVisible();
});

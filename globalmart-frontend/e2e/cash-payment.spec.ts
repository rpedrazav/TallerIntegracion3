import { expect, test, Page } from '@playwright/test';

const saleId = '11111111-1111-1111-1111-111111111111';
const shiftId = '22222222-2222-2222-2222-222222222222';
const pendingSale = {
  id: saleId,
  turno_id: shiftId,
  subtotal: 100,
  impuestos: 19,
  total: 119,
  metodo_pago: 'EFECTIVO',
  estado: 'PENDIENTE',
  items: [{ id: '33333333-3333-3333-3333-333333333333', producto_id: '44444444-4444-4444-4444-444444444444', nombre_producto: 'Producto A', cantidad: 1, precio_unitario: 100, subtotal: 100 }],
};

async function openCashModal(page: Page) {
  await page.addInitScript(() => {
    window.api = {
      ping: async () => 'pong',
      getToken: async () => 'test-token',
      setToken: async () => undefined,
      logout: async () => undefined,
    };
  });
  await page.route('**/api/turnos/activo', route => route.fulfill({ json: { id: shiftId } }));
  await page.route('**/ventas/turno/**', route => route.fulfill({ json: [pendingSale] }));
  await page.route(`**/ventas/${saleId}`, route => route.fulfill({ json: pendingSale }));
  await page.route('**/products**', route => route.fulfill({ json: [] }));
  await page.goto('/');
  await page.getByRole('button', { name: 'COBRAR' }).click();
  await expect(page.getByRole('dialog')).toBeVisible();
}

test('cobra efectivo y muestra el vuelto devuelto por MS-5', async ({ page }) => {
  await openCashModal(page);
  let requestCount = 0;
  await page.route(`**/ventas/${saleId}/cobrar`, async route => {
    requestCount += 1;
    expect(route.request().postDataJSON()).toEqual({ monto_recibido: 200 });
    await route.fulfill({ json: { venta: { ...pendingSale, estado: 'COMPLETADA' }, montoRecibido: 200, vuelto: 81 } });
  });
  await page.getByLabel('Monto recibido').fill('200');
  await page.getByRole('button', { name: 'Confirmar efectivo' }).click();
  await expect(page.getByRole('dialog')).toContainText('Vuelto calculado por MS-5: $ 81');
  expect(requestCount).toBe(1);
  await page.getByRole('button', { name: 'Cerrar cobro' }).click();
  await expect(page.getByText('Carrito vacío')).toBeVisible();
});

test('monto insuficiente conserva la venta pendiente y muestra la diferencia de MS-5', async ({ page }) => {
  await openCashModal(page);
  await page.route(`**/ventas/${saleId}/cobrar`, route => route.fulfill({
    status: 400,
    json: { error: 'Monto insuficiente', total_venta: 119, monto_recibido: 100, diferencia: 19 },
  }));
  await page.getByLabel('Monto recibido').fill('100');
  await page.getByRole('button', { name: 'Confirmar efectivo' }).click();
  await expect(page.getByRole('dialog')).toContainText('Faltan según MS-5: $ 19');
  await page.getByRole('button', { name: 'Volver al carrito' }).click();
  await expect(page.getByText('Producto A')).toBeVisible();
  await expect(page.getByText(/Venta pendiente #/)).toBeVisible();
});

import { test, expect, Page } from '@playwright/test';

const products = [
  { id: '11111111-1111-1111-1111-111111111111', nombre: 'Producto A', precioBase: 100, codigoBarras: '111' },
  { id: '22222222-2222-2222-2222-222222222222', nombre: 'Producto B', precioBase: 50, codigoBarras: '222' }
];
async function prepare(page: Page) {
  await page.addInitScript(() => {
    (window as any).api = { getToken: async () => sessionStorage.getItem('tenant') || 'tenant-a' };
  });
  // Catálogo controlado: nunca consulta el servicio remoto ni datos reales.
  await page.route('**/products**', route => route.fulfill({ json: products }));
  await page.goto('/');
  await page.getByPlaceholder('Escanea o busca un producto...').fill('Producto');
  await expect(page.getByText('Producto A', { exact: true })).toBeVisible();
}
async function add(page: Page, name: string) {
  await page.getByPlaceholder('Escanea o busca un producto...').fill(name);
  await page.getByText(name, { exact: true }).click();
  await expect(page.locator('tbody tr').filter({ hasText: name })).toBeVisible();
}

test('agregar, cambiar cantidad, eliminar y recargar conservan el carrito', async ({ page }) => {
  await prepare(page);
  await add(page, 'Producto A');
  await add(page, 'Producto B');
  const row = page.locator('tbody tr').filter({ hasText: 'Producto A' });
  await row.getByRole('spinbutton').fill('3');
  await expect(row.getByRole('spinbutton')).toHaveValue('3');
  await page.locator('tbody tr').filter({ hasText: 'Producto B' }).getByTitle('Eliminar producto').click();
  await expect(page.locator('tbody tr')).toHaveCount(1);
  await page.reload();
  await expect(row.getByRole('spinbutton')).toHaveValue('3');
  await expect(page.locator('tbody tr')).toHaveCount(1);
});

test('el flujo obtiene IVA y totales de una respuesta real de MS-5', async ({ page }) => {
  const responses: any[] = [];
  page.on('response', async response => {
    if (/\/ventas(?:\/|\?|$)/.test(response.url()) && response.ok()) {
      try { responses.push(await response.json()); } catch { /* no JSON */ }
    }
  });
  await prepare(page);
  await add(page, 'Producto A');
  await add(page, 'Producto B');
  await page.locator('tbody tr').filter({ hasText: 'Producto A' }).getByRole('spinbutton').fill('3');
  await page.locator('tbody tr').filter({ hasText: 'Producto B' }).getByTitle('Eliminar producto').click();
  await page.reload();
  await expect(page.locator('tbody tr')).toHaveCount(1);
  // No sustituir MS-5 por un mock: la ausencia de respuesta incumple aceptación.
  await expect.poll(() => responses.length, { timeout: 3000, message: 'El carrito debe recibir totales desde MS-5' }).toBeGreaterThan(0);
  const sale = responses.at(-1);
  for (const [label, value] of [['Subtotal:', sale.subtotal], [/^IVA/, sale.impuestos ?? sale.iva], ['TOTAL:', sale.total]] as const) {
    expect(typeof value).toBe('number');
    await expect(page.getByText(label, { exact: typeof label === 'string' }).locator('..').locator('span').last())
      .toHaveText(`$ ${value.toLocaleString('es-CL')}`);
  }
});

test('cambiar de tenant en el mismo navegador no recupera el carrito ajeno', async ({ page }) => {
  await prepare(page);
  await add(page, 'Producto A');
  await page.evaluate(() => sessionStorage.setItem('tenant', 'tenant-b'));
  await page.reload();
  await expect(page.getByText('Carrito vacío', { exact: true })).toBeVisible();
  await expect(page.locator('tbody tr')).toHaveCount(0);
});

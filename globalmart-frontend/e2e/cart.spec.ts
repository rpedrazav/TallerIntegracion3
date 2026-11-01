import { test, expect, Page } from '@playwright/test';

const products = [
  { id: '11111111-1111-1111-1111-111111111111', nombre: 'Producto A', precioBase: 100, codigoBarras: '111' },
  { id: '22222222-2222-2222-2222-222222222222', nombre: 'Producto B', precioBase: 50, codigoBarras: '222' }
];
// MS-5 controlado por HTTP para la regresión del renderer; integración real en .NET/Electron.
async function prepare(page: Page) {
  await page.addInitScript(() => {
    (window as any).api = { getToken: async () => [btoa('{}'), btoa(JSON.stringify({ sub: 'cashier', tenant_id: sessionStorage.getItem('tenant') || 'tenant-a' })), ''].join('.') };
  });
  const sales = new Map<string, any>();
  await page.route('**/products**', route => route.fulfill({ json: { data: products, totalCount: products.length } }));
  await page.route('**/api/turnos/activo', route => route.fulfill({ json: { id: 'shift-1', estado: 'ABIERTO' } }));
  await page.route('**/ventas**', async route => {
    const owner = await page.evaluate(() => sessionStorage.getItem('tenant') || 'tenant-a');
    const url = new URL(route.request().url());
    const method = route.request().method();
    let sale = sales.get(owner);
    if (url.pathname.includes('/ventas/turno/')) return route.fulfill({ json: sale ? [{ id: sale.id, estado: sale.estado }] : [] });
    if (method === 'POST' && url.pathname.endsWith('/ventas')) {
      sale = { id: 'sale-1', turno_id: 'shift-1', estado: 'PENDIENTE', metodo_pago: 'EFECTIVO', items: [] };
      sales.set(owner, sale);
      for (const input of route.request().postDataJSON().items) append(input);
    } else if (method === 'POST') append(route.request().postDataJSON());
    else if (method === 'PUT') {
      const item = sale.items.find((item: any) => url.pathname.endsWith('/' + item.id));
      item.cantidad = route.request().postDataJSON().cantidad;
      item.subtotal = item.cantidad * item.precio_unitario;
    } else if (method === 'DELETE') sale.items = sale.items.filter((item: any) => !url.pathname.endsWith('/' + item.id));
    function append(input: any) {
      const product = products.find(product => product.id === input.producto_id)!;
      const cantidad = input.cantidad ?? 1;
      sale.items.push({ id: product.id, producto_id: product.id, nombre_producto: product.nombre, cantidad, precio_unitario: product.precioBase, subtotal: cantidad * product.precioBase });
    }
    if (sale) { sale.subtotal = sale.items.reduce((sum: number, item: any) => sum + item.subtotal, 0); sale.impuestos = sale.subtotal * 0.2; sale.total = sale.subtotal + sale.impuestos; }
    return route.fulfill({ status: method === 'DELETE' ? 204 : 200, ...(method === 'DELETE' ? {} : { json: sale }) });
  });
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

test('el flujo obtiene IVA y totales de la respuesta HTTP de MS-5 (fixture)', async ({ page }) => {
  const responses: any[] = [];
  page.on('response', async response => {
    if (/\/ventas(?:\/|\?|$)/.test(response.url()) && response.ok()) {
      try { const data = await response.json(); if (typeof data.subtotal === 'number') responses.push(data); } catch { /* no JSON */ }
    }
  });
  await prepare(page);
  await add(page, 'Producto A');
  await add(page, 'Producto B');
  await page.locator('tbody tr').filter({ hasText: 'Producto A' }).getByRole('spinbutton').fill('3');
  await page.locator('tbody tr').filter({ hasText: 'Producto B' }).getByTitle('Eliminar producto').click();
  await page.reload();
  await expect(page.locator('tbody tr')).toHaveCount(1);
  // La tasa de fixture (20 %) comprueba que el renderer respeta los importes del servidor.
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

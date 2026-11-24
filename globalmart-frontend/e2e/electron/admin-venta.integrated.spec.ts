// S2-112: camino principal contra los servicios reales (MS-1..MS-5 en localhost, sin mocks).
// Requiere docker compose + run-apis levantados. Ver e2e/electron/README.md.
import { test, expect, api, tokenOf, login, snap, MS5 } from './integrated-fixture';

const run = Date.now().toString();
const product = { name: `E2E S2-112 ${run}`, barcode: `780112${run.slice(-7)}`, price: 1025 }; // 2 × 1.025 = 2.050; IVA 389,5 → 390 (CLP sin centavos, ,5 sube)
const fondo = 20000;

interface Sale { id: string; subtotal: number; impuestos: number; total: number; estado: string; items: { subtotal: number }[] }

test.describe.configure({ mode: 'serial', timeout: 90000 });

test('@integrado ADMIN entra al panel y crea un producto en MS-3', async ({ app: page }) => {
  await login(page, 'admin@demo.cl');
  await expect(page).toHaveURL(/#\/admin$/);
  await page.goto(page.url().replace(/#.*$/, '#/admin/productos'));
  await expect(page.getByRole('heading', { name: 'Catálogo de Productos' })).toBeVisible();
  await page.getByRole('button', { name: /Nuevo Producto/ }).click();
  await page.locator('#modal-nombre').fill(product.name);
  await page.locator('#modal-barras').fill(product.barcode);
  await page.locator('#modal-precio').fill(String(product.price));
  const created = page.waitForResponse(r => r.request().method() === 'POST' && r.url().endsWith('/products'));
  await page.getByRole('button', { name: /Crear Producto/ }).click();
  expect((await created).status()).toBe(201);
  await expect(page.locator('tbody tr').filter({ hasText: product.name })).toBeVisible();
  await snap(page, '01-admin-producto-creado');
});

test('@integrado CAJERO abre turno, vende en efectivo y cierra sin totales divergentes', async ({ app: page }) => {
  // Precondición: el cajero demo empieza sin turno abierto (una corrida anterior pudo dejarlo abierto).
  const cajero = await tokenOf('cajero@demo.cl');
  if ((await api(`${MS5}/api/turnos/activo`, cajero)).status === 200) {
    await api(`${MS5}/api/turnos/cuadre`, cajero, { method: 'POST', body: JSON.stringify({ monto_declarado: 0 }) });
    expect((await api(`${MS5}/api/turnos/cerrar`, cajero, { method: 'POST' })).status).toBe(200);
  }

  await login(page, 'cajero@demo.cl');
  await expect(page.getByRole('heading', { name: 'Apertura de Turno de Caja' })).toBeVisible();
  await page.getByLabel('Monto inicial de caja').fill(String(fondo));
  await page.getByRole('button', { name: 'Abrir Turno', exact: true }).click();
  await expect(page.getByText('Carrito vacío', { exact: true })).toBeVisible({ timeout: 10000 });
  await snap(page, '02-turno-abierto');

  // Carrito: el producto creado por ADMIN, cantidad 2.
  const sales: Sale[] = [];
  page.on('response', async r => {
    if (/\/ventas(\/[^/]+)?(\/cobrar)?$/.test(r.url()) && r.ok()) {
      const data = await r.json().catch(() => null);
      const sale = data?.venta ?? data;
      if (typeof sale?.total === 'number') sales.push(sale);
    }
  });
  await page.getByPlaceholder('Escanea o busca un producto...').fill(product.name);
  await page.getByText(product.name, { exact: true }).click();
  const row = page.locator('tbody tr').filter({ hasText: product.name });
  await expect(row).toBeVisible();
  await page.getByRole('button', { name: 'INICIAR VENTA' }).click();
  await expect(page.getByRole('button', { name: 'COBRAR' })).toBeVisible();
  await row.getByRole('spinbutton').fill('2');
  await row.getByRole('spinbutton').press('Enter');
  await expect(row.getByRole('spinbutton')).toHaveValue('2');
  await expect(page.getByText('Sincronizando ítems con MS-5...')).toBeHidden();

  // Totales: la pantalla debe mostrar exactamente lo que devuelve MS-5 y ser coherente con el precio.
  await expect.poll(() => sales.at(-1)?.items.length ? sales.at(-1)!.subtotal : 0).toBeGreaterThan(product.price);
  const sale = sales.at(-1)!;
  expect(sale.items.reduce((s, i) => s + i.subtotal, 0)).toBe(sale.subtotal);
  expect(sale.subtotal + sale.impuestos).toBe(sale.total);
  expect([sale.subtotal, sale.impuestos, sale.total], 'CLP sin centavos').toEqual([2050, 390, 2440]);
  for (const [label, value] of [['Subtotal:', sale.subtotal], ['Impuestos:', sale.impuestos], ['TOTAL:', sale.total]] as const)
    await expect(page.getByText(label, { exact: true }).locator('..').locator('span').last()).toHaveText(`$ ${value.toLocaleString('es-CL')}`);
  await snap(page, '03-carrito-totales');

  // Cobro en efectivo con vuelto calculado por MS-5.
  const recibido = Math.ceil(sale.total / 1000) * 1000 + 1000;
  await page.getByRole('button', { name: 'COBRAR' }).click();
  await page.getByLabel('Monto recibido').fill(String(recibido));
  const charged = page.waitForResponse(r => r.url().endsWith(`/ventas/${sale.id}/cobrar`));
  await page.getByRole('button', { name: 'Confirmar efectivo' }).click();
  const cobro = await (await charged).json();
  expect(cobro.venta.estado).toBe('COMPLETADA');
  expect(cobro.venta.total).toBe(sale.total);
  expect(cobro.vuelto).toBe(recibido - sale.total);
  await expect(page.getByRole('dialog')).toContainText(`Vuelto calculado por MS-5: $ ${cobro.vuelto.toLocaleString('es-CL')}`);
  await snap(page, '04-cobro-efectivo');
  await page.getByRole('button', { name: 'Cerrar cobro' }).click();
  await expect(page.getByText('Carrito vacío', { exact: true })).toBeVisible();

  // La venta persistida en MS-5 coincide con lo cobrado.
  const persisted = (await api(`${MS5}/ventas/${sale.id}`, cajero)).body as Sale;
  expect([persisted.estado, persisted.subtotal, persisted.impuestos, persisted.total]).toEqual(['COMPLETADA', sale.subtotal, sale.impuestos, sale.total]);

  // Cierre: MS-5 define efectivo_esperado como ventas en efectivo netas de vuelto (sin fondo inicial).
  // Se declara exactamente eso; el cuadre debe dar diferencia 0.
  await page.goto(page.url().replace(/#.*$/, '#/cerrar-turno'));
  await expect(page.getByRole('heading', { name: 'Cierre de Turno y Cuadre de Caja' })).toBeVisible();
  const esperado = sale.total;
  const billetes = Math.floor(esperado / 1000);
  await page.locator('#input-denominacion-b_1000').fill(String(billetes));
  let resto = esperado - billetes * 1000;
  for (const [id, valor] of [['m_500', 500], ['m_100', 100], ['m_50', 50], ['m_10', 10]] as const) {
    const n = Math.floor(resto / valor);
    if (n) await page.locator(`#input-denominacion-${id}`).fill(String(n));
    resto -= n * valor;
  }
  expect(resto, 'El total debe poder declararse con denominaciones CLP').toBe(0);
  const cuadreRes = page.waitForResponse(r => r.url().endsWith('/api/turnos/cuadre'));
  await page.getByRole('button', { name: /Calcular Cuadre/ }).click();
  const cuadre = await (await cuadreRes).json();
  expect([cuadre.efectivo_esperado, cuadre.monto_declarado, cuadre.diferencia]).toEqual([esperado, esperado, 0]);
  await snap(page, '05-cuadre');
  const closed = page.waitForResponse(r => r.url().endsWith('/api/turnos/cerrar'));
  await page.getByRole('button', { name: /Confirmar y Cerrar Turno/ }).click();
  await page.getByRole('button', { name: 'Confirmar cierre' }).click();
  expect((await closed).status()).toBe(200);
  await expect(page.getByText('¡Turno cerrado exitosamente!')).toBeVisible();
  await snap(page, '06-turno-cerrado');
  expect((await api(`${MS5}/api/turnos/activo`, cajero)).status).toBe(404);
});

import { expect, Page, test } from '@playwright/test';

interface TestProduct {
  id: string;
  nombre: string;
  descripcion: string | null;
  codigoBarras: string;
  categoriaId: string | null;
  precioBase: number;
  esPesoVariable: boolean;
  isActive: boolean;
  createdAt: string;
}

const adminToken = [
  Buffer.from(JSON.stringify({ alg: 'none', typ: 'JWT' })).toString('base64url'),
  Buffer.from(JSON.stringify({ tenant_id: 'tenant-a', active_role: 'ADMIN', roles: ['ADMIN'] })).toString('base64url'),
  '',
].join('.');

const category = { id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', nombre: 'Abarrotes' };
const initialProduct: TestProduct = {
  id: '11111111-1111-1111-1111-111111111111',
  nombre: 'Arroz',
  descripcion: 'Arroz grado 1',
  codigoBarras: '780000000001',
  categoriaId: category.id,
  precioBase: 1500,
  esPesoVariable: false,
  isActive: true,
  createdAt: '2026-11-11T12:00:00Z',
};

async function prepareSession(page: Page) {
  await page.addInitScript(token => {
    window.api = {
      ping: async () => 'pong',
      getToken: async () => token,
      setToken: async () => undefined,
      logout: async () => undefined,
    };
  }, adminToken);
}

async function openProducts(page: Page) {
  await prepareSession(page);
  await page.goto('/?component=app#/admin/productos');
  await expect(page.getByRole('heading', { name: 'Catálogo de Productos' })).toBeVisible();
}

async function mockCategories(page: Page) {
  await page.route('**/categories**', route => route.fulfill({ json: [category] }));
}

async function fillProductForm(page: Page, values: { name: string; barcode: string; price: string }) {
  await page.locator('#modal-nombre').fill(values.name);
  await page.locator('#modal-barras').fill(values.barcode);
  await page.locator('#modal-precio').fill(values.price);
  await page.locator('#modal-categoria').selectOption(category.id);
}

test('crear, editar, filtrar y desactivar actualiza la tabla sin recarga manual', async ({ page }) => {
  let products = [initialProduct];
  let postCount = 0;
  let putCount = 0;
  let deleteCount = 0;

  await mockCategories(page);
  await page.route('**/products**', async route => {
    const request = route.request();
    const path = new URL(request.url()).pathname;
    if (request.method() === 'GET') {
      await route.fulfill({ json: { data: products, totalCount: products.length, page: 1, pageSize: 500 } });
      return;
    }
    if (request.method() === 'POST' && path.endsWith('/products')) {
      postCount += 1;
      const body = request.postDataJSON();
      const created: TestProduct = {
        ...initialProduct,
        ...body,
        id: '22222222-2222-2222-2222-222222222222',
        createdAt: '2026-11-11T12:05:00Z',
      };
      products = [...products, created];
      await route.fulfill({ status: 201, json: created });
      return;
    }
    if (request.method() === 'PUT') {
      putCount += 1;
      const id = path.split('/').at(-1);
      const body = request.postDataJSON();
      products = products.map(product => product.id === id ? { ...product, ...body } : product);
      await route.fulfill({ json: products.find(product => product.id === id) });
      return;
    }
    if (request.method() === 'DELETE') {
      deleteCount += 1;
      const id = path.split('/').at(-1);
      products = products.map(product => product.id === id ? { ...product, isActive: false } : product);
      await route.fulfill({ status: 204 });
      return;
    }
    await route.abort();
  });

  await openProducts(page);
  await page.locator('#btn-nuevo-producto').click();
  await fillProductForm(page, { name: 'Leche', barcode: '780000000002', price: '1200' });
  await page.locator('#btn-guardar-modal-producto').click();
  await expect(page.getByText('Leche', { exact: true })).toBeVisible();
  await expect(page.locator('tbody tr')).toHaveCount(2);
  expect(postCount).toBe(1);

  const createdId = '22222222-2222-2222-2222-222222222222';
  await page.locator(`#btn-editar-producto-${createdId}`).click();
  await page.locator('#modal-nombre').fill('Leche editada');
  await page.locator('#modal-barras').fill('780000009999');
  await page.locator('#btn-guardar-modal-producto').click();
  const editedRow = page.locator('tbody tr').filter({ hasText: 'Leche editada' });
  await expect(editedRow).toContainText('780000009999');
  await expect(page.locator('tbody tr')).toHaveCount(2);
  expect(putCount).toBe(1);

  await page.locator('#input-busqueda-productos').fill('Leche editada');
  await expect(page.locator('tbody tr')).toHaveCount(1);
  await page.locator('#input-busqueda-productos').fill('');
  await page.locator('#select-categoria-filtro').selectOption(category.id);
  await expect(page.locator('tbody tr')).toHaveCount(2);
  await page.locator('#select-categoria-filtro').selectOption('');

  page.once('dialog', dialog => dialog.accept());
  await page.locator(`#btn-eliminar-producto-${createdId}`).click();
  await expect(editedRow.getByText('Inactivo', { exact: true })).toBeVisible();
  await expect(page.locator('tbody tr')).toHaveCount(2);
  expect(deleteCount).toBe(1);

  await page.locator('#toggle-solo-activos').check();
  await expect(page.locator('tbody tr')).toHaveCount(1);
  await expect(page.getByText('Leche editada', { exact: true })).toHaveCount(0);
});

test('precio cero se rechaza en React sin enviar POST y conserva el formulario', async ({ page }) => {
  let postCount = 0;
  await mockCategories(page);
  await page.route('**/products**', async route => {
    if (route.request().method() === 'GET') {
      await route.fulfill({ json: { data: [initialProduct], totalCount: 1 } });
      return;
    }
    postCount += 1;
    await route.abort();
  });

  await openProducts(page);
  await page.locator('#btn-nuevo-producto').click();
  await fillProductForm(page, { name: 'Producto sin precio', barcode: '780000000003', price: '0' });
  await page.locator('#btn-guardar-modal-producto').click();

  await expect(page.getByRole('alert')).toContainText('El precio debe ser un número válido mayor que 0.');
  await expect(page.locator('#modal-nombre')).toHaveValue('Producto sin precio');
  expect(postCount).toBe(0);
});

for (const failure of [
  {
    name: '400 FluentValidation',
    status: 400,
    body: [{ propertyName: 'CodigoBarras', errorMessage: 'El código de barras ya existe en su catálogo.' }],
    expected: 'El código de barras ya existe en su catálogo.',
  },
  { name: '401', status: 401, body: {}, expected: 'Tu sesión no es válida o expiró.' },
  { name: '403', status: 403, body: {}, expected: 'No tienes permisos para guardar productos.' },
  { name: '404', status: 404, body: {}, expected: 'El producto no existe o pertenece a otro tenant.' },
]) {
  test(`guardar explica ${failure.name} y conserva los datos`, async ({ page }) => {
    await mockCategories(page);
    await page.route('**/products**', async route => {
      if (route.request().method() === 'GET') {
        await route.fulfill({ json: { data: [initialProduct], totalCount: 1 } });
        return;
      }
      await route.fulfill({ status: failure.status, json: failure.body });
    });

    await openProducts(page);
    await page.locator(`#btn-editar-producto-${initialProduct.id}`).click();
    await page.locator('#modal-nombre').fill('Arroz editado');
    await page.locator('#btn-guardar-modal-producto').click();

    await expect(page.getByRole('alert')).toContainText(failure.expected);
    await expect(page.locator('#modal-nombre')).toHaveValue('Arroz editado');
    await expect(page.locator('#modal-nombre')).toBeVisible();
  });
}

test('fallo de red al guardar se explica y conserva los datos', async ({ page }) => {
  await mockCategories(page);
  await page.route('**/products**', async route => {
    if (route.request().method() === 'GET') {
      await route.fulfill({ json: { data: [initialProduct], totalCount: 1 } });
      return;
    }
    await route.abort('connectionrefused');
  });

  await openProducts(page);
  await page.locator(`#btn-editar-producto-${initialProduct.id}`).click();
  await page.locator('#modal-nombre').fill('Arroz sin conexión');
  await page.locator('#btn-guardar-modal-producto').click();

  await expect(page.getByRole('alert')).toContainText('No se pudo conectar con el servicio de catálogo');
  await expect(page.locator('#modal-nombre')).toHaveValue('Arroz sin conexión');
});

test('desactivación 403 conserva la fila activa y muestra el motivo', async ({ page }) => {
  await mockCategories(page);
  await page.route('**/products**', async route => {
    if (route.request().method() === 'GET') {
      await route.fulfill({ json: { data: [initialProduct], totalCount: 1 } });
      return;
    }
    await route.fulfill({ status: 403, json: {} });
  });

  await openProducts(page);
  page.once('dialog', dialog => dialog.accept());
  await page.locator(`#btn-eliminar-producto-${initialProduct.id}`).click();

  await expect(page.getByRole('alert')).toContainText('No tienes permisos para desactivar productos.');
  const row = page.locator('tbody tr').filter({ hasText: initialProduct.nombre });
  await expect(row.getByText('Activo', { exact: true })).toBeVisible();
  await expect(row).toHaveCount(1);
});

test('modal producto contiene foco, cierra con Escape, devuelve foco y envía con Enter una sola vez', async ({ page }) => {
  let products = [initialProduct];
  let postCount = 0;
  await mockCategories(page);
  await page.route('**/products**', async route => {
    if (route.request().method() === 'GET') {
      await route.fulfill({ json: { data: products, totalCount: products.length } });
      return;
    }
    if (route.request().method() === 'POST') {
      postCount += 1;
      const created = {
        ...initialProduct,
        ...route.request().postDataJSON(),
        id: '33333333-3333-3333-3333-333333333333',
        createdAt: '2026-11-11T13:00:00Z',
      };
      products = [...products, created];
      await route.fulfill({ status: 201, json: created });
      return;
    }
    await route.abort();
  });

  await openProducts(page);
  const opener = page.locator('#btn-nuevo-producto');
  await opener.click();
  let modal = page.getByRole('dialog', { name: 'Nuevo Producto' });
  let first = modal.locator('#modal-nombre');
  const boundaryFirst = modal.locator('#btn-cerrar-modal-producto');
  let last = modal.locator('#btn-guardar-modal-producto');
  await expect(first).toBeFocused();

  await boundaryFirst.focus();
  await boundaryFirst.press('Shift+Tab');
  await expect(last).toBeFocused();
  await last.press('Tab');
  await expect(boundaryFirst).toBeFocused();
  await first.focus();
  await first.press('Escape');
  await expect(modal).toBeHidden();
  await expect(opener).toBeFocused();

  await opener.click();
  modal = page.getByRole('dialog', { name: 'Nuevo Producto' });
  first = modal.locator('#modal-nombre');
  last = modal.locator('#btn-guardar-modal-producto');
  await fillProductForm(page, { name: 'Café', barcode: '780000000004', price: '0' });
  await modal.locator('#modal-precio').press('Enter');
  const price = modal.locator('#modal-precio');
  await expect(price).toHaveAttribute('aria-invalid', 'true');
  await expect(price).toHaveAttribute('aria-describedby', 'producto-modal-error');
  await expect(modal.locator('#producto-modal-error')).toContainText('mayor que 0');
  expect(postCount).toBe(0);

  await price.fill('1900');
  await price.press('Enter');
  await expect(modal).toBeHidden();
  await expect(page.getByText('Café', { exact: true })).toBeVisible();
  expect(postCount).toBe(1);
});

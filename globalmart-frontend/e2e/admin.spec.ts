import { expect, test } from '@playwright/test';

const tenantId = 'aaaaaaaa-0000-0000-0000-000000000001';
const token = [
  btoa(JSON.stringify({ alg: 'none', typ: 'JWT' })),
  btoa(JSON.stringify({ sub: '11111111-1111-1111-1111-111111111111', tenant_id: tenantId, active_role: 'ADMIN', roles: ['ADMIN'] })),
  '',
].join('.');

test.beforeEach(async ({ page }) => {
  await page.addInitScript((jwt: string) => {
    const bridge = { getToken: async () => jwt, setToken: async () => undefined, logout: async () => undefined };
    Object.defineProperty(window, 'api', { value: bridge });
  }, token);
  await page.route('**/sucursales', route => route.fulfill({ json: [{ id: '22222222-2222-2222-2222-222222222222', nombre: 'Centro', activa: true, direccion: 'Calle 1', zonaHoraria: 'America/Santiago' }] }));
});

test('ADMIN abre vista propia y crea un cajero con datos copiables', async ({ page }) => {
  let createBody: Record<string, unknown> | undefined;
  await page.route('**/users*', async route => {
    const request = route.request();
    if (request.method() === 'POST' && new URL(request.url()).pathname.endsWith('/users')) {
      createBody = request.postDataJSON();
      await route.fulfill({ status: 201, json: { id: '33333333-3333-3333-3333-333333333333', roles: ['CAJERO'] } });
      return;
    }
    await route.fulfill({ json: { items: [], totalPages: 1 } });
  });

  await page.goto('/?component=app#/');
  await expect(page).toHaveURL(/#\/admin$/);
  await expect(page.getByRole('heading', { name: 'GlobalMart Admin' })).toBeVisible();
  await expect(page.getByText('Punto de Venta', { exact: true })).toHaveCount(0);
  await page.getByRole('link', { name: 'Usuarios', exact: true }).click();
  await page.getByRole('button', { name: 'Crear usuario' }).click();
  await page.getByLabel('Nombre', { exact: true }).fill('Cajero Nuevo');
  await page.getByLabel('Correo electrónico').fill('nuevo@example.cl');
  await page.getByLabel('Sucursal inicial').selectOption('22222222-2222-2222-2222-222222222222');
  await page.getByRole('dialog').getByRole('button', { name: 'Crear usuario' }).click();

  await expect.poll(() => createBody?.tenantId).toBe(tenantId);
  expect(createBody?.email).toBe('nuevo@example.cl');
  expect(createBody?.roles).toEqual(['CAJERO']);
  expect(createBody?.sucursalId).toBe('22222222-2222-2222-2222-222222222222');
  await expect(page.getByLabel('Datos de acceso')).toContainText('nuevo@example.cl');
  await expect(page.getByLabel('Datos de acceso')).toContainText('Contraseña inicial:');
});

test('inventario consulta y registra un ajuste', async ({ page }) => {
  let adjustment: Record<string, unknown> | undefined;
  await page.route('**/products?*', route => route.fulfill({ json: { data: [{ id: '44444444-4444-4444-4444-444444444444', nombre: 'Arroz' }], totalCount: 1, page: 1, pageSize: 500 } }));
  await page.route('**/stock/ajuste', async route => {
    adjustment = route.request().postDataJSON();
    await route.fulfill({ json: { ok: true } });
  });
  await page.route('**/stock?*', route => route.fulfill({ json: [] }));

  await page.goto('/?component=app#/admin/inventario');
  await expect(page.getByRole('heading', { name: 'Inventario por sucursal' })).toBeVisible();
  await page.getByLabel('Cantidad actual').fill('12.5');
  await page.getByLabel('Stock mínimo').fill('2');
  await page.getByLabel('Motivo').fill('Conteo inicial');
  await page.getByRole('button', { name: 'Guardar ajuste' }).click();
  await expect.poll(() => adjustment?.cantidadActual).toBe(12.5);
  expect(adjustment?.productoId).toBe('44444444-4444-4444-4444-444444444444');
  expect(adjustment?.sucursalId).toBe('22222222-2222-2222-2222-222222222222');
});

test('inventario muestra productos de páginas posteriores del catálogo', async ({ page }) => {
  await page.route('**/products?*', route => {
    const pageNumber = new URL(route.request().url()).searchParams.get('page');
    const product = pageNumber === '1'
      ? { id: '44444444-4444-4444-4444-444444444444', nombre: 'Arroz' }
      : { id: '55555555-5555-5555-5555-555555555555', nombre: 'Leche' };
    return route.fulfill({ json: { data: [product], totalCount: 2, page: Number(pageNumber), pageSize: 1 } });
  });
  await page.route('**/stock?*', route => route.fulfill({ json: [] }));

  await page.goto('/?component=app#/admin/inventario');
  await expect(page.getByLabel('Producto')).toContainText('Leche');
});

test('ADMIN crea y elimina un rol propio del tenant', async ({ page }) => {
  const roles = [
    { id: '11111111-0000-0000-0000-000000000001', nombre: 'CAJERO', descripcion: 'Caja', tenantId: null, rolBase: null },
    { id: '11111111-0000-0000-0000-000000000003', nombre: 'ADMIN', descripcion: 'Admin', tenantId: null, rolBase: null },
  ];
  await page.route('**/users*', route => route.fulfill({ json: { items: [], totalPages: 1 } }));
  await page.route('**/users/roles**', async route => {
    if (route.request().method() === 'POST') {
      const body = route.request().postDataJSON();
      roles.push({ id: '66666666-6666-6666-6666-666666666666', nombre: body.nombre, descripcion: body.descripcion, tenantId, rolBase: body.rolBase });
      return route.fulfill({ status: 201, json: roles.at(-1) });
    }
    if (route.request().method() === 'DELETE') {
      roles.pop();
      return route.fulfill({ status: 204 });
    }
    return route.fulfill({ json: roles });
  });

  await page.goto('/?component=app#/admin/roles');
  await page.getByLabel('Nombre del rol').fill('SUPERVISOR_CAJA');
  await page.getByLabel('Descripción').fill('Supervisa caja');
  await page.getByLabel('Permisos base').selectOption('ADMIN');
  await page.getByRole('button', { name: 'Crear rol' }).click();
  await expect(page.getByText('SUPERVISOR_CAJA', { exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Eliminar rol' }).click();
  await expect(page.getByText('SUPERVISOR_CAJA', { exact: true })).toHaveCount(0);
});

test('ADMIN asigna un rol propio al crear un usuario', async ({ page }) => {
  let createBody: Record<string, unknown> | undefined;
  await page.route('**/users*', route => {
    if (route.request().method() === 'POST') {
      createBody = route.request().postDataJSON();
      return route.fulfill({ status: 201, json: { id: '77777777-7777-7777-7777-777777777777', roles: ['SUPERVISOR_CAJA'] } });
    }
    return route.fulfill({ json: { items: [], totalPages: 1 } });
  });
  await page.route('**/users/roles', route => route.fulfill({ json: [
    { id: '11111111-0000-0000-0000-000000000001', nombre: 'CAJERO' },
    { id: '66666666-6666-6666-6666-666666666666', nombre: 'SUPERVISOR_CAJA' },
  ] }));

  await page.goto('/?component=app#/admin/usuarios');
  await page.getByRole('button', { name: 'Crear usuario' }).click();
  await page.getByLabel('Nombre', { exact: true }).fill('Supervisor Nuevo');
  await page.getByLabel('Correo electrónico').fill('supervisor@example.cl');
  await page.getByLabel('Rol', { exact: true }).selectOption('SUPERVISOR_CAJA');
  await page.getByRole('dialog').getByRole('button', { name: 'Crear usuario' }).click();
  await expect.poll(() => createBody?.roles).toEqual(['SUPERVISOR_CAJA']);
});

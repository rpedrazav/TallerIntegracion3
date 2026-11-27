import { expect, test, Page } from '@playwright/test';

// S3-098: pantalla de minimarkets solo para SUPER_ADMIN; MS-1 se simula con page.route.
const crearToken = (activeRole: string) => [
  Buffer.from(JSON.stringify({ alg: 'none', typ: 'JWT' }), 'utf8').toString('base64url'),
  Buffer.from(JSON.stringify({
    sub: '11111111-1111-1111-1111-111111111111',
    tenant_id: 'aaaaaaaa-0000-0000-0000-000000000099',
    tenant_nombre: 'GlobalMart OS',
    active_role: activeRole,
    roles: [activeRole],
  }), 'utf8').toString('base64url'),
  '',
].join('.');

async function abrirComo(page: Page, activeRole: string, ruta: string) {
  await page.addInitScript((jwt: string) => {
    const bridge = { getToken: async () => jwt, setToken: async () => undefined, logout: async () => undefined };
    Object.defineProperty(window, 'api', { value: bridge });
  }, crearToken(activeRole));
  await page.goto(`/?component=app#${ruta}`);
}

async function completarFormulario(page: Page, codigo: string) {
  await page.getByLabel('Nombre', { exact: true }).fill('Minimarket Don Pepe');
  await page.getByLabel('Código de acceso').fill(codigo);
  await page.getByLabel('Correo del administrador').fill('admin@donpepe.cl');
  await page.getByLabel('Contraseña inicial del administrador').fill('Gm7!Alta2026');
  await page.getByRole('button', { name: 'Crear minimarket' }).click();
}

test('SUPER_ADMIN entra a Minimarkets, lista y crea un minimarket con su ADMIN', async ({ page }) => {
  const minimarkets = [{ id: '1', nombre: 'Minimarket Demo', codigo: 'demo', activo: true, creadoEn: '2026-11-27T12:00:00Z' }];
  let body: unknown;
  await page.route('**/api/tenants', async route => {
    if (route.request().method() === 'POST') {
      body = route.request().postDataJSON();
      const nuevo = { id: '2', nombre: 'Minimarket Don Pepe', codigo: 'donpepe', activo: true, creadoEn: '2026-11-27T13:00:00Z' };
      minimarkets.push(nuevo);
      return route.fulfill({ status: 201, json: nuevo });
    }
    return route.fulfill({ json: minimarkets });
  });

  await abrirComo(page, 'SUPER_ADMIN', '/');

  await expect(page).toHaveURL(/#\/admin\/minimarkets$/);
  await expect(page.getByText('GlobalMart OS')).toBeVisible();
  await expect(page.getByRole('link', { name: 'Minimarkets' })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Usuarios', exact: true })).toHaveCount(0);
  await expect(page.getByRole('cell', { name: 'demo', exact: true })).toBeVisible();

  await completarFormulario(page, 'donpepe');

  await expect(page.getByRole('status')).toContainText('«donpepe»');
  await expect(page.getByRole('cell', { name: 'donpepe', exact: true })).toBeVisible();
  expect(body).toEqual({ nombre: 'Minimarket Don Pepe', codigo: 'donpepe', adminEmail: 'admin@donpepe.cl', adminPassword: 'Gm7!Alta2026' });
});

test('código duplicado muestra el mensaje 409 de MS-1', async ({ page }) => {
  await page.route('**/api/tenants', route => route.request().method() === 'POST'
    ? route.fulfill({ status: 409, json: { message: "Ya existe un minimarket con el código 'demo' o con ese nombre." } })
    : route.fulfill({ json: [] }));

  await abrirComo(page, 'SUPER_ADMIN', '/admin/minimarkets');
  await completarFormulario(page, 'demo');

  await expect(page.getByRole('alert')).toContainText("Ya existe un minimarket con el código 'demo'");
});

test('ADMIN de un minimarket no ve la opción Minimarkets', async ({ page }) => {
  await abrirComo(page, 'ADMIN', '/');

  await expect(page).toHaveURL(/#\/admin$/);
  await expect(page.getByRole('link', { name: 'Usuarios', exact: true })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Minimarkets' })).toHaveCount(0);
});

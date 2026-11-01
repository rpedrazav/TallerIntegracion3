import { test as base, expect, _electron, Page } from '@playwright/test';
import * as fs from 'node:fs';
import * as path from 'node:path';

const tenant = 'aaaaaaaa-0000-0000-0000-000000000024';
const branch = 'bbbbbbbb-0000-0000-0000-000000000024';
const shift = 'cccccccc-0000-0000-0000-000000000024';
const credentials = { tenantId: tenant, email: 'cashier@example.test', password: 'dummy-e2e-only' };
const token = [Buffer.from(JSON.stringify({ alg: 'none' })).toString('base64url'),
  Buffer.from(JSON.stringify({ sub: 'e2e-cashier', tenant_id: tenant, sucursal_id: branch, active_role: 'CAJERO', roles: ['CAJERO'] })).toString('base64url'), 'dummy'].join('.');

const test = base.extend<{ pos: Page }>({
  pos: async ({}, use, info) => {
    const profile = info.outputPath('profile');
    fs.mkdirSync(profile, { recursive: true });
    const env: NodeJS.ProcessEnv = { ...process.env, E2E_USER_DATA: profile };
    delete env.ELECTRON_RUN_AS_NODE;
    const app = await _electron.launch({ args: [path.resolve('e2e/electron/launch.cjs')], env });
    const context = app.context();
    const unexpected: string[] = [];
    await context.tracing.start({ screenshots: true, snapshots: true });
    await context.route(/^https?:/, async route => {
      unexpected.push(`${route.request().method()} ${route.request().url()}`);
      await route.abort();
    });
    // The real main opens DevTools; it can be reported before the POS window.
    await expect.poll(() => app.windows().some(window => window.url().startsWith('file:'))).toBe(true);
    const page = app.windows().find(window => window.url().startsWith('file:'))!;
    await app.evaluate(({ BrowserWindow }) => {
      for (const window of BrowserWindow.getAllWindows()) {
        window.webContents.closeDevTools();
        window.setSize(1440, 900);
      }
    });
    try {
      await expect(page.getByRole('button', { name: 'Ingresar', exact: true })).toBeVisible();
      expect(await page.evaluate(() => window.api.getToken())).toBeNull();
      await use(page);
      expect(unexpected, 'No requests may escape the explicit mocks').toEqual([]);
    } finally {
      if (info.status !== info.expectedStatus) {
        await page.screenshot({ path: info.outputPath('failure.png') }).catch(() => undefined);
        await context.tracing.stop({ path: info.outputPath('trace.zip') });
      } else await context.tracing.stop();
      await app.close();
    }
  },
});

async function login(page: Page) {
  await page.locator('input[type=text]').fill(credentials.tenantId);
  await page.locator('input[type=email]').fill(credentials.email);
  await page.locator('input[type=password]').fill(credentials.password);
  await page.getByRole('button', { name: 'Ingresar', exact: true }).click();
}

test('login → apertura de turno → POS con sesión IPC persistente', async ({ pos: page }) => {
  let opened = false;
  let openings = 0;
  let activeChecks = 0;
  await page.route('http://127.0.0.1:4199/**', async route => {
    const req = route.request();
    const url = new URL(req.url());
    if (url.pathname === '/auth/login' && req.method() === 'POST') {
      expect(req.postDataJSON()).toEqual(credentials);
      expect(req.headers()['x-tenant-id']).toBe(tenant);
      return route.fulfill({ json: { token } });
    }
    expect(req.headers().authorization).toBe(`Bearer ${token}`);
    if (url.pathname === '/api/turnos/activo' && req.method() === 'GET') {
      activeChecks++;
      return route.fulfill({ status: opened ? 200 : 404, json: opened ? { id: shift, tenantId: tenant, sucursalId: branch, estado: 0 } : { error: 'Sin turno abierto' } });
    }
    if (url.pathname === '/api/turnos/abrir' && req.method() === 'POST') {
      expect(req.postDataJSON()).toEqual({ sucursal_id: branch, monto_fondo_inicial: 20000 });
      expect(opened).toBe(false);
      opened = true; openings++;
      return route.fulfill({ json: { id: shift } });
    }
    if (url.pathname === `/ventas/turno/${shift}` && req.method() === 'GET') {
      expect(opened).toBe(true);
      return route.fulfill({ json: [] });
    }
    if (url.pathname === '/products' && req.method() === 'GET') {
      return route.fulfill({ json: { data: [], totalCount: 0, page: 1, pageSize: 500 } });
    }
    throw new Error(`Unexpected API: ${req.method()} ${url.pathname}`);
  });
  await login(page);
  await expect(page.getByRole('heading', { name: 'Apertura de Turno de Caja' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'INICIAR VENTA', exact: true })).toHaveCount(0);
  await page.getByLabel('Monto inicial de caja').fill('20000');
  await page.getByRole('button', { name: 'Abrir Turno', exact: true }).click();
  await expect(page.getByText('Carrito vacío', { exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: 'INICIAR VENTA', exact: true })).toBeDisabled();
  expect(openings).toBe(1);
  expect(activeChecks).toBeGreaterThanOrEqual(2);
  await page.reload();
  await expect(page.getByText('Carrito vacío', { exact: true })).toBeVisible();
  expect(await page.evaluate(() => window.api.getToken())).toBe(token);
});

test('credenciales rechazadas mantienen sesión vacía y no entran al POS', async ({ pos: page }) => {
  await page.route('http://127.0.0.1:4199/auth/login', route => route.fulfill({ status: 401, json: { error: 'Credenciales inválidas' } }));
  await login(page);
  await expect(page.getByText('Credenciales o Tenant ID incorrectos.')).toBeVisible();
  expect(await page.evaluate(() => window.api.getToken())).toBeNull();
  await expect(page.getByRole('button', { name: 'INICIAR VENTA', exact: true })).toHaveCount(0);
});

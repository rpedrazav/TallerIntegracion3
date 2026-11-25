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
    // The real main opens DevTools in dev mode; wait for the window to load before interacting.
    await expect.poll(() => app.windows().some(window => window.url().startsWith('file:'))).toBe(true);
    const page = app.windows().find(window => window.url().startsWith('file:'))!;
    await page.waitForLoadState('domcontentloaded');
    await app.evaluate(({ BrowserWindow }) => {
      for (const window of BrowserWindow.getAllWindows()) {
        if (window.webContents.isDevToolsOpened()) {
          window.webContents.closeDevTools();
        }
        window.setSize(1440, 900);
      }
    }).catch(() => undefined);
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
  await page.route('http://localhost:8000/**', async route => {
    const req = route.request();
    const url = new URL(req.url());
    if (url.pathname === '/api/auth/login' && req.method() === 'POST') {
      expect(req.postDataJSON()).toEqual(credentials);
      expect(req.headers()['x-tenant-id']).toBe(tenant);
      return route.fulfill({ json: { token, usuario: { id: 'e2e-cashier', nombre: 'Cajero E2E', email: credentials.email, activeRole: 'CAJERO', roles: ['CAJERO'] } } });
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
    if (url.pathname === `/api/ventas/turno/${shift}` && req.method() === 'GET') {
      expect(opened).toBe(true);
      return route.fulfill({ json: [] });
    }
    if (url.pathname === '/api/products' && req.method() === 'GET') {
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
  await page.route('http://localhost:8000/api/auth/login', route => route.fulfill({ status: 401, json: { error: 'Credenciales inválidas' } }));
  await login(page);
  await expect(page.getByText('Credenciales o Tenant ID incorrectos.')).toBeVisible();
  expect(await page.evaluate(() => window.api.getToken())).toBeNull();
  await expect(page.getByRole('button', { name: 'INICIAR VENTA', exact: true })).toHaveCount(0);
});

test('S3-028: modo local (Kong) completa login, apertura de turno, venta y cobro en efectivo sin /api duplicado', async ({ pos: page }) => {
  let opened = false;
  const product = { id: '44444444-4444-4444-4444-444444444444', nombre: 'Leche Kong 1L', codigoBarras: '780999', precioBase: 1000, esPesoVariable: false };
  const saleId = '11111111-1111-1111-1111-111111111111';
  const baseSale = {
    id: saleId,
    turno_id: shift,
    subtotal: 1000,
    impuestos: 190,
    total: 1190,
    metodo_pago: 'EFECTIVO',
    estado: 'PENDIENTE',
    created_at: '2026-11-17T22:55:00.000Z',
    items: [{ id: '33333333-3333-3333-3333-333333333333', producto_id: product.id, nombre_producto: product.nombre, cantidad: 1, precio_unitario: 1000, subtotal: 1000 }],
  };
  const requestedPaths: string[] = [];

  await page.route('http://localhost:8000/**', async route => {
    const req = route.request();
    const url = new URL(req.url());
    requestedPaths.push(`${req.method()} ${url.pathname}`);
    expect(url.pathname).not.toContain('/api/api/');

    if (url.pathname === '/api/auth/login' && req.method() === 'POST') {
      return route.fulfill({ json: { token, usuario: { id: 'e2e-cashier', nombre: 'Cajero E2E', email: credentials.email, activeRole: 'CAJERO', roles: ['CAJERO'] } } });
    }
    if (url.pathname === '/api/turnos/activo' && req.method() === 'GET') {
      return route.fulfill({ status: opened ? 200 : 404, json: opened ? { id: shift, tenantId: tenant, sucursalId: branch, estado: 0 } : { error: 'Sin turno abierto' } });
    }
    if (url.pathname === '/api/turnos/abrir' && req.method() === 'POST') {
      opened = true;
      return route.fulfill({ json: { id: shift } });
    }
    if (url.pathname === `/api/ventas/turno/${shift}` && req.method() === 'GET') {
      return route.fulfill({ json: [] });
    }
    if (url.pathname === '/api/products/search' && req.method() === 'GET') {
      return route.fulfill({ json: { data: [product], totalCount: 1, page: 1, pageSize: 15 } });
    }
    if (url.pathname === '/api/ventas' && req.method() === 'POST') {
      return route.fulfill({ status: 201, json: baseSale });
    }
    if (url.pathname === `/api/ventas/${saleId}/cobrar` && req.method() === 'POST') {
      return route.fulfill({ json: { venta: { ...baseSale, estado: 'COMPLETADA' }, montoRecibido: 2000, vuelto: 810 } });
    }
    throw new Error(`Unexpected API in local mode: ${req.method()} ${url.pathname}`);
  });

  await login(page);
  await page.getByLabel('Monto inicial de caja').fill('20000');
  await page.getByRole('button', { name: 'Abrir Turno', exact: true }).click();
  await expect(page.getByText('Carrito vacío', { exact: true })).toBeVisible();

  await page.getByPlaceholder('Escanea o busca un producto...').fill('Leche');
  await page.getByText('Leche Kong 1L', { exact: true }).click();
  await page.getByRole('button', { name: 'INICIAR VENTA', exact: true }).click();
  await expect(page.getByText(/Venta pendiente #11111111/)).toBeVisible();

  await page.getByRole('button', { name: 'COBRAR', exact: true }).click();
  await page.getByLabel('Monto recibido').fill('2000');
  await page.getByRole('button', { name: 'Confirmar efectivo' }).click();
  await expect(page.getByRole('dialog')).toContainText('Cobro aprobado');
  await expect(page.getByRole('dialog')).toContainText('Vuelto calculado por MS-5: $ 810');
  await expect(page.getByRole('dialog')).toContainText('*** COMPROBANTE DE PAGO SIMULADO ***');

  expect(requestedPaths).toContain('POST /api/auth/login');
  expect(requestedPaths).toContain('POST /api/turnos/abrir');
  expect(requestedPaths).toContain('POST /api/ventas');
  expect(requestedPaths).toContain(`POST /api/ventas/${saleId}/cobrar`);
});

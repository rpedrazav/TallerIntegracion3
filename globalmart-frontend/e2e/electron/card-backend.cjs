// Invoked by S2-055's .NET fixture, which owns MS-5, PostgreSQL and the simulator.
// No intercepted requests or fake IPC: this drives the production Electron UI.
const { _electron, expect } = require('@playwright/test');
const fs = require('node:fs');
const path = require('node:path');

(async () => {
  const output = process.env.S2055_OUTPUT;
  if (!output || !process.env.S2055_TENANT) throw new Error('Run through the S2-055 backend fixture');
  fs.mkdirSync(output, { recursive: true });
  const env = { ...process.env, E2E_USER_DATA: path.join(output, 'profile') };
  delete env.ELECTRON_RUN_AS_NODE;
  const app = await _electron.launch({ args: [path.resolve('e2e/electron/launch.cjs')], env });
  let page;
  await app.context().tracing.start({ screenshots: true, snapshots: true });
  try {
    await expect.poll(() => app.windows().some(w => w.url().startsWith('file:'))).toBe(true);
    page = app.windows().find(w => w.url().startsWith('file:'));
    await app.evaluate(({ BrowserWindow }) => {
      for (const window of BrowserWindow.getAllWindows()) {
        window.webContents.closeDevTools(); window.setSize(1440, 900);
      }
    });
    await page.locator('input[type=text]').fill(process.env.S2055_TENANT);
    await page.locator('input[type=email]').fill('cashier@example.test');
    await page.locator('input[type=password]').fill('dummy-e2e-only');
    await page.getByRole('button', { name: 'Ingresar', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Apertura de Turno de Caja' })).toBeVisible();
    await page.getByLabel('Monto inicial de caja').fill('20000');
    await page.screenshot({ path: path.join(output, '01-apertura.png') });
    const opening = page.waitForResponse(r => r.url().endsWith('/api/turnos/abrir') && r.request().method() === 'POST');
    await page.getByRole('button', { name: 'Abrir Turno', exact: true }).click();
    expect((await opening).status()).toBe(200);
    await expect(page.getByText('Carrito vacío', { exact: true })).toBeVisible();
    const search = page.getByPlaceholder('Escanea o busca un producto...');
    await search.fill('7800055'); await search.press('Enter');
    await expect(page.getByText('Leche S2-055', { exact: true })).toBeVisible();
    await page.getByRole('combobox', { name: /Método de pago/ }).selectOption('TARJETA');
    const creation = page.waitForResponse(r => r.url().endsWith('/ventas') && r.request().method() === 'POST');
    await page.getByRole('button', { name: 'INICIAR VENTA', exact: true }).click();
    const created = await creation;
    expect(created.status()).toBe(201);
    const sale = await created.json();
    expect(sale.estado).toBe('PENDIENTE'); expect(sale.total).toBe(119);
    await page.screenshot({ path: path.join(output, '02-venta.png') });
    await page.getByRole('button', { name: 'COBRAR', exact: true }).click();
    const charge = page.waitForResponse(r => r.url().endsWith('/cobrar-tarjeta') && r.request().method() === 'POST');
    await page.getByRole('button', { name: 'Confirmar tarjeta', exact: true }).click();
    const response = await charge;
    const approved = process.env.S2055_SCENARIO === 'sim-approved-v1';
    expect(response.status()).toBe(approved ? 200 : 422);
    await expect(page.getByRole('dialog')).toContainText(approved ? 'Cobro aprobado' : 'Cobro rechazado');
    const body = await response.json();
    if (approved) await expect(page.getByRole('dialog')).toContainText(`Referencia opaca: ${body.gateway_reference}`);
    await page.screenshot({ path: path.join(output, '03-resultado.png') });
    fs.writeFileSync(path.join(output, 'ui-result.json'), JSON.stringify({ sale, status: response.status(), body,
      request: response.request().postDataJSON() }, null, 2));
    await page.getByRole('button', { name: 'Cerrar cobro', exact: true }).click();
    if (approved) await expect(page.getByText('Carrito vacío', { exact: true })).toBeVisible();
    else await expect(page.getByRole('button', { name: 'COBRAR', exact: true })).toBeVisible();
  } catch (error) {
    if (page) await page.screenshot({ path: path.join(output, 'failure.png') }).catch(() => {});
    throw error;
  } finally {
    await app.context().tracing.stop({ path: path.join(output, 'trace.zip') });
    await app.close();
  }
})().catch(error => { console.error(error); process.exitCode = 1; });

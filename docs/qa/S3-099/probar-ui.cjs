const { chromium, expect } = require(process.cwd() + '/globalmart-frontend/node_modules/@playwright/test');
const fs = require('node:fs/promises');
(async () => {
  const browser = await chromium.launch({ headless: true });
  const results = [];
  try {
    for (const [code, suffix] of [['demo', '1'], ['donpepe', '2']]) {
      const login = await fetch('http://localhost:5124/api/auth/login', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ tenantCodigo: code, email: `admin@${code}.cl`, password: 'demo1234' }) });
      const { token } = await login.json();
      if (!token) throw new Error('Login failed: ' + code);
      const context = await browser.newContext();
      const page = await context.newPage();
      await page.addInitScript(jwt => Object.defineProperty(window, 'api', { value: { getToken: async () => jwt, setToken: async () => undefined, logout: async () => undefined } }), token);
      const ports = { 'auth-rpedraza.dev.censei.cl': 5124, 'catalog-rpedraza.dev.censei.cl': 5203, 'inventory-rpedraza.dev.censei.cl': 5205, 'pos-rpedraza.dev.censei.cl': 5000 };
      await page.route('https://*.dev.censei.cl/**', async route => {
        const url = new URL(route.request().url());
        const response = await route.fetch({ url: `http://localhost:${ports[url.hostname]}${url.pathname}${url.search}` });
        await route.fulfill({ response });
      });
      await page.goto('http://127.0.0.1:4175/?component=app#/admin/inventario');
      await expect(page.getByRole('heading', { name: 'Inventario por sucursal' })).toBeVisible();
      const adjust = page.getByRole('form', { name: 'Registrar ajuste' });
      await expect(adjust.getByLabel('Producto')).not.toBeEmpty();
      await expect.poll(() => adjust.getByLabel('Producto').inputValue()).not.toBe('');
      const product = await adjust.getByLabel('Producto').inputValue();
      await adjust.getByLabel('Cantidad actual').fill('10');
      await adjust.getByLabel('Stock mínimo').fill('2');
      await adjust.getByLabel('Motivo').fill('S3-099 diagnóstico local');
      await adjust.getByRole('button', { name: 'Guardar ajuste' }).click();
      await expect(page.getByRole('status').filter({ hasText: 'Inventario actualizado.' })).toBeVisible();
      const receipt = page.getByRole('form', { name: 'Recepción de mercadería' });
      await receipt.getByLabel('Artículo a recibir').selectOption(product);
      await receipt.getByLabel('Cantidad recibida').fill('2');
      const expires = new Date(Date.now() + 7 * 86400000).toISOString().slice(0, 10);
      await receipt.getByLabel('Fecha de vencimiento').fill(expires);
      await receipt.getByLabel('Ubicación (opcional)').fill('QA S3-099');
      await receipt.getByRole('button', { name: 'Registrar recepción' }).click();
      await expect(receipt.getByRole('status')).toHaveText('Recepción registrada.');
      const branch = `cccccccc-0000-0000-0000-00000000000${suffix}`;
      const headers = { Authorization: `Bearer ${token}` };
      const stock = await (await fetch(`http://localhost:5205/api/stock?sucursal_id=${branch}`, { headers })).json();
      const lots = await (await fetch(`http://localhost:5205/api/lotes?porVencer=30&sucursal_id=${branch}`, { headers })).json();
      expect(stock.find(s => s.productoId === product).cantidadActual).toBe(12);
      expect(lots.some(l => l.productoId === product && l.cantidad === 2)).toBe(true);
      results.push({ tenant: code, product, branch, adjustment: 10, receipt: 2, stock, lots, tables: await page.locator('table').count(), text: await page.locator('section').last().innerText() });
      await page.screenshot({ path: `docs/qa/S3-099/inventario-${code}.png`, fullPage: true });
      await context.close();
    }
  } finally {
    await fs.writeFile('docs/qa/S3-099/ui-resultados.json', JSON.stringify(results, null, 2) + '\n');
    await browser.close();
  }
  console.log('2 tenants: ajuste y recepción persistidos; stock 12 y lote 2; capturas guardadas.');
})().catch(e => { console.error(e); process.exitCode = 1; });

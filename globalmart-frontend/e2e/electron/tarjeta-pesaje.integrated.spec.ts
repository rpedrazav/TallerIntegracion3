// S2-113: tarjeta (aprobado, rechazado, timeout) y pesaje (balanza simulada y entrada manual)
// contra MS-1..MS-5 reales y el simulador de pasarela de docker compose. Ver e2e/electron/README.md.
// Únicas intervenciones de prueba: el escenario del simulador se agrega a la solicitud real de cobro
// (la UI no lo expone) y, para la entrada manual, la lectura de balanza falla en el proceso main.
import { Page } from '@playwright/test';
import { test, expect, api, tokenOf, login, snap, posDb, MS3, MS5 } from './integrated-fixture';

const run = Date.now().toString();
const unitario = { nombre: `Tarjeta S2-113 ${run}`, codigoBarras: `780113${run.slice(-7)}`, precioBase: 1000, esPesoVariable: false };
const pesable = { nombre: `Queso S2-113 ${run}`, codigoBarras: `790113${run.slice(-7)}`, precioBase: 8990, esPesoVariable: true };

test.describe.configure({ mode: 'serial', timeout: 120000 });

test.beforeAll(async () => {
  const admin = await tokenOf('admin@demo.cl');
  for (const p of [unitario, pesable]) {
    const res = await api(`${MS3}/products`, admin, { method: 'POST', body: JSON.stringify({ ...p, descripcion: null, categoriaId: null, isActive: true, uomBaseId: '00000000-0000-0000-0000-000000000001' }) });
    expect(res.status, JSON.stringify(res.body)).toBe(201);
  }
});

// Cada prueba parte de un turno nuevo: el POS restaura la última venta PENDIENTE del turno
// (p. ej. la que deja el timeout) y no debe arrastrarse a la prueba siguiente.
test.beforeEach(async () => {
  const cajero = await tokenOf('cajero@demo.cl');
  if ((await api(`${MS5}/api/turnos/activo`, cajero)).status === 200)
    expect((await api(`${MS5}/api/turnos/cerrar`, cajero, { method: 'POST' })).status).toBe(200);
  expect((await api(`${MS5}/api/turnos/abrir`, cajero, { method: 'POST', body: JSON.stringify({ sucursal_id: '00000000-0000-0000-0000-000000000001', monto_fondo_inicial: 20000 }) })).status).toBe(200);
});

const cobros = (ventaId: string) => ({
  pagos: Number(posDb(`select count(*) from "Pagos" where "VentaId" = '${ventaId}'`)),
  intentos: posDb(`select string_agg((array['PENDIENTE','APROBADO','RECHAZADO','FALLIDO'])["Estado" + 1], ',' order by "CreatedAt") from "IntentosPago" where "VentaId" = '${ventaId}'`),
  estado: Number(posDb(`select "Estado" from "Ventas" where "Id" = '${ventaId}'`)),
});
const COMPLETADA = 1, PENDIENTE = 0;

async function entrarAlPos(page: Page) {
  await login(page, 'cajero@demo.cl');
  await expect(page.getByPlaceholder('Escanea o busca un producto...')).toBeVisible({ timeout: 10000 });
}
async function agregar(page: Page, nombre: string) {
  await page.getByPlaceholder('Escanea o busca un producto...').fill(nombre);
  await page.getByText(nombre, { exact: true }).click();
}
/** Inicia la venta con el método dado y devuelve la venta creada por MS-5. */
async function iniciarVenta(page: Page, metodo: 'TARJETA' | 'EFECTIVO') {
  await page.getByRole('combobox', { name: /Método de pago/ }).selectOption(metodo);
  const creada = page.waitForResponse(r => r.url().endsWith('/ventas') && r.request().method() === 'POST');
  await page.getByRole('button', { name: 'INICIAR VENTA', exact: true }).click();
  const res = await creada;
  expect(res.status()).toBe(201);
  return res.json();
}
/** Agrega scenario_id a la solicitud real de cobro con tarjeta (las respuestas no se tocan). */
async function escenario(page: Page, scenarioId: string) {
  await page.unroute('**/cobrar-tarjeta');
  await page.route('**/cobrar-tarjeta', route => route.continue({ postData: JSON.stringify({ ...route.request().postDataJSON(), scenario_id: scenarioId }) }));
}
async function cobrarTarjeta(page: Page) {
  const cobro = page.waitForResponse(r => r.url().endsWith('/cobrar-tarjeta'), { timeout: 60000 });
  await page.getByRole('button', { name: 'Confirmar tarjeta', exact: true }).click();
  return cobro;
}

test('@integrado tarjeta aprobada completa la venta con un solo pago', async ({ app: page }) => {
  await entrarAlPos(page);
  await agregar(page, unitario.nombre);
  const venta = await iniciarVenta(page, 'TARJETA');
  await escenario(page, 'sim-approved-v1');
  await page.getByRole('button', { name: 'COBRAR', exact: true }).click();
  const res = await cobrarTarjeta(page);
  expect(res.status()).toBe(200);
  const body = await res.json();
  await expect(page.getByRole('dialog')).toContainText('Cobro aprobado');
  await expect(page.getByRole('dialog')).toContainText(`Referencia opaca: ${body.gateway_reference}`);
  await snap(page, '01-tarjeta-aprobada');
  expect(cobros(venta.id)).toEqual({ pagos: 1, intentos: 'APROBADO', estado: COMPLETADA });
});

test('@integrado tarjeta rechazada no completa la venta y el reintento cobra una sola vez', async ({ app: page }) => {
  test.info().annotations.push({ type: 'api-esperada', description: '422' });
  await entrarAlPos(page);
  await agregar(page, unitario.nombre);
  const venta = await iniciarVenta(page, 'TARJETA');
  await escenario(page, 'sim-declined-v1');
  await page.getByRole('button', { name: 'COBRAR', exact: true }).click();
  expect((await cobrarTarjeta(page)).status()).toBe(422);
  await expect(page.getByRole('dialog')).toContainText('Cobro rechazado');
  await snap(page, '02-tarjeta-rechazada');
  expect(cobros(venta.id)).toEqual({ pagos: 0, intentos: 'RECHAZADO', estado: PENDIENTE });

  await escenario(page, 'sim-approved-v1');
  await page.getByRole('button', { name: 'Reintentar tarjeta', exact: true }).click(); // nueva clave de intento
  expect((await cobrarTarjeta(page)).status()).toBe(200);
  await expect(page.getByRole('dialog')).toContainText('Cobro aprobado');
  await snap(page, '03-reintento-aprobado');
  expect(cobros(venta.id)).toEqual({ pagos: 1, intentos: 'RECHAZADO,APROBADO', estado: COMPLETADA });
});

test('@integrado timeout de pasarela deja la venta pendiente y no permite un segundo cargo', async ({ app: page }) => {
  await entrarAlPos(page);
  await agregar(page, unitario.nombre);
  const venta = await iniciarVenta(page, 'TARJETA');
  await escenario(page, 'sim-timeout-v1');
  await page.getByRole('button', { name: 'COBRAR', exact: true }).click();
  // El renderer corta a los 15 s (axios) y MS-5 espera 30 s a la pasarela, que responde 504 a los 35 s:
  // no hay respuesta HTTP para la UI, que reconcilia el intento y queda en "sin confirmar".
  test.info().annotations.push({ type: 'api-esperada', description: '409,502,504' });
  await page.getByRole('button', { name: 'Confirmar tarjeta', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Verificar venta' })).toBeVisible({ timeout: 60000 });
  await snap(page, '04-timeout-incierto');
  expect(cobros(venta.id)).toMatchObject({ pagos: 0, estado: PENDIENTE });

  // Verificar consulta el intento: sigue incierto y la venta no se completa.
  await page.getByRole('button', { name: 'Verificar venta' }).click();
  await expect(page.getByRole('button', { name: 'Verificar venta' })).toBeVisible();
  // Un segundo cobro con otra clave es rechazado por MS-5 sin llamar a la pasarela.
  const cajero = await tokenOf('cajero@demo.cl');
  const segundo = await api(`${MS5}/ventas/${venta.id}/cobrar-tarjeta`, cajero, { method: 'POST', body: JSON.stringify({ idempotency_key: crypto.randomUUID(), scenario_id: 'sim-approved-v1' }) });
  expect(segundo.status).toBe(409);
  await page.waitForTimeout(25000); // deja vencer el timeout de MS-5 (30 s) y la respuesta 504 del simulador (35 s)
  const final = cobros(venta.id);
  expect(final.pagos).toBe(0);
  expect(final.estado).toBe(PENDIENTE);
  expect(final.intentos.split(',')).toHaveLength(1);
});

test('@integrado peso estable de la balanza simulada llega a MS-5 como una línea', async ({ app: page }) => {
  await entrarAlPos(page);
  await agregar(page, pesable.nombre);
  await expect(page.getByText('Peso estable: 0.750 kg')).toBeVisible();
  await page.getByRole('button', { name: 'Confirmar peso' }).click();
  await expect(page.locator('tbody tr')).toHaveCount(1);
  const venta = await iniciarVenta(page, 'EFECTIVO');
  // 8.990 × 0,75 = 6.742,5 → 6.743 (CLP); IVA 1.281,17 → 1.281
  expect(venta.items).toHaveLength(1);
  expect([venta.items[0].peso_kg, venta.items[0].subtotal, venta.subtotal, venta.impuestos, venta.total]).toEqual([0.75, 6743, 6743, 1281, 8024]);
  await expect(page.locator('tbody tr')).toContainText('0.750 kg');
  await snap(page, '05-peso-simulado');
});

test('@integrado balanza con falla permite entrada manual y conserva una sola línea', async ({ electron, app: page }) => {
  await electron.evaluate(({ ipcMain }) => {
    ipcMain.removeHandler('scale:read-stable');
    ipcMain.handle('scale:read-stable', () => ({ ok: false, error: { code: 'device_error', message: 'Falla simulada de balanza (S2-113).', retryable: true }, state: { connectionStatus: 'connected', readingStatus: 'error' } }));
  });
  await entrarAlPos(page);
  await agregar(page, pesable.nombre);
  await page.getByRole('button', { name: 'Ingresar peso manualmente' }).click();
  const input = page.getByRole('textbox', { name: 'Peso manual en kg' });
  await input.fill('0'); // peso inválido: no se puede confirmar
  await expect(page.getByRole('button', { name: 'Confirmar peso' })).toHaveCount(0);
  await input.fill('1.234');
  await page.getByRole('button', { name: 'Confirmar peso' }).click();
  await expect(page.locator('tbody tr')).toHaveCount(1);
  const venta = await iniciarVenta(page, 'EFECTIVO');
  // 8.990 × 1,234 = 11.093,66 → 11.094; IVA 2.107,86 → 2.108
  expect([venta.items.length, venta.items[0].peso_kg, venta.subtotal, venta.impuestos, venta.total]).toEqual([1, 1.234, 11094, 2108, 13202]);
  await snap(page, '06-peso-manual');
});

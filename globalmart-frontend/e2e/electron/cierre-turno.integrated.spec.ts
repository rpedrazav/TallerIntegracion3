// S3-029: caso S3-006 (docs/qa/S3-006/caso-prueba.md) — cierre de turno con el modal de confirmación
// contra MS-1, MS-3 y MS-5 reales. Ver e2e/electron/README.md.
import { test, expect, api, tokenOf, login, snap, MS3, MS5 } from './integrated-fixture';

const run = Date.now().toString();
// 1.000 + IVA 190 = 1.190: múltiplo de 10, declarable con denominaciones CLP.
const producto = { nombre: `Cierre S3-029 ${run}`, codigoBarras: `780029${run.slice(-7)}`, precioBase: 1000, esPesoVariable: false };
const sucursal = '00000000-0000-0000-0000-000000000001';
const post = (body?: unknown) => ({ method: 'POST', body: body === undefined ? undefined : JSON.stringify(body) });

test.describe.configure({ timeout: 90000 });

test('@integrado CAJERO confirma el cierre en el modal y la app sigue respondiendo', async ({ app: page }) => {
  // P3: turno ABIERTO con una venta en efectivo completada, para que efectivo_esperado ≠ 0.
  const admin = await tokenOf('admin@demo.cl');
  const creado = await api(`${MS3}/products`, admin, post({ ...producto, descripcion: null, categoriaId: null, isActive: true, uomBaseId: '00000000-0000-0000-0000-000000000001' }));
  expect(creado.status, JSON.stringify(creado.body)).toBe(201);
  const cajero = await tokenOf('cajero@demo.cl');
  if ((await api(`${MS5}/api/turnos/activo`, cajero)).status === 200)
    expect((await api(`${MS5}/api/turnos/cerrar`, cajero, post())).status).toBe(200);
  const abierto = await api(`${MS5}/api/turnos/abrir`, cajero, post({ sucursal_id: sucursal, monto_fondo_inicial: 10000 }));
  expect(abierto.status).toBe(200);
  const venta = await api(`${MS5}/ventas`, cajero, post({ items: [{ producto_id: creado.body.id, cantidad: 1 }], metodo_pago: 'EFECTIVO' }));
  expect(venta.status).toBe(201);
  expect((await api(`${MS5}/ventas/${venta.body.id}/cobrar`, cajero, post({ monto_recibido: venta.body.total }))).status).toBe(200);

  const nativos: string[] = [];
  page.on('dialog', d => nativos.push(d.message()));
  const cierres: number[] = [];
  page.on('request', r => { if (r.method() === 'POST' && r.url().endsWith('/api/turnos/cerrar')) cierres.push(Date.now()); });

  // 1. Entrar a Cerrar Turno: se precarga E con el cuadre en 0.
  await login(page, 'cajero@demo.cl');
  await expect(page.getByPlaceholder('Escanea o busca un producto...')).toBeVisible({ timeout: 10000 });
  const precarga = page.waitForResponse(r => r.url().endsWith('/api/turnos/cuadre'));
  await page.getByRole('link', { name: 'Cerrar Turno' }).click();
  await expect(page.getByRole('heading', { name: 'Cierre de Turno y Cuadre de Caja' })).toBeVisible();
  const E: number = (await (await precarga).json()).efectivo_esperado;
  expect(E).toBe(venta.body.total);

  // 2. Declarar denominaciones que suman exactamente E.
  let resto = E;
  for (const [id, valor] of [['b_1000', 1000], ['m_100', 100], ['m_50', 50], ['m_10', 10]] as const) {
    const n = Math.floor(resto / valor);
    if (n) await page.locator(`#input-denominacion-${id}`).fill(String(n));
    resto -= n * valor;
  }
  expect(resto, 'E debe poder declararse con denominaciones CLP').toBe(0);

  // 3. Calcular Cuadre: lo decide MS-5.
  const cuadreRes = page.waitForResponse(r => r.url().endsWith('/api/turnos/cuadre'));
  await page.getByRole('button', { name: /Calcular Cuadre/ }).click();
  const cuadre = await (await cuadreRes).json();
  expect([cuadre.efectivo_esperado, cuadre.monto_declarado, cuadre.diferencia]).toEqual([E, E, 0]);

  // 4. Abrir el modal: sin diálogo nativo y sin POST /cerrar.
  const cerrar = page.getByRole('button', { name: /Confirmar y Cerrar Turno/ });
  const modal = page.getByRole('dialog', { name: 'Confirmar cierre de turno' });
  await cerrar.click();
  await expect(modal).toBeVisible();
  await expect(modal).toHaveAttribute('aria-modal', 'true');
  await expect(modal).toContainText(`$${E.toLocaleString('es-CL')}`);
  await snap(page, '01-modal-cierre');

  // 5. Cancelar con mouse: se cierra y el formulario conserva las cantidades.
  await modal.getByRole('button', { name: 'Cancelar' }).click();
  await expect(modal).toBeHidden();
  await expect(page.locator('#input-denominacion-b_1000')).toHaveValue(String(Math.floor(E / 1000)));

  // 6. Cancelar con Escape: el foco vuelve al botón de cierre.
  await cerrar.click();
  await expect(modal).toBeVisible();
  await page.keyboard.press('Escape');
  await expect(modal).toBeHidden();
  await expect(cerrar).toBeFocused();
  expect(cierres, 'Cancelar no debe cerrar el turno').toEqual([]);
  expect((await api(`${MS5}/api/turnos/activo`, cajero)).status).toBe(200);

  // 7. El teclado sigue funcionando tras cancelar.
  const input = page.locator('#input-denominacion-m_500');
  await input.fill('');
  await input.pressSequentially('7');
  await expect(input).toHaveValue('7');
  await input.fill('0');

  // 8. Confirmar con teclado: Tab desde «Cancelar» hasta «Confirmar cierre» y Enter.
  await cerrar.click();
  await expect(modal.getByRole('button', { name: 'Cancelar' })).toBeFocused();
  await page.keyboard.press('Tab');
  await expect(modal.getByRole('button', { name: 'Confirmar cierre' })).toBeFocused();
  const cerrado = page.waitForResponse(r => r.url().endsWith('/api/turnos/cerrar'));
  await page.keyboard.press('Enter');
  expect((await cerrado).status()).toBe(200);
  await expect(modal).toBeHidden();

  // 9. Resultado: éxito en pantalla y MS-5 ya no tiene turno activo.
  await expect(page.getByText('¡Turno cerrado exitosamente!')).toBeVisible();
  await snap(page, '02-turno-cerrado');
  expect(cierres, 'Una sola solicitud de cierre').toHaveLength(1);
  expect((await api(`${MS5}/api/turnos/activo`, cajero)).status).toBe(404);

  // 10–11. La app responde: navegación al POS → apertura, y el teclado escribe.
  await page.getByRole('button', { name: 'Volver al POS' }).click();
  await expect(page.getByRole('heading', { name: 'Apertura de Turno de Caja' })).toBeVisible({ timeout: 10000 });
  await page.locator('#monto-inicial').fill('');
  await page.locator('#monto-inicial').pressSequentially('15000');
  await expect(page.locator('#monto-inicial')).toHaveValue('15000');

  // 12. Abrir un turno nuevo desde la UI.
  await page.getByRole('button', { name: 'Abrir Turno', exact: true }).click();
  await expect(page.getByText('Carrito vacío', { exact: true })).toBeVisible({ timeout: 10000 });
  const nuevo = await api(`${MS5}/api/turnos/activo`, cajero);
  expect(nuevo.status).toBe(200);
  expect(nuevo.body.id).not.toBe(abierto.body.id);
  expect(nativos, 'El cierre no debe usar diálogos nativos').toEqual([]);

  // 13. Limpieza: el cajero queda sin turno para la siguiente corrida.
  expect((await api(`${MS5}/api/turnos/cerrar`, cajero, post())).status).toBe(200);
});

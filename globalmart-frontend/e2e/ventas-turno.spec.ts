import { expect, test, Page } from '@playwright/test';

const ventaId = '11111111-1111-1111-1111-111111111111';
const turnoId = '22222222-2222-2222-2222-222222222222';
const venta = { id: ventaId, turno_id: turnoId, subtotal: 100, impuestos: 19, total: 119, metodo_pago: 'EFECTIVO', estado: 'COMPLETADA', items: [] };
const turno = { id: turnoId, cajeroId: '33333333-3333-3333-3333-333333333333', abiertoAt: '2026-11-22T12:00:00Z' };

function jwt(role: string) {
  const payload = Buffer.from(JSON.stringify({ tenant_id: 't', active_role: role })).toString('base64url');
  return `h.${payload}.s`;
}

async function openSales(page: Page, role: string) {
  await page.addInitScript((token) => {
    window.api = { ping: async () => 'pong', getToken: async () => token, setToken: async () => undefined, logout: async () => undefined };
  }, jwt(role));
  let estado = 'COMPLETADA';
  await page.route('**/api/turnos/activo', route => route.fulfill({ json: turno }));
  await page.route('**/api/turnos/abiertos', route => route.fulfill({ json: [turno] }));
  await page.route('**/ventas/turno/**', route => route.fulfill({ json: [{ ...venta, estado }] }));
  await page.route(`**/ventas/${ventaId}/anular`, route => {
    estado = 'ANULADA';
    return route.fulfill({ json: { venta_id: ventaId, estado, anulacion: {}, reembolso_tarjeta: 'NO_APLICA', devolucion_efectivo: 'GESTION_MANUAL' } });
  });
  await page.goto('/?component=app#/ventas-turno');
}

test('CAJERO ve las ventas del turno sin la acción Anular', async ({ page }) => {
  await openSales(page, 'CAJERO');
  await expect(page.getByRole('cell', { name: 'COMPLETADA' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Anular' })).toHaveCount(0);
});

test('ADMIN anula con motivo obligatorio y la lista muestra ANULADA', async ({ page }) => {
  await openSales(page, 'ADMIN');
  let body: unknown;
  page.on('request', r => { if (r.url().endsWith('/anular')) body = r.postDataJSON(); });
  await page.getByRole('button', { name: 'Anular', exact: true }).click();
  const confirmar = page.getByRole('button', { name: 'Confirmar anulación' });
  await expect(confirmar).toBeDisabled();
  await page.getByLabel('Motivo (obligatorio)').fill('  Venta por error  ');
  await confirmar.click();
  await expect(page.getByRole('cell', { name: 'ANULADA' })).toBeVisible();
  expect(body).toEqual({ motivo: 'Venta por error' });
  await expect(page.getByRole('button', { name: 'Anular', exact: true })).toHaveCount(0);
});

const erroresAnulacion = [
  [400, 'SOLICITUD_INVALIDA', 'Revisa el motivo'],
  [401, 'IDENTIDAD_INVALIDA', 'identidad válida'],
  [404, 'VENTA_NO_ENCONTRADA', 'no existe o no tienes acceso'],
  [409, 'ESTADO_NO_ANULABLE', 'estado actual de la venta'],
  [409, 'VENTA_YA_ANULADA', 'ya está anulada'],
  [409, 'PAGO_INCIERTO', 'cobro aún no tiene un resultado definitivo'],
  [409, 'METODO_NO_SOPORTADO', 'pago mixto'],
  [409, 'REEMBOLSO_NO_DISPONIBLE', 'sin una devolución confirmada'],
  [409, 'PAGO_INCONSISTENTE', 'pagos registrados no coinciden'],
  [409, 'TURNO_NO_ANULABLE', 'turno original está cerrado'],
  [409, 'REVERSA_RECHAZADA', 'tarjeta no fue reembolsada'],
  [409, 'REVERSA_PENDIENTE', 'pendiente de confirmación'],
  [500, 'ERROR_PERSISTENCIA', 'confirmar la anulación en el sistema'],
  [400, '', 'Revisa el motivo'],
  [401, '', 'sesión venció'],
  [403, '', 'Solo un administrador'],
  [404, '', 'no existe o no tienes acceso'],
  [409, 'DESCONOCIDO', 'conflicto'],
  [500, '', 'confirmar el resultado'],
  [0, '', 'confirmar el resultado'],
] as const;

for (const [status, codigo, mensaje] of erroresAnulacion) {
  test(`anulación ${status} ${codigo || 'sin código'} conserva la venta y explica el problema`, async ({ page }) => {
    await openSales(page, 'ADMIN');
    let solicitudes = 0;
    await page.route(`**/ventas/${ventaId}/anular`, route => {
      solicitudes++;
      return status === 0 ? route.abort('failed') : route.fulfill({ status, json: { codigo, error: 'Texto técnico que no debe mostrarse' } });
    });
    await page.getByRole('button', { name: 'Anular', exact: true }).click();
    const dialog = page.getByRole('dialog');
    await page.getByLabel('Motivo (obligatorio)').fill('Venta por error');
    await dialog.getByRole('button', { name: 'Confirmar anulación' }).click();
    if (status === 401) {
      // El interceptor compartido invalida la sesión y vuelve al login.
      await expect(page.getByRole('button', { name: 'Ingresar', exact: true })).toBeVisible();
      expect(solicitudes).toBe(1);
      return;
    }
    await expect(dialog.getByRole('alert')).toContainText(mensaje);
    await expect(dialog.getByLabel('Motivo (obligatorio)')).toHaveValue('Venta por error');
    await expect(dialog.getByRole('button', { name: 'Confirmar anulación' })).toBeDisabled();
    await dialog.getByRole('button', { name: 'Cerrar', exact: true }).click();
    await expect(page.getByRole('cell', { name: 'COMPLETADA' })).toBeVisible();
    await expect(page.getByRole('cell', { name: 'ANULADA', exact: true })).toHaveCount(0);
    await expect(page.getByRole('alert')).toContainText(mensaje);
    expect(solicitudes).toBe(1);
  });
}

test('respuesta perdida recupera ANULADA del servidor sin repetir el POST', async ({ page }) => {
  await openSales(page, 'ADMIN');
  let solicitudes = 0;
  await page.route(`**/ventas/${ventaId}/anular`, async route => {
    solicitudes++;
    await page.route('**/ventas/turno/**', r => r.fulfill({ json: [{ ...venta, estado: 'ANULADA' }] }));
    await route.abort('failed');
  });
  await page.getByRole('button', { name: 'Anular', exact: true }).click();
  await page.getByLabel('Motivo (obligatorio)').fill('Venta por error');
  await page.getByRole('button', { name: 'Confirmar anulación' }).click();
  await page.getByRole('button', { name: 'Cerrar', exact: true }).click();
  await expect(page.getByRole('cell', { name: 'ANULADA' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Anular', exact: true })).toHaveCount(0);
  expect(solicitudes).toBe(1);
});

test('fallo al refrescar no deja una venta desactualizada como confirmada', async ({ page }) => {
  await openSales(page, 'ADMIN');
  await page.route(`**/ventas/${ventaId}/anular`, async route => {
    await page.route('**/ventas/turno/**', r => r.abort('failed'));
    await route.fulfill({ status: 409, json: { codigo: 'REVERSA_PENDIENTE' } });
  });
  await page.getByRole('button', { name: 'Anular', exact: true }).click();
  await page.getByLabel('Motivo (obligatorio)').fill('Venta por error');
  await page.getByRole('button', { name: 'Confirmar anulación' }).click();
  const dialog = page.getByRole('dialog');
  await expect(dialog).toContainText('No se pudo actualizar el estado');
  await expect(dialog).toContainText('pendiente de confirmación');
  await dialog.getByRole('button', { name: 'Cerrar', exact: true }).click();
  await expect(page.getByRole('cell', { name: 'COMPLETADA' })).toHaveCount(0);
  await expect(page.getByText('Este turno aún no tiene ventas.')).toHaveCount(0);
});

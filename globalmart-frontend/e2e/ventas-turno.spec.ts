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

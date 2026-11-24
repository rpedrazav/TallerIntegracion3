import { expect, test } from '@playwright/test';

test('TenantConfig convierte porcentajeIva a number antes de actualizar', async ({ page }) => {
  let updateBody: Record<string, unknown> | undefined;
  const config = {
    pais: 'CL',
    moneda: 'CLP',
    idioma: 'es',
    porcentajeIva: 19,
    zonaHoraria: 'America/Santiago',
  };

  await page.route('**/api/tenants/tenant-a/config', async route => {
    if (route.request().method() === 'PUT') {
      updateBody = route.request().postDataJSON();
      await route.fulfill({ json: updateBody });
      return;
    }

    await route.fulfill({ json: config });
  });

  await page.goto('/?component=tenant-config');
  await expect(page.getByLabel('País (código ISO)')).toHaveValue('CL');
  const ivaInput = page.getByLabel('Porcentaje IVA (%)');
  await expect(ivaInput).toBeEnabled();
  await expect(ivaInput).toHaveValue('19');
  await ivaInput.fill('18.5');
  const guardar = page.getByRole('button', { name: 'Guardar Cambios' });
  await expect(guardar).toBeEnabled();
  await guardar.click();

  await expect.poll(() => updateBody?.porcentajeIva).toBe(18.5);
  expect(typeof updateBody?.porcentajeIva).toBe('number');
  expect(updateBody?.porcentajeIva).not.toBe('18.5');
  await expect(page.getByText('Configuración del tenant actualizada exitosamente.')).toBeVisible();
});

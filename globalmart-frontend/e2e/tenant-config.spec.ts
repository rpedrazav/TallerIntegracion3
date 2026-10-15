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

  await page.route('**/tenants/tenant-a/config', async route => {
    if (route.request().method() === 'PUT') {
      updateBody = route.request().postDataJSON();
      await route.fulfill({ json: updateBody });
      return;
    }

    await route.fulfill({ json: config });
  });

  await page.goto('/?component=tenant-config');
  const ivaInput = page.getByLabel('Porcentaje IVA (%)');
  await expect(ivaInput).toHaveValue('19');
  await ivaInput.fill('18.5');
  await page.getByRole('button', { name: 'Guardar Cambios' }).click();

  await expect.poll(() => updateBody?.porcentajeIva).toBe(18.5);
  expect(typeof updateBody?.porcentajeIva).toBe('number');
  expect(updateBody?.porcentajeIva).not.toBe('18.5');
});

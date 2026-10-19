import { expect, Page, test } from '@playwright/test';

interface TestUser {
  id: string;
  nombre: string;
  email: string;
  activo: boolean;
  creadoEn: string;
  roles: string[];
}

const adminToken = [
  Buffer.from(JSON.stringify({ alg: 'none', typ: 'JWT' })).toString('base64url'),
  Buffer.from(JSON.stringify({ tenant_id: 'tenant-a', roles: ['ADMIN'] })).toString('base64url'),
  '',
].join('.');

const usuarioInicial: TestUser = {
  id: '11111111-1111-1111-1111-111111111111',
  nombre: 'Ana Cajera',
  email: 'ana@globalmart.cl',
  activo: true,
  creadoEn: '2026-10-18T12:00:00Z',
  roles: ['CAJERO'],
};

const configInicial = {
  pais: 'CL',
  moneda: 'CLP',
  idioma: 'es',
  porcentajeIva: 19,
  zonaHoraria: 'America/Santiago',
};

async function prepararSesion(page: Page) {
  await page.addInitScript(token => {
    window.api = {
      ping: async () => 'pong',
      getToken: async () => token,
      setToken: async () => undefined,
      logout: async () => undefined,
    };
  }, adminToken);
}

async function abrirAdmin(page: Page) {
  await prepararSesion(page);
  await page.goto('/?component=admin');
  await expect(page.getByRole('heading', { name: 'Panel de Administración' })).toBeVisible();
}

async function completarAlta(page: Page) {
  await page.getByRole('button', { name: 'Crear usuario' }).click();
  const modal = page.getByRole('dialog', { name: 'Crear usuario' });
  await modal.getByLabel('Nombre').fill('Carla Operadora');
  await modal.getByLabel('Correo electrónico').fill('Carla@GlobalMart.cl');
  await modal.getByLabel('Contraseña', { exact: true }).fill('ClaveSegura1!');
  await modal.getByLabel('Confirmar contraseña').fill('ClaveSegura1!');
  await modal.getByLabel('Rol').selectOption('REPONEDOR');
  return modal;
}

async function comprobarAltaConservada(page: Page) {
  const modal = page.getByRole('dialog', { name: 'Crear usuario' });
  await expect(modal).toBeVisible();
  await expect(modal.getByLabel('Nombre')).toHaveValue('Carla Operadora');
  await expect(modal.getByLabel('Correo electrónico')).toHaveValue('Carla@GlobalMart.cl');
  await expect(modal.getByLabel('Contraseña', { exact: true })).toHaveValue('ClaveSegura1!');
  await expect(modal.getByLabel('Confirmar contraseña')).toHaveValue('ClaveSegura1!');
  await expect(modal.getByLabel('Rol')).toHaveValue('REPONEDOR');
}

async function abrirEdicion(page: Page, email: string) {
  await page.getByRole('button', { name: `Editar usuario ${usuarioInicial.nombre}` }).click();
  const modal = page.getByRole('dialog', { name: 'Editar usuario' });
  await expect(modal.getByLabel('Correo electrónico')).toHaveValue(usuarioInicial.email);
  await modal.getByLabel('Correo electrónico').fill(email);
  return modal;
}

async function abrirConfiguracion(page: Page) {
  await page.getByRole('button', { name: 'Configuración del Tenant' }).click();
  await expect(page.getByLabel('Porcentaje IVA (%)')).toHaveValue('19');
}

test('alta exitosa agrega el usuario a la lista', async ({ page }) => {
  let usuarios = [usuarioInicial];
  let postCount = 0;

  await page.route('**/users**', async route => {
    const request = route.request();
    if (request.method() === 'GET') {
      await route.fulfill({ json: { items: usuarios } });
      return;
    }

    if (request.method() === 'POST' && new URL(request.url()).pathname.endsWith('/users')) {
      postCount += 1;
      expect(request.postDataJSON()).toEqual({
        nombre: 'Carla Operadora',
        email: 'carla@globalmart.cl',
        password: 'ClaveSegura1!',
        roles: ['REPONEDOR'],
      });
      const creado: TestUser = {
        id: '22222222-2222-2222-2222-222222222222',
        nombre: 'Carla Operadora',
        email: 'carla@globalmart.cl',
        activo: true,
        creadoEn: '2026-10-18T12:30:00Z',
        roles: ['REPONEDOR'],
      };
      usuarios = [...usuarios, creado];
      await route.fulfill({ status: 201, json: creado });
      return;
    }

    await route.abort();
  });

  await abrirAdmin(page);
  const modal = await completarAlta(page);
  await modal.getByRole('button', { name: 'Crear usuario' }).click();

  await expect(modal).toBeHidden();
  await expect(page.getByText('carla@globalmart.cl', { exact: true })).toBeVisible();
  await expect(page.locator('tbody tr')).toHaveCount(2);
  expect(postCount).toBe(1);
});

for (const caso of [
  {
    status: 400,
    response: { errors: { Email: ['El correo informado no es válido.'] } },
    mensaje: 'El correo informado no es válido.',
  },
  {
    status: 403,
    response: {},
    mensaje: 'No tienes permisos para crear usuarios.',
  },
  {
    status: 409,
    response: { message: 'El email ya pertenece a otro usuario.' },
    mensaje: 'El email ya pertenece a otro usuario.',
  },
]) {
  test(`alta explica ${caso.status} y conserva todos los datos`, async ({ page }) => {
    await page.route('**/users**', async route => {
      const request = route.request();
      if (request.method() === 'GET') {
        await route.fulfill({ json: { items: [usuarioInicial] } });
        return;
      }
      await route.fulfill({ status: caso.status, json: caso.response });
    });

    await abrirAdmin(page);
    const modal = await completarAlta(page);
    await modal.getByRole('button', { name: 'Crear usuario' }).click();

    await expect(modal.getByRole('alert')).toContainText(caso.mensaje);
    await comprobarAltaConservada(page);
  });
}

test('edición exitosa actualiza la misma fila sin crear otra', async ({ page }) => {
  let usuarios = [usuarioInicial];
  let putCount = 0;

  await page.route('**/users**', async route => {
    const request = route.request();
    if (request.method() === 'GET') {
      await route.fulfill({ json: { items: usuarios } });
      return;
    }
    if (request.method() === 'PUT') {
      putCount += 1;
      expect(new URL(request.url()).pathname).toMatch(new RegExp(`/users/${usuarioInicial.id}$`));
      expect(request.postDataJSON()).toEqual({ email: 'ana.editada@globalmart.cl' });
      usuarios = [{ ...usuarioInicial, email: 'ana.editada@globalmart.cl' }];
      await route.fulfill({ json: usuarios[0] });
      return;
    }
    await route.abort();
  });

  await abrirAdmin(page);
  const modal = await abrirEdicion(page, 'Ana.Editada@GlobalMart.cl');
  await modal.getByRole('button', { name: 'Guardar cambios' }).click();

  await expect(modal).toBeHidden();
  await expect(page.getByText('ana.editada@globalmart.cl', { exact: true })).toBeVisible();
  await expect(page.locator('tbody tr')).toHaveCount(1);
  expect(putCount).toBe(1);
});

for (const caso of [
  {
    status: 400,
    response: { errors: { Email: ['El correo informado no es válido.'] } },
    mensaje: 'El correo informado no es válido.',
  },
  {
    status: 403,
    response: {},
    mensaje: 'No tienes permisos para editar usuarios.',
  },
  {
    status: 409,
    response: {},
    mensaje: 'El email ya pertenece a otro usuario del tenant.',
  },
]) {
  test(`edición explica ${caso.status} y conserva el email`, async ({ page }) => {
    await page.route('**/users**', async route => {
      const request = route.request();
      if (request.method() === 'GET') {
        await route.fulfill({ json: { items: [usuarioInicial] } });
        return;
      }
      await route.fulfill({ status: caso.status, json: caso.response });
    });

    await abrirAdmin(page);
    const modal = await abrirEdicion(page, 'Ana.Editada@GlobalMart.cl');
    await modal.getByRole('button', { name: 'Guardar cambios' }).click();

    await expect(modal.getByRole('alert')).toContainText(caso.mensaje);
    await expect(modal).toBeVisible();
    await expect(modal.getByLabel('Correo electrónico')).toHaveValue('Ana.Editada@GlobalMart.cl');
  });
}

test('baja confirma, muestra progreso y conserva la fila como Inactivo tras 204', async ({ page }) => {
  let deleteStarted = false;
  let releaseDelete: (() => void) | undefined;
  const continueDelete = new Promise<void>(resolve => { releaseDelete = resolve; });

  await page.route('**/users**', async route => {
    const request = route.request();
    if (request.method() === 'GET') {
      await route.fulfill({ json: { items: [usuarioInicial] } });
      return;
    }
    if (request.method() === 'DELETE') {
      expect(new URL(request.url()).pathname).toMatch(new RegExp(`/users/${usuarioInicial.id}$`));
      deleteStarted = true;
      await continueDelete;
      await route.fulfill({ status: 204 });
      return;
    }
    await route.abort();
  });

  await abrirAdmin(page);
  page.once('dialog', async dialog => {
    expect(dialog.message()).toContain('desactivar a Ana Cajera');
    await dialog.accept();
  });
  await page.getByRole('button', { name: `Desactivar usuario ${usuarioInicial.nombre}` }).click();

  await expect.poll(() => deleteStarted).toBe(true);
  await expect(page.getByRole('button', { name: `Desactivar usuario ${usuarioInicial.nombre}` })).toContainText('Desactivando');
  releaseDelete?.();

  const fila = page.locator('tbody tr').filter({ hasText: usuarioInicial.email });
  await expect(fila).toHaveCount(1);
  await expect(fila.getByText('Inactivo', { exact: true })).toBeVisible();
  await expect(page.locator('tbody tr')).toHaveCount(1);
});

test('baja fallida explica la autodesactivación y conserva la fila activa', async ({ page }) => {
  await page.route('**/users**', async route => {
    if (route.request().method() === 'GET') {
      await route.fulfill({ json: { items: [usuarioInicial] } });
      return;
    }
    await route.fulfill({ status: 400, json: { message: 'Un administrador no puede desactivarse a sí mismo.' } });
  });

  await abrirAdmin(page);
  page.once('dialog', dialog => dialog.accept());
  await page.getByRole('button', { name: `Desactivar usuario ${usuarioInicial.nombre}` }).click();

  await expect(page.getByRole('alert')).toContainText('Un administrador no puede desactivarse a sí mismo.');
  const fila = page.locator('tbody tr').filter({ hasText: usuarioInicial.email });
  await expect(fila).toHaveCount(1);
  await expect(fila.getByText('Activo', { exact: true })).toBeVisible();
});

test('configuración guarda el IVA y muestra feedback de éxito', async ({ page }) => {
  let updateBody: Record<string, unknown> | undefined;
  await page.route('**/users**', route => route.fulfill({ json: { items: [usuarioInicial] } }));
  await page.route('**/tenants/tenant-a/config', async route => {
    if (route.request().method() === 'PUT') {
      updateBody = route.request().postDataJSON();
      await route.fulfill({ json: updateBody });
      return;
    }
    await route.fulfill({ json: configInicial });
  });

  await abrirAdmin(page);
  await abrirConfiguracion(page);
  await page.getByLabel('Porcentaje IVA (%)').fill('18.5');
  await page.getByRole('button', { name: 'Guardar Cambios' }).click();

  await expect(page.getByRole('status')).toContainText('Configuración del tenant actualizada exitosamente.');
  await expect.poll(() => updateBody?.porcentajeIva).toBe(18.5);
  expect(typeof updateBody?.porcentajeIva).toBe('number');
});

for (const caso of [
  {
    status: 400,
    response: { errors: { PorcentajeIva: ['El porcentaje de IVA no es válido para el tenant.'] } },
    mensaje: 'El porcentaje de IVA no es válido para el tenant.',
  },
  {
    status: 403,
    response: {},
    mensaje: 'No tienes permisos de Administrador para modificar la configuración.',
  },
]) {
  test(`configuración explica ${caso.status} y conserva los valores`, async ({ page }) => {
    await page.route('**/users**', route => route.fulfill({ json: { items: [usuarioInicial] } }));
    await page.route('**/tenants/tenant-a/config', async route => {
      if (route.request().method() === 'PUT') {
        await route.fulfill({ status: caso.status, json: caso.response });
        return;
      }
      await route.fulfill({ json: configInicial });
    });

    await abrirAdmin(page);
    await abrirConfiguracion(page);
    await page.getByLabel('País (código ISO)').fill('AR');
    await page.getByLabel('Moneda Base (ISO)').fill('ARS');
    await page.getByLabel('Idioma').fill('es-AR');
    await page.getByLabel('Porcentaje IVA (%)').fill('18.5');
    await page.getByLabel('Zona Horaria (IANA)').fill('America/Argentina/Buenos_Aires');
    await page.getByRole('button', { name: 'Guardar Cambios' }).click();

    await expect(page.getByRole('alert')).toContainText(caso.mensaje);
    await expect(page.getByLabel('País (código ISO)')).toHaveValue('AR');
    await expect(page.getByLabel('Moneda Base (ISO)')).toHaveValue('ARS');
    await expect(page.getByLabel('Idioma')).toHaveValue('es-AR');
    await expect(page.getByLabel('Porcentaje IVA (%)')).toHaveValue('18.5');
    await expect(page.getByLabel('Zona Horaria (IANA)')).toHaveValue('America/Argentina/Buenos_Aires');
  });
}

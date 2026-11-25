import { expect, test } from '@playwright/test';

const tenantId = 'aaaaaaaa-0000-0000-0000-000000000001';
const crearToken = (activeRole: string | undefined, roles: string[]) => [
  Buffer.from(JSON.stringify({ alg: 'none', typ: 'JWT' }), 'utf8').toString('base64url'),
  Buffer.from(JSON.stringify({
    sub: '11111111-1111-1111-1111-111111111111',
    tenant_id: tenantId,
    ...(activeRole ? { active_role: activeRole } : {}),
    roles,
  }), 'utf8').toString('base64url'),
  '',
].join('.');

const token = crearToken('ADMIN', ['ADMIN']);

test.beforeEach(async ({ page }) => {
  await page.addInitScript((jwt: string) => {
    const bridge = { getToken: async () => jwt, setToken: async () => undefined, logout: async () => undefined };
    Object.defineProperty(window, 'api', { value: bridge });
  }, token);
  await page.route('**/sucursales', route => route.fulfill({ json: [{ id: '22222222-2222-2222-2222-222222222222', nombre: 'Centro', activa: true, direccion: 'Calle 1', zonaHoraria: 'America/Santiago' }] }));
});

test('ADMIN activo visualiza el acceso administrativo desde POS', async ({ page }) => {
  await page.goto('/?component=app#/pos');
  await expect(page.getByRole('link', { name: 'Volver a administración' })).toBeVisible();
});

test('CAJERO activo con ADMIN asignado no ve ni abre rutas administrativas', async ({ browser }) => {
  const context = await browser.newContext();
  const page = await context.newPage();
  const tokenCajeroMultirol = crearToken('CAJERO', ['ADMIN', 'CAJERO']);

  await page.addInitScript((jwt: string) => {
    const bridge = { getToken: async () => jwt, setToken: async () => undefined, logout: async () => undefined };
    Object.defineProperty(window, 'api', { value: bridge });
  }, tokenCajeroMultirol);

  await page.goto('/?component=app#/pos');
  await expect(page.getByRole('link', { name: 'Volver a administración' })).toHaveCount(0);

  await page.goto('/?component=app#/admin');
  await expect(page.getByRole('alert')).toContainText('Acceso denegado');
  await expect(page.getByRole('heading', { name: 'GlobalMart Admin' })).toHaveCount(0);

  await page.goto('/?component=app#/admin/usuarios');
  await expect(page.getByRole('alert')).toContainText('Acceso denegado');
  await expect(page.getByRole('heading', { name: 'GlobalMart Admin' })).toHaveCount(0);

  await page.goto('/?component=app#/admin/productos');
  await expect(page.getByRole('alert')).toContainText('Acceso denegado');
  await expect(page.getByRole('heading', { name: 'CatÃ¡logo de Productos' })).toHaveCount(0);

  await context.close();
});

test('sin sesión conserva la redirección al login', async ({ browser }) => {
  const context = await browser.newContext();
  const page = await context.newPage();

  await page.addInitScript(() => {
    const bridge = { getToken: async () => null, setToken: async () => undefined, logout: async () => undefined };
    Object.defineProperty(window, 'api', { value: bridge });
  });

  await page.goto('/?component=app#/admin');
  await expect(page).toHaveURL(/#\/login$/);

  await context.close();
});

test('ADMIN abre vista propia y crea un cajero con datos copiables', async ({ page }) => {
  let createBody: Record<string, unknown> | undefined;
  await page.route('**/users*', async route => {
    const request = route.request();
    if (request.method() === 'POST' && new URL(request.url()).pathname.endsWith('/users')) {
      createBody = request.postDataJSON();
      await route.fulfill({ status: 201, json: { id: '33333333-3333-3333-3333-333333333333', roles: ['CAJERO'] } });
      return;
    }
    await route.fulfill({ json: { items: [], totalPages: 1 } });
  });

  await page.goto('/?component=app#/');
  await expect(page).toHaveURL(/#\/admin$/);
  await expect(page.getByRole('heading', { name: 'GlobalMart Admin' })).toBeVisible();
  await expect(page.getByText('Punto de Venta', { exact: true })).toHaveCount(0);
  await page.getByRole('link', { name: 'Usuarios', exact: true }).click();
  await page.getByRole('button', { name: 'Crear usuario' }).click();
  await page.getByLabel('Nombre', { exact: true }).fill('Cajero Nuevo');
  await page.getByLabel('Correo electrónico').fill('nuevo@example.cl');
  await page.getByLabel('Sucursal inicial').selectOption('22222222-2222-2222-2222-222222222222');
  await page.getByRole('dialog').getByRole('button', { name: 'Crear usuario' }).click();

  await expect.poll(() => createBody?.tenantId).toBe(tenantId);
  expect(createBody?.email).toBe('nuevo@example.cl');
  expect(createBody?.roles).toEqual(['CAJERO']);
  expect(createBody?.sucursalId).toBe('22222222-2222-2222-2222-222222222222');
  await expect(page.getByLabel('Datos de acceso')).toContainText('nuevo@example.cl');
  await expect(page.getByLabel('Datos de acceso')).toContainText('Contraseña inicial:');
});

test('inventario consulta y registra un ajuste', async ({ page }) => {
  let adjustment: Record<string, unknown> | undefined;
  await page.route('**/products?*', route => route.fulfill({ json: { data: [{ id: '44444444-4444-4444-4444-444444444444', nombre: 'Arroz' }], totalCount: 1, page: 1, pageSize: 500 } }));
  await page.route('**/stock/ajuste', async route => {
    adjustment = route.request().postDataJSON();
    await route.fulfill({ json: { ok: true } });
  });
  await page.route('**/stock?*', route => route.fulfill({ json: [] }));

  await page.goto('/?component=app#/admin/inventario');
  await expect(page.getByRole('heading', { name: 'Inventario por sucursal' })).toBeVisible();
  await page.getByLabel('Cantidad actual').fill('12.5');
  await page.getByLabel('Stock mínimo').fill('2');
  await page.getByLabel('Motivo').fill('Conteo inicial');
  await page.getByRole('button', { name: 'Guardar ajuste' }).click();
  await expect.poll(() => adjustment?.cantidadActual).toBe(12.5);
  expect(adjustment?.productoId).toBe('44444444-4444-4444-4444-444444444444');
  expect(adjustment?.sucursalId).toBe('22222222-2222-2222-2222-222222222222');
});

test('inventario muestra productos de páginas posteriores del catálogo', async ({ page }) => {
  await page.route('**/products?*', route => {
    const pageNumber = new URL(route.request().url()).searchParams.get('page');
    const product = pageNumber === '1'
      ? { id: '44444444-4444-4444-4444-444444444444', nombre: 'Arroz' }
      : { id: '55555555-5555-5555-5555-555555555555', nombre: 'Leche' };
    return route.fulfill({ json: { data: [product], totalCount: 2, page: Number(pageNumber), pageSize: 1 } });
  });
  await page.route('**/stock?*', route => route.fulfill({ json: [] }));

  await page.goto('/?component=app#/admin/inventario');
  await expect(page.getByRole('form', { name: 'Registrar ajuste' }).getByLabel('Producto')).toContainText('Leche');
});

test.describe('recepción de mercadería', () => {
  const productoId = '44444444-4444-4444-4444-444444444444';
  const sucursalId = '33333333-3333-3333-3333-333333333333';
  const fechaVencimiento = new Date(Date.now() + 86_400_000).toISOString().slice(0, 10);
  const rutaRecepcion = /\/api\/recepciones$/;

  test.beforeEach(async ({ page }) => {
    await page.route('**/products?*', route => route.fulfill({ json: {
      data: [
        { id: productoId, nombre: 'Arroz', isActive: true },
        { id: '55555555-5555-5555-5555-555555555555', nombre: 'Producto inactivo', isActive: false },
      ],
      totalCount: 2,
    } }));
    await page.route('**/sucursales', route => route.fulfill({ json: [
      { id: '22222222-2222-2222-2222-222222222222', nombre: 'Centro', activa: true },
      { id: sucursalId, nombre: 'Norte', activa: true },
      { id: '66666666-6666-6666-6666-666666666666', nombre: 'Sucursal inactiva', activa: false },
    ] }));
  });

  async function prepararFormulario(page: import('@playwright/test').Page) {
    await page.goto('/?component=app#/admin/inventario');
    const form = page.getByRole('form', { name: 'Recepción de mercadería' });
    const articulo = form.getByLabel('Artículo a recibir');
    const sucursal = form.getByLabel('Sucursal');
    await articulo.selectOption(productoId);
    await sucursal.selectOption(sucursalId);
    await expect(articulo.locator('option')).toHaveCount(2);
    await expect(sucursal.locator('option')).toHaveCount(3);
    await form.getByLabel('Cantidad recibida').fill('2.125');
    await form.getByLabel('Fecha de vencimiento').fill(fechaVencimiento);
    await form.getByLabel('Ubicación (opcional)').fill(' Estante A ');
    return form;
  }

  test('201 envía ID y JWT una sola vez y refresca stock de la sucursal seleccionada', async ({ page }) => {
    let posts = 0;
    let confirmado = false;
    const consultas: Array<string | null> = [];
    let liberarPost!: () => void;
    const espera = new Promise<void>(resolve => { liberarPost = resolve; });
    await page.route('**/stock?*', route => {
      const sucursal = new URL(route.request().url()).searchParams.get('sucursal_id');
      consultas.push(sucursal);
      return route.fulfill({ json: [{ productoId, sucursalId: sucursal, cantidadActual: confirmado ? 7.125 : 5, stockMinimo: 1 }] });
    });
    await page.route(rutaRecepcion, async route => {
      posts++;
      expect(route.request().method()).toBe('POST');
      expect(route.request().headers().authorization).toBe(`Bearer ${token}`);
      expect(route.request().postDataJSON()).toEqual({
        productoId, sucursalId, cantidad: 2.125, fechaVencimiento, ubicacion: 'Estante A',
      });
      await espera;
      confirmado = true;
      await route.fulfill({ status: 201, json: { id: 'lote-fixture' } });
    });
    const form = await prepararFormulario(page);
    const fila = page.getByRole('row').filter({ hasText: 'Arroz' });
    await expect(fila.getByRole('cell', { name: '5', exact: true })).toBeVisible();
    const lecturasPrevias = consultas.length;
    try {
      await form.evaluate(element => {
        (element as HTMLFormElement).requestSubmit();
        (element as HTMLFormElement).requestSubmit();
      });
      await expect.poll(() => posts).toBe(1);
      await expect(form.getByRole('button', { name: 'Registrando…' })).toBeDisabled();
      await expect(form.getByRole('status')).toHaveCount(0);
      expect(consultas).toHaveLength(lecturasPrevias);
    } finally {
      liberarPost();
    }
    await expect(form.getByRole('status')).toHaveText('Recepción registrada.');
    await expect(fila.getByRole('cell', { name: '7.125', exact: true })).toBeVisible();
    expect(consultas.slice(lecturasPrevias)).toEqual([sucursalId]);
    expect(posts).toBe(1);
  });

  for (const [status, mensaje] of [
    [400, 'Revisa los datos'],
    [401, 'Vuelve a iniciar sesión'],
    [403, 'No tienes permisos'],
    [404, 'no está disponible'],
    [503, 'no pudo validar'],
    [0, 'No se pudo conectar con MS-4'],
  ] as const) {
    test(`explica ${status || 'el fallo de red'} y conserva los datos sin refrescar stock`, async ({ page }) => {
      let posts = 0;
      let lecturasTrasPost = 0;
      await page.route('**/stock?*', route => {
        if (posts) lecturasTrasPost++;
        const sucursal = new URL(route.request().url()).searchParams.get('sucursal_id');
        return route.fulfill({ json: [{ productoId, sucursalId: sucursal, cantidadActual: 5, stockMinimo: 1 }] });
      });
      await page.route(rutaRecepcion, route => {
        posts++;
        return status ? route.fulfill({ status, json: {} }) : route.abort('connectionfailed');
      });
      const form = await prepararFormulario(page);
      const stock = page.getByRole('cell', { name: '5', exact: true });
      await expect(stock).toBeVisible();
      await form.getByRole('button', { name: 'Registrar recepción' }).click();
      await expect(form.getByRole('alert')).toContainText(mensaje);
      await expect(form.getByRole('status')).toHaveCount(0);
      await expect(form.getByLabel('Artículo a recibir')).toHaveValue(productoId);
      await expect(form.getByLabel('Sucursal')).toHaveValue(sucursalId);
      await expect(form.getByLabel('Cantidad recibida')).toHaveValue('2.125');
      await expect(form.getByLabel('Fecha de vencimiento')).toHaveValue(fechaVencimiento);
      await expect(form.getByLabel('Ubicación (opcional)')).toHaveValue(' Estante A ');
      await expect(stock).toBeVisible();
      expect(posts).toBe(1);
      expect(lecturasTrasPost).toBe(0);
    });
  }

  test('un GET fallido después del 201 permite reintentar solo la lectura', async ({ page }) => {
    let posts = 0;
    let lecturasTrasPost = 0;
    await page.route(rutaRecepcion, route => {
      posts++;
      return route.fulfill({ status: 201, json: { id: 'lote-fixture' } });
    });
    await page.route('**/stock?*', route => {
      if (posts && ++lecturasTrasPost === 1) return route.fulfill({ status: 503 });
      const sucursal = new URL(route.request().url()).searchParams.get('sucursal_id');
      return route.fulfill({ json: [{ productoId, sucursalId: sucursal, cantidadActual: posts ? 7.125 : 5, stockMinimo: 1 }] });
    });
    const form = await prepararFormulario(page);
    await expect(page.getByRole('cell', { name: '5', exact: true })).toBeVisible();
    await form.getByRole('button', { name: 'Registrar recepción' }).click();
    await expect(form.getByRole('status')).toHaveText('Recepción registrada.');
    await expect(page.getByRole('alert')).toContainText('No se pudo consultar el inventario');
    await page.getByRole('button', { name: 'Reintentar consulta de stock' }).click();
    await expect(page.getByRole('cell', { name: '7.125', exact: true })).toBeVisible();
    await expect(form.getByRole('status')).toHaveText('Recepción registrada.');
    expect(lecturasTrasPost).toBe(2);
    expect(posts).toBe(1);
  });

  test('SUPER_ADMIN ve inventario y el formulario está reservado a ADMIN activo', async ({ browser }) => {
    const page = await browser.newPage();
    try {
      await page.addInitScript(jwt => {
        Object.defineProperty(window, 'api', { value: { getToken: async () => jwt } });
      }, crearToken('SUPER_ADMIN', ['SUPER_ADMIN', 'ADMIN']));
      await page.route('**/products?*', route => route.fulfill({ json: { data: [], totalCount: 0 } }));
      await page.route('**/sucursales', route => route.fulfill({ json: [] }));
      await page.goto('/?component=app#/admin/inventario');
      await expect(page.getByRole('heading', { name: 'Inventario por sucursal' })).toBeVisible();
      await expect(page.getByRole('form', { name: 'Recepción de mercadería' })).toHaveCount(0);
    } finally {
      await page.close();
    }
  });
});

test('ADMIN crea y elimina un rol propio del tenant', async ({ page }) => {
  const roles = [
    { id: '11111111-0000-0000-0000-000000000001', nombre: 'CAJERO', descripcion: 'Caja', tenantId: null, rolBase: null },
    { id: '11111111-0000-0000-0000-000000000003', nombre: 'ADMIN', descripcion: 'Admin', tenantId: null, rolBase: null },
  ];
  await page.route('**/users*', route => route.fulfill({ json: { items: [], totalPages: 1 } }));
  await page.route('**/users/roles**', async route => {
    if (route.request().method() === 'POST') {
      const body = route.request().postDataJSON();
      roles.push({ id: '66666666-6666-6666-6666-666666666666', nombre: body.nombre, descripcion: body.descripcion, tenantId, rolBase: body.rolBase });
      return route.fulfill({ status: 201, json: roles.at(-1) });
    }
    if (route.request().method() === 'DELETE') {
      roles.pop();
      return route.fulfill({ status: 204 });
    }
    return route.fulfill({ json: roles });
  });

  await page.goto('/?component=app#/admin/roles');
  await page.getByLabel('Nombre del rol').fill('SUPERVISOR_CAJA');
  await page.getByLabel('Descripción').fill('Supervisa caja');
  await page.getByLabel('Permisos base').selectOption('ADMIN');
  await page.getByRole('button', { name: 'Crear rol' }).click();
  await expect(page.getByText('SUPERVISOR_CAJA', { exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Eliminar rol' }).click();
  await expect(page.getByText('SUPERVISOR_CAJA', { exact: true })).toHaveCount(0);
});

test('ADMIN asigna un rol propio al crear un usuario', async ({ page }) => {
  let createBody: Record<string, unknown> | undefined;
  await page.route('**/users*', route => {
    if (route.request().method() === 'POST') {
      createBody = route.request().postDataJSON();
      return route.fulfill({ status: 201, json: { id: '77777777-7777-7777-7777-777777777777', roles: ['SUPERVISOR_CAJA'] } });
    }
    return route.fulfill({ json: { items: [], totalPages: 1 } });
  });
  await page.route('**/users/roles', route => route.fulfill({ json: [
    { id: '11111111-0000-0000-0000-000000000001', nombre: 'CAJERO' },
    { id: '66666666-6666-6666-6666-666666666666', nombre: 'SUPERVISOR_CAJA' },
  ] }));

  await page.goto('/?component=app#/admin/usuarios');
  await page.getByRole('button', { name: 'Crear usuario' }).click();
  await page.getByLabel('Nombre', { exact: true }).fill('Supervisor Nuevo');
  await page.getByLabel('Correo electrónico').fill('supervisor@example.cl');
  await page.getByLabel('Rol', { exact: true }).selectOption('SUPERVISOR_CAJA');
  await page.getByRole('dialog').getByRole('button', { name: 'Crear usuario' }).click();
  await expect.poll(() => createBody?.roles).toEqual(['SUPERVISOR_CAJA']);
});

#!/usr/bin/env node
/**
 * Script de verificación en vivo para S3-028:
 * Ejecuta login, apertura de turno, búsqueda en catálogo, creación de venta,
 * cobro en efectivo, consulta GET /ventas/{id} y cierre de turno en modo `remote` o `local`.
 *
 * Uso:
 *   node scripts/verify-s3-028.mjs --mode=remote
 *   node scripts/verify-s3-028.mjs --mode=local
 */

const modeArg = process.argv.find((arg) => arg.startsWith('--mode='))?.split('=')[1] ?? 'remote';
const mode = modeArg === 'local' ? 'local' : 'remote';

const MODE_URLS = {
  remote: {
    identity: 'https://auth-rpedraza.dev.censei.cl/api',
    catalog: 'https://catalog-rpedraza.dev.censei.cl/api',
    pos: 'https://pos-rpedraza.dev.censei.cl/api',
  },
  local: {
    identity: 'http://localhost:8000/api',
    catalog: 'http://localhost:8000/api',
    pos: 'http://localhost:8000/api',
  },
};

const urls = MODE_URLS[mode];
const tenantId = process.env.DEMO_TENANT_ID || 'aaaaaaaa-0000-0000-0000-000000000001';
const email = process.env.DEMO_EMAIL || 'cajero@demo.cl';
const password = process.env.DEMO_PASSWORD || 'demo1234';

async function run() {
  console.log(`\n=== S3-028 Verificación en vivo (API_MODE=${mode}) ===`);
  console.log(`URLs: identity=${urls.identity} | catalog=${urls.catalog} | pos=${urls.pos}`);

  // 1. Login
  const loginUrl = `${urls.identity}/auth/login`;
  const loginRes = await fetch(loginUrl, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'x-tenant-id': tenantId },
    body: JSON.stringify({ email, password, tenantId }),
  });
  const loginBody = await loginRes.json().catch(() => null);
  console.log(`[1] POST ${loginUrl} -> HTTP ${loginRes.status}`);
  if (!loginRes.ok || !loginBody?.token) {
    throw new Error(`Login falló (${loginRes.status}): ${JSON.stringify(loginBody)}`);
  }

  const jwtPayload = JSON.parse(Buffer.from(loginBody.token.split('.')[1], 'base64url').toString());
  const sucursalId = jwtPayload.sucursal_id || 'bbbbbbbb-0000-0000-0000-000000000001';
  const headers = {
    'Content-Type': 'application/json',
    Authorization: `Bearer ${loginBody.token}`,
  };

  // 2. Verificar/abrir turno
  let shiftOpenedByScript = false;
  const checkShiftUrl = `${urls.pos}/turnos/activo`;
  const checkShiftRes = await fetch(checkShiftUrl, { headers });
  console.log(`[2] GET  ${checkShiftUrl} -> HTTP ${checkShiftRes.status}`);

  if (checkShiftRes.status === 404) {
    const openUrl = `${urls.pos}/turnos/abrir`;
    const openRes = await fetch(openUrl, {
      method: 'POST',
      headers,
      body: JSON.stringify({ sucursal_id: sucursalId, monto_fondo_inicial: 20000 }),
    });
    const openBody = await openRes.json().catch(() => null);
    console.log(`[3] POST ${openUrl} -> HTTP ${openRes.status} (turno=${openBody?.id})`);
    if (!openRes.ok) {
      throw new Error(`Apertura de turno falló (${openRes.status}): ${JSON.stringify(openBody)}`);
    }
    shiftOpenedByScript = true;
  } else if (!checkShiftRes.ok) {
    throw new Error(`Error inesperado consultando turno activo: HTTP ${checkShiftRes.status}`);
  }

  try {
    // 4. Catálogo
    const productsUrl = `${urls.catalog}/products?page=1&pageSize=3`;
    const productsRes = await fetch(productsUrl, { headers });
    const productsBody = await productsRes.json().catch(() => null);
    console.log(`[4] GET  ${productsUrl} -> HTTP ${productsRes.status} (items=${productsBody?.data?.length ?? 0})`);
    if (!productsRes.ok || !productsBody?.data?.length) {
      throw new Error(`Catálogo sin productos (${productsRes.status})`);
    }
    const product = productsBody.data[0];

    // 5. Crear venta
    const createSaleUrl = `${urls.pos}/ventas`;
    const createRes = await fetch(createSaleUrl, {
      method: 'POST',
      headers,
      body: JSON.stringify({
        items: [{ producto_id: product.id, cantidad: 1 }],
        metodo_pago: 'EFECTIVO',
      }),
    });
    const sale = await createRes.json().catch(() => null);
    console.log(`[5] POST ${createSaleUrl} -> HTTP ${createRes.status} (venta=${sale?.id}, total=${sale?.total}, estado=${sale?.estado})`);
    if (!createRes.ok || !sale?.id) {
      throw new Error(`Crear venta falló (${createRes.status}): ${JSON.stringify(sale)}`);
    }

    // 6. Cobro en efectivo
    const paid = Math.ceil(sale.total / 1000) * 1000 + 1000;
    const chargeUrl = `${urls.pos}/ventas/${sale.id}/cobrar`;
    const chargeRes = await fetch(chargeUrl, {
      method: 'POST',
      headers,
      body: JSON.stringify({ monto_recibido: paid }),
    });
    const charge = await chargeRes.json().catch(() => null);
    console.log(`[6] POST ${chargeUrl} -> HTTP ${chargeRes.status} (estado=${charge?.venta?.estado}, recibido=${charge?.montoRecibido}, vuelto=${charge?.vuelto})`);
    if (!chargeRes.ok || charge?.venta?.estado !== 'COMPLETADA') {
      throw new Error(`Cobro en efectivo falló (${chargeRes.status}): ${JSON.stringify(charge)}`);
    }

    // 7. Consultar venta persistida
    const getSaleUrl = `${urls.pos}/ventas/${sale.id}`;
    const getSaleRes = await fetch(getSaleUrl, { headers });
    const fetched = await getSaleRes.json().catch(() => null);
    console.log(`[7] GET  ${getSaleUrl} -> HTTP ${getSaleRes.status} (estado=${fetched?.estado}, total=${fetched?.total})`);
    if (!getSaleRes.ok || fetched?.estado !== 'COMPLETADA') {
      throw new Error(`Verificación GET /ventas/{id} falló (${getSaleRes.status})`);
    }
  } finally {
    if (shiftOpenedByScript) {
      const closeUrl = `${urls.pos}/turnos/cerrar`;
      const closeRes = await fetch(closeUrl, { method: 'POST', headers });
      console.log(`[8] POST ${closeUrl} -> HTTP ${closeRes.status}`);
    }
  }

  console.log(`=== OK: Flujo completo verificado en modo ${mode} ===\n`);
}

run().catch((err) => {
  console.error(`ERROR en modo ${mode}:`, err.message);
  process.exit(1);
});

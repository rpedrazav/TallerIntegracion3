#!/usr/bin/env node
/**
 * S3-056 — E2E de recepción y FEFO contra docker compose (Kafka real) y MS-1, MS-3, MS-4, MS-5 en localhost.
 * Recibe dos lotes (el tardío primero), vende 3 unidades y comprueba que baja el lote que vence antes.
 * Requiere: docker compose (PostgreSQL + Kafka), run-apis (MS-1, MS-3, MS-4, MS-5). Uso: node scripts/e2e-fefo.mjs
 */
import assert from 'node:assert/strict';

const MS1 = 'http://localhost:5124', MS3 = 'http://localhost:5203', MS4 = 'http://localhost:5205', MS5 = 'http://localhost:5000';
const tenantId = 'aaaaaaaa-0000-0000-0000-000000000001';

async function call(url, token, body, method = body ? 'POST' : 'GET') {
  const res = await fetch(url, {
    method,
    headers: { 'Content-Type': 'application/json', 'x-tenant-id': tenantId, ...(token && { Authorization: `Bearer ${token}` }) },
    body: body && JSON.stringify(body),
  });
  return { status: res.status, body: await res.json().catch(() => null) };
}
const login = async email => (await call(`${MS1}/auth/login`, null, { email, password: 'demo1234', tenantId })).body.token;
const dia = n => new Date(Date.now() + n * 864e5).toISOString().slice(0, 10);
const ok = (r, what) => (assert.ok(r.status < 300, `${what} → HTTP ${r.status} ${JSON.stringify(r.body)}`), r.body);

const admin = await login('admin@demo.cl');
const cajero = await login('cajero@demo.cl');
const run = Date.now().toString();

// El JWT del cajero demo no trae sucursal; se usa la primera del tenant o se crea una.
const sucursales = ok(await call(`${MS1}/sucursales`, admin), 'GET /sucursales');
const sucursalId = (sucursales[0] ?? ok(await call(`${MS1}/sucursales`, admin, { nombre: `E2E FEFO ${run}`, direccion: 'Calle Falsa 123' }), 'POST /sucursales')).id;

// Producto nuevo: ningún otro lote puede vencer antes que los de esta prueba.
const uomBaseId = ok(await call(`${MS3}/products?page=1&pageSize=1`, admin), 'GET /products').data[0].uomBaseId;
const producto = ok(await call(`${MS3}/products`, admin, {
  nombre: `E2E FEFO ${run}`, codigoBarras: `780034${run.slice(-7)}`, uomBaseId, precioBase: 1000, esPesoVariable: false, isActive: true,
}), 'POST /products');

// 1. Recepción de dos lotes; el que vence después se recibe primero para que el orden no sea el de llegada.
const recibir = async (cantidad, fechaVencimiento, ubicacion) =>
  ok(await call(`${MS4}/recepciones`, admin, { productoId: producto.id, sucursalId, cantidad, fechaVencimiento, ubicacion }), `POST /recepciones ${ubicacion}`);
const tardio = await recibir(5, dia(60), 'E2E-tardio');
const temprano = await recibir(5, dia(10), 'E2E-temprano');
console.log(`[1] Lotes recibidos: tardío ${tardio.id} (+60 d), temprano ${temprano.id} (+10 d)`);

const lotes = async () => (ok(await call(`${MS4}/lotes?porVencer=365&sucursal_id=${sucursalId}`, admin), 'GET /lotes'))
  .filter(l => l.productoId === producto.id);
const cantidad = (ls, id) => ls.find(l => l.id === id)?.cantidad ?? 0;

let antes = await lotes();
assert.equal(cantidad(antes, temprano.id), 5);
assert.equal(cantidad(antes, tardio.id), 5);

// 2. Venta en efectivo de 3 unidades con turno abierto.
const cerrarTurno = async () => {
  await call(`${MS5}/api/turnos/cuadre`, cajero, { monto_declarado: 0 });
  return call(`${MS5}/api/turnos/cerrar`, cajero, {}, 'POST');
};
// Un turno previo puede ser de otra sucursal: se cierra y se abre uno en la de la prueba.
if ((await call(`${MS5}/api/turnos/activo`, cajero)).status === 200) ok(await cerrarTurno(), 'cerrar turno previo');
ok(await call(`${MS5}/api/turnos/abrir`, cajero, { sucursal_id: sucursalId, monto_fondo_inicial: 20000 }), 'abrir turno');
try {
  const venta = ok(await call(`${MS5}/api/ventas`, cajero, { items: [{ producto_id: producto.id, cantidad: 3 }], metodo_pago: 'EFECTIVO' }), 'crear venta');
  const cobro = ok(await call(`${MS5}/api/ventas/${venta.id}/cobrar`, cajero, { monto_recibido: Math.ceil(venta.total / 1000) * 1000 + 1000 }), 'cobrar');
  assert.equal(cobro.venta.estado, 'COMPLETADA');
  console.log(`[2] Venta ${venta.id} COMPLETADA (3 unidades, total ${venta.total})`);
} finally {
  await cerrarTurno();
}

// 3. sale.completed viaja por outbox → Kafka → MS-4. Se espera hasta 60 s a que baje el lote.
let despues;
for (let i = 0; i < 30; i++) {
  despues = await lotes();
  if (cantidad(despues, temprano.id) !== 5) break;
  await new Promise(r => setTimeout(r, 2000));
}
assert.equal(cantidad(despues, temprano.id), 2, 'El lote que vence antes baja primero (5 − 3)');
assert.equal(cantidad(despues, tardio.id), 5, 'El lote tardío no se toca');
const stock = ok(await call(`${MS4}/stock/${producto.id}?sucursal_id=${sucursalId}`, admin), 'GET /stock/{id}');
assert.equal(stock.cantidadActual, 7, 'Stock agregado 10 − 3');
console.log('[3] OK: temprano 5→2, tardío 5→5, stock agregado 7. FEFO verificado de punta a punta.');

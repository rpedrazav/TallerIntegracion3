import { readFile, writeFile } from 'node:fs/promises';
const fixtures = JSON.parse(await readFile('docs/qa/S3-099/ui-resultados.json', 'utf8'));
const results = [];
for (const f of fixtures) {
  const login = await fetch('http://localhost:5124/api/auth/login', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ tenantCodigo: f.tenant, email: `cajero@${f.tenant}.cl`, password: 'demo1234' }) });
  const { token } = await login.json();
  if (!token) throw new Error('Login cajero falló');
  const headers = { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` };
  async function call(path, body) {
    const r = await fetch('http://localhost:5000/api/' + path, { headers, method: body ? 'POST' : 'GET', body: body ? JSON.stringify(body) : undefined });
    const text = await r.text(); let response; try { response = JSON.parse(text); } catch { response = text; }
    results.push({ tenant: f.tenant, path, status: r.status, response });
    return { status: r.status, response };
  }
  const active = await call('turnos/activo');
  let opened = false;
  if (active.status === 404) {
    const turn = await call('turnos/abrir', { sucursal_id: f.branch, monto_fondo_inicial: 0 });
    if (turn.status !== 200) continue;
    opened = true;
  } else if (active.status !== 200 || (active.response.sucursalId ?? active.response.sucursal_id) !== f.branch) continue;
  try {
    const sale = await call('ventas', { items: [{ producto_id: f.product, cantidad: 1 }], metodo_pago: 'EFECTIVO' });
    if (sale.status !== 201) continue;
    await call(`ventas/${sale.response.id}/cobrar`, { monto_recibido: sale.response.total });
    await new Promise(resolve => setTimeout(resolve, 6000));
    const r = await fetch(`http://localhost:5205/api/stock?sucursal_id=${f.branch}`, { headers });
    results.push({ tenant: f.tenant, path: 'stock después de venta (6s)', status: r.status, response: await r.json() });
  } finally { if (opened) await call('turnos/cerrar', {}); }
}
await writeFile('docs/qa/S3-099/venta-resultados.json', JSON.stringify(results, null, 2) + '\n');
console.log(results.map(x => ({ tenant:x.tenant,path:x.path,status:x.status,estado:x.response?.venta?.estado })));

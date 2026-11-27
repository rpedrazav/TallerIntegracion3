import { mkdir, writeFile } from 'node:fs/promises';
const results = [];
async function request(name, url, token, body) {
  try {
    const r = await fetch(url, { method: body ? 'POST' : 'GET', headers: { ...(token ? { Authorization: `Bearer ${token}` } : {}), 'Content-Type': 'application/json' }, body: body ? JSON.stringify(body) : undefined, signal: AbortSignal.timeout(12000) });
    const text = await r.text();
    let data; try { data = JSON.parse(text); } catch { data = text.slice(0, 200); }
    results.push({ name, url, status: r.status, response: data?.token ? { tokenReceived: true } : data });
    return data;
  } catch (e) { results.push({ name, url, error: e.message, cause: e.cause?.code }); }
}
for (const [code, n] of [['demo', '1'], ['donpepe', '2']]) {
  const branch = `cccccccc-0000-0000-0000-00000000000${n}`;
  const login = await request(`${code}: login local`, 'http://localhost:5124/api/auth/login', null, { tenantCodigo: code, email: `admin@${code}.cl`, password: 'demo1234' });
  if (login?.token) {
    const token = login.token;
    await request(`${code}: sucursales`, 'http://localhost:5124/api/sucursales', token);
    await request(`${code}: catalogo`, 'http://localhost:5203/api/products?page=1&pageSize=100', token);
    await request(`${code}: stock directo`, `http://localhost:5205/api/stock?sucursal_id=${branch}`, token);
    await request(`${code}: stock Kong`, `http://localhost:8000/api/stock?sucursal_id=${branch}`, token);
    await request(`${code}: lotes`, `http://localhost:5205/api/lotes?porVencer=30&sucursal_id=${branch}`, token);
    if (code === 'donpepe') await request(`${code}: seed`, 'http://localhost:5205/seed', token, {});
    const foreign = n === '1' ? '2' : '1';
    await request(`${code}: stock sucursal ajena`, `http://localhost:5205/api/stock?sucursal_id=cccccccc-0000-0000-0000-00000000000${foreign}`, token);
  }
  await request(`${code}: login remoto contrato actual`, 'https://auth-rpedraza.dev.censei.cl/api/auth/login', null, { tenantCodigo: code, email: `admin@${code}.cl`, password: 'demo1234' });
}
await request('cluster: salud inventario TLS', 'https://inventory-rpedraza.dev.censei.cl/health');
await mkdir('docs/qa/S3-099', { recursive: true });
await writeFile('docs/qa/S3-099/api-resultados.json', JSON.stringify({ date: new Date().toISOString(), results }, null, 2) + '\n');
console.log(results.map(({name,status,error,response}) => ({name,status,error,count:Array.isArray(response)?response.length:response?.totalCount})));

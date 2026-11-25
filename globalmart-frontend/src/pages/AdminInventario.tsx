import React, { useEffect, useRef, useState } from 'react';
import axios from 'axios';
import { catalogApi, CatalogProduct } from '../api/client';
import { buildServiceUrl } from '../config/api';
import { useAuth } from '../hooks/useAuth';
import { parseJwt } from '../utils/jwt';
import RecepcionMercaderiaForm from '../components/admin/RecepcionMercaderiaForm';

interface Sucursal { id: string; nombre: string; activa: boolean }
interface Stock { productoId: string; sucursalId: string; cantidadActual: number; stockMinimo: number }

export default function AdminInventario() {
  const { token } = useAuth();
  const esAdmin = parseJwt(token)?.active_role?.toUpperCase() === 'ADMIN';
  const [productos, setProductos] = useState<CatalogProduct[]>([]);
  const [sucursales, setSucursales] = useState<Sucursal[]>([]);
  const [stock, setStock] = useState<Stock[]>([]);
  const [sucursalId, setSucursalId] = useState('');
  const [productoId, setProductoId] = useState('');
  const [cantidad, setCantidad] = useState('');
  const [minimo, setMinimo] = useState('');
  const [motivo, setMotivo] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [saving, setSaving] = useState(false);
  const [recibiendo, setRecibiendo] = useState(false);
  const [errorStock, setErrorStock] = useState(false);
  const [consultandoStock, setConsultandoStock] = useState(false);
  const consultaStock = useRef(0);
  const headers = { Authorization: `Bearer ${token}` };

  useEffect(() => {
    if (!token) return;
    let active = true;
    const cargarProductos = async () => {
      const first = await catalogApi.listProducts(1, 500);
      const all = [...first.data];
      for (let page = 2; all.length < first.totalCount; page++) {
        const next = await catalogApi.listProducts(page, 500);
        if (!next.data.length) break;
        all.push(...next.data);
      }
      return all;
    };
    Promise.all([
      cargarProductos(),
      axios.get<Sucursal[]>(buildServiceUrl('identity', '/sucursales'), { headers }),
    ]).then(([products, branches]) => {
      if (!active) return;
      setProductos(products);
      setSucursales(branches.data);
      setSucursalId(branches.data[0]?.id ?? '');
      setProductoId(products[0]?.id ?? '');
    }).catch(() => { if (active) setError('No se pudo cargar el catálogo o las sucursales.'); });
    return () => { active = false; };
  }, [token]);

  const cargarStock = async () => {
    if (!token || !sucursalId) return;
    const consulta = ++consultaStock.current;
    setConsultandoStock(true);
    setErrorStock(false);
    try {
      const response = await axios.get<Stock[]>(buildServiceUrl('inventory', '/stock'), { headers, params: { sucursal_id: sucursalId } });
      if (consulta === consultaStock.current) setStock(response.data);
    } catch {
      if (consulta === consultaStock.current) setErrorStock(true);
    } finally {
      if (consulta === consultaStock.current) setConsultandoStock(false);
    }
  };
  useEffect(() => { void cargarStock(); }, [sucursalId, token]);

  const guardar = async (event: React.FormEvent) => {
    event.preventDefault();
    const cantidadActual = Number(cantidad);
    const stockMinimo = Number(minimo);
    if (!productoId || !sucursalId || !Number.isFinite(cantidadActual) || cantidadActual < 0 ||
      !Number.isFinite(stockMinimo) || stockMinimo < 0 || !motivo.trim()) {
      setError('Completa producto, sucursal, cantidades válidas y motivo.'); return;
    }
    setSaving(true); setError(''); setSuccess('');
    try {
      await axios.post(buildServiceUrl('inventory', '/stock/ajuste'), { productoId, sucursalId, cantidadActual, stockMinimo, motivo }, { headers });
      await cargarStock();
      setSuccess('Inventario actualizado.'); setMotivo('');
    } catch (e) { setError(axios.isAxiosError(e) ? e.response?.data?.message ?? 'No se pudo ajustar el inventario.' : 'No se pudo ajustar el inventario.'); }
    finally { setSaving(false); }
  };

  return <section>
    <h2>Inventario por sucursal</h2>
    <label>Sucursal <select disabled={recibiendo} value={sucursalId} onChange={e => setSucursalId(e.target.value)}>{sucursales.map(s => <option key={s.id} value={s.id}>{s.nombre}</option>)}</select></label>
    {error && <p role="alert">{error}</p>}{success && <p role="status">{success}</p>}
    {errorStock && <p role="alert">No se pudo consultar el inventario de la sucursal.{' '}
      <button type="button" disabled={consultandoStock || recibiendo} onClick={cargarStock}>Reintentar consulta de stock</button>
    </p>}
    <table style={{ width: '100%', margin: '1rem 0', borderCollapse: 'collapse' }}><thead><tr><th>Producto</th><th>Actual</th><th>Mínimo</th></tr></thead>
      <tbody>{stock.filter(s => s.sucursalId === sucursalId).map(s => <tr key={`${s.sucursalId}-${s.productoId}`}><td>{productos.find(p => p.id === s.productoId)?.nombre ?? s.productoId}</td><td>{s.cantidadActual}</td><td>{s.stockMinimo}</td></tr>)}</tbody></table>
    <form aria-labelledby="ajuste-inventario-titulo" onSubmit={guardar} style={{ display: 'grid', gap: '0.75rem', maxWidth: 520, background: 'var(--color-surface)', padding: '1rem', border: '1px solid var(--color-line)', borderRadius: 8 }}>
      <h3 id="ajuste-inventario-titulo">Registrar ajuste</h3>
      <label>Producto <select value={productoId} onChange={e => setProductoId(e.target.value)}>{productos.map(p => <option key={p.id} value={p.id}>{p.nombre}</option>)}</select></label>
      <label>Cantidad actual <input type="number" min="0" step="0.0001" value={cantidad} onChange={e => setCantidad(e.target.value)} required /></label>
      <label>Stock mínimo <input type="number" min="0" step="0.0001" value={minimo} onChange={e => setMinimo(e.target.value)} required /></label>
      <label>Motivo <input value={motivo} onChange={e => setMotivo(e.target.value)} maxLength={180} required /></label>
      <button type="submit" disabled={saving || recibiendo}>{saving ? 'Guardando…' : 'Guardar ajuste'}</button>
    </form>
    {esAdmin && token && <RecepcionMercaderiaForm
      productos={productos.filter(p => 'isActive' in p && p.isActive === true)}
      sucursales={sucursales.filter(s => s.activa)}
      sucursalId={sucursalId}
      onSucursalChange={setSucursalId}
      token={token}
      ajustePendiente={saving}
      onPendiente={setRecibiendo}
      onRegistrada={cargarStock}
    />}
  </section>;
}

import React, { useEffect, useState } from 'react';
import axios from 'axios';
import { catalogApi, CatalogProduct } from '../api/client';
import { buildServiceUrl } from '../config/api';
import { useAuth } from '../hooks/useAuth';

interface Sucursal { id: string; nombre: string }
interface Stock { productoId: string; sucursalId: string; cantidadActual: number; stockMinimo: number }

export default function AdminInventario() {
  const { token } = useAuth();
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
  const headers = { Authorization: `Bearer ${token}` };

  useEffect(() => {
    if (!token) return;
    let active = true;
    Promise.all([
      catalogApi.listProducts(1, 500),
      axios.get<Sucursal[]>(buildServiceUrl('identity', '/sucursales'), { headers }),
    ]).then(([products, branches]) => {
      if (!active) return;
      setProductos(products.data);
      setSucursales(branches.data);
      setSucursalId(branches.data[0]?.id ?? '');
      setProductoId(products.data[0]?.id ?? '');
    }).catch(() => { if (active) setError('No se pudo cargar el catálogo o las sucursales.'); });
    return () => { active = false; };
  }, [token]);

  const cargarStock = async () => {
    if (!sucursalId) return;
    try {
      const response = await axios.get<Stock[]>(buildServiceUrl('inventory', '/stock'), { headers, params: { sucursal_id: sucursalId } });
      setStock(response.data);
      setError('');
    } catch { setError('No se pudo consultar el inventario de la sucursal.'); }
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
    <label>Sucursal <select value={sucursalId} onChange={e => setSucursalId(e.target.value)}>{sucursales.map(s => <option key={s.id} value={s.id}>{s.nombre}</option>)}</select></label>
    {error && <p role="alert">{error}</p>}{success && <p role="status">{success}</p>}
    <table style={{ width: '100%', margin: '1rem 0', borderCollapse: 'collapse' }}><thead><tr><th>Producto</th><th>Actual</th><th>Mínimo</th></tr></thead>
      <tbody>{stock.map(s => <tr key={`${s.sucursalId}-${s.productoId}`}><td>{productos.find(p => p.id === s.productoId)?.nombre ?? s.productoId}</td><td>{s.cantidadActual}</td><td>{s.stockMinimo}</td></tr>)}</tbody></table>
    <form onSubmit={guardar} style={{ display: 'grid', gap: '0.75rem', maxWidth: 520, background: 'var(--color-surface)', padding: '1rem', border: '1px solid var(--color-line)', borderRadius: 8 }}>
      <h3>Registrar ajuste</h3>
      <label>Producto <select value={productoId} onChange={e => setProductoId(e.target.value)}>{productos.map(p => <option key={p.id} value={p.id}>{p.nombre}</option>)}</select></label>
      <label>Cantidad actual <input type="number" min="0" step="0.0001" value={cantidad} onChange={e => setCantidad(e.target.value)} required /></label>
      <label>Stock mínimo <input type="number" min="0" step="0.0001" value={minimo} onChange={e => setMinimo(e.target.value)} required /></label>
      <label>Motivo <input value={motivo} onChange={e => setMotivo(e.target.value)} maxLength={180} required /></label>
      <button type="submit" disabled={saving}>{saving ? 'Guardando…' : 'Guardar ajuste'}</button>
    </form>
  </section>;
}

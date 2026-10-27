import React, { useEffect, useState } from 'react';
import axios from 'axios';
import { useAuth } from '../hooks/useAuth';
import { buildServiceUrl } from '../config/api';

interface Sucursal { id: string; nombre: string; direccion: string; zonaHoraria: string; activa: boolean }

export default function AdminSucursales() {
  const { token } = useAuth();
  const [sucursales, setSucursales] = useState<Sucursal[]>([]);
  const [nombre, setNombre] = useState('');
  const [direccion, setDireccion] = useState('');
  const [zonaHoraria, setZonaHoraria] = useState('');
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(false);

  const cargar = async () => {
    const response = await axios.get<Sucursal[]>(buildServiceUrl('identity', '/sucursales'), {
      headers: { Authorization: `Bearer ${token}` },
    });
    setSucursales(response.data);
  };

  useEffect(() => { if (token) void cargar().catch(() => setError('No se pudieron cargar las sucursales.')); }, [token]);

  const crear = async (event: React.FormEvent) => {
    event.preventDefault();
    setSaving(true); setError('');
    try {
      await axios.post(buildServiceUrl('identity', '/sucursales'),
        { nombre: nombre.trim(), direccion: direccion.trim(), zonaHoraria: zonaHoraria.trim() || null },
        { headers: { Authorization: `Bearer ${token}` } });
      setNombre(''); setDireccion(''); setZonaHoraria('');
      await cargar();
    } catch (err) {
      setError(axios.isAxiosError(err) ? err.response?.data?.message ?? 'No se pudo crear la sucursal.' : 'No se pudo crear la sucursal.');
    } finally { setSaving(false); }
  };

  return <section>
    <h2>Sucursales</h2>
    <p>Las sucursales pertenecen al tenant de tu sesión. Puedes usarlas para asignar cajeros y gestionar stock.</p>
    {error && <p role="alert">{error}</p>}
    <table style={{ width: '100%', marginBottom: '1.5rem' }}><thead><tr><th>Nombre</th><th>Dirección</th><th>Zona horaria</th><th>Estado</th></tr></thead>
      <tbody>{sucursales.map(s => <tr key={s.id}><td>{s.nombre}</td><td>{s.direccion}</td><td>{s.zonaHoraria}</td><td>{s.activa ? 'Activa' : 'Inactiva'}</td></tr>)}</tbody></table>
    <form onSubmit={crear} style={{ display: 'grid', gap: '0.75rem', maxWidth: 520 }}>
      <h3>Nueva sucursal</h3>
      <label>Nombre <input value={nombre} onChange={e => setNombre(e.target.value)} required maxLength={200} /></label>
      <label>Dirección <input value={direccion} onChange={e => setDireccion(e.target.value)} required /></label>
      <label>Zona horaria IANA (opcional) <input value={zonaHoraria} onChange={e => setZonaHoraria(e.target.value)} placeholder="America/Santiago" /></label>
      <button type="submit" disabled={saving}>{saving ? 'Creando…' : 'Crear sucursal'}</button>
    </form>
  </section>;
}

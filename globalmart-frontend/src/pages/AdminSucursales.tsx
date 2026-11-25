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
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editNombre, setEditNombre] = useState('');
  const [editDireccion, setEditDireccion] = useState('');
  const [editZonaHoraria, setEditZonaHoraria] = useState('');
  const [confirmDeactivateId, setConfirmDeactivateId] = useState<string | null>(null);
  const [error, setError] = useState('');
  const [feedback, setFeedback] = useState('');
  const [saving, setSaving] = useState(false);

  const cargar = async () => {
    const response = await axios.get<Sucursal[]>(buildServiceUrl('identity', '/sucursales'), {
      headers: { Authorization: `Bearer ${token}` },
    });
    setSucursales(response.data);
  };

  useEffect(() => {
    if (!token) return;
    let active = true;
    axios.get<Sucursal[]>(buildServiceUrl('identity', '/sucursales'), {
      headers: { Authorization: `Bearer ${token}` },
    }).then((response) => {
      if (active) setSucursales(response.data);
    }).catch(() => {
      if (active) setError('No se pudieron cargar las sucursales.');
    });
    return () => { active = false; };
  }, [token]);

  const crear = async (event: React.FormEvent) => {
    event.preventDefault();
    setSaving(true); setError(''); setFeedback('');
    try {
      await axios.post(buildServiceUrl('identity', '/sucursales'),
        { nombre: nombre.trim(), direccion: direccion.trim(), zonaHoraria: zonaHoraria.trim() || null },
        { headers: { Authorization: `Bearer ${token}` } });
      setNombre(''); setDireccion(''); setZonaHoraria('');
      setFeedback('Sucursal creada correctamente.');
      await cargar();
    } catch (err) {
      setError(axios.isAxiosError(err) ? err.response?.data?.message ?? 'No se pudo crear la sucursal.' : 'No se pudo crear la sucursal.');
    } finally { setSaving(false); }
  };

  const iniciarEdicion = (s: Sucursal) => {
    setEditingId(s.id);
    setEditNombre(s.nombre);
    setEditDireccion(s.direccion);
    setEditZonaHoraria(s.zonaHoraria ?? '');
    setConfirmDeactivateId(null);
    setError('');
    setFeedback('');
  };

  const guardarEdicion = async (id: string) => {
    if (!editNombre.trim() || !editDireccion.trim()) {
      setError('Nombre y dirección son obligatorios.');
      return;
    }
    setSaving(true); setError(''); setFeedback('');
    try {
      await axios.put(
        buildServiceUrl('identity', `/sucursales/${id}`),
        { nombre: editNombre.trim(), direccion: editDireccion.trim(), zonaHoraria: editZonaHoraria.trim() || null },
        { headers: { Authorization: `Bearer ${token}` } },
      );
      setEditingId(null);
      setFeedback('Sucursal actualizada correctamente.');
      await cargar();
    } catch (err) {
      setError(axios.isAxiosError(err) ? err.response?.data?.message ?? 'No se pudo actualizar la sucursal.' : 'No se pudo actualizar la sucursal.');
    } finally { setSaving(false); }
  };

  const desactivar = async (id: string) => {
    setSaving(true); setError(''); setFeedback('');
    try {
      await axios.delete(buildServiceUrl('identity', `/sucursales/${id}`), {
        headers: { Authorization: `Bearer ${token}` },
      });
      setConfirmDeactivateId(null);
      setFeedback('Sucursal desactivada correctamente.');
      await cargar();
    } catch (err) {
      setError(axios.isAxiosError(err) ? err.response?.data?.message ?? 'No se pudo desactivar la sucursal.' : 'No se pudo desactivar la sucursal.');
    } finally { setSaving(false); }
  };

  return <section>
    <h2>Sucursales</h2>
    <p>Las sucursales pertenecen al tenant de tu sesión. Puedes usarlas para asignar cajeros y gestionar stock.</p>
    {error && <p role="alert">{error}</p>}
    {feedback && <p role="status">{feedback}</p>}
    <table style={{ width: '100%', marginBottom: '1.5rem' }}>
      <thead><tr><th>Nombre</th><th>Dirección</th><th>Zona horaria</th><th>Estado</th><th>Acciones</th></tr></thead>
      <tbody>
        {sucursales.map(s => (
          <tr key={s.id}>
            <td>
              {editingId === s.id
                ? <input aria-label="Editar nombre" value={editNombre} onChange={e => setEditNombre(e.target.value)} maxLength={200} />
                : s.nombre}
            </td>
            <td>
              {editingId === s.id
                ? <input aria-label="Editar dirección" value={editDireccion} onChange={e => setEditDireccion(e.target.value)} />
                : s.direccion}
            </td>
            <td>
              {editingId === s.id
                ? <input aria-label="Editar zona horaria" value={editZonaHoraria} onChange={e => setEditZonaHoraria(e.target.value)} placeholder="America/Santiago" />
                : s.zonaHoraria}
            </td>
            <td>{s.activa ? 'Activa' : 'Inactiva'}</td>
            <td>
              {editingId === s.id ? (
                <div style={{ display: 'flex', gap: '0.5rem' }}>
                  <button type="button" onClick={() => void guardarEdicion(s.id)} disabled={saving}>Guardar</button>
                  <button type="button" onClick={() => setEditingId(null)} disabled={saving}>Cancelar</button>
                </div>
              ) : confirmDeactivateId === s.id ? (
                <div style={{ display: 'flex', gap: '0.5rem' }}>
                  <button type="button" onClick={() => void desactivar(s.id)} disabled={saving}>Confirmar baja</button>
                  <button type="button" onClick={() => setConfirmDeactivateId(null)} disabled={saving}>Cancelar</button>
                </div>
              ) : (
                <div style={{ display: 'flex', gap: '0.5rem' }}>
                  <button type="button" onClick={() => iniciarEdicion(s)} disabled={saving}>Editar</button>
                  {s.activa && (
                    <button type="button" onClick={() => setConfirmDeactivateId(s.id)} disabled={saving}>Desactivar</button>
                  )}
                </div>
              )}
            </td>
          </tr>
        ))}
      </tbody>
    </table>
    <form onSubmit={crear} style={{ display: 'grid', gap: '0.75rem', maxWidth: 520 }}>
      <h3>Nueva sucursal</h3>
      <label>Nombre <input value={nombre} onChange={e => setNombre(e.target.value)} required maxLength={200} /></label>
      <label>Dirección <input value={direccion} onChange={e => setDireccion(e.target.value)} required /></label>
      <label>Zona horaria IANA (opcional) <input value={zonaHoraria} onChange={e => setZonaHoraria(e.target.value)} placeholder="America/Santiago" /></label>
      <button type="submit" disabled={saving}>{saving ? 'Creando…' : 'Crear sucursal'}</button>
    </form>
  </section>;
}

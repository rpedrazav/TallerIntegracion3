import React, { useEffect, useState } from 'react';
import axios from 'axios';
import { useAuth } from '../hooks/useAuth';
import { buildServiceUrl } from '../config/api';
import { parseJwt } from '../utils/jwt';
import type { Usuario } from '../components/admin/UsuariosList';

interface Rol { id: string; nombre: string; descripcion: string }

export default function AdminRoles() {
  const { token } = useAuth();
  const [roles, setRoles] = useState<Rol[]>([]);
  const [usuarios, setUsuarios] = useState<Usuario[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [seleccion, setSeleccion] = useState<Record<string, string[]>>({});
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [saving, setSaving] = useState<string | null>(null);
  const actorId = parseJwt(token)?.sub;

  useEffect(() => {
    if (!token) return;
    let active = true;
    const headers = { Authorization: `Bearer ${token}` };
    Promise.all([
      axios.get<Rol[]>(buildServiceUrl('identity', '/users/roles'), { headers }),
      axios.get<{ items: Usuario[]; totalPages: number }>(buildServiceUrl('identity', '/users'), { headers, params: { page, pageSize: 100 } }),
    ]).then(([roleResponse, userResponse]) => {
      if (!active) return;
      setRoles(roleResponse.data);
      setUsuarios(userResponse.data.items);
      setTotalPages(userResponse.data.totalPages);
      setSeleccion(Object.fromEntries(userResponse.data.items.map(u => [u.id, roleResponse.data.filter(r => u.roles?.includes(r.nombre)).map(r => r.id)])));
    }).catch(() => { if (active) setError('No se pudieron cargar usuarios y roles.'); });
    return () => { active = false; };
  }, [token, page]);

  const guardar = async (usuario: Usuario) => {
    const roleIds = seleccion[usuario.id] ?? [];
    if (!roleIds.length) { setError('Selecciona al menos un rol.'); return; }
    if (usuario.id === actorId && !roleIds.includes(roles.find(r => r.nombre === 'ADMIN')?.id ?? '')) {
      setError('No puedes quitar tu propio rol ADMIN.'); return;
    }
    setSaving(usuario.id);
    setError(''); setSuccess('');
    try {
      const response = await axios.post<Usuario>(buildServiceUrl('identity', `/users/${usuario.id}/roles`), { roleIds }, { headers: { Authorization: `Bearer ${token}` } });
      setUsuarios(current => current.map(u => u.id === usuario.id ? response.data : u));
      setSuccess(`Roles de ${usuario.nombre} actualizados.`);
    } catch (e) {
      setError(axios.isAxiosError(e) ? e.response?.data?.message ?? 'No se pudieron guardar los roles.' : 'No se pudieron guardar los roles.');
    } finally { setSaving(null); }
  };

  return <section>
    <h2>Roles del tenant</h2>
    <p>Asigna funciones a las cuentas de tu tienda. Los roles disponibles son globales y no se pueden crear desde un tenant.</p>
    {error && <p role="alert">{error}</p>}{success && <p role="status">{success}</p>}
    {usuarios.map(u => <div key={u.id} style={{ padding: '1rem', marginBottom: '0.75rem', border: '1px solid var(--color-line)', borderRadius: 8, background: 'var(--color-surface)' }}>
      <strong>{u.nombre}</strong> <span>{u.email}</span>
      <div style={{ display: 'flex', gap: '1rem', margin: '0.75rem 0' }}>
        {roles.map(r => <label key={r.id} title={r.descripcion}><input type="checkbox" checked={(seleccion[u.id] ?? []).includes(r.id)} onChange={event => setSeleccion(current => ({ ...current, [u.id]: event.target.checked ? [...(current[u.id] ?? []), r.id] : (current[u.id] ?? []).filter(id => id !== r.id) }))} /> {r.nombre}</label>)}
      </div>
      <button type="button" disabled={saving === u.id} onClick={() => { void guardar(u); }}>{saving === u.id ? 'Guardando…' : 'Guardar roles'}</button>
    </div>)}
    <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center' }}>
      <button type="button" disabled={page <= 1} onClick={() => setPage(p => p - 1)}>Anterior</button>
      <span>Página {page} de {totalPages}</span>
      <button type="button" disabled={page >= totalPages} onClick={() => setPage(p => p + 1)}>Siguiente</button>
    </div>
  </section>;
}

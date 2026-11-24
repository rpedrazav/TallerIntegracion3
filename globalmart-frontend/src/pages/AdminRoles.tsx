import React, { useEffect, useState } from 'react';
import axios from 'axios';
import { useAuth } from '../hooks/useAuth';
import { buildServiceUrl } from '../config/api';
import { parseJwt } from '../utils/jwt';
import type { Usuario } from '../components/admin/UsuariosList';

interface Rol { id: string; nombre: string; descripcion: string; tenantId: string | null; rolBase: string | null }

const controlStyle: React.CSSProperties = {
  display: 'block', width: '100%', boxSizing: 'border-box', marginTop: '0.25rem',
  padding: '0.55rem 0.75rem', borderRadius: 8, border: '1px solid var(--color-line)',
  background: 'var(--color-bg)', color: 'var(--color-ink)', fontFamily: 'var(--font-sans)',
};
const buttonStyle: React.CSSProperties = {
  padding: '0.55rem 0.9rem', borderRadius: 8, border: '1px solid var(--color-line)',
  background: 'var(--color-surface)', color: 'var(--color-ink)', fontWeight: 600,
};
const primaryButtonStyle: React.CSSProperties = {
  ...buttonStyle, background: 'var(--color-primary)', color: 'var(--color-on-primary)',
  borderColor: 'var(--color-primary)',
};
const messageStyle: React.CSSProperties = {
  display: 'flex', alignItems: 'flex-start', gap: '0.65rem', padding: '0.75rem 1rem',
  borderRadius: 8, marginBottom: '1rem',
};

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
  const [reload, setReload] = useState(0);
  const [nuevoNombre, setNuevoNombre] = useState('');
  const [nuevaDescripcion, setNuevaDescripcion] = useState('');
  const [nuevoRolBase, setNuevoRolBase] = useState('CAJERO');
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
  }, [token, page, reload]);

  const guardar = async (usuario: Usuario) => {
    if (!usuario.activo) {
      setSuccess('');
      setError('No se pueden modificar los roles de un usuario inactivo.');
      return;
    }

    const roleIds = seleccion[usuario.id] ?? [];
    if (!roleIds.length) { setError('Selecciona al menos un rol.'); return; }
    if (usuario.id === actorId && !roleIds.some(id => roles.some(r => r.id === id && (r.nombre === 'ADMIN' || r.rolBase === 'ADMIN')))) {
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

  const crearRol = async (event: React.FormEvent) => {
    event.preventDefault();
    setSaving('nuevo'); setError(''); setSuccess('');
    try {
      await axios.post(buildServiceUrl('identity', '/users/roles'),
        { nombre: nuevoNombre, descripcion: nuevaDescripcion, rolBase: nuevoRolBase },
        { headers: { Authorization: `Bearer ${token}` } });
      setNuevoNombre(''); setNuevaDescripcion(''); setReload(n => n + 1);
      setSuccess('Rol creado. Ya puedes asignarlo a usuarios.');
    } catch (e) {
      setError(axios.isAxiosError(e) ? e.response?.data?.message ?? 'No se pudo crear el rol.' : 'No se pudo crear el rol.');
    } finally { setSaving(null); }
  };

  const eliminarRol = async (rol: Rol) => {
    setSaving(rol.id); setError(''); setSuccess('');
    try {
      await axios.delete(buildServiceUrl('identity', `/users/roles/${rol.id}`),
        { headers: { Authorization: `Bearer ${token}` } });
      setReload(n => n + 1);
      setSuccess(`Rol ${rol.nombre} eliminado.`);
    } catch (e) {
      setError(axios.isAxiosError(e) ? e.response?.data?.message ?? 'No se pudo eliminar el rol.' : 'No se pudo eliminar el rol.');
    } finally { setSaving(null); }
  };

  return <section>
    <h2>Roles del tenant</h2>
    <p>Asigna funciones a las cuentas de tu tienda. Los roles propios heredan los permisos de un rol base.</p>
    {error && <div role="alert" style={{ ...messageStyle, background: 'var(--color-danger-bg)', color: 'var(--color-danger-text)', border: '1px solid var(--color-danger)' }}>
      <span aria-hidden="true">⚠️</span><span><strong>Error</strong><br />{error}</span>
    </div>}
    {success && <div role="status" style={{ ...messageStyle, background: 'var(--color-success-bg)', color: 'var(--color-success-text)', border: '1px solid var(--color-success)' }}>
      <span aria-hidden="true">✅</span><span><strong>Operación completada</strong><br />{success}</span>
    </div>}
    <form onSubmit={event => { void crearRol(event); }} style={{ display: 'grid', gap: '0.75rem', maxWidth: 520, marginBottom: '1.5rem' }}>
      <h3>Crear rol del tenant</h3>
      <label>Nombre del rol <input style={controlStyle} value={nuevoNombre} onChange={e => setNuevoNombre(e.target.value.toUpperCase())} minLength={3} maxLength={50} pattern="[A-Z][A-Z0-9_]{2,49}" required /></label>
      <label>Descripción <input style={controlStyle} value={nuevaDescripcion} onChange={e => setNuevaDescripcion(e.target.value)} maxLength={200} /></label>
      <label>Permisos base <select style={controlStyle} value={nuevoRolBase} onChange={e => setNuevoRolBase(e.target.value)}>
        <option value="CAJERO">CAJERO</option><option value="REPONEDOR">REPONEDOR</option><option value="ADMIN">ADMIN</option>
      </select></label>
      <button type="submit" disabled={saving === 'nuevo'} style={primaryButtonStyle}>{saving === 'nuevo' ? 'Creando…' : 'Crear rol'}</button>
    </form>
    <h3>Roles disponibles</h3>
    {roles.map(r => <div key={r.id} style={{ marginBottom: '0.5rem' }}>
      <strong>{r.nombre}</strong> {r.rolBase && <span>(permisos {r.rolBase})</span>} {r.descripcion}
      {r.tenantId && <button type="button" disabled={saving === r.id} onClick={() => { void eliminarRol(r); }} style={{ ...buttonStyle, marginLeft: '0.75rem', color: 'var(--color-danger-text)', borderColor: 'var(--color-danger)' }}>Eliminar rol</button>}
    </div>)}
    <h3>Asignación a usuarios</h3>
    {usuarios.map(u => <div key={u.id} aria-label={`Roles de ${u.nombre}`} style={{ padding: '1rem', marginBottom: '0.75rem', border: '1px solid var(--color-line)', borderRadius: 8, background: 'var(--color-surface)' }}>
      <strong>{u.nombre}</strong> <span>{u.email}</span>{' '}
      <span style={{ fontWeight: 600, color: u.activo ? 'var(--color-success-text)' : 'var(--color-danger-text)' }}>
        {u.activo ? 'Activo' : 'Inactivo'}
      </span>
      <div style={{ display: 'flex', gap: '1rem', margin: '0.75rem 0' }}>
        {roles.map(r => <label key={r.id} title={r.descripcion}><input type="checkbox" disabled={!u.activo} checked={(seleccion[u.id] ?? []).includes(r.id)} onChange={event => setSeleccion(current => ({ ...current, [u.id]: event.target.checked ? [...(current[u.id] ?? []), r.id] : (current[u.id] ?? []).filter(id => id !== r.id) }))} /> {r.nombre}</label>)}
      </div>
      {!u.activo && <p style={{ color: 'var(--color-ink-soft)' }}>Modificación de roles bloqueada: usuario inactivo.</p>}
      <button type="button" aria-label={`Guardar roles de ${u.nombre}`} aria-disabled={!u.activo} disabled={saving === u.id} onClick={() => { void guardar(u); }} style={{ ...primaryButtonStyle, cursor: u.activo ? 'pointer' : 'not-allowed', opacity: u.activo ? 1 : 0.6 }}>{saving === u.id ? 'Guardando…' : 'Guardar roles'}</button>
    </div>)}
    <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center' }}>
      <button type="button" disabled={page <= 1} onClick={() => setPage(p => p - 1)} style={buttonStyle}>Anterior</button>
      <span>Página {page} de {totalPages}</span>
      <button type="button" disabled={page >= totalPages} onClick={() => setPage(p => p + 1)} style={buttonStyle}>Siguiente</button>
    </div>
  </section>;
}

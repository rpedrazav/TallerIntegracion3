import { useCallback, useEffect, useState } from 'react';
import axios from 'axios';
import { useAuth } from '../hooks/useAuth';
import CrearUsuarioModal, { CrearUsuarioValues } from '../components/admin/CrearUsuarioModal';

// MS-1 Tenant & Identity
const IDENTITY_URL = 'https://auth-rpedraza.dev.censei.cl';

interface Usuario {
  id: string;
  nombre: string;
  email: string;
  activo: boolean;
  creadoEn: string;
}

const th: React.CSSProperties = {
  textAlign: 'left', padding: '0.6rem 0.9rem', fontSize: '0.875rem',
  color: 'var(--color-ink-soft)', borderBottom: '1px solid var(--color-line)',
};
const td: React.CSSProperties = { padding: '0.6rem 0.9rem', borderBottom: '1px solid var(--color-line)' };

export default function Admin() {
  const { token } = useAuth();
  const [usuarios, setUsuarios] = useState<Usuario[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [modalAbierto, setModalAbierto] = useState(false);

  const cargar = useCallback(async () => {
    if (!token) return;
    setLoading(true);
    setError('');
    try {
      const res = await axios.get(`${IDENTITY_URL}/api/v1/users`, {
        params: { page: 1, pageSize: 100 },
        headers: { Authorization: `Bearer ${token}` },
      });
      setUsuarios(res.data.items ?? []);
    } catch (err) {
      const status = axios.isAxiosError(err) ? err.response?.status : undefined;
      setError(status === 403
        ? 'No tienes permisos para administrar usuarios.'
        : 'No se pudo cargar la lista de usuarios.');
    } finally {
      setLoading(false);
    }
  }, [token]);

  useEffect(() => { cargar(); }, [cargar]);

  const crearUsuario = async (v: CrearUsuarioValues) => {
    const headers = { Authorization: `Bearer ${token}` };
    try {
      const res = await axios.post(
        `${IDENTITY_URL}/api/v1/users`,
        { nombre: v.nombre, email: v.email, password: v.password },
        { headers },
      );
      await axios.post(`${IDENTITY_URL}/api/v1/users/${res.data.id}/roles`, { roles: [v.rol] }, { headers });
    } catch (err) {
      const msg = axios.isAxiosError(err) ? err.response?.data?.message : undefined;
      throw new Error(msg ?? 'No se pudo crear el usuario.');
    }
    await cargar();
  };

  return (
    <div style={{ fontFamily: 'var(--font-sans)', color: 'var(--color-ink)' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
        <h2 style={{ margin: 0 }}>Administración</h2>
        <button
          type="button"
          onClick={() => setModalAbierto(true)}
          style={{
            background: 'var(--color-primary)', color: 'var(--color-on-primary)', border: 'none',
            padding: '0.6rem 1.2rem', borderRadius: '8px', fontWeight: 600, cursor: 'pointer',
          }}
        >
          Crear usuario
        </button>
      </div>

      {error && (
        <div role="alert" style={{
          background: 'var(--color-danger-bg)', color: 'var(--color-danger-text)',
          border: '1px solid var(--color-danger)', borderRadius: '8px', padding: '0.7rem 1rem', marginBottom: '1rem',
        }}>
          <b>Error.</b> {error}
        </div>
      )}

      <div style={{ background: 'var(--color-surface)', border: '1px solid var(--color-line)', borderRadius: '12px', overflow: 'hidden' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead>
            <tr><th style={th}>Nombre</th><th style={th}>Correo</th><th style={th}>Estado</th></tr>
          </thead>
          <tbody>
            {loading && <tr><td style={td} colSpan={3}>Cargando…</td></tr>}
            {!loading && usuarios.length === 0 && !error && (
              <tr><td style={td} colSpan={3}>No hay usuarios.</td></tr>
            )}
            {usuarios.map(u => (
              <tr key={u.id}>
                <td style={td}>{u.nombre}</td>
                <td style={td}>{u.email}</td>
                <td style={td}>
                  <span style={{
                    padding: '0.1rem 0.6rem', borderRadius: '9999px', fontSize: '0.875rem', fontWeight: 600,
                    background: u.activo ? 'var(--color-success-bg)' : 'var(--color-danger-bg)',
                    color: u.activo ? 'var(--color-success-text)' : 'var(--color-danger-text)',
                  }}>
                    {u.activo ? 'Activo' : 'Inactivo'}
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <CrearUsuarioModal
        isOpen={modalAbierto}
        onClose={() => setModalAbierto(false)}
        onSubmit={crearUsuario}
      />
    </div>
  );
}

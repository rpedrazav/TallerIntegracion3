import React, { useCallback, useEffect, useState } from 'react';
import axios, { isAxiosError } from 'axios';
import { useAuth } from '../hooks/useAuth';
import CrearUsuarioModal, { CrearUsuarioValues } from '../components/admin/CrearUsuarioModal';
import UsuariosList, { Usuario } from '../components/admin/UsuariosList';

// MS-1 Tenant & Identity
const IDENTITY_URL = 'https://auth-rpedraza.dev.censei.cl';

export default function Admin() {
  const { token } = useAuth();
  const [usuarios, setUsuarios] = useState<Usuario[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [modalAbierto, setModalAbierto] = useState(false);
  const [reloadTick, setReloadTick] = useState(0);

  const recargar = useCallback(() => {
    setReloadTick(t => t + 1);
  }, []);

  useEffect(() => {
    let isMounted = true;

    async function load() {
      if (!token) return;
      setLoading(true);
      setError('');
      try {
        const res = await axios.get(`${IDENTITY_URL}/users`, {
          params: { page: 1, pageSize: 100 },
          headers: { Authorization: `Bearer ${token}` },
        });
        if (!isMounted) return;
        setUsuarios(res.data.items ?? (Array.isArray(res.data) ? res.data : []));
      } catch (err) {
        if (!isMounted) return;
        const status = isAxiosError(err) ? err.response?.status : undefined;
        setError(status === 403
          ? 'No tienes permisos para administrar usuarios.'
          : 'No se pudo cargar la lista de usuarios.');
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    }

    void load();
    return () => { isMounted = false; };
  }, [token, reloadTick]);

  const crearUsuario = async (v: CrearUsuarioValues) => {
    const headers = { Authorization: `Bearer ${token}` };
    try {
      const res = await axios.post(
        `${IDENTITY_URL}/users`,
        {
          nombre: v.nombre,
          email: v.email,
          password: v.password,
          roles: [v.rol],
        },
        { headers },
      );
      if (!res.data?.roles || res.data.roles.length === 0) {
        await axios.post(`${IDENTITY_URL}/users/${res.data.id}/roles`, { roles: [v.rol] }, { headers });
      }
    } catch (err) {
      let msg: string | undefined;
      if (isAxiosError(err)) {
        const data = err.response?.data;
        if (data?.message) {
          msg = data.message;
        } else if (data?.errors && typeof data.errors === 'object') {
          const list = Object.values(data.errors).flat();
          if (list.length > 0) msg = String(list[0]);
        } else if (data?.title) {
          msg = data.title;
        }
      }
      throw new Error(msg ?? 'No se pudo crear el usuario.');
    }
    recargar();
  };

  return (
    <div style={{ fontFamily: 'var(--font-sans)', color: 'var(--color-ink)' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
        <h2 style={{ margin: 0 }}>Administración</h2>
        <div style={{ display: 'flex', gap: '0.5rem' }}>
          <button
            type="button"
            onClick={recargar}
            disabled={loading}
            style={{
              background: 'var(--color-surface)', color: 'var(--color-ink)', border: '1px solid var(--color-line)',
              padding: '0.6rem 1rem', borderRadius: '8px', fontWeight: 600, cursor: loading ? 'not-allowed' : 'pointer',
            }}
          >
            {loading ? 'Cargando…' : 'Refrescar'}
          </button>
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
      </div>

      {error && (
        <div role="alert" style={{
          background: 'var(--color-danger-bg)', color: 'var(--color-danger-text)',
          border: '1px solid var(--color-danger)', borderRadius: '8px', padding: '0.7rem 1rem', marginBottom: '1rem',
        }}>
          <b>Error.</b> {error}
        </div>
      )}

      <UsuariosList usuarios={usuarios} loading={loading} error={error} />

      <CrearUsuarioModal
        isOpen={modalAbierto}
        onClose={() => setModalAbierto(false)}
        onSubmit={crearUsuario}
      />
    </div>
  );
}

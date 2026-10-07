import React, { useCallback, useEffect, useState } from 'react';
import axios, { isAxiosError } from 'axios';
import { useAuth } from '../hooks/useAuth';
import CrearUsuarioModal, { CrearUsuarioValues } from '../components/admin/CrearUsuarioModal';
import UsuariosList, { Usuario } from '../components/admin/UsuariosList';
import TenantConfig from '../components/admin/TenantConfig';

// MS-1 Tenant & Identity
const IDENTITY_URL = 'https://auth-rpedraza.dev.censei.cl';

export default function Admin() {
  const { token } = useAuth();
  const [tabActiva, setTabActiva] = useState<'usuarios' | 'configuracion'>('usuarios');
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
      const status = isAxiosError(err) ? err.response?.status : undefined;
      setError(
        status === 403
          ? 'No tienes permisos para administrar usuarios.'
          : 'No se pudo cargar la lista de usuarios.'
      );
    } finally {
      setLoading(false);
    }
  }, [token]);

  useEffect(() => {
    let cancel = false;
    if (tabActiva === 'usuarios' && token) {
      (async () => {
        if (!cancel) {
          await cargar();
        }
      })();
    }
    return () => {
      cancel = true;
    };
  }, [cargar, tabActiva, token]);

  const crearUsuario = async (v: CrearUsuarioValues) => {
    const headers = { Authorization: `Bearer ${token}` };
    try {
      const res = await axios.post(
        `${IDENTITY_URL}/api/v1/users`,
        { nombre: v.nombre, email: v.email, password: v.password },
        { headers }
      );
      await axios.post(`${IDENTITY_URL}/api/v1/users/${res.data.id}/roles`, { roles: [v.rol] }, { headers });
    } catch (err) {
      const msg = isAxiosError(err) ? err.response?.data?.message : undefined;
      throw new Error(msg ?? 'No se pudo crear el usuario.');
    }
    await cargar();
  };

  const tabButtonStyle = (activa: boolean): React.CSSProperties => ({
    padding: '0.6rem 1.25rem',
    borderRadius: '8px',
    border: 'none',
    fontWeight: 600,
    cursor: 'pointer',
    backgroundColor: activa ? 'var(--color-primary)' : 'var(--color-surface)',
    color: activa ? 'var(--color-on-primary)' : 'var(--color-ink)',
    boxShadow: activa ? '0 2px 4px rgba(0,0,0,0.1)' : 'none',
    transition: 'all 0.2s ease',
  });

  return (
    <div style={{ fontFamily: 'var(--font-sans)', color: 'var(--color-ink)' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem' }}>
        <div>
          <h2 style={{ margin: 0 }}>Panel de Administración</h2>
          <p style={{ margin: '0.25rem 0 0 0', fontSize: '0.875rem', color: 'var(--color-ink-soft)' }}>
            Administración de usuarios y configuración regional/fiscal del tenant
          </p>
        </div>
        {tabActiva === 'usuarios' && (
          <button
            type="button"
            onClick={() => setModalAbierto(true)}
            style={{
              background: 'var(--color-primary)',
              color: 'var(--color-on-primary)',
              border: 'none',
              padding: '0.6rem 1.2rem',
              borderRadius: '8px',
              fontWeight: 600,
              cursor: 'pointer',
            }}
          >
            Crear usuario
          </button>
        )}
      </div>

      {/* Selector de pestañas */}
      <div style={{ display: 'flex', gap: '0.5rem', marginBottom: '1.5rem', borderBottom: '1px solid var(--color-line)', paddingBottom: '0.75rem' }}>
        <button
          type="button"
          onClick={() => setTabActiva('usuarios')}
          style={tabButtonStyle(tabActiva === 'usuarios')}
        >
          Usuarios
        </button>
        <button
          type="button"
          onClick={() => setTabActiva('configuracion')}
          style={tabButtonStyle(tabActiva === 'configuracion')}
        >
          Configuración del Tenant
        </button>
      </div>

      {tabActiva === 'usuarios' && (
        <>
          {error && (
            <div
              role="alert"
              style={{
                background: 'var(--color-danger-bg)',
                color: 'var(--color-danger-text)',
                border: '1px solid var(--color-danger)',
                borderRadius: '8px',
                padding: '0.7rem 1rem',
                marginBottom: '1rem',
              }}
            >
              <b>Error:</b> {error}
            </div>
          )}

          <UsuariosList usuarios={usuarios} loading={loading} error={error} />

          <CrearUsuarioModal
            isOpen={modalAbierto}
            onClose={() => setModalAbierto(false)}
            onSubmit={crearUsuario}
          />
        </>
      )}

      {tabActiva === 'configuracion' && <TenantConfig token={token} />}
    </div>
  );
}

import React, { useCallback, useEffect, useState } from 'react';
import axios, { isAxiosError } from 'axios';
import { useAuth } from '../hooks/useAuth';
import CrearUsuarioModal, { CrearUsuarioValues } from '../components/admin/CrearUsuarioModal';
import EditarUsuarioModal from '../components/admin/EditarUsuarioModal';
import { SolicitudEdicionUsuario } from '../components/admin/editarUsuario';
import {
  crearSolicitudDesactivacionUsuario,
  marcarUsuarioInactivo,
} from '../components/admin/desactivarUsuario';
import UsuariosList, { Usuario } from '../components/admin/UsuariosList';
import TenantConfig from '../components/admin/TenantConfig';
import { buildServiceUrl } from '../config/api';

export default function Admin() {
  const { token } = useAuth();
  const [tabActiva, setTabActiva] = useState<'usuarios' | 'configuracion'>('usuarios');
  const [usuarios, setUsuarios] = useState<Usuario[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [modalAbierto, setModalAbierto] = useState(false);
  const [usuarioEditando, setUsuarioEditando] = useState<Usuario | null>(null);
  const [deactivatingUserId, setDeactivatingUserId] = useState<string | null>(null);
  const [success, setSuccess] = useState('');
  const [reloadTick, setReloadTick] = useState(0);

  const recargar = useCallback(() => {
    setReloadTick(t => t + 1);
  }, []);

  useEffect(() => {
    let isMounted = true;

    async function load() {
      if (!token || tabActiva !== 'usuarios') return;
      setLoading(true);
      setError('');
      try {
        const res = await axios.get(buildServiceUrl('identity', '/users'), {
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
  }, [token, reloadTick, tabActiva]);

  const crearUsuario = async (v: CrearUsuarioValues) => {
    const headers = { Authorization: `Bearer ${token}` };
    try {
      const res = await axios.post(
        buildServiceUrl('identity', '/users'),
        {
          nombre: v.nombre,
          email: v.email,
          password: v.password,
          roles: [v.rol],
        },
        { headers },
      );
      if (!res.data?.roles || res.data.roles.length === 0) {
        await axios.post(buildServiceUrl('identity', `/users/${res.data.id}/roles`), { roles: [v.rol] }, { headers });
      }
    } catch (err) {
      let msg: string | undefined;
      if (isAxiosError(err)) {
        const data = err.response?.data;
        if (err.response?.status === 403) {
          msg = data?.message ?? 'No tienes permisos para crear usuarios.';
        } else if (data?.message) {
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

  const editarUsuario = async (solicitud: SolicitudEdicionUsuario) => {
    try {
      await axios.request({
        method: solicitud.method,
        url: buildServiceUrl('identity', solicitud.path),
        data: solicitud.body,
        headers: { Authorization: `Bearer ${token}` },
      });
    } catch (err) {
      if (!isAxiosError(err)) throw new Error('No se pudo actualizar el usuario.');

      const data = err.response?.data;
      const serverMessage = typeof data?.message === 'string' ? data.message : undefined;
      if (!err.response) throw new Error('No se pudo conectar con el servicio de usuarios.');

      switch (err.response.status) {
        case 400: {
          const validationErrors = data?.errors && typeof data.errors === 'object'
            ? Object.values(data.errors).flat()
            : [];
          throw new Error(validationErrors.length > 0
            ? String(validationErrors[0])
            : serverMessage ?? 'Los datos ingresados no son válidos.');
        }
        case 401:
          throw new Error(serverMessage ?? 'La sesión no es válida. Vuelve a iniciar sesión.');
        case 403:
          throw new Error(serverMessage ?? 'No tienes permisos para editar usuarios.');
        case 404:
          throw new Error(serverMessage ?? 'El usuario no existe o pertenece a otro tenant.');
        case 409:
          throw new Error(serverMessage ?? 'El email ya pertenece a otro usuario del tenant.');
        default:
          throw new Error(serverMessage ?? 'No se pudo actualizar el usuario.');
      }
    }
  };

  const completarEdicion = () => {
    setUsuarioEditando(null);
    setSuccess('Usuario actualizado exitosamente.');
    recargar();
  };

  const desactivarUsuario = async (usuario: Usuario) => {
    const solicitud = crearSolicitudDesactivacionUsuario(usuario);
    setDeactivatingUserId(usuario.id);
    setError('');
    setSuccess('');

    try {
      const response = await axios.request({
        method: solicitud.method,
        url: buildServiceUrl('identity', solicitud.path),
        headers: { Authorization: `Bearer ${token}` },
      });

      if (response.status !== 204) throw new Error('Respuesta inesperada al desactivar el usuario.');

      setUsuarios(actuales => marcarUsuarioInactivo(actuales, usuario.id));
      setSuccess(`${usuario.nombre} fue desactivado y permanece visible como Inactivo.`);
    } catch (err) {
      if (!isAxiosError(err)) {
        setError(err instanceof Error ? err.message : 'No se pudo desactivar el usuario.');
        return;
      }

      const data = err.response?.data;
      const serverMessage = typeof data?.message === 'string' ? data.message : undefined;
      if (!err.response) {
        setError('No se pudo conectar con el servicio de usuarios.');
        return;
      }

      switch (err.response.status) {
        case 400:
          setError(serverMessage ?? 'Un administrador no puede desactivarse a sí mismo.');
          break;
        case 401:
          setError(serverMessage ?? 'La sesión no es válida. Vuelve a iniciar sesión.');
          break;
        case 403:
          setError(serverMessage ?? 'No tienes permisos para desactivar usuarios.');
          break;
        case 404:
          setError(serverMessage ?? 'El usuario no existe o pertenece a otro tenant.');
          break;
        default:
          setError(serverMessage ?? 'No se pudo desactivar el usuario.');
      }
    } finally {
      setDeactivatingUserId(null);
    }
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
          <div style={{ display: 'flex', gap: '0.5rem' }}>
            <button
              type="button"
              onClick={recargar}
              disabled={loading}
              style={{
                background: 'var(--color-surface)',
                color: 'var(--color-ink)',
                border: '1px solid var(--color-line)',
                padding: '0.6rem 1rem',
                borderRadius: '8px',
                fontWeight: 600,
                cursor: loading ? 'not-allowed' : 'pointer',
              }}
            >
              {loading ? 'Cargando…' : 'Refrescar'}
            </button>
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
          </div>
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

          {success && (
            <div
              role="status"
              style={{
                background: 'var(--color-success-bg)',
                color: 'var(--color-success-text)',
                border: '1px solid var(--color-success)',
                borderRadius: '8px',
                padding: '0.7rem 1rem',
                marginBottom: '1rem',
              }}
            >
              {success}
            </div>
          )}

          <UsuariosList
            usuarios={usuarios}
            loading={loading}
            error={error}
            onEdit={usuario => {
              setSuccess('');
              setUsuarioEditando(usuario);
            }}
            onDeactivate={desactivarUsuario}
            deactivatingUserId={deactivatingUserId}
          />

          <CrearUsuarioModal
            isOpen={modalAbierto}
            onClose={() => setModalAbierto(false)}
            onSubmit={crearUsuario}
          />

          <EditarUsuarioModal
            usuario={usuarioEditando}
            isOpen={usuarioEditando !== null}
            onClose={() => setUsuarioEditando(null)}
            onSubmit={editarUsuario}
            onSuccess={completarEdicion}
          />
        </>
      )}

      {tabActiva === 'configuracion' && <TenantConfig token={token} />}
    </div>
  );
}

import { useEffect } from 'react';
import { Outlet, Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { parseJwtPayload } from '../utils/jwt';

export default function AppLayout() {
  const { token, loading, logout } = useAuth();
  const navigate = useNavigate();

  // Protección de ruta: si no hay sesión activa, redirigir a /login
  useEffect(() => {
    if (!loading && !token) {
      navigate('/login', { replace: true });
    }
  }, [loading, token, navigate]);

  const handleLogout = async () => {
    // Tarea 3 (TI3-164): Limpia el electron-store y redirige a /login
    await logout();
    navigate('/login', { replace: true });
  };

  const payload = token ? parseJwtPayload(token) : null;
  const userRole = payload?.active_role || 'CAJERO';
  const userSub = payload?.sub || 'Usuario';

  if (loading) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh' }}>
        <p>Cargando sesión...</p>
      </div>
    );
  }

  return (
    <div>
      <nav
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          padding: '0.875rem 1.5rem',
          borderBottom: '1px solid #e5e7eb',
          backgroundColor: '#ffffff',
          boxShadow: '0 1px 3px rgba(0,0,0,0.05)',
        }}
      >
        <span style={{ fontSize: '1.125rem' }}>
          <strong>GlobalMart OS</strong>
        </span>

        <div style={{ display: 'flex', gap: '1.25rem' }}>
          <Link
            to="/pos"
            style={{
              textDecoration: 'none',
              color: '#2563eb',
              fontWeight: 500,
            }}
          >
            Punto de Venta
          </Link>
          <Link
            to="/admin"
            style={{
              textDecoration: 'none',
              color: '#4b5563',
              fontWeight: 500,
            }}
          >
            Administración
          </Link>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
          <span
            style={{
              fontSize: '0.875rem',
              color: '#374151',
              backgroundColor: '#f3f4f6',
              padding: '0.25rem 0.625rem',
              borderRadius: '9999px',
              fontWeight: 500,
            }}
          >
            Rol: {userRole} ({userSub})
          </span>

          <button
            id="btn-logout"
            onClick={handleLogout}
            style={{
              padding: '0.45rem 0.9rem',
              backgroundColor: '#dc2626',
              color: '#ffffff',
              border: 'none',
              borderRadius: '4px',
              cursor: 'pointer',
              fontSize: '0.875rem',
              fontWeight: 500,
              display: 'inline-flex',
              alignItems: 'center',
              gap: '0.25rem',
            }}
          >
            Cerrar Sesión
          </button>
        </div>
      </nav>

      <main style={{ padding: '2rem' }}>
        <Outlet />
      </main>
    </div>
  );
}

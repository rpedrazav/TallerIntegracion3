import React from 'react';
import { HashRouter, Routes, Route, Navigate } from 'react-router-dom';
import AppLayout from './components/AppLayout';
import Login from './pages/Login';
import Pos from './pages/Pos';
import Admin from './pages/Admin';
import AbrirTurnoPage from './pages/AbrirTurnoPage';
import CerrarTurnoPage from './pages/CerrarTurnoPage';
import ProductosPage from './pages/ProductosPage';
import { useAuth } from './hooks/useAuth';
import { useTenantStore } from './store/useTenantStore';
import { canAccessAdmin } from './utils/adminAccess';

interface AdminRouteProps {
  children: React.ReactNode;
}

function AdminRoute({ children }: AdminRouteProps) {
  const { token, loading } = useAuth();
  const { token: tenantToken, esAdmin, setToken } = useTenantStore();

  React.useEffect(() => {
    if (!loading) setToken(token);
  }, [loading, setToken, token]);

  if (loading || token !== tenantToken) return null;

  if (!canAccessAdmin(esAdmin)) {
    return (
      <div
        role="alert"
        style={{
          maxWidth: '640px',
          margin: '3rem auto',
          padding: '1.5rem',
          border: '1px solid var(--color-danger)',
          borderRadius: '12px',
          backgroundColor: 'var(--color-danger-bg)',
          color: 'var(--color-danger-text)',
        }}
      >
        <h2 style={{ marginTop: 0 }}>Acceso denegado</h2>
        <p style={{ marginBottom: 0 }}>No tienes permisos para acceder a funciones administrativas.</p>
      </div>
    );
  }

  return <>{children}</>;
}

export default function App() {
  return (
    <HashRouter>
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route element={<AppLayout />}>
          <Route path="/pos" element={<Pos />} />
          <Route path="/abrir-turno" element={<AbrirTurnoPage />} />
          <Route path="/cerrar-turno" element={<CerrarTurnoPage />} />
          <Route path="/admin/productos" element={<AdminRoute><ProductosPage /></AdminRoute>} />
          <Route path="/admin/*" element={<AdminRoute><Admin /></AdminRoute>} />
        </Route>
        {/* Redirige la raíz al login por defecto */}
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    </HashRouter>
  );
}

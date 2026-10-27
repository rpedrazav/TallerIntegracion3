import React from 'react';
import { HashRouter, Routes, Route, Navigate } from 'react-router-dom';
import AppLayout from './components/AppLayout';
import AdminLayout from './components/AdminLayout';
import Login from './pages/Login';
import Pos from './pages/Pos';
import Admin from './pages/Admin';
import AbrirTurnoPage from './pages/AbrirTurnoPage';
import CerrarTurnoPage from './pages/CerrarTurnoPage';
import ProductosPage from './pages/ProductosPage';
import { useAuth } from './hooks/useAuth';
import { useTenantStore } from './store/useTenantStore';
import { canAccessAdmin } from './utils/adminAccess';
import { parseJwt } from './utils/jwt';
import AdminDashboard from './pages/AdminDashboard';
import AdminRoles from './pages/AdminRoles';
import AdminInventario from './pages/AdminInventario';
import AdminSucursales from './pages/AdminSucursales';

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

  if (!token) return <Navigate to="/login" replace />;
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

function AuthRoute({ children }: AdminRouteProps) {
  const { token, loading } = useAuth();
  if (loading) return null;
  return token ? <>{children}</> : <Navigate to="/login" replace />;
}

function Home() {
  const { token, loading } = useAuth();
  if (loading) return null;
  if (!token) return <Navigate to="/login" replace />;
  const role = parseJwt(token)?.active_role?.toUpperCase();
  return <Navigate to={role === 'ADMIN' || role === 'SUPER_ADMIN' ? '/admin' : '/pos'} replace />;
}

export default function App() {
  return (
    <HashRouter>
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route path="/" element={<Home />} />
        <Route element={<AuthRoute><AppLayout /></AuthRoute>}>
          <Route path="/pos" element={<Pos />} />
          <Route path="/abrir-turno" element={<AbrirTurnoPage />} />
          <Route path="/cerrar-turno" element={<CerrarTurnoPage />} />
        </Route>
        <Route element={<AdminRoute><AdminLayout /></AdminRoute>}>
          <Route path="/admin" element={<AdminDashboard />} />
          <Route path="/admin/usuarios" element={<Admin />} />
          <Route path="/admin/roles" element={<AdminRoles />} />
          <Route path="/admin/inventario" element={<AdminInventario />} />
          <Route path="/admin/sucursales" element={<AdminSucursales />} />
          <Route path="/admin/productos" element={<ProductosPage />} />
          <Route path="/admin/configuracion" element={<Admin />} />
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </HashRouter>
  );
}

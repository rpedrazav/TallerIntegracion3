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

function AdminRoute() {
  const { token, loading } = useAuth();
  const { token: tenantToken, esAdmin, setToken } = useTenantStore();

  React.useEffect(() => {
    if (!loading) setToken(token);
  }, [loading, setToken, token]);

  if (loading || token !== tenantToken) return null;

  return canAccessAdmin(esAdmin) ? <Admin /> : <Navigate to="/pos" replace />;
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
          <Route path="/admin/productos" element={<ProductosPage />} />
          <Route path="/admin/*" element={<AdminRoute />} />
        </Route>
        {/* Redirige la raíz al login por defecto */}
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    </HashRouter>
  );
}

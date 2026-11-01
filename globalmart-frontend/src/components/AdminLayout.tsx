import React from 'react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { parseJwt } from '../utils/jwt';
import { useTenantStore } from '../store/useTenantStore';

const links = [
  ['/admin', 'Resumen'],
  ['/admin/usuarios', 'Usuarios'],
  ['/admin/roles', 'Roles'],
  ['/admin/inventario', 'Inventario'],
  ['/admin/sucursales', 'Sucursales'],
  ['/admin/productos', 'Productos'],
  ['/admin/configuracion', 'Tenant'],
] as const;

export default function AdminLayout() {
  const { token, logout } = useAuth();
  const navigate = useNavigate();
  const tenantId = parseJwt(token)?.tenant_id ?? '';

  const salir = async () => {
    await logout();
    localStorage.removeItem('token');
    localStorage.removeItem('tenant');
    useTenantStore.getState().clear();
    navigate('/login', { replace: true });
  };

  return (
    <div style={{ minHeight: '100vh', display: 'flex', background: 'var(--color-bg)', color: 'var(--color-ink)', fontFamily: 'var(--font-sans)' }}>
      <aside style={{ width: 240, background: 'var(--color-primary)', color: 'var(--color-on-primary)', padding: '1.5rem', display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
        <h1 style={{ fontSize: '1.3rem', margin: '0 0 0.3rem' }}>GlobalMart Admin</h1>
        <small style={{ overflowWrap: 'anywhere', marginBottom: '1rem', opacity: 0.8 }}>Tenant: {tenantId}</small>
        {links.map(([to, label]) => <NavLink key={to} to={to} end={to === '/admin'} style={({ isActive }) => ({ color: 'var(--color-on-primary)', padding: '0.7rem 0.85rem', borderRadius: 8, textDecoration: 'none', background: isActive ? 'rgba(255,255,255,0.2)' : 'transparent', fontWeight: isActive ? 700 : 500 })}>{label}</NavLink>)}
        <NavLink to="/pos" style={{ color: 'var(--color-on-primary)', padding: '0.7rem 0.85rem', textDecoration: 'none', marginTop: 'auto' }}>Ir al punto de venta</NavLink>
        <button type="button" onClick={salir} style={{ padding: '0.7rem', borderRadius: 8, border: 0, cursor: 'pointer' }}>Cerrar sesión</button>
      </aside>
      <main style={{ flex: 1, minWidth: 0, padding: '2rem' }}><Outlet /></main>
    </div>
  );
}

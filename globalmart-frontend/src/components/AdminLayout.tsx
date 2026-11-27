import React from 'react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { useTenantStore } from '../store/useTenantStore';

type Enlace = readonly [ruta: string, etiqueta: string];

const linksTienda: readonly Enlace[] = [
  ['/admin', 'Resumen'],
  ['/admin/usuarios', 'Usuarios'],
  ['/admin/roles', 'Roles'],
  ['/admin/inventario', 'Inventario'],
  ['/admin/sucursales', 'Sucursales'],
  ['/admin/productos', 'Productos'],
  ['/admin/configuracion', 'Tenant'],
];

// SUPER_ADMIN administra la plataforma, no una tienda: solo ve el alta de minimarkets (S3-098).
const linksPlataforma: readonly Enlace[] = [['/admin/minimarkets', 'Minimarkets']];

export default function AdminLayout() {
  const { logout } = useAuth();
  const navigate = useNavigate();
  const { tenantNombre, esSuperAdmin } = useTenantStore();
  const links = esSuperAdmin ? linksPlataforma : linksTienda;

  const salir = async () => {
    await logout();
    localStorage.removeItem('token');
    useTenantStore.getState().clear();
    navigate('/login', { replace: true });
  };

  return (
    <div style={{ minHeight: '100vh', display: 'flex', background: 'var(--color-bg)', color: 'var(--color-ink)', fontFamily: 'var(--font-sans)' }}>
      <aside style={{ width: 240, background: 'var(--color-primary)', color: 'var(--color-on-primary)', padding: '1.5rem', display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
        <h1 style={{ fontSize: '1.3rem', margin: '0 0 0.3rem' }}>GlobalMart Admin</h1>
        <small style={{ overflowWrap: 'anywhere', marginBottom: '1rem', opacity: 0.8 }}>{tenantNombre}</small>
        {links.map(([to, label]) => <NavLink key={to} to={to} end={to === '/admin'} style={({ isActive }) => ({ color: 'var(--color-on-primary)', padding: '0.7rem 0.85rem', borderRadius: 8, textDecoration: 'none', background: isActive ? 'rgba(255,255,255,0.2)' : 'transparent', fontWeight: isActive ? 700 : 500 })}>{label}</NavLink>)}
        <div style={{ marginTop: 'auto', display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
          {!esSuperAdmin && <NavLink to="/pos" style={{ color: 'var(--color-on-primary)', padding: '0.7rem 0.85rem', textDecoration: 'none' }}>Ir al punto de venta</NavLink>}
          <button type="button" onClick={salir} style={{ padding: '0.7rem', borderRadius: 8, border: 0, cursor: 'pointer' }}>Cerrar sesión</button>
        </div>
      </aside>
      <main style={{ flex: 1, minWidth: 0, padding: '2rem' }}><Outlet /></main>
    </div>
  );
}

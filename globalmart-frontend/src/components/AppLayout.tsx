import React from 'react';
import { Outlet, Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';

export default function AppLayout() {
  const { logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  return (
    <div style={{ backgroundColor: '#f4f6f8', minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      <nav style={{ 
        display: 'flex', 
        justifyContent: 'space-between', 
        alignItems: 'center', 
        padding: '1rem 2rem', 
        backgroundColor: '#38bdf8', /* Azul del logo GlobalMart */
        color: 'white',
        boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.1)'
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '2rem' }}>
          <span style={{ fontSize: '1.5rem', fontWeight: 800, letterSpacing: '1px' }}>
            GLOBALMART
          </span>
          <div style={{ display: 'flex', gap: '1.5rem' }}>
            <Link to="/pos" style={{ color: 'white', textDecoration: 'none', fontWeight: 500, opacity: 0.9, transition: 'opacity 0.2s' }} onMouseOver={(e) => e.currentTarget.style.opacity = '1'} onMouseOut={(e) => e.currentTarget.style.opacity = '0.9'}>Punto de Venta</Link>
            <Link to="/abrir-turno" style={{ color: 'white', textDecoration: 'none', fontWeight: 500, opacity: 0.9, transition: 'opacity 0.2s' }} onMouseOver={(e) => e.currentTarget.style.opacity = '1'} onMouseOut={(e) => e.currentTarget.style.opacity = '0.9'}>Abrir Turno</Link>
            <Link to="/cerrar-turno" style={{ color: 'white', textDecoration: 'none', fontWeight: 500, opacity: 0.9, transition: 'opacity 0.2s' }} onMouseOver={(e) => e.currentTarget.style.opacity = '1'} onMouseOut={(e) => e.currentTarget.style.opacity = '0.9'}>Cerrar Turno</Link>
            <Link to="/admin" style={{ color: 'white', textDecoration: 'none', fontWeight: 500, opacity: 0.9, transition: 'opacity 0.2s' }} onMouseOver={(e) => e.currentTarget.style.opacity = '1'} onMouseOut={(e) => e.currentTarget.style.opacity = '0.9'}>Administración</Link>
          </div>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
          <span style={{ fontWeight: 500 }}>Cajero Demo</span>
          <button 
            onClick={handleLogout}
            style={{
              backgroundColor: 'white',
              color: '#38bdf8',
              border: 'none',
              padding: '0.5rem 1rem',
              borderRadius: '9999px',
              fontWeight: 600,
              cursor: 'pointer',
              transition: 'transform 0.1s, boxShadow 0.1s',
              boxShadow: '0 1px 3px rgba(0,0,0,0.1)'
            }}
            onMouseOver={(e) => { e.currentTarget.style.transform = 'scale(1.05)'; e.currentTarget.style.boxShadow = '0 4px 6px rgba(0,0,0,0.15)' }}
            onMouseOut={(e) => { e.currentTarget.style.transform = 'scale(1)'; e.currentTarget.style.boxShadow = '0 1px 3px rgba(0,0,0,0.1)' }}
          >
            Cerrar sesión
          </button>
        </div>
      </nav>
      <main style={{ padding: '2rem', flex: 1 }}>
        <Outlet />
      </main>
    </div>
  );
}

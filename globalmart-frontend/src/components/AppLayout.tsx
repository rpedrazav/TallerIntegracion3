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
    <div>
      <nav style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '1rem', borderBottom: '1px solid #ccc' }}>
        <span><strong>GlobalMart OS</strong></span>
        <div style={{ display: 'flex', gap: '1rem' }}>
          <Link to="/pos">Punto de Venta</Link>
          <Link to="/admin">Administración</Link>
        </div>
        <div>
          <span style={{ marginRight: '1rem' }}>Usuario Demo</span>
          <button onClick={handleLogout}>Cerrar sesión</button>
        </div>
      </nav>
      <main style={{ padding: '1rem' }}>
        <Outlet />
      </main>
    </div>
  );
}

import { Link } from 'react-router-dom';

export default function Login() {
  return (
    <div style={{ padding: '2rem', textAlign: 'center' }}>
      <h2>Login</h2>
      <p>Página de autenticación (placeholder)</p>
      <Link to="/pos" style={{ padding: '0.5rem 1rem', background: '#0070f3', color: '#fff', textDecoration: 'none', borderRadius: '4px' }}>
        Ingresar al sistema (Demo)
      </Link>
    </div>
  );
}

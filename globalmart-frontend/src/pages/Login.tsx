import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import axios from 'axios';
import { useAuth } from '../hooks/useAuth';

export default function Login() {
  const [email, setEmail] = useState('cajero@demo.cl');
  const [password, setPassword] = useState('demo1234');
  const [tenantId, setTenantId] = useState('aaaaaaaa-0000-0000-0000-000000000001');
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const { login } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setIsLoading(true);

    try {
      // Llamada real a la API de Identity
      const response = await axios.post('http://127.0.0.1:5124/auth/login', {
        email,
        password,
        tenantId
      });

      // Si es exitoso, la API devuelve el token en la respuesta
      // (Suponemos que viene en response.data.token)
      const token = response.data.token || response.data.accessToken || response.data;
      if (typeof token === 'string') {
        await login(token);
        navigate('/pos');
      } else {
        setError('El servidor no devolvi un token vlido');
      }
    } catch (err: any) {
      console.error(err);
      if (err.response) {
        // El servidor respondi con un cdigo de error
        const data = err.response.data;
        setError(typeof data === 'string' ? data : JSON.stringify(data));
      } else if (err.request) {
        // La peticin se hizo pero no hubo respuesta (CORS o servidor cado)
        setError('Error de conexin (CORS o servidor cado). Revisa la consola (Ctrl+Shift+I).');
      } else {
        setError('Error al enviar peticin: ' + err.message);
      }
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh', background: '#f4f6f8' }}>
      <form onSubmit={handleSubmit} style={{ background: 'white', padding: '2rem', borderRadius: '8px', boxShadow: '0 4px 6px rgba(0,0,0,0.1)', display: 'flex', flexDirection: 'column', gap: '1rem', width: '300px' }}>
        <h2 style={{ textAlign: 'center', color: '#38bdf8', margin: 0 }}>GlobalMart OS</h2>
        <p style={{ textAlign: 'center', margin: 0, color: '#666' }}>Inicia Sesin</p>
        
        {error && <div style={{ color: '#DC2626', background: '#FEE2E2', padding: '0.5rem', borderRadius: '4px', fontSize: '0.875rem' }}>{error}</div>}

        <div style={{ display: 'flex', flexDirection: 'column' }}>
          <label style={{ fontSize: '0.875rem', marginBottom: '0.25rem' }}>Tenant ID</label>
          <input type="text" value={tenantId} onChange={e => setTenantId(e.target.value)} required style={{ padding: '0.5rem', borderRadius: '4px', border: '1px solid #ccc' }} />
        </div>

        <div style={{ display: 'flex', flexDirection: 'column' }}>
          <label style={{ fontSize: '0.875rem', marginBottom: '0.25rem' }}>Email</label>
          <input type="email" value={email} onChange={e => setEmail(e.target.value)} required style={{ padding: '0.5rem', borderRadius: '4px', border: '1px solid #ccc' }} />
        </div>

        <div style={{ display: 'flex', flexDirection: 'column' }}>
          <label style={{ fontSize: '0.875rem', marginBottom: '0.25rem' }}>Contrasea</label>
          <input type="password" value={password} onChange={e => setPassword(e.target.value)} required style={{ padding: '0.5rem', borderRadius: '4px', border: '1px solid #ccc' }} />
        </div>

        <button type="submit" disabled={isLoading} style={{ padding: '0.75rem', background: '#38bdf8', color: 'white', border: 'none', borderRadius: '4px', cursor: isLoading ? 'not-allowed' : 'pointer', fontWeight: 'bold', marginTop: '0.5rem' }}>
          {isLoading ? 'Conectando...' : 'Ingresar'}
        </button>
      </form>
    </div>
  );
}


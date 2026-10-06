import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import axios from 'axios';

export default function Login() {
  const [tenantId, setTenantId] = useState('aaaaaaaa-0000-0000-0000-000000000001');
  const [email, setEmail] = useState('cajero@demo.cl');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const navigate = useNavigate();

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    try {
      // Usamos HTTPS para evitar el redirect 301 del Ingress (que rompe CORS)
      const response = await axios.post('https://auth-rpedraza.dev.censei.cl/auth/login', {
        email,
        password,
        tenantId
      }, {
        headers: {
          'x-tenant-id': tenantId
        }
      });
      
      const { token } = response.data;
      await window.api.setToken(token); localStorage.setItem('token', token);
      localStorage.setItem('tenant', tenantId);
      navigate('/pos');
    } catch (err: any) {
      if(err.response && err.response.status === 401) { setError('Credenciales o Tenant ID incorrectos.'); } else { setError('Error de conexin o configuracin. Revisa consola.'); }
      console.error(err);
    }
  };

  return (
    <div style={{ display: 'flex', height: '100vh', justifyContent: 'center', alignItems: 'center', backgroundColor: 'var(--color-bg)' }}>
      <div style={{ backgroundColor: 'var(--color-surface)', padding: '2rem', borderRadius: '8px', boxShadow: '0 4px 6px rgba(0,0,0,0.1)', width: '350px' }}>
        <h1 style={{ textAlign: 'center', color: 'var(--color-primary)', marginBottom: '0.5rem' }}>GlobalMart OS</h1>
        <p style={{ textAlign: 'center', color: 'var(--color-ink-soft)', marginBottom: '1.5rem' }}>Inicia Sesin</p>
        
        {error && (
          <div style={{ backgroundColor: 'var(--color-danger-bg)', color: 'var(--color-danger)', padding: '0.75rem', borderRadius: '4px', marginBottom: '1rem', fontSize: '0.875rem' }}>
            {error}
          </div>
        )}

        <form onSubmit={handleLogin}>
          <div style={{ marginBottom: '1rem' }}>
            <label style={{ display: 'block', marginBottom: '0.5rem', fontSize: '0.875rem' }}>Tenant ID</label>
            <input 
              type="text" 
              value={tenantId}
              onChange={(e) => setTenantId(e.target.value)}
              style={{ width: '100%', padding: '0.5rem', border: '1px solid var(--color-line)', borderRadius: '4px' }} 
            />
          </div>
          <div style={{ marginBottom: '1rem' }}>
            <label style={{ display: 'block', marginBottom: '0.5rem', fontSize: '0.875rem' }}>Email</label>
            <input 
              type="email" 
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              style={{ width: '100%', padding: '0.5rem', border: '1px solid var(--color-line)', borderRadius: '4px' }} 
            />
          </div>
          <div style={{ marginBottom: '1.5rem' }}>
            <label style={{ display: 'block', marginBottom: '0.5rem', fontSize: '0.875rem' }}>Contraseña</label>
            <input 
              type="password" 
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              style={{ width: '100%', padding: '0.5rem', border: '1px solid var(--color-line)', borderRadius: '4px' }} 
            />
          </div>
          <button type="submit" style={{ width: '100%', padding: '0.75rem', backgroundColor: 'var(--color-primary)', color: 'var(--color-surface)', border: 'none', borderRadius: '4px', cursor: 'pointer', fontWeight: 'bold' }}>
            Ingresar
          </button>
        </form>
      </div>
    </div>
  );
}






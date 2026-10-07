import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import axios, { AxiosError } from 'axios';

/**
 * TI3-471: Vista de Login con manejo de errores y spinner de carga.
 * - Muestra spinner mientras se procesa la autenticación.
 * - Mensajes de error claros según el tipo de fallo (401, red, etc.).
 * - Desactiva el botón durante la carga para evitar doble-submit.
 */
export default function Login() {
  const [tenantId, setTenantId] = useState('aaaaaaaa-0000-0000-0000-000000000001');
  const [email, setEmail] = useState('cajero@demo.cl');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const navigate = useNavigate();

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setIsLoading(true);

    try {
      const response = await axios.post('https://auth-rpedraza.dev.censei.cl/auth/login', {
        email,
        password,
        tenantId
      }, {
        headers: {
          'x-tenant-id': tenantId
        },
        timeout: 15000 // TI3-471: timeout de 15 segundos
      });
      
      const { token } = response.data;
      await window.api.setToken(token);
      localStorage.setItem('token', token);
      localStorage.setItem('tenant', tenantId);
      navigate('/pos');
    } catch (err: unknown) {
      // TI3-471: Manejo de errores específico según tipo de fallo
      const axiosErr = err as AxiosError<{ message?: string; error?: string }>;
      if (axiosErr.code === 'ECONNABORTED') {
        setError('La solicitud tardó demasiado. Verifica tu conexión e intenta de nuevo.');
      } else if (axiosErr.response) {
        // Error HTTP del servidor
        switch (axiosErr.response.status) {
          case 401:
            setError('Credenciales o Tenant ID incorrectos.');
            break;
          case 403:
            setError('No tienes permiso para acceder. Contacta al administrador.');
            break;
          case 404:
            setError('El servicio de autenticación no está disponible. Intenta más tarde.');
            break;
          case 500:
            setError('Error interno del servidor. Intenta nuevamente en unos minutos.');
            break;
          default:
            setError(`Error del servidor (${axiosErr.response.status}). Intenta nuevamente.`);
        }
      } else if (axiosErr.request) {
        // Error de red (no se recibió respuesta)
        setError('No se pudo conectar con el servidor. Verifica tu conexión a internet y que el servicio esté activo.');
      } else {
        const message = err instanceof Error ? err.message : String(err);
        setError(`Error inesperado: ${message}`);
      }
      console.error('[Login] Error:', err);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div style={{ display: 'flex', height: '100vh', justifyContent: 'center', alignItems: 'center', backgroundColor: 'var(--color-bg)' }}>
      <div style={{ backgroundColor: 'var(--color-surface)', padding: '2rem', borderRadius: '12px', boxShadow: '0 4px 16px rgba(0,0,0,0.08)', width: '380px', border: '1px solid var(--color-line)' }}>
        <h1 style={{ textAlign: 'center', color: 'var(--color-primary)', marginBottom: '0.5rem', fontSize: '1.5rem', fontWeight: 700 }}>GlobalMart OS</h1>
        <p style={{ textAlign: 'center', color: 'var(--color-ink-soft)', marginBottom: '1.5rem', fontSize: '0.9rem' }}>Inicia Sesión</p>
        
        {/* TI3-471: Banner de error con icono */}
        {error && (
          <div style={{
            backgroundColor: 'var(--color-danger-bg)',
            border: '1px solid var(--color-danger)',
            color: 'var(--color-danger)',
            padding: '0.875rem 1rem',
            borderRadius: '8px',
            marginBottom: '1rem',
            fontSize: '0.875rem',
            display: 'flex',
            alignItems: 'flex-start',
            gap: '0.5rem'
          }}>
            <span style={{ flexShrink: 0, marginTop: '1px' }}>⚠️</span>
            <span>{error}</span>
          </div>
        )}

        <form onSubmit={handleLogin}>
          <div style={{ marginBottom: '1rem' }}>
            <label style={{ display: 'block', marginBottom: '0.5rem', fontSize: '0.875rem', fontWeight: 600, color: 'var(--color-ink)' }}>Tenant ID</label>
            <input 
              type="text" 
              value={tenantId}
              onChange={(e) => setTenantId(e.target.value)}
              disabled={isLoading}
              style={{
                width: '100%',
                boxSizing: 'border-box',
                padding: '0.625rem 0.75rem',
                border: '1px solid var(--color-line)',
                borderRadius: '6px',
                fontSize: '0.875rem',
                color: 'var(--color-ink)',
                backgroundColor: isLoading ? 'var(--color-bg)' : 'var(--color-surface)',
                outline: 'none',
                transition: 'border-color 0.2s'
              }}
            />
          </div>
          <div style={{ marginBottom: '1rem' }}>
            <label style={{ display: 'block', marginBottom: '0.5rem', fontSize: '0.875rem', fontWeight: 600, color: 'var(--color-ink)' }}>Email</label>
            <input 
              type="email" 
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              disabled={isLoading}
              style={{
                width: '100%',
                boxSizing: 'border-box',
                padding: '0.625rem 0.75rem',
                border: '1px solid var(--color-line)',
                borderRadius: '6px',
                fontSize: '0.875rem',
                color: 'var(--color-ink)',
                backgroundColor: isLoading ? 'var(--color-bg)' : 'var(--color-surface)',
                outline: 'none',
                transition: 'border-color 0.2s'
              }}
            />
          </div>
          <div style={{ marginBottom: '1.5rem' }}>
            <label style={{ display: 'block', marginBottom: '0.5rem', fontSize: '0.875rem', fontWeight: 600, color: 'var(--color-ink)' }}>Contraseña</label>
            <input 
              type="password" 
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              disabled={isLoading}
              style={{
                width: '100%',
                boxSizing: 'border-box',
                padding: '0.625rem 0.75rem',
                border: '1px solid var(--color-line)',
                borderRadius: '6px',
                fontSize: '0.875rem',
                color: 'var(--color-ink)',
                backgroundColor: isLoading ? 'var(--color-bg)' : 'var(--color-surface)',
                outline: 'none',
                transition: 'border-color 0.2s'
              }}
            />
          </div>
          {/* TI3-471: Botón con spinner de carga */}
          <button
            type="submit"
            disabled={isLoading}
            style={{
              width: '100%',
              padding: '0.85rem',
              backgroundColor: isLoading ? 'var(--color-muted)' : 'var(--color-primary)',
              color: 'var(--color-surface)',
              border: 'none',
              borderRadius: '8px',
              cursor: isLoading ? 'not-allowed' : 'pointer',
              fontWeight: 700,
              fontSize: '1rem',
              transition: 'background-color 0.2s, transform 0.1s',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              gap: '0.5rem'
            }}
          >
            {isLoading && <span className="spinner" />}
            {isLoading ? 'Autenticando...' : 'Ingresar'}
          </button>
        </form>
      </div>
    </div>
  );
}

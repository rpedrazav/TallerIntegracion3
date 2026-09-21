import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { API_BASE_URL } from '../config';
import { useAuth } from '../hooks/useAuth';
import { parseJwtPayload, createDemoToken } from '../utils/jwt';
import { LoginResponse } from '../types/auth.types';
import Spinner from '../components/Spinner';

// Credenciales oficiales de prueba para el equipo
const TEST_CAJERO_EMAIL = 'cajero@globalmart.cl';
const TEST_CAJERO_PASSWORD = 'cajero123';

export default function Login() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const navigate = useNavigate();
  const { login } = useAuth();

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setLoading(true);
    setError(null);

    try {
      let isBackendAvailable = false;
      let response: Response | null = null;
      let data: LoginResponse = {};

      // 1. Siempre se intenta la llamada real al API Gateway / Backend primero
      try {
        response = await fetch(`${API_BASE_URL}/api/auth/login`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ email, password }),
        });

        try {
          data = await response.json();
        } catch {
          // Respuesta no es JSON
        }

        // Tarea 1 (TI3-162): Si el servidor responde 401 -> Credenciales incorrectas
        if (response.status === 401) {
          throw new Error('Credenciales incorrectas');
        }

        if (response.ok) {
          isBackendAvailable = true;
        }
      } catch (fetchErr) {
        // Si ya fue un 401 capturado, relanzarlo
        if (fetchErr instanceof Error && fetchErr.message === 'Credenciales incorrectas') {
          throw fetchErr;
        }
        // Kong está caído o sin backend (503 / Network Error): pasamos a validar usuario de prueba
      }

      // Si el backend real respondió exitosamente:
      if (isBackendAvailable && response?.ok) {
        const token = data.token || data.access_token;
        if (!token) {
          throw new Error('La respuesta del servidor no incluyó un token válido');
        }

        await login(token);
        const payload = parseJwtPayload(token);
        const activeRole = payload?.active_role?.toUpperCase();

        if (activeRole === 'ADMIN') {
          navigate('/admin');
        } else {
          navigate('/pos');
        }
        return;
      }

      // 2. Validación de usuario cajero de prueba (permite testear 401 y login real sin backend)
      await new Promise((resolve) => setTimeout(resolve, 500)); // Latencia para ver el spinner

      const inputEmail = email.trim().toLowerCase();

      if (inputEmail === TEST_CAJERO_EMAIL) {
        if (password === TEST_CAJERO_PASSWORD) {
          // Credenciales correctas -> Login exitoso como CAJERO
          const token = createDemoToken('CAJERO');
          await login(token);
          navigate('/pos');
          return;
        } else {
          // Contraseña incorrecta -> Error 401 real
          throw new Error('Credenciales incorrectas');
        }
      }

      // Cualquier otro usuario no registrado
      throw new Error('Credenciales incorrectas');
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error desconocido al iniciar sesión';
      setError(message);
    } finally {
      setLoading(false);
    }
  };

  const handleFillDemo = () => {
    setEmail(TEST_CAJERO_EMAIL);
    setPassword(TEST_CAJERO_PASSWORD);
    setError(null);
  };

  return (
    <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '100vh', backgroundColor: '#f9fafb' }}>
      <form
        onSubmit={handleSubmit}
        style={{
          width: '100%',
          maxWidth: '400px',
          padding: '2.5rem',
          backgroundColor: '#ffffff',
          borderRadius: '8px',
          boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06)',
          border: '1px solid #e5e7eb',
        }}
      >
        <h2 style={{ textAlign: 'center', marginBottom: '1.75rem', color: '#111827', fontSize: '1.5rem', fontWeight: 700 }}>
          GlobalMart OS
        </h2>

        <div style={{ marginBottom: '1.25rem' }}>
          <label htmlFor="email" style={{ display: 'block', marginBottom: '0.375rem', fontWeight: 500, fontSize: '0.875rem', color: '#374151' }}>
            Correo electrónico
          </label>
          <input
            id="email"
            type="email"
            placeholder="usuario@ejemplo.com"
            required
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            disabled={loading}
            style={{
              width: '100%',
              padding: '0.625rem 0.75rem',
              borderRadius: '6px',
              border: '1px solid #d1d5db',
              fontSize: '0.95rem',
              boxSizing: 'border-box',
              outline: 'none',
            }}
          />
        </div>

        <div style={{ marginBottom: '1.5rem' }}>
          <label htmlFor="password" style={{ display: 'block', marginBottom: '0.375rem', fontWeight: 500, fontSize: '0.875rem', color: '#374151' }}>
            Contraseña
          </label>
          <input
            id="password"
            type="password"
            placeholder="••••••••"
            required
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            disabled={loading}
            style={{
              width: '100%',
              padding: '0.625rem 0.75rem',
              borderRadius: '6px',
              border: '1px solid #d1d5db',
              fontSize: '0.95rem',
              boxSizing: 'border-box',
              outline: 'none',
            }}
          />
        </div>

        <button
          type="submit"
          disabled={loading}
          style={{
            width: '100%',
            padding: '0.625rem',
            background: loading ? '#93c5fd' : '#2563eb',
            color: '#fff',
            border: 'none',
            borderRadius: '6px',
            fontSize: '1rem',
            fontWeight: 600,
            cursor: loading ? 'not-allowed' : 'pointer',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            minHeight: '2.625rem',
            transition: 'background 0.2s',
          }}
        >
          {loading ? <Spinner size={20} color="#ffffff" /> : 'Ingresar'}
        </button>

        {error && (
          <p
            id="login-error"
            style={{
              color: '#dc2626',
              marginTop: '1rem',
              marginBottom: 0,
              fontSize: '0.875rem',
              textAlign: 'center',
              fontWeight: 500,
            }}
          >
            {error}
          </p>
        )}

        {/* Tarjeta de información para pruebas del equipo */}
        <div
          style={{
            marginTop: '1.5rem',
            padding: '0.75rem 1rem',
            backgroundColor: '#f8fafc',
            border: '1px solid #e2e8f0',
            borderRadius: '6px',
            fontSize: '0.75rem',
            color: '#475569',
          }}
        >
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.25rem' }}>
            <strong>Usuario de prueba (Cajero):</strong>
            <button
              type="button"
              onClick={handleFillDemo}
              style={{
                background: 'none',
                border: 'none',
                color: '#2563eb',
                cursor: 'pointer',
                fontSize: '0.75rem',
                textDecoration: 'underline',
                padding: 0,
              }}
            >
              Autocompletar
            </button>
          </div>
          <div>Correo: <code>cajero@globalmart.cl</code></div>
          <div>Contraseña correcta: <code>cajero123</code></div>
          <div style={{ marginTop: '0.25rem', color: '#94a3b8' }}>
            <em>* Escribe otra contraseña para probar el error 401 &quot;Credenciales incorrectas&quot;.</em>
          </div>
        </div>
      </form>
    </div>
  );
}

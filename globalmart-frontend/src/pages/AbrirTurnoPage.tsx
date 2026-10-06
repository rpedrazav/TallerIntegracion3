import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import axios, { AxiosError } from 'axios';
import { useAuth } from '../hooks/useAuth';

// URLs del microservicio MS-5 POS & Cart
const KONG_GATEWAY_URL = 'https://pos-rpedraza.dev.censei.cl';
const POS_DIRECT_URL = 'https://pos-rpedraza.dev.censei.cl';

interface JwtPayload {
  sub?: string;
  tenant_id?: string;
  nombre?: string;
  email?: string;
  active_role?: string;
  sucursal_id?: string;
}

interface TurnoActivoData {
  id: string;
  tenantId: string;
  cajeroId: string;
  sucursalId: string;
  montoApertura: number;
  estado: number;
  abiertoAt: string;
}

function parseJwt(token: string | null): JwtPayload | null {
  if (!token) return null;
  try {
    const base64Url = token.split('.')[1];
    if (!base64Url) return null;
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );
    return JSON.parse(jsonPayload);
  } catch {
    return null;
  }
}

export default function AbrirTurnoPage() {
  const { token } = useAuth();
  const navigate = useNavigate();

  const [montoInicial, setMontoInicial] = useState<string>('0');
  const [error, setError] = useState<string>('');
  const [successMsg, setSuccessMsg] = useState<string>('');
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [turnoActivo, setTurnoActivo] = useState<TurnoActivoData | null>(null);

  const userInfo = parseJwt(token);

  // Helper para realizar peticiones hacia MS-5 POS (directo en puerto 5000 o vía gateway)
  const callPosApi = useCallback(
    async (path: string, method: 'GET' | 'POST', data?: unknown) => {
      const headers = { Authorization: `Bearer ${token}` };
      const normalizedPath = path.startsWith('/') ? path : `/${path}`;
      const apiPath = normalizedPath.startsWith('/api') ? normalizedPath : `/api${normalizedPath}`;
      const directPath = normalizedPath.startsWith('/api') ? normalizedPath.replace(/^\/api/, '') : normalizedPath;

      // Intentar primero directo al microservicio MS-5 POS (puerto 5000 en dev local)
      try {
        const directUrl = `${POS_DIRECT_URL}${apiPath}`;
        return await axios({ method, url: directUrl, data, headers });
      } catch (err) {
        const axiosErr = err as AxiosError;
        // Si da 404 o error de conexión, intentar sin prefijo /api o a través de Kong Gateway
        if ((axiosErr.request && !axiosErr.response) || axiosErr.response?.status === 404) {
          try {
            const fallbackDirect = `${POS_DIRECT_URL}${directPath}`;
            return await axios({ method, url: fallbackDirect, data, headers });
          } catch (innerErr) {
            const innerAxios = innerErr as AxiosError;
            if (innerAxios.request && !innerAxios.response) {
              try {
                const kongUrl = `${KONG_GATEWAY_URL}${apiPath}`;
                return await axios({ method, url: kongUrl, data, headers });
              } catch {
                throw innerAxios.response ? innerAxios : axiosErr;
              }
            }
            throw innerAxios;
          }
        }
        throw axiosErr;
      }
    },
    [token]
  );

  // Verificar si ya existe un turno activo para este cajero
  useEffect(() => {
    let isMounted = true;

    async function checkTurnoActivo() {
      if (!token) return;
      try {
        const response = await callPosApi('/api/turnos/activo', 'GET');
        if (isMounted && response?.data) {
          setTurnoActivo(response.data as TurnoActivoData);
        }
      } catch {
        // 404 indica que no hay turno abierto, comportamiento esperado.
      }
    }

    checkTurnoActivo();

    return () => {
      isMounted = false;
    };
  }, [token, callPosApi]);

  // Validaciones
  const montoNum = parseFloat(montoInicial);
  const isMontoValid = !isNaN(montoNum) && montoNum >= 0 && montoInicial.trim() !== '';

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setSuccessMsg('');

    if (!isMontoValid) {
      setError('El monto inicial de caja debe ser un número mayor o igual a 0.');
      return;
    }

    setIsLoading(true);

    try {
      const sucursalId =
        userInfo?.sucursal_id && userInfo.sucursal_id.length > 10
          ? userInfo.sucursal_id
          : '00000000-0000-0000-0000-000000000001';

      await callPosApi('/api/turnos/abrir', 'POST', {
        sucursal_id: sucursalId,
        monto_fondo_inicial: montoNum,
      });

      const confirmMsg = '¡Turno de caja abierto exitosamente!';
      setSuccessMsg(`${confirmMsg} Redirigiendo al Punto de Venta...`);

      setTimeout(() => {
        navigate('/pos', {
          state: {
            mensajeConfirmacion: confirmMsg,
          },
        });
      }, 800);
    } catch (err) {
      console.error('Error al abrir turno:', err);
      const axiosErr = err as AxiosError<{ error?: string }>;
      if (axiosErr.response) {
        if (axiosErr.response.status === 409) {
          setError('Ya tienes un turno abierto');
        } else if (axiosErr.response.data?.error) {
          setError(axiosErr.response.data.error);
        } else if (typeof axiosErr.response.data === 'string') {
          setError(axiosErr.response.data);
        } else {
          setError(`Error del servidor (${axiosErr.response.status}). Intente nuevamente.`);
        }
      } else if (axiosErr.request) {
        setError('No se pudo conectar con el microservicio de Turnos (MS-5 POS). Verifique que esté ejecutándose.');
      } else {
        setError(`Error: ${axiosErr.message}`);
      }
    } finally {
      setIsLoading(false);
    }
  };

  const handlePresetClick = (amount: number) => {
    setMontoInicial(amount.toString());
    setError('');
  };

  return (
    <div style={{ maxWidth: '640px', margin: '2rem auto', padding: '0 1rem' }}>
      {/* Tarjeta Principal */}
      <div
        style={{
          background: 'var(--color-surface)',
          borderRadius: '12px',
          boxShadow: '0 4px 16px rgba(0, 0, 0, 0.08)',
          border: '1px solid var(--color-line)',
          overflow: 'hidden',
        }}
      >
        {/* Cabecera */}
        <div
          style={{
            background: 'linear-gradient(135deg, var(--color-primary) 0%, var(--color-primary) 100%)',
            color: 'var(--color-surface)',
            padding: '1.75rem 2rem',
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '0.5rem' }}>
            <span style={{ fontSize: '1.75rem' }}>💼</span>
            <h1 style={{ margin: 0, fontSize: '1.5rem', fontWeight: 700, letterSpacing: '-0.02em' }}>
              Apertura de Turno de Caja
            </h1>
          </div>
          <p style={{ margin: 0, opacity: 0.9, fontSize: '0.925rem' }}>
            Registra el monto inicial de efectivo en caja antes de comenzar la sesión de ventas.
          </p>
        </div>

        {/* Cuerpo */}
        <div style={{ padding: '2rem' }}>
          {/* Si ya hay un turno activo detectado */}
          {turnoActivo && (
            <div
              style={{
                marginBottom: '1.5rem',
                padding: '1rem',
                background: 'var(--color-primary-bg)',
                border: '1px solid var(--color-muted)',
                borderRadius: '8px',
                color: 'var(--color-primary)',
                fontSize: '0.9rem',
              }}
            >
              <div style={{ fontWeight: 600, marginBottom: '0.25rem', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                <span>ℹ️</span> Turno Activo Detectado
              </div>
              <p style={{ margin: '0 0 0.75rem 0' }}>
                Ya tienes un turno activo abierto (ID:{' '}
                <code style={{ background: 'var(--color-primary-bg)', padding: '0.1rem 0.3rem', borderRadius: '4px' }}>
                  {turnoActivo.id}
                </code>
                ). Puedes continuar directamente al Punto de Venta.
              </p>
              <button
                type="button"
                onClick={() => navigate('/pos')}
                style={{
                  background: 'var(--color-primary)',
                  color: 'var(--color-surface)',
                  border: 'none',
                  padding: '0.5rem 1rem',
                  borderRadius: '6px',
                  fontWeight: 600,
                  fontSize: '0.875rem',
                  cursor: 'pointer',
                }}
              >
                Ir al Punto de Venta →
              </button>
            </div>
          )}

          {/* Banner de Cajero / Sesión */}
          <div
            style={{
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
              background: 'var(--color-bg)',
              border: '1px solid var(--color-line)',
              borderRadius: '8px',
              padding: '0.875rem 1rem',
              marginBottom: '1.5rem',
            }}
          >
            <div>
              <div style={{ fontSize: '0.75rem', textTransform: 'uppercase', color: 'var(--color-ink-soft)', fontWeight: 600 }}>
                Cajero en Sesión
              </div>
              <div style={{ fontWeight: 600, color: 'var(--color-ink)', fontSize: '0.95rem' }}>
                {userInfo?.nombre || 'Cajero Autenticado'}
              </div>
            </div>
            <div style={{ textAlign: 'right' }}>
              <div style={{ fontSize: '0.75rem', textTransform: 'uppercase', color: 'var(--color-ink-soft)', fontWeight: 600 }}>
                Rol
              </div>
              <span
                style={{
                  display: 'inline-block',
                  background: 'var(--color-primary-bg)',
                  color: 'var(--color-primary)',
                  padding: '0.2rem 0.6rem',
                  borderRadius: '9999px',
                  fontSize: '0.75rem',
                  fontWeight: 700,
                }}
              >
                {userInfo?.active_role || 'CAJERO'}
              </span>
            </div>
          </div>

          {/* Mensajes de Alerta */}
          {error && (
            <div
              style={{
                backgroundColor: 'var(--color-danger-bg)',
                border: '1px solid var(--color-danger)',
                color: 'var(--color-danger)',
                padding: '0.875rem 1rem',
                borderRadius: '8px',
                fontSize: '0.875rem',
                marginBottom: '1.25rem',
                display: 'flex',
                alignItems: 'center',
                gap: '0.5rem',
              }}
            >
              <span>⚠️</span>
              <span>{error}</span>
            </div>
          )}

          {successMsg && (
            <div
              style={{
                backgroundColor: 'var(--color-success-bg)',
                border: '1px solid var(--color-success)',
                color: 'var(--color-success)',
                padding: '0.875rem 1rem',
                borderRadius: '8px',
                fontSize: '0.875rem',
                marginBottom: '1.25rem',
                display: 'flex',
                alignItems: 'center',
                gap: '0.5rem',
              }}
            >
              <span>✅</span>
              <span>{successMsg}</span>
            </div>
          )}

          {/* Formulario */}
          <form onSubmit={handleSubmit}>
            <div style={{ marginBottom: '1.5rem' }}>
              <label
                htmlFor="monto-inicial"
                style={{
                  display: 'block',
                  fontWeight: 600,
                  color: 'var(--color-ink)',
                  fontSize: '0.95rem',
                  marginBottom: '0.5rem',
                }}
              >
                Monto inicial de caja
              </label>

              <div style={{ position: 'relative' }}>
                <span
                  style={{
                    position: 'absolute',
                    left: '1rem',
                    top: '50%',
                    transform: 'translateY(-50%)',
                    fontWeight: 700,
                    color: 'var(--color-ink-soft)',
                    fontSize: '1.25rem',
                  }}
                >
                  $
                </span>
                <input
                  id="monto-inicial"
                  type="number"
                  min="0"
                  step="1"
                  value={montoInicial}
                  onChange={(e) => {
                    setMontoInicial(e.target.value);
                    if (error) setError('');
                  }}
                  placeholder="0"
                  required
                  style={{
                    width: '100%',
                    boxSizing: 'border-box',
                    padding: '0.875rem 1rem 0.875rem 2.5rem',
                    fontSize: '1.25rem',
                    fontWeight: 600,
                    borderRadius: '8px',
                    border: !isMontoValid && montoInicial !== '' ? '2px solid var(--color-danger)' : '1px solid var(--color-line)',
                    outline: 'none',
                    transition: 'border-color 0.2s',
                    color: 'var(--color-ink)',
                  }}
                  onFocus={(e) => (e.target.style.borderColor = 'var(--color-primary)')}
                  onBlur={(e) =>
                    (e.target.style.borderColor = !isMontoValid && montoInicial !== '' ? 'var(--color-danger)' : 'var(--color-line)')
                  }
                />
              </div>

              {/* Mensaje de validación rápida */}
              <div style={{ marginTop: '0.375rem', fontSize: '0.8rem', color: 'var(--color-ink-soft)' }}>
                {!isMontoValid && montoInicial !== '' ? (
                  <span style={{ color: 'var(--color-danger)', fontWeight: 600 }}>
                    El monto debe ser un número mayor o igual a 0.
                  </span>
                ) : (
                  'Ingresa el efectivo disponible en gaveta para dar vuelto (>= 0).'
                )}
              </div>

              {/* Botones de montos frecuentes */}
              <div style={{ marginTop: '1rem' }}>
                <div style={{ fontSize: '0.8rem', color: 'var(--color-ink-soft)', marginBottom: '0.4rem', fontWeight: 500 }}>
                  Montos frecuentes sugeridos:
                </div>
                <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
                  {[0, 20000, 50000, 100000].map((val) => (
                    <button
                      key={val}
                      type="button"
                      onClick={() => handlePresetClick(val)}
                      style={{
                        background: montoNum === val ? 'var(--color-primary-bg)' : 'var(--color-bg)',
                        color: montoNum === val ? 'var(--color-primary)' : 'var(--color-ink-soft)',
                        border: montoNum === val ? '1px solid var(--color-primary)' : '1px solid var(--color-line)',
                        borderRadius: '6px',
                        padding: '0.4rem 0.8rem',
                        fontSize: '0.85rem',
                        fontWeight: 600,
                        cursor: 'pointer',
                        transition: 'all 0.15s ease',
                      }}
                    >
                      ${val.toLocaleString('es-CL')}
                    </button>
                  ))}
                </div>
              </div>
            </div>

            {/* Botón de acción */}
            <button
              type="submit"
              disabled={isLoading || !isMontoValid}
              style={{
                width: '100%',
                padding: '0.95rem',
                backgroundColor: isLoading || !isMontoValid ? 'var(--color-muted)' : 'var(--color-primary)',
                color: 'var(--color-surface)',
                border: 'none',
                borderRadius: '8px',
                fontSize: '1.05rem',
                fontWeight: 700,
                cursor: isLoading || !isMontoValid ? 'not-allowed' : 'pointer',
                transition: 'background-color 0.2s, transform 0.1s',
                boxShadow: '0 4px 6px -1px color-mix(in srgb, var(--color-primary) 25%, transparent)',
              }}
              onMouseOver={(e) => {
                if (!isLoading && isMontoValid) e.currentTarget.style.backgroundColor = 'var(--color-primary)';
              }}
              onMouseOut={(e) => {
                if (!isLoading && isMontoValid) e.currentTarget.style.backgroundColor = 'var(--color-primary)';
              }}
            >
              {isLoading ? 'Abriendo Turno...' : 'Abrir Turno'}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}

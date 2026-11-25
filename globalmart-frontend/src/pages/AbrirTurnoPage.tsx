import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import axios, { AxiosError } from 'axios';
import { useAuth } from '../hooks/useAuth';
import { posApi, TurnoActivo } from '../api/client';
import './AbrirTurnoPage.css';

interface JwtPayload {
  sub?: string;
  tenant_id?: string;
  nombre?: string;
  email?: string;
  active_role?: string;
  sucursal_id?: string;
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
  const [turnoActivo, setTurnoActivo] = useState<TurnoActivo | null>(null);

  const userInfo = parseJwt(token);

  // Verificar si ya existe un turno activo para este cajero
  useEffect(() => {
    let isMounted = true;

    async function checkTurnoActivo() {
      if (!token) return;
      try {
        const activeShift = await posApi.activeShift();
        if (isMounted && activeShift) {
          setTurnoActivo(activeShift);
        }
      } catch {
        // 404 indica que no hay turno abierto, comportamiento esperado.
      }
    }

    checkTurnoActivo();

    return () => {
      isMounted = false;
    };
  }, [token]);

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

      if (!token) return;
      await posApi.openShift(sucursalId, montoNum);

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
    <div className="abrir-turno-page">
      <div className="abrir-turno-card">
        <div className="abrir-turno-header">
          <div className="abrir-turno-title-row">
            <span className="abrir-turno-icon" aria-hidden="true">💼</span>
            <h1 className="abrir-turno-title">Apertura de Turno de Caja</h1>
          </div>
          <p className="abrir-turno-subtitle">
            Registra el monto inicial de efectivo en caja antes de comenzar la sesión de ventas.
          </p>
        </div>

        <div className="abrir-turno-body">
          {turnoActivo && (
            <div className="abrir-turno-info-box" role="status">
              <div className="abrir-turno-info-title">
                <span aria-hidden="true">ℹ️</span> Turno Activo Detectado
              </div>
              <p className="abrir-turno-info-text">
                Ya tienes un turno activo abierto (ID:{' '}
                <code className="abrir-turno-code">{turnoActivo.id}</code>). Puedes continuar
                directamente al Punto de Venta.
              </p>
              <button
                type="button"
                onClick={() => navigate('/pos')}
                className="abrir-turno-btn-go-pos"
              >
                Ir al Punto de Venta →
              </button>
            </div>
          )}

          <div className="abrir-turno-banner">
            <div>
              <div className="abrir-turno-meta-label">Cajero en Sesión</div>
              <div className="abrir-turno-cashier-name">
                {userInfo?.nombre || 'Cajero Autenticado'}
              </div>
            </div>
            <div className="abrir-turno-role-box">
              <div className="abrir-turno-meta-label">Rol</div>
              <span className="abrir-turno-role-badge">
                {userInfo?.active_role || 'CAJERO'}
              </span>
            </div>
          </div>

          {error && (
            <div className="abrir-turno-alert abrir-turno-alert--danger" role="alert">
              <span aria-hidden="true">⚠️</span>
              <span>{error}</span>
            </div>
          )}

          {successMsg && (
            <div className="abrir-turno-alert abrir-turno-alert--success" role="status">
              <span aria-hidden="true">✅</span>
              <span>{successMsg}</span>
            </div>
          )}

          <form onSubmit={handleSubmit}>
            <div className="abrir-turno-field">
              <label htmlFor="monto-inicial" className="abrir-turno-label">
                Monto inicial de caja
              </label>

              <div className="abrir-turno-input-wrap">
                <span className="abrir-turno-currency" aria-hidden="true">$</span>
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
                  className={`abrir-turno-input${!isMontoValid && montoInicial !== '' ? ' abrir-turno-input--invalid' : ''}`}
                />
              </div>

              <div className="abrir-turno-hint">
                {!isMontoValid && montoInicial !== '' ? (
                  <span className="abrir-turno-hint--error">
                    El monto debe ser un número mayor o igual a 0.
                  </span>
                ) : (
                  'Ingresa el efectivo disponible en gaveta para dar vuelto (>= 0).'
                )}
              </div>

              <div className="abrir-turno-presets">
                <div className="abrir-turno-presets-label">Montos frecuentes sugeridos:</div>
                <div className="abrir-turno-presets-list">
                  {[0, 20000, 50000, 100000].map((val) => (
                    <button
                      key={val}
                      type="button"
                      onClick={() => handlePresetClick(val)}
                      className={`abrir-turno-preset-btn${montoNum === val ? ' abrir-turno-preset-btn--active' : ''}`}
                    >
                      ${val.toLocaleString('es-CL')}
                    </button>
                  ))}
                </div>
              </div>
            </div>

            <button
              type="submit"
              disabled={isLoading || !isMontoValid}
              className="abrir-turno-submit"
            >
              {isLoading && <span className="spinner" />}
              {isLoading ? 'Abriendo Turno...' : 'Abrir Turno'}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}

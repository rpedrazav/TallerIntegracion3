import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import axios, { AxiosError } from 'axios';
import { useAuth } from '../hooks/useAuth';

// URLs del microservicio MS-5 POS & Cart
const KONG_GATEWAY_URL = 'http://127.0.0.1:8000';
const POS_DIRECT_URL = 'http://127.0.0.1:5000';

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

interface CuadreResult {
  turno_id: string;
  efectivo_esperado: number;
  monto_declarado: number;
  diferencia: number;
}

interface DenominacionConfig {
  id: string;
  valor: number;
  label: string;
  tipo: 'billete' | 'moneda';
}

const DENOMINACIONES: DenominacionConfig[] = [
  { id: 'b_20000', valor: 20000, label: '$20.000', tipo: 'billete' },
  { id: 'b_10000', valor: 10000, label: '$10.000', tipo: 'billete' },
  { id: 'b_5000',  valor: 5000,  label: '$5.000',  tipo: 'billete' },
  { id: 'b_2000',  valor: 2000,  label: '$2.000',  tipo: 'billete' },
  { id: 'b_1000',  valor: 1000,  label: '$1.000',  tipo: 'billete' },
  { id: 'm_500',   valor: 500,   label: '$500',    tipo: 'moneda' },
  { id: 'm_100',   valor: 100,   label: '$100',    tipo: 'moneda' },
  { id: 'm_50',    valor: 50,    label: '$50',     tipo: 'moneda' },
  { id: 'm_10',    valor: 10,    label: '$10',     tipo: 'moneda' },
];

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

export default function CerrarTurnoPage() {
  const { token } = useAuth();
  const navigate = useNavigate();
  const userInfo = parseJwt(token);

  // Cantidades por cada denominación (clave: id denominación, valor: cantidad numérica)
  const [cantidades, setCantidades] = useState<Record<string, number>>(() => {
    const initial: Record<string, number> = {};
    DENOMINACIONES.forEach((d) => {
      initial[d.id] = 0;
    });
    return initial;
  });

  const [turnoActivo, setTurnoActivo] = useState<TurnoActivoData | null>(null);
  const [checkingTurno, setCheckingTurno] = useState<boolean>(true);
  const [noTurnoOpen, setNoTurnoOpen] = useState<boolean>(false);

  const [efectivoEsperado, setEfectivoEsperado] = useState<number | null>(null);
  const [cuadreResult, setCuadreResult] = useState<CuadreResult | null>(null);
  const [isCalculatingCuadre, setIsCalculatingCuadre] = useState<boolean>(false);
  const [cuadreSynced, setCuadreSynced] = useState<boolean>(false);

  const [isClosingTurno, setIsClosingTurno] = useState<boolean>(false);
  const [turnoClosedSuccess, setTurnoClosedSuccess] = useState<boolean>(false);

  const [errorMsg, setErrorMsg] = useState<string>('');
  const [successMsg, setSuccessMsg] = useState<string>('');

  // Helper para realizar peticiones hacia MS-5 POS (directo en puerto 5000 o vía Kong)
  const callPosApi = useCallback(
    async (path: string, method: 'GET' | 'POST', data?: unknown) => {
      const headers = { Authorization: `Bearer ${token}` };
      const normalizedPath = path.startsWith('/') ? path : `/${path}`;
      const apiPath = normalizedPath.startsWith('/api') ? normalizedPath : `/api${normalizedPath}`;
      const directPath = normalizedPath.startsWith('/api') ? normalizedPath.replace(/^\/api/, '') : normalizedPath;

      try {
        const directUrl = `${POS_DIRECT_URL}${apiPath}`;
        return await axios({ method, url: directUrl, data, headers });
      } catch (err) {
        const axiosErr = err as AxiosError;
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

  // Verificar turno activo al cargar la pantalla y obtener efectivo esperado inicial
  useEffect(() => {
    let isMounted = true;

    async function checkTurnoActivo() {
      if (!token) {
        if (isMounted) setCheckingTurno(false);
        return;
      }

      try {
        const res = await callPosApi('/api/turnos/activo', 'GET');
        if (isMounted) {
          if (res?.data) {
            const activeTurno = res.data as TurnoActivoData;
            setTurnoActivo(activeTurno);
            setNoTurnoOpen(false);

            // Llamar a POST /api/turnos/cuadre para obtener el efectivo esperado de ventas del turno
            try {
              const cuadreRes = await callPosApi('/api/turnos/cuadre', 'POST', {
                monto_declarado: 0,
              });
              if (isMounted && cuadreRes?.data) {
                const data = cuadreRes.data as CuadreResult;
                setEfectivoEsperado(data.efectivo_esperado);
                setCuadreResult(data);
                setCuadreSynced(true);
              }
            } catch (cuadreErr) {
              console.warn('No se pudo precargar el efectivo esperado:', cuadreErr);
            }
          } else {
            setNoTurnoOpen(true);
          }
        }
      } catch (err) {
        if (isMounted) {
          const axiosErr = err as AxiosError;
          if (axiosErr.response?.status === 404) {
            setNoTurnoOpen(true);
            setTurnoActivo(null);
          } else {
            setErrorMsg('No se pudo verificar el estado del turno actual.');
          }
        }
      } finally {
        if (isMounted) {
          setCheckingTurno(false);
        }
      }
    }

    checkTurnoActivo();

    return () => {
      isMounted = false;
    };
  }, [token, callPosApi]);

  // Manejo de cambio en el input de cantidad
  const handleCantidadChange = (id: string, valueStr: string) => {
    setErrorMsg('');
    setCuadreSynced(false);
    const parsed = parseInt(valueStr, 10);
    const validQty = isNaN(parsed) || parsed < 0 ? 0 : parsed;

    setCantidades((prev) => ({
      ...prev,
      [id]: validQty,
    }));
  };

  // Ajuste rápido con botones +/-
  const handleStepChange = (id: string, delta: number) => {
    setErrorMsg('');
    setCuadreSynced(false);
    setCantidades((prev) => {
      const current = prev[id] || 0;
      const next = Math.max(0, current + delta);
      return { ...prev, [id]: next };
    });
  };

  // Resetear todo a 0
  const handleReset = () => {
    const reset: Record<string, number> = {};
    DENOMINACIONES.forEach((d) => {
      reset[d.id] = 0;
    });
    setCantidades(reset);
    setCuadreSynced(false);
    setErrorMsg('');
  };

  // Cálculos automáticos de subtotales y total declarado sumando (denominacion × cantidad)
  const { totalBilletes, totalMonedas, totalDeclarado } = useMemo(() => {
    let billetes = 0;
    let monedas = 0;

    DENOMINACIONES.forEach((d) => {
      const qty = cantidades[d.id] || 0;
      const sub = d.valor * qty;
      if (d.tipo === 'billete') {
        billetes += sub;
      } else {
        monedas += sub;
      }
    });

    return {
      totalBilletes: billetes,
      totalMonedas: monedas,
      totalDeclarado: billetes + monedas,
    };
  }, [cantidades]);

  // Diferencia calculada dinámicamente en tiempo real
  const diferenciaCalculada = useMemo(() => {
    if (efectivoEsperado === null) return null;
    return Math.round((totalDeclarado - efectivoEsperado) * 100) / 100;
  }, [totalDeclarado, efectivoEsperado]);

  // Ejecutar llamada a POST /api/turnos/cuadre para sincronizar con MS-5
  const handleCalcularCuadre = async () => {
    setErrorMsg('');
    setIsCalculatingCuadre(true);

    try {
      const res = await callPosApi('/api/turnos/cuadre', 'POST', {
        monto_declarado: totalDeclarado,
      });

      if (res?.data) {
        const data = res.data as CuadreResult;
        setCuadreResult(data);
        setEfectivoEsperado(data.efectivo_esperado);
        setCuadreSynced(true);
      }
    } catch (err) {
      console.error('Error al calcular cuadre:', err);
      const axiosErr = err as AxiosError<{ error?: string }>;
      if (axiosErr.response?.data?.error) {
        setErrorMsg(axiosErr.response.data.error);
      } else if (axiosErr.response?.status === 404) {
        setErrorMsg('El cajero no tiene un turno ABIERTO.');
        setNoTurnoOpen(true);
      } else {
        setErrorMsg('Error al conectar con el servicio de cuadre de caja.');
      }
    } finally {
      setIsCalculatingCuadre(false);
    }
  };

  // Confirmar y cerrar turno (POST /api/turnos/cerrar)
  const handleCerrarTurno = async () => {
    if (!window.confirm(`¿Confirmas el cierre del turno con un monto declarado de $${totalDeclarado.toLocaleString('es-CL')}?`)) {
      return;
    }

    setIsClosingTurno(true);
    setErrorMsg('');

    try {
      await callPosApi('/api/turnos/cerrar', 'POST');
      setTurnoClosedSuccess(true);
      setSuccessMsg('¡Turno cerrado exitosamente!');
    } catch (err) {
      console.error('Error al cerrar turno:', err);
      const axiosErr = err as AxiosError<{ error?: string }>;
      if (axiosErr.response?.data?.error) {
        setErrorMsg(axiosErr.response.data.error);
      } else if (axiosErr.response?.status === 404) {
        setErrorMsg('No existe un turno abierto para cerrar.');
        setNoTurnoOpen(true);
      } else {
        setErrorMsg('Ocurrió un error al procesar el cierre de turno.');
      }
    } finally {
      setIsClosingTurno(false);
    }
  };

  return (
    <div style={{ maxWidth: '900px', margin: '1.5rem auto', padding: '0 1rem' }}>
      {/* Contenedor Principal */}
      <div
        style={{
          background: '#ffffff',
          borderRadius: '16px',
          boxShadow: '0 10px 25px -5px rgba(0, 0, 0, 0.08), 0 8px 10px -6px rgba(0, 0, 0, 0.04)',
          border: '1px solid #e2e8f0',
          overflow: 'hidden',
        }}
      >
        {/* Cabecera */}
        <div
          style={{
            background: 'linear-gradient(135deg, #0369a1 0%, #0284c7 60%, #38bdf8 100%)',
            color: 'white',
            padding: '2rem',
            position: 'relative',
          }}
        >
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', flexWrap: 'wrap', gap: '1rem' }}>
            <div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '0.5rem' }}>
                <span style={{ fontSize: '2rem' }}>🧾</span>
                <h1 style={{ margin: 0, fontSize: '1.75rem', fontWeight: 800, letterSpacing: '-0.025em' }}>
                  Cierre de Turno y Cuadre de Caja
                </h1>
              </div>
              <p style={{ margin: 0, opacity: 0.95, fontSize: '0.95rem' }}>
                Conteo y desglose de denominaciones de billetes y monedas para el arqueo final.
              </p>
            </div>

            {/* Datos de Cajero y Turno */}
            <div
              style={{
                background: 'rgba(255, 255, 255, 0.15)',
                backdropFilter: 'blur(8px)',
                borderRadius: '10px',
                padding: '0.75rem 1.25rem',
                border: '1px solid rgba(255, 255, 255, 0.25)',
                fontSize: '0.85rem',
              }}
            >
              <div><strong>Cajero:</strong> {userInfo?.nombre || 'Cajero en Sesión'}</div>
              <div><strong>Rol:</strong> {userInfo?.active_role || 'CAJERO'}</div>
              {checkingTurno ? (
                <div style={{ marginTop: '0.25rem', opacity: 0.85, fontSize: '0.8rem' }}>
                  <em>Verificando turno activo...</em>
                </div>
              ) : turnoActivo ? (
                <div style={{ marginTop: '0.25rem' }}>
                  <strong>Turno ID:</strong> <code style={{ fontSize: '0.8rem' }}>{turnoActivo.id.slice(0, 8)}...</code>
                </div>
              ) : null}
            </div>
          </div>
        </div>

        {/* Contenido */}
        <div style={{ padding: '2rem' }}>
          {/* Mensajes de Alerta */}
          {errorMsg && (
            <div
              style={{
                backgroundColor: '#fee2e2',
                border: '1px solid #fca5a5',
                color: '#b91c1c',
                padding: '1rem',
                borderRadius: '10px',
                fontSize: '0.925rem',
                marginBottom: '1.5rem',
                display: 'flex',
                alignItems: 'center',
                gap: '0.75rem',
              }}
            >
              <span style={{ fontSize: '1.25rem' }}>⚠️</span>
              <span>{errorMsg}</span>
            </div>
          )}

          {successMsg && (
            <div
              style={{
                backgroundColor: '#dcfce7',
                border: '1px solid #86efac',
                color: '#15803d',
                padding: '1.25rem',
                borderRadius: '10px',
                fontSize: '0.95rem',
                marginBottom: '1.5rem',
                display: 'flex',
                flexDirection: 'column',
                gap: '0.75rem',
              }}
            >
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', fontWeight: 700, fontSize: '1.05rem' }}>
                <span>✅</span>
                <span>{successMsg}</span>
              </div>
              <p style={{ margin: 0 }}>
                El turno ha finalizado correctamente. La gaveta ha sido registrada con un monto total declarado de{' '}
                <strong>${totalDeclarado.toLocaleString('es-CL')}</strong>.
              </p>
              <div style={{ display: 'flex', gap: '1rem', marginTop: '0.5rem' }}>
                <button
                  type="button"
                  onClick={() => navigate('/abrir-turno')}
                  style={{
                    backgroundColor: '#16a34a',
                    color: 'white',
                    border: 'none',
                    padding: '0.6rem 1.2rem',
                    borderRadius: '8px',
                    fontWeight: 600,
                    cursor: 'pointer',
                  }}
                >
                  Abrir Nuevo Turno →
                </button>
                <button
                  type="button"
                  onClick={() => navigate('/pos')}
                  style={{
                    backgroundColor: '#ffffff',
                    color: '#15803d',
                    border: '1px solid #86efac',
                    padding: '0.6rem 1.2rem',
                    borderRadius: '8px',
                    fontWeight: 600,
                    cursor: 'pointer',
                  }}
                >
                  Volver al POS
                </button>
              </div>
            </div>
          )}

          {/* Aviso si no hay turno abierto */}
          {noTurnoOpen && !turnoClosedSuccess && (
            <div
              style={{
                backgroundColor: '#fef3c7',
                border: '1px solid #fde68a',
                color: '#92400e',
                padding: '1.25rem',
                borderRadius: '10px',
                marginBottom: '1.5rem',
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
                flexWrap: 'wrap',
                gap: '1rem',
              }}
            >
              <div>
                <div style={{ fontWeight: 700, fontSize: '1rem', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                  <span>ℹ️</span> No tienes un turno abierto actualmente
                </div>
                <div style={{ fontSize: '0.875rem', marginTop: '0.25rem' }}>
                  Para realizar ventas o efectuar el arqueo de caja debes abrir un turno previamente.
                </div>
              </div>
              <button
                type="button"
                onClick={() => navigate('/abrir-turno')}
                style={{
                  background: '#d97706',
                  color: 'white',
                  border: 'none',
                  padding: '0.6rem 1.2rem',
                  borderRadius: '8px',
                  fontWeight: 600,
                  fontSize: '0.9rem',
                  cursor: 'pointer',
                }}
              >
                Abrir Turno Ahora →
              </button>
            </div>
          )}

          {/* Si el turno ya fue cerrado con éxito, ocultar formulario de edición */}
          {turnoClosedSuccess ? null : (
            <>
              {/* Barra de Acciones y Resumen Rápido */}
              <div
                style={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                  marginBottom: '1.25rem',
                  flexWrap: 'wrap',
                  gap: '1rem',
                }}
              >
                <div>
                  <h2 style={{ margin: 0, fontSize: '1.25rem', fontWeight: 700, color: '#1e293b' }}>
                    Desglose de Efectivo en Gaveta
                  </h2>
                  <span style={{ fontSize: '0.85rem', color: '#64748b' }}>
                    Indica la cantidad física de cada billete y moneda encontrada.
                  </span>
                </div>
                <div style={{ display: 'flex', gap: '0.75rem' }}>
                  <button
                    type="button"
                    onClick={handleReset}
                    style={{
                      background: '#f1f5f9',
                      color: '#475569',
                      border: '1px solid #cbd5e1',
                      padding: '0.5rem 1rem',
                      borderRadius: '8px',
                      fontSize: '0.85rem',
                      fontWeight: 600,
                      cursor: 'pointer',
                      transition: 'all 0.15s ease',
                    }}
                    title="Reiniciar todos los contadores a 0"
                  >
                    🔄 Limpiar Conteo
                  </button>
                </div>
              </div>

              {/* TABLA DE DENOMINACIONES */}
              <div
                style={{
                  overflowX: 'auto',
                  border: '1px solid #e2e8f0',
                  borderRadius: '12px',
                  marginBottom: '2rem',
                }}
              >
                <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
                  <thead>
                    <tr style={{ background: '#f8fafc', borderBottom: '2px solid #e2e8f0', color: '#475569' }}>
                      <th style={{ padding: '0.875rem 1.25rem', fontSize: '0.85rem', fontWeight: 700, textTransform: 'uppercase' }}>
                        Tipo
                      </th>
                      <th style={{ padding: '0.875rem 1.25rem', fontSize: '0.85rem', fontWeight: 700, textTransform: 'uppercase' }}>
                        Denominación
                      </th>
                      <th
                        style={{
                          padding: '0.875rem 1.25rem',
                          fontSize: '0.85rem',
                          fontWeight: 700,
                          textTransform: 'uppercase',
                          width: '240px',
                          textAlign: 'center',
                        }}
                      >
                        Cantidad
                      </th>
                      <th
                        style={{
                          padding: '0.875rem 1.25rem',
                          fontSize: '0.85rem',
                          fontWeight: 700,
                          textTransform: 'uppercase',
                          textAlign: 'right',
                        }}
                      >
                        Subtotal
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {DENOMINACIONES.map((den, index) => {
                      const qty = cantidades[den.id] || 0;
                      const subtotal = den.valor * qty;
                      const isEven = index % 2 === 0;

                      return (
                        <tr
                          key={den.id}
                          style={{
                            backgroundColor: isEven ? '#ffffff' : '#fcfcfd',
                            borderBottom: '1px solid #f1f5f9',
                            transition: 'background-color 0.15s',
                          }}
                        >
                          {/* Tipo */}
                          <td style={{ padding: '0.875rem 1.25rem' }}>
                            <span
                              style={{
                                display: 'inline-flex',
                                alignItems: 'center',
                                gap: '0.4rem',
                                padding: '0.25rem 0.6rem',
                                borderRadius: '9999px',
                                fontSize: '0.75rem',
                                fontWeight: 700,
                                background: den.tipo === 'billete' ? '#dbeafe' : '#fef3c7',
                                color: den.tipo === 'billete' ? '#1d4ed8' : '#b45309',
                              }}
                            >
                              <span>{den.tipo === 'billete' ? '💵' : '🪙'}</span>
                              <span style={{ textTransform: 'capitalize' }}>{den.tipo}</span>
                            </span>
                          </td>

                          {/* Denominación */}
                          <td style={{ padding: '0.875rem 1.25rem', fontWeight: 700, fontSize: '1.05rem', color: '#1e293b' }}>
                            {den.label}
                          </td>

                          {/* Cantidad Input con Stepper */}
                          <td style={{ padding: '0.875rem 1.25rem', textAlign: 'center' }}>
                            <div style={{ display: 'inline-flex', alignItems: 'center', gap: '0.35rem' }}>
                              <button
                                type="button"
                                onClick={() => handleStepChange(den.id, -1)}
                                style={{
                                  width: '32px',
                                  height: '34px',
                                  borderRadius: '6px',
                                  border: '1px solid #cbd5e1',
                                  background: '#f8fafc',
                                  fontWeight: 'bold',
                                  fontSize: '1rem',
                                  color: '#334155',
                                  cursor: 'pointer',
                                  display: 'flex',
                                  alignItems: 'center',
                                  justifyContent: 'center',
                                  userSelect: 'none',
                                }}
                              >
                                -
                              </button>

                              <input
                                id={`input-denominacion-${den.id}`}
                                type="number"
                                min="0"
                                step="1"
                                value={qty === 0 ? '' : qty}
                                onChange={(e) => handleCantidadChange(den.id, e.target.value)}
                                placeholder="0"
                                style={{
                                  width: '90px',
                                  padding: '0.45rem 0.5rem',
                                  textAlign: 'center',
                                  fontSize: '1rem',
                                  fontWeight: 700,
                                  borderRadius: '6px',
                                  border: '1px solid #cbd5e1',
                                  outline: 'none',
                                  color: '#0f172a',
                                  backgroundColor: qty > 0 ? '#f0fdf4' : '#ffffff',
                                  borderColor: qty > 0 ? '#86efac' : '#cbd5e1',
                                  transition: 'all 0.15s ease',
                                }}
                              />

                              <button
                                type="button"
                                onClick={() => handleStepChange(den.id, 1)}
                                style={{
                                  width: '32px',
                                  height: '34px',
                                  borderRadius: '6px',
                                  border: '1px solid #cbd5e1',
                                  background: '#f8fafc',
                                  fontWeight: 'bold',
                                  fontSize: '1rem',
                                  color: '#334155',
                                  cursor: 'pointer',
                                  display: 'flex',
                                  alignItems: 'center',
                                  justifyContent: 'center',
                                  userSelect: 'none',
                                }}
                              >
                                +
                              </button>
                            </div>
                          </td>

                          {/* Subtotal */}
                          <td style={{ padding: '0.875rem 1.25rem', textAlign: 'right', fontWeight: 700, color: subtotal > 0 ? '#0284c7' : '#94a3b8' }}>
                            ${subtotal.toLocaleString('es-CL')}
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                  <tfoot>
                    {/* Fila Total Billetes */}
                    <tr style={{ background: '#f8fafc', borderTop: '2px solid #e2e8f0' }}>
                      <td colSpan={2} style={{ padding: '0.75rem 1.25rem', fontWeight: 600, color: '#475569' }}>
                        💵 Subtotal Billetes:
                      </td>
                      <td colSpan={2} style={{ padding: '0.75rem 1.25rem', textAlign: 'right', fontWeight: 700, color: '#1e293b' }}>
                        ${totalBilletes.toLocaleString('es-CL')}
                      </td>
                    </tr>
                    {/* Fila Total Monedas */}
                    <tr style={{ background: '#f8fafc' }}>
                      <td colSpan={2} style={{ padding: '0.75rem 1.25rem', fontWeight: 600, color: '#475569' }}>
                        🪙 Subtotal Monedas:
                      </td>
                      <td colSpan={2} style={{ padding: '0.75rem 1.25rem', textAlign: 'right', fontWeight: 700, color: '#1e293b' }}>
                        ${totalMonedas.toLocaleString('es-CL')}
                      </td>
                    </tr>
                    {/* Total Declarado */}
                    <tr style={{ background: '#e0f2fe', borderTop: '2px solid #38bdf8' }}>
                      <td colSpan={2} style={{ padding: '1rem 1.25rem', fontWeight: 800, fontSize: '1.15rem', color: '#0369a1' }}>
                        TOTAL DECLARADO EN CAJA:
                      </td>
                      <td colSpan={2} style={{ padding: '1rem 1.25rem', textAlign: 'right', fontWeight: 800, fontSize: '1.45rem', color: '#0284c7' }}>
                        ${totalDeclarado.toLocaleString('es-CL')}
                      </td>
                    </tr>
                  </tfoot>
                </table>
              </div>

              {/* SECCIÓN DE CUADRE DE CAJA */}
              <div
                style={{
                  background: '#f8fafc',
                  border: '1px solid #e2e8f0',
                  borderRadius: '12px',
                  padding: '1.5rem',
                  marginBottom: '2rem',
                }}
              >
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem', flexWrap: 'wrap', gap: '0.5rem' }}>
                  <div>
                    <h3 style={{ margin: 0, fontSize: '1.15rem', fontWeight: 700, color: '#1e293b' }}>
                      Arqueo y Comparación de Cuadre
                    </h3>
                    <div style={{ fontSize: '0.85rem', color: '#64748b' }}>
                      Verifica la diferencia entre el efectivo esperado por ventas y lo contado físicamente.
                    </div>
                  </div>

                  <button
                    type="button"
                    onClick={handleCalcularCuadre}
                    disabled={isCalculatingCuadre || noTurnoOpen}
                    style={{
                      background: '#0284c7',
                      color: 'white',
                      border: 'none',
                      padding: '0.65rem 1.25rem',
                      borderRadius: '8px',
                      fontWeight: 700,
                      fontSize: '0.9rem',
                      cursor: isCalculatingCuadre || noTurnoOpen ? 'not-allowed' : 'pointer',
                      boxShadow: '0 2px 4px rgba(2, 132, 199, 0.2)',
                    }}
                  >
                    {isCalculatingCuadre ? 'Calculando...' : '🔍 Calcular Cuadre'}
                  </button>
                </div>

                {/* Resultado del Cuadre (calculado automáticamente y verificado con MS-5) */}
                {efectivoEsperado !== null && (
                  <div style={{ marginTop: '1rem' }}>
                    <div
                      style={{
                        display: 'grid',
                        gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
                        gap: '1rem',
                      }}
                    >
                      {/* Efectivo Esperado */}
                      <div
                        style={{
                          background: '#ffffff',
                          border: '1px solid #e2e8f0',
                          borderRadius: '8px',
                          padding: '1rem',
                        }}
                      >
                        <div style={{ fontSize: '0.75rem', textTransform: 'uppercase', color: '#64748b', fontWeight: 600 }}>
                          Efectivo Esperado (Ventas)
                        </div>
                        <div style={{ fontSize: '1.35rem', fontWeight: 800, color: '#1e293b', marginTop: '0.25rem' }}>
                          ${efectivoEsperado.toLocaleString('es-CL')}
                        </div>
                        <div style={{ fontSize: '0.75rem', color: '#64748b', marginTop: '0.25rem' }}>
                          Vía MS-5 (pagos efectivo completados)
                        </div>
                      </div>

                      {/* Efectivo Declarado */}
                      <div
                        style={{
                          background: '#ffffff',
                          border: '1px solid #e2e8f0',
                          borderRadius: '8px',
                          padding: '1rem',
                        }}
                      >
                        <div style={{ fontSize: '0.75rem', textTransform: 'uppercase', color: '#64748b', fontWeight: 600 }}>
                          Efectivo Declarado (Gaveta)
                        </div>
                        <div style={{ fontSize: '1.35rem', fontWeight: 800, color: '#0284c7', marginTop: '0.25rem' }}>
                          ${totalDeclarado.toLocaleString('es-CL')}
                        </div>
                        <div style={{ fontSize: '0.75rem', color: '#64748b', marginTop: '0.25rem' }}>
                          Suma automática de billetes + monedas
                        </div>
                       </div>
                       {/* Diferencia */}
                       {(() => {
                        const diff = diferenciaCalculada ?? 0;
                        const isExact = diff === 0;
                        const isSobrante = diff > 0;
                        // Cualquier discrepancia (sobrante o faltante) se muestra en rojo
                        const hasDiscrepancy = !isExact;
                        return (
                          <div
                            style={{
                              background: isExact ? '#f0fdf4' : '#fef2f2',
                              border: `1px solid ${isExact ? '#86efac' : '#fca5a5'}`,
                              borderRadius: '8px',
                              padding: '1rem',
                            }}
                          >
                            <div
                              style={{
                                fontSize: '0.75rem',
                                textTransform: 'uppercase',
                                fontWeight: 700,
                                color: isExact ? '#15803d' : '#b91c1c',
                              }}
                            >
                              {isExact
                                ? '✅ Diferencia (Cuadre Exacto)'
                                : isSobrante
                                ? '⚠️ Diferencia (Sobrante)'
                                : '⚠️ Diferencia (Faltante)'}
                            </div>
                            <div
                              style={{
                                fontSize: '1.5rem',
                                fontWeight: 800,
                                marginTop: '0.25rem',
                                color: isExact ? '#15803d' : '#dc2626',
                              }}
                            >
                              {isSobrante ? '+' : ''}${diff.toLocaleString('es-CL')}
                            </div>
                            <div style={{ fontSize: '0.75rem', marginTop: '0.25rem', color: isExact ? '#15803d' : '#b91c1c' }}>
                              {isExact
                                ? 'La gaveta coincide exactamente con las ventas registradas'
                                : isSobrante
                                ? 'Hay más efectivo del esperado — revisar conteo'
                                : 'Falta efectivo respecto a las ventas — revisar conteo'}
                            </div>
                            {hasDiscrepancy && (
                              <div style={{ marginTop: '0.5rem', padding: '0.4rem 0.6rem', background: '#fee2e2', borderRadius: '6px', fontSize: '0.75rem', color: '#991b1b', fontWeight: 600 }}>
                                Discrepancia detectada: se recomienda revisar el conteo antes de cerrar el turno.
                              </div>
                            )}
                          </div>
                        );
                       })()}
                    </div>

                    {/* Estado de sincronización */}
                    <div style={{ marginTop: '0.75rem', fontSize: '0.8rem', color: '#64748b', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                      {cuadreSynced ? (
                        <span style={{ color: '#16a34a', fontWeight: 600 }}>
                          ✓ Cuadre verificado y sincronizado con el microservicio POS (MS-5)
                          {cuadreResult?.turno_id ? ` (Turno: ${cuadreResult.turno_id.slice(0, 8)}...)` : ''}.
                        </span>
                      ) : (
                        <span>
                          ℹ️ Conteo actualizado localmente. Puedes presionar <strong>&ldquo;Calcular Cuadre&rdquo;</strong> para re-validar con el servidor.
                        </span>
                      )}
                    </div>
                  </div>
                )}
              </div>

              {/* BOTÓN FINAL DE CIERRE DE TURNO */}
              <div style={{ display: 'flex', gap: '1rem', justifyContent: 'flex-end', flexWrap: 'wrap' }}>
                <button
                  type="button"
                  onClick={() => navigate('/pos')}
                  style={{
                    padding: '0.85rem 1.5rem',
                    background: '#ffffff',
                    color: '#475569',
                    border: '1px solid #cbd5e1',
                    borderRadius: '8px',
                    fontWeight: 600,
                    fontSize: '1rem',
                    cursor: 'pointer',
                  }}
                >
                  Volver al POS
                </button>

                <button
                  type="button"
                  onClick={handleCerrarTurno}
                  disabled={isClosingTurno || noTurnoOpen}
                  style={{
                    padding: '0.85rem 2rem',
                    backgroundColor: isClosingTurno || noTurnoOpen ? '#94a3b8' : '#e11d48',
                    color: 'white',
                    border: 'none',
                    borderRadius: '8px',
                    fontWeight: 700,
                    fontSize: '1.05rem',
                    cursor: isClosingTurno || noTurnoOpen ? 'not-allowed' : 'pointer',
                    boxShadow: '0 4px 6px -1px rgba(225, 29, 72, 0.25)',
                    transition: 'all 0.15s ease',
                  }}
                  onMouseOver={(e) => {
                    if (!isClosingTurno && !noTurnoOpen) e.currentTarget.style.backgroundColor = '#be123c';
                  }}
                  onMouseOut={(e) => {
                    if (!isClosingTurno && !noTurnoOpen) e.currentTarget.style.backgroundColor = '#e11d48';
                  }}
                >
                  {isClosingTurno ? 'Cerrando Turno...' : '🔒 Confirmar y Cerrar Turno'}
                </button>
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
}

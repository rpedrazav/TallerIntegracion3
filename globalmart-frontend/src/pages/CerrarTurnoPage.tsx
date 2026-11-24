import React, { useState, useEffect, useMemo, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { AxiosError } from 'axios';
import { useAuth } from '../hooks/useAuth';
import { posApi, TurnoActivo, CuadreResult } from '../api/client';
import { parseJwt } from '../utils/jwt';
import TablaDenominaciones, { DENOMINACIONES } from '../components/pos/TablaDenominaciones';
import CuadreCaja from '../components/pos/CuadreCaja';
import ConfirmarCierreModal from '../components/pos/ConfirmarCierreModal';
import ResultadoCierre from '../components/pos/ResultadoCierre';

export default function CerrarTurnoPage() {
  const { token } = useAuth();
  const navigate = useNavigate();
  const userInfo = parseJwt(token);

  // Cantidades por cada denominación
  const [cantidades, setCantidades] = useState<Record<string, number>>(() => {
    const initial: Record<string, number> = {};
    DENOMINACIONES.forEach((d) => {
      initial[d.id] = 0;
    });
    return initial;
  });

  const [turnoActivo, setTurnoActivo] = useState<TurnoActivo | null>(null);
  const [checkingTurno, setCheckingTurno] = useState<boolean>(true);
  const [noTurnoOpen, setNoTurnoOpen] = useState<boolean>(false);

  const [efectivoEsperado, setEfectivoEsperado] = useState<number | null>(null);
  const [cuadreResult, setCuadreResult] = useState<CuadreResult | null>(null);
  const [isCalculatingCuadre, setIsCalculatingCuadre] = useState<boolean>(false);
  const [cuadreSynced, setCuadreSynced] = useState<boolean>(false);

  const [isClosingTurno, setIsClosingTurno] = useState<boolean>(false);
  const confirmDialogRef = useRef<HTMLDialogElement>(null);
  const [turnoClosedSuccess, setTurnoClosedSuccess] = useState<boolean>(false);

  const [errorMsg, setErrorMsg] = useState<string>('');
  const [successMsg, setSuccessMsg] = useState<string>('');

  // Verificar turno activo al cargar la pantalla y obtener efectivo esperado inicial
  useEffect(() => {
    let isMounted = true;

    async function checkTurnoActivo() {
      if (!token) {
        if (isMounted) setCheckingTurno(false);
        return;
      }

      try {
        const res = await posApi.activeShift();
        if (isMounted) {
          if (res) {
            setTurnoActivo(res);
            setNoTurnoOpen(false);

            try {
              const cuadreRes = await posApi.balanceShift(0);
              if (isMounted && cuadreRes) {
                setEfectivoEsperado(cuadreRes.efectivo_esperado);
                setCuadreResult(cuadreRes);
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
  }, [token]);

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

  const handleStepChange = (id: string, delta: number) => {
    setErrorMsg('');
    setCuadreSynced(false);
    setCantidades((prev) => {
      const current = prev[id] || 0;
      const next = Math.max(0, current + delta);
      return { ...prev, [id]: next };
    });
  };

  const handleReset = () => {
    const reset: Record<string, number> = {};
    DENOMINACIONES.forEach((d) => {
      reset[d.id] = 0;
    });
    setCantidades(reset);
    setCuadreSynced(false);
    setErrorMsg('');
  };

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

  const diferenciaCalculada = useMemo(() => {
    if (efectivoEsperado === null) return null;
    return Math.round((totalDeclarado - efectivoEsperado) * 100) / 100;
  }, [totalDeclarado, efectivoEsperado]);

  const handleCalcularCuadre = async () => {
    setErrorMsg('');
    setIsCalculatingCuadre(true);

    try {
      if (!token) return;
      const res = await posApi.balanceShift(totalDeclarado);

      if (res) {
        setCuadreResult(res);
        setEfectivoEsperado(res.efectivo_esperado);
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

  const handleCerrarTurno = async () => {
    confirmDialogRef.current?.close();
    setIsClosingTurno(true);
    setErrorMsg('');

    try {
      if (!token) return;
      await posApi.closeShift();
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
      <div
        style={{
          background: 'var(--color-surface)',
          borderRadius: '16px',
          boxShadow: '0 10px 25px -5px rgba(0, 0, 0, 0.08), 0 8px 10px -6px rgba(0, 0, 0, 0.04)',
          border: '1px solid var(--color-line)',
          overflow: 'hidden',
        }}
      >
        {/* Cabecera */}
        <div
          style={{
            background: 'linear-gradient(135deg, var(--color-primary) 0%, var(--color-primary) 60%, var(--color-primary) 100%)',
            color: 'var(--color-surface)',
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
          {errorMsg && (
            <div
              style={{
                backgroundColor: 'var(--color-danger-bg)',
                border: '1px solid var(--color-danger)',
                color: 'var(--color-danger)',
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
            <ResultadoCierre
              successMsg={successMsg}
              totalDeclarado={totalDeclarado}
              onAbrirTurno={() => navigate('/abrir-turno')}
              onVolverPos={() => navigate('/pos')}
            />
          )}

          {noTurnoOpen && !turnoClosedSuccess && (
            <div
              style={{
                backgroundColor: 'var(--color-warning-bg)',
                border: '1px solid var(--color-warning)',
                color: 'var(--color-warning-text)',
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
                  background: 'var(--color-warning)',
                  color: 'var(--color-surface)',
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

          {!turnoClosedSuccess && (
            <>
              <TablaDenominaciones
                cantidades={cantidades}
                onCantidadChange={handleCantidadChange}
                onStepChange={handleStepChange}
                onReset={handleReset}
                totalBilletes={totalBilletes}
                totalMonedas={totalMonedas}
                totalDeclarado={totalDeclarado}
              />

              <CuadreCaja
                efectivoEsperado={efectivoEsperado}
                totalDeclarado={totalDeclarado}
                diferenciaCalculada={diferenciaCalculada}
                isCalculatingCuadre={isCalculatingCuadre}
                cuadreSynced={cuadreSynced}
                cuadreResult={cuadreResult}
                noTurnoOpen={noTurnoOpen}
                onCalcularCuadre={handleCalcularCuadre}
              />

              <div style={{ display: 'flex', gap: '1rem', justifyContent: 'flex-end', flexWrap: 'wrap' }}>
                <button
                  type="button"
                  onClick={() => navigate('/pos')}
                  style={{
                    padding: '0.85rem 1.5rem',
                    background: 'var(--color-surface)',
                    color: 'var(--color-ink-soft)',
                    border: '1px solid var(--color-line)',
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
                  onClick={() => confirmDialogRef.current?.showModal()}
                  disabled={isClosingTurno || noTurnoOpen}
                  style={{
                    padding: '0.85rem 2rem',
                    backgroundColor: isClosingTurno || noTurnoOpen ? 'var(--color-muted)' : 'var(--color-danger)',
                    color: 'var(--color-surface)',
                    border: 'none',
                    borderRadius: '8px',
                    fontWeight: 700,
                    fontSize: '1.05rem',
                    cursor: isClosingTurno || noTurnoOpen ? 'not-allowed' : 'pointer',
                    boxShadow: '0 4px 6px -1px color-mix(in srgb, var(--color-danger) 25%, transparent)',
                    transition: 'all 0.15s ease',
                  }}
                >
                  {isClosingTurno ? 'Cerrando Turno...' : '🔒 Confirmar y Cerrar Turno'}
                </button>
              </div>

              <ConfirmarCierreModal
                dialogRef={confirmDialogRef}
                totalDeclarado={totalDeclarado}
                cuadreSynced={cuadreSynced}
                cuadreResult={cuadreResult}
                onConfirmar={handleCerrarTurno}
                onCancelar={() => confirmDialogRef.current?.close()}
              />
            </>
          )}
        </div>
      </div>
    </div>
  );
}

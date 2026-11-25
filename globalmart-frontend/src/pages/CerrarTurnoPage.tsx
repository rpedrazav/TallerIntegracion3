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
import './CerrarTurnoPage.css';

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
    <div className="cerrar-turno-page">
      <div className="cerrar-turno-card">
        <div className="cerrar-turno-header">
          <div className="cerrar-turno-header-row">
            <div>
              <div className="cerrar-turno-title-row">
                <span className="cerrar-turno-icon" aria-hidden="true">🧾</span>
                <h1 className="cerrar-turno-title">Cierre de Turno y Cuadre de Caja</h1>
              </div>
              <p className="cerrar-turno-subtitle">
                Conteo y desglose de denominaciones de billetes y monedas para el arqueo final.
              </p>
            </div>

            <div className="cerrar-turno-meta">
              <div><strong>Cajero:</strong> {userInfo?.nombre || 'Cajero en Sesión'}</div>
              <div><strong>Rol:</strong> {userInfo?.active_role || 'CAJERO'}</div>
              {checkingTurno ? (
                <div className="cerrar-turno-meta-sub">
                  <em>Verificando turno activo...</em>
                </div>
              ) : turnoActivo ? (
                <div className="cerrar-turno-meta-sub">
                  <strong>Turno ID:</strong>{' '}
                  <code className="cerrar-turno-code">{turnoActivo.id.slice(0, 8)}...</code>
                </div>
              ) : null}
            </div>
          </div>
        </div>

        <div className="cerrar-turno-body">
          {errorMsg && (
            <div className="cerrar-turno-alert cerrar-turno-alert--danger" role="alert">
              <span aria-hidden="true">⚠️</span>
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
            <div className="cerrar-turno-alert cerrar-turno-alert--warning" role="status">
              <div>
                <div className="cerrar-turno-alert-title">
                  <span aria-hidden="true">ℹ️</span> No tienes un turno abierto actualmente
                </div>
                <div className="cerrar-turno-alert-detail">
                  Para realizar ventas o efectuar el arqueo de caja debes abrir un turno previamente.
                </div>
              </div>
              <button
                type="button"
                onClick={() => navigate('/abrir-turno')}
                className="cerrar-turno-btn-warning"
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

              <div className="cerrar-turno-actions">
                <button
                  type="button"
                  onClick={() => navigate('/pos')}
                  className="cerrar-turno-btn-secondary"
                >
                  Volver al POS
                </button>

                <button
                  type="button"
                  onClick={() => confirmDialogRef.current?.showModal()}
                  disabled={isClosingTurno || noTurnoOpen}
                  className="cerrar-turno-btn-danger"
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

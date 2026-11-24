import { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import axios from 'axios';
import { posApi, VentaPendiente } from '../api/client';
import { PaymentState } from '../components/pos/PaymentModal';
import {
  clearRecoveryAttempt,
  loadRecoveryAttempt,
  recoverPosStateOnReload,
} from '../payments/posRecovery';

export interface PosRecoveryResolved {
  turnoId: string;
  venta: VentaPendiente | null;
  paymentState: PaymentState;
  paymentError: string;
  gatewayReference: string | null;
  attemptReference: string | null;
  recoveryNotice: string | null;
  openModal: boolean;
}

export function usePosRecovery(onRecoveryResolved: (data: PosRecoveryResolved) => void) {
  const navigate = useNavigate();
  const [turnoStatus, setTurnoStatus] = useState<'checking' | 'ready' | 'error'>('checking');
  const [turnoError, setTurnoError] = useState(
    'No se pudo verificar el turno de caja. Intenta nuevamente antes de iniciar una venta.'
  );
  const [turnoRetry, setTurnoRetry] = useState(0);
  const [turnoId, setTurnoId] = useState<string | null>(null);

  const onRecoveryResolvedRef = useRef(onRecoveryResolved);
  onRecoveryResolvedRef.current = onRecoveryResolved;

  useEffect(() => {
    let active = true;
    let shiftConfirmed = false;
    setTurnoStatus('checking');
    setTurnoError('No se pudo verificar el turno de caja. Intenta nuevamente antes de iniciar una venta.');

    posApi.activeShift()
      .then(async (turno) => {
        shiftConfirmed = true;
        const ventas = await posApi.salesByShift(turno.id);
        if (!active) return;
        const persistedAttempt = loadRecoveryAttempt(localStorage, turno.id);
        const recovery = await recoverPosStateOnReload({
          turnoId: turno.id,
          ventas,
          persistedAttempt,
          ms5: posApi,
          terminal: window.api?.terminal,
        });
        if (!active) return;

        if (recovery.shouldClearPersisted) {
          clearRecoveryAttempt(localStorage, turno.id);
        }

        setTurnoId(turno.id);
        setTurnoStatus('ready');

        onRecoveryResolvedRef.current({
          turnoId: turno.id,
          venta: recovery.venta,
          paymentState: recovery.paymentState,
          paymentError: recovery.errorMessage,
          gatewayReference: recovery.gatewayReference,
          attemptReference: recovery.attemptReference,
          recoveryNotice: recovery.recoveryNotice,
          openModal: recovery.openModal,
        });
      })
      .catch((error: unknown) => {
        if (!active) return;
        if (!shiftConfirmed && axios.isAxiosError(error) && error.response?.status === 404) {
          navigate('/abrir-turno', { replace: true });
        } else if (!(axios.isAxiosError(error) && error.response?.status === 401)) {
          setTurnoError(
            shiftConfirmed
              ? 'No se pudo recuperar la venta pendiente desde MS-5. Reintenta antes de modificar el carrito.'
              : 'No se pudo verificar el turno de caja. Intenta nuevamente antes de iniciar una venta.'
          );
          setTurnoStatus('error');
        }
      });

    return () => {
      active = false;
    };
  }, [navigate, turnoRetry]);

  const retryTurno = () => setTurnoRetry((retry) => retry + 1);

  return {
    turnoStatus,
    turnoError,
    turnoId,
    retryTurno,
  };
}

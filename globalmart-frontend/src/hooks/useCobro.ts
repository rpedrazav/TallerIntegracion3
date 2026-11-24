import { useState, useRef, useCallback } from 'react';
import axios from 'axios';
import { posApi, VentaPendiente } from '../api/client';
import { PaymentMethod, PaymentState } from '../components/pos/PaymentModal';
import {
  reconcileCardPayment,
  releasePaymentSingleFlight,
  synchronizeCardPayment,
  tryAcquirePaymentSingleFlight,
} from '../payments/synchronizeCardPayment';
import {
  cancelPaymentAttemptSafely,
  clearRecoveryAttempt,
  saveRecoveryAttempt,
} from '../payments/posRecovery';

function saleErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as { error?: string; message?: string } | undefined;
    return data?.error || data?.message || (error.code === 'ECONNABORTED'
      ? 'La solicitud tardó demasiado. Se debe actualizar la venta desde MS-5.'
      : 'No se pudo sincronizar el ítem con MS-5.');
  }
  return 'No se pudo sincronizar el ítem con MS-5.';
}

interface UseCobroOptions {
  venta: VentaPendiente | null;
  onUpdateVenta: (venta: VentaPendiente | null) => void;
  turnoId: string | null;
}

export function useCobro({ venta, onUpdateVenta, turnoId }: UseCobroOptions) {
  const [isPaymentOpen, setIsPaymentOpen] = useState(false);
  const [paymentState, setPaymentState] = useState<PaymentState>('available');
  const [paymentError, setPaymentError] = useState('');
  const [paymentShortfall, setPaymentShortfall] = useState<number | null>(null);
  const [paymentChange, setPaymentChange] = useState<number | null>(null);
  const [paymentCashReceived, setPaymentCashReceived] = useState<number | null>(null);
  const [paymentReference, setPaymentReference] = useState<string | null>(null);
  const [attemptReferenceView, setAttemptReferenceView] = useState<string | null>(null);
  const [recoveryNotice, setRecoveryNotice] = useState<string | null>(null);

  const saleRef = useRef<VentaPendiente | null>(venta);
  saleRef.current = venta;

  const paymentInFlight = useRef(false);
  const cancelInFlight = useRef(false);
  const paymentAttemptReference = useRef<string | null>(null);

  const applyRecoveryPayment = useCallback((data: {
    paymentState: PaymentState;
    paymentError: string;
    gatewayReference: string | null;
    attemptReference: string | null;
    recoveryNotice: string | null;
    openModal: boolean;
  }) => {
    paymentAttemptReference.current = data.attemptReference;
    setAttemptReferenceView(data.attemptReference);
    setPaymentState(data.paymentState);
    setPaymentReference(data.gatewayReference);
    setPaymentError(data.paymentError);
    setRecoveryNotice(data.recoveryNotice);
    setIsPaymentOpen(data.openModal);
  }, []);

  const persistRecovery = useCallback((
    current: VentaPendiente,
    estado: Exclude<PaymentState, 'available'>,
    claveIdempotencia: string | null,
  ) => {
    const activeTurnoId = turnoId ?? current.turno_id;
    if (!activeTurnoId) return;
    saveRecoveryAttempt(localStorage, {
      ventaId: current.id,
      turnoId: activeTurnoId,
      metodoPago: current.metodo_pago === 'TARJETA' ? 'TARJETA' : 'EFECTIVO',
      claveIdempotencia,
      estado,
      updatedAt: new Date().toISOString(),
    });
  }, [turnoId]);

  const handleCashPayment = async (method: PaymentMethod, cashReceived?: number) => {
    const current = saleRef.current;
    if (paymentState !== 'available' || method !== 'cash' || !current || current.estado !== 'PENDIENTE' || current.metodo_pago !== 'EFECTIVO'
      || cashReceived === undefined || !Number.isFinite(cashReceived) || cashReceived <= 0 || paymentInFlight.current) return;

    paymentInFlight.current = true;
    setPaymentCashReceived(cashReceived);
    setPaymentState('processing');
    setPaymentError('');
    setPaymentShortfall(null);
    setRecoveryNotice(null);
    persistRecovery(current, 'processing', null);
    try {
      const result = await posApi.chargeCash(current.id, cashReceived);
      if (result.venta?.id !== current.id || result.venta.estado !== 'COMPLETADA' || !Number.isFinite(result.vuelto)) {
        setPaymentState('uncertain');
        setPaymentError('MS-5 no devolvió una confirmación completa. Verifica la venta antes de otro intento.');
        persistRecovery(current, 'uncertain', null);
        return;
      }
      saleRef.current = result.venta;
      onUpdateVenta(result.venta);
      setPaymentCashReceived(Number.isFinite(result.montoRecibido) ? result.montoRecibido : cashReceived);
      setPaymentChange(result.vuelto);
      setPaymentState('approved');
      persistRecovery(result.venta, 'approved', null);
    } catch (error: unknown) {
      if (axios.isAxiosError(error) && error.response?.status === 400) {
        const data = error.response.data as { error?: string; diferencia?: number } | undefined;
        setPaymentState('available');
        setPaymentError(data?.error || 'No se pudo cobrar la venta en efectivo.');
        setPaymentShortfall(data?.error === 'Monto insuficiente' && typeof data.diferencia === 'number' ? data.diferencia : null);
        clearRecoveryAttempt(localStorage, turnoId ?? current.turno_id);
      } else {
        setPaymentState('uncertain');
        setPaymentError('No se pudo confirmar el resultado del cobro. Verifica la venta antes de otro intento.');
        persistRecovery(current, 'uncertain', null);
      }
    } finally {
      paymentInFlight.current = false;
    }
  };

  const handleCardPayment = async (method: PaymentMethod) => {
    const current = saleRef.current;
    if (paymentState !== 'available' || method !== 'card' || !current || current.estado !== 'PENDIENTE'
      || current.metodo_pago !== 'TARJETA') return;
    if (!tryAcquirePaymentSingleFlight(paymentInFlight)) return;
    setPaymentState('processing');
    setPaymentError('');
    setPaymentReference(null);
    setRecoveryNotice(null);
    const claveIdempotencia = paymentAttemptReference.current ?? crypto.randomUUID();
    paymentAttemptReference.current = claveIdempotencia;
    setAttemptReferenceView(claveIdempotencia);
    persistRecovery(current, 'processing', claveIdempotencia);
    try {
      const result = await synchronizeCardPayment({
        terminal: window.api.terminal,
        ms5: posApi,
        ventaId: current.id,
        monto: current.total,
        claveIdempotencia,
      });
      paymentAttemptReference.current = result.referenciaIntento;
      setAttemptReferenceView(result.referenciaIntento);
      const effectiveVenta = result.venta ?? current;
      if (result.venta) {
        saleRef.current = result.venta;
        onUpdateVenta(result.venta);
      }
      if (result.estado === 'declined') {
        setPaymentState('declined');
        setPaymentError('La pasarela rechazó el cobro.');
        persistRecovery(effectiveVenta, 'declined', result.referenciaIntento);
        return;
      }
      if (result.estado === 'cancelled') {
        setPaymentState('cancelled');
        clearRecoveryAttempt(localStorage, turnoId ?? effectiveVenta.turno_id);
        return;
      }
      if (result.estado === 'processing') return;
      if (result.estado === 'approved' && result.venta?.estado === 'COMPLETADA') {
        setPaymentReference(result.gatewayReference);
        setPaymentState('approved');
        persistRecovery(result.venta, 'approved', result.referenciaIntento);
        return;
      }
      if (result.estado === 'approved') {
        setPaymentState('uncertain');
        setPaymentError('MS-5 no confirmó la venta como COMPLETADA. Verifica el estado antes de continuar.');
        persistRecovery(effectiveVenta, 'uncertain', result.referenciaIntento ?? claveIdempotencia);
        return;
      }
      setPaymentState('uncertain');
      setPaymentError('MS-5 no devolvió una confirmación completa. Verifica la venta antes de otro intento.');
      persistRecovery(effectiveVenta, 'uncertain', result.referenciaIntento ?? claveIdempotencia);
    } catch {
      setPaymentState('uncertain');
      setPaymentError('No se pudo confirmar el resultado de la pasarela. Verifica la venta antes de otro cobro.');
      persistRecovery(current, 'uncertain', claveIdempotencia);
    } finally {
      releasePaymentSingleFlight(paymentInFlight);
    }
  };

  const handlePayment = (method: PaymentMethod, cashReceived?: number) => {
    if (method === 'cash') void handleCashPayment(method, cashReceived);
    else void handleCardPayment(method);
  };

  const changePaymentMethod = async (method: PaymentMethod) => {
    const current = saleRef.current;
    if (!current || current.estado !== 'PENDIENTE' || paymentInFlight.current || paymentState === 'uncertain') return;
    paymentInFlight.current = true;
    setPaymentError('');
    try {
      const updated = await posApi.changeSaleMethod(current.id, method === 'cash' ? 'EFECTIVO' : 'TARJETA');
      saleRef.current = updated;
      onUpdateVenta(updated);
      setPaymentState('available');
      setPaymentReference(null);
      setRecoveryNotice(null);
      paymentAttemptReference.current = null;
      setAttemptReferenceView(null);
      clearRecoveryAttempt(localStorage, turnoId ?? updated.turno_id);
    } catch (error: unknown) {
      setPaymentError(saleErrorMessage(error));
    } finally {
      paymentInFlight.current = false;
    }
  };

  const verifyPayment = async () => {
    const current = saleRef.current;
    if (!current || paymentInFlight.current) return;
    paymentInFlight.current = true;
    setPaymentState('processing');
    setPaymentError('');
    try {
      if (current.metodo_pago === 'TARJETA') {
        const intento = await reconcileCardPayment({
          ms5: posApi,
          ventaId: current.id,
          referenciaEsperada: paymentAttemptReference.current,
        });
        paymentAttemptReference.current = intento.referenciaIntento;
        setAttemptReferenceView(intento.referenciaIntento);
        const effectiveVenta = intento.venta ?? current;
        if (intento.venta) {
          saleRef.current = intento.venta;
          onUpdateVenta(intento.venta);
        }
        if (intento.estado === 'approved' && intento.venta?.estado === 'COMPLETADA') {
          setPaymentReference(intento.gatewayReference);
          setPaymentState('approved');
          persistRecovery(intento.venta, 'approved', intento.referenciaIntento);
        } else if (intento.estado === 'declined') {
          setPaymentState('declined');
          setPaymentError('La pasarela rechazó el pago. Puedes elegir otro método o reintentar manualmente.');
          persistRecovery(effectiveVenta, 'declined', intento.referenciaIntento);
        } else {
          setPaymentState('uncertain');
          setPaymentError('El intento aún no tiene un resultado definitivo. No se iniciará otra autorización.');
          persistRecovery(effectiveVenta, 'uncertain', intento.referenciaIntento);
        }
        return;
      }
      const updated = await posApi.getSale(current.id);
      saleRef.current = updated;
      onUpdateVenta(updated);
      if (updated.estado === 'COMPLETADA') {
        setPaymentState('approved');
        setPaymentError('La venta está completada. El vuelto no está disponible en esta consulta.');
        persistRecovery(updated, 'approved', null);
      } else {
        setPaymentState('uncertain');
        setPaymentError('La venta aún figura pendiente. Confirma el resultado antes de intentar otro cobro.');
        persistRecovery(updated, 'uncertain', null);
      }
    } catch {
      setPaymentState('uncertain');
      setPaymentError('No se pudo consultar la venta. Reintenta la verificación.');
    } finally {
      paymentInFlight.current = false;
    }
  };

  const cancelCardAttempt = async () => {
    const current = saleRef.current;
    if (!current || current.metodo_pago !== 'TARJETA' || cancelInFlight.current) return;
    if (paymentState !== 'processing' && paymentState !== 'uncertain') return;
    cancelInFlight.current = true;
    try {
      const result = await cancelPaymentAttemptSafely({
        ventaId: current.id,
        monto: current.total,
        referenciaIntento: paymentAttemptReference.current,
        ms5: posApi,
        terminal: window.api?.terminal,
      });
      if (result.venta) {
        saleRef.current = result.venta;
        onUpdateVenta(result.venta);
      }
      if (result.outcome === 'already_approved') {
        paymentAttemptReference.current = result.attemptReference;
        setAttemptReferenceView(result.attemptReference);
        setPaymentReference(result.gatewayReference);
        setPaymentState('approved');
        setPaymentError('');
        setRecoveryNotice(result.message);
        if (result.venta) persistRecovery(result.venta, 'approved', result.attemptReference);
      } else if (result.outcome === 'cancelled') {
        paymentAttemptReference.current = null;
        setAttemptReferenceView(null);
        setPaymentReference(null);
        setPaymentState('cancelled');
        setPaymentError(result.message);
        clearRecoveryAttempt(localStorage, turnoId ?? current.turno_id);
        releasePaymentSingleFlight(paymentInFlight);
      } else {
        paymentAttemptReference.current = result.attemptReference;
        setAttemptReferenceView(result.attemptReference);
        setPaymentState('uncertain');
        setPaymentError(result.message);
        persistRecovery(result.venta ?? current, 'uncertain', result.attemptReference);
      }
    } finally {
      cancelInFlight.current = false;
    }
  };

  const continueAfterCancel = () => {
    const current = saleRef.current;
    if (paymentState !== 'cancelled' || !current || current.estado !== 'PENDIENTE' || paymentInFlight.current) return;
    setPaymentState('available');
    setPaymentError('');
    setPaymentReference(null);
    setRecoveryNotice(null);
    paymentAttemptReference.current = null;
    setAttemptReferenceView(null);
    clearRecoveryAttempt(localStorage, turnoId ?? current.turno_id);
  };

  const retryDeclinedCard = () => {
    const current = saleRef.current;
    if (paymentState !== 'declined' || current?.metodo_pago !== 'TARJETA' || paymentInFlight.current) return;
    setPaymentState('available');
    setPaymentError('');
    setPaymentReference(null);
    setRecoveryNotice(null);
    paymentAttemptReference.current = null;
    setAttemptReferenceView(null);
    clearRecoveryAttempt(localStorage, turnoId ?? current.turno_id);
  };

  const closePayment = () => {
    if (paymentState === 'processing') return;
    setIsPaymentOpen(false);
    if (paymentState === 'approved') {
      const current = saleRef.current;
      clearRecoveryAttempt(localStorage, turnoId ?? current?.turno_id);
      saleRef.current = null;
      onUpdateVenta(null);
      setPaymentState('available');
      setPaymentChange(null);
      setPaymentCashReceived(null);
      setPaymentError('');
      setPaymentShortfall(null);
      setPaymentReference(null);
      setRecoveryNotice(null);
      paymentAttemptReference.current = null;
      setAttemptReferenceView(null);
    }
  };

  const openPaymentModal = () => {
    if (paymentState === 'available') {
      setPaymentError('');
      setPaymentShortfall(null);
      setPaymentChange(null);
      setPaymentCashReceived(null);
    }
    setIsPaymentOpen(true);
  };

  const handleCashInputChange = () => {
    setPaymentError('');
    setPaymentShortfall(null);
  };

  return {
    isPaymentOpen,
    setIsPaymentOpen,
    paymentState,
    setPaymentState,
    paymentError,
    setPaymentError,
    paymentShortfall,
    paymentChange,
    paymentCashReceived,
    paymentReference,
    attemptReferenceView,
    recoveryNotice,
    handlePayment,
    changePaymentMethod,
    verifyPayment,
    cancelCardAttempt,
    continueAfterCancel,
    retryDeclinedCard,
    closePayment,
    openPaymentModal,
    applyRecoveryPayment,
    handleCashInputChange,
  };
}
